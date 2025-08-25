using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Microsoft.MixedReality.QR;
using TMPro;

namespace QRTracking
{
    public class QRCodesVisualizer : MonoBehaviour
    {
        public WeeksManager weeksManager;
        public GameObject qrCodePrefab;
        public GameObject objPrefab;
        public GameObject orienterPrefab;
        public GameObject orienter;

        public GameObject menu;
        public TextMeshProUGUI infoText;

        public Vector3 positionOffset = new Vector3(0.0f, 0.0f, 0.0f);
        public Vector3 rotationOffset = new Vector3(0.0f, 0.0f, 0.0f);

        public UiValue xValue, yValue, zValue;
        public UiValue xRot, yRot, zRot;

        private const string PosKey = "SavedOffsetPosition";
        private const string RotKey = "SavedOffsetRotation";

        private GameObject obj;
        private Vector3 previousPosition;
        private Vector3 previousRotation;
        private bool objPlaced = false;

        private Vector3 lastUiPosition;
        private Vector3 lastUiRotation;
        private Vector3 lastOrienterPosition;
        private Vector3 lastOrienterRotation;

        private SortedDictionary<System.Guid, GameObject> qrCodesObjectsList = new SortedDictionary<System.Guid, GameObject>();
        private bool clearExisting = true;

        private HashSet<string> usedQRData = new HashSet<string>();
        private bool qrFoundThisSession = false;

        private bool firstSyncDone = false;

        public TextMeshProUGUI debugText;

        private List<QRCode> qrCodesFound = new List<QRCode>();
        private int? qrCodesRequired = null;

        private HashSet<string> sessionQRData = new HashSet<string>();

        struct ActionData
        {
            public enum Type { Added, Updated, Removed }
            public Type type;
            public Microsoft.MixedReality.QR.QRCode qrCode;

            public ActionData(Type type, Microsoft.MixedReality.QR.QRCode qRCode) : this()
            {
                this.type = type;
                qrCode = qRCode;
            }
        }

        private Queue<ActionData> pendingActions = new Queue<ActionData>();

        void Awake()
        {
            ResetSession();
        }

        private IEnumerator Start()
        {
            // 0) Basic null guards so we fail fast & clear
            if (qrCodePrefab == null) throw new System.Exception("qrCodePrefab not assigned");
            if (menu == null || infoText == null) Debug.LogWarning("menu/infoText not assigned");

            debugText.text = "Starting QRCode Visualizer...";
            // 1) Wait until the manager exists
            while (QRCodesManager.Instance == null)
                yield return null;

            // 2) Wait until the manager finished capability init (RequestAccessAsync)
            // Expose IsCapabilityInitialized in the manager (you already added a property in a previous message)
            while (!QRCodesManager.Instance.IsSupported)
                yield return null;

            debugText.text += "QRCode Visualizer: Manager exists and is supported.";
            debugText.text += "QRCode Visualizer started.";

            // 3) Now it's safe to touch the manager
            qrCodesObjectsList = new SortedDictionary<System.Guid, GameObject>();

            // Stopping is a no-op if not running; still guard it
            try { QRCodesManager.Instance.StopQRTracking(); } catch { /* ignore */ }

            debugText.text += "\nQR Tracking stopped for reset.";

            ResetSession();
            QRCodesManager.Instance.AutoStartQRTracking = true;

            debugText.text += "\nQR Tracking will auto-start.";

            // 4) Subscribe *after* we’re ready
            QRCodesManager.Instance.QRCodesTrackingStateChanged += Instance_QRCodesTrackingStateChanged;
            QRCodesManager.Instance.QRCodeAdded += Instance_QRCodeAdded;
            QRCodesManager.Instance.QRCodeUpdated += Instance_QRCodeUpdated;
            QRCodesManager.Instance.QRCodeRemoved += Instance_QRCodeRemoved;

            LoadOffsets();

            debugText.text += "\nSubscribed to QR events and loaded offsets.";
        }


        private void Instance_QRCodesTrackingStateChanged(object sender, bool status)
        {
            if (!status) clearExisting = true;
            debugText.text = $"QR Tracking State Changed: {status}";
        }

        private void Instance_QRCodeAdded(object sender, QRCodeEventArgs<Microsoft.MixedReality.QR.QRCode> e)
        {
            lock (pendingActions)
            {
                pendingActions.Enqueue(new ActionData(ActionData.Type.Added, e.Data));
            }
        }

        private void Instance_QRCodeUpdated(object sender, QRCodeEventArgs<Microsoft.MixedReality.QR.QRCode> e)
        {
            lock (pendingActions)
                pendingActions.Enqueue(new ActionData(ActionData.Type.Updated, e.Data));
        }

        private void Instance_QRCodeRemoved(object sender, QRCodeEventArgs<Microsoft.MixedReality.QR.QRCode> e)
        {
            lock (pendingActions)
                pendingActions.Enqueue(new ActionData(ActionData.Type.Removed, e.Data));
        }

        private void HandleEvents()
        {
            lock (pendingActions)
            {
                while (pendingActions.Count > 0)
                {
                    var action = pendingActions.Dequeue();
                    var qrData = action.qrCode?.Data;

                    if (action.type == ActionData.Type.Added || action.type == ActionData.Type.Updated)
                    {
                        if (!usedQRData.Contains(qrData))
                        {
                            GameObject qrCodeObject = Instantiate(qrCodePrefab, Vector3.zero, Quaternion.identity);
                            qrCodeObject.GetComponent<SpatialGraphCoordinateSystem>().Id = action.qrCode.SpatialGraphNodeId;
                            qrCodeObject.GetComponent<QRCode>().qrCode = action.qrCode;

                            qrCodesObjectsList[action.qrCode.Id] = qrCodeObject;
                            usedQRData.Add(qrData);
                        }
                    }
                    else if (action.type == ActionData.Type.Removed)
                    {
                        if (qrCodesObjectsList.TryGetValue(action.qrCode.Id, out GameObject go))
                        {
                            Destroy(go);
                            qrCodesObjectsList.Remove(action.qrCode.Id);
                            usedQRData.Remove(qrData);
                        }
                    }
                }
            }
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.W)) weeksManager.AddWeeks();

            if (Input.GetKeyDown(KeyCode.K))
            {
                obj = Instantiate(objPrefab, Vector3.zero, Quaternion.identity, transform);

                Vector3 sum = Vector3.zero;
                int count = 0;
                foreach (var qr in FindObjectsOfType<QRCode>())
                {
                    if (qr.orienter != null)
                    {
                        sum += qr.orienter.transform.position;
                        count++;
                    }
                }

                if(count > 0) sum /= count;
                orienter = Instantiate(orienterPrefab, sum, Quaternion.identity);
            }

            HandleEvents();

            if (clearExisting)
            {
                ResetSession();
                clearExisting = false;
            }

            bool foundQR = false;

            foreach (var pair in qrCodesObjectsList)
            {
                var qrGO = pair.Value;
                QRCode qrCode = qrGO.GetComponent<QRCode>();
                var qrData = qrCode.qrCode?.Data;

                if (!qrData.StartsWith("QR_") || qrCodesFound.Contains(qrCode)) continue;
                //debugText.text += $"Found QR Code: {qrData}";

                if (qrCodesFound.Count <= 0)
                {
                    qrCodesRequired = int.Parse(qrData.Split('_')[1]);
                }

                qrCodesFound.Add(qrCode);
                infoText.text = $"Found {qrCodesFound.Count}/{qrCodesRequired} QR Codes";

                //debugText.text += $"\nQR Codes Found: {qrCodesFound.Count}/{qrCodesRequired}";

                var spatial = qrGO.GetComponent<SpatialGraphCoordinateSystem>();
                //debugText.text += $"\nSpatial Graph Node: |{spatial}| && {spatial?.IsLocated} && {qrCodesFound.Count >= qrCodesRequired}";

                if (qrCodesFound.Count >= qrCodesRequired)
                {
                    UpdateObjectTransform(); // still needed to position the actual object in the world
                    SyncOrienterAndUiValues();
                    foundQR = true;
                    qrFoundThisSession = true;

                    //debugText.text += $"Obj: {obj} Orienter: {orienter}";

                    menu.SetActive(true);
                    infoText.gameObject.SetActive(false);

                    break;
                }
            }

            if (obj)
            {
                SyncOrienterAndUiValues();
                UpdateObjectTransform();
            }

            if (qrCodesFound.Count <= 0 && obj != null)
            {
                Destroy(obj);
                obj = null;
                qrFoundThisSession = false;
            }

        }

        private void UpdateObjectTransform()
        {
            if (orienter == null)
            {
                orienter = Instantiate(orienterPrefab, Vector3.zero, Quaternion.identity);
            }

            debugText.text = "Creating orienter...";
            Vector3 sum = Vector3.zero;
            int count = 0;
            foreach (var qr in qrCodesFound)
            {
                var spatial = qr.orienter.GetComponent<SpatialGraphCoordinateSystem>();
                if (spatial != null && spatial.IsLocated)
                {
                    debugText.text += $"\nUsing QR Code for orienter: {qr.orienter.transform.position}";
                    sum += qr.orienter.transform.position;
                    count++;
                }
                else
                {
                    debugText.text += "\nSkipping orienter (not located yet)";
                }
            }

            if (count > 0)
            {
                Vector3 finalPos = sum / count;
                debugText.text += $"\nSetting orienter position to: {finalPos}";
                orienter.transform.position = finalPos;
            }

            if (obj == null)
            {
                obj = Instantiate(objPrefab, Vector3.zero, Quaternion.identity, transform);
                weeksManager.AddWeeks();
            }

            if (orienter != null && obj != null)
            {
                obj.transform.position = orienter.transform.position + positionOffset;
                obj.transform.rotation = Quaternion.Euler(orienter.transform.eulerAngles + rotationOffset);
            }
        }

        private void SaveOffsets(Vector3 position, Vector3 rotation)
        {
            PlayerPrefs.SetString(PosKey, $"{position.x},{position.y},{position.z}");
            PlayerPrefs.SetString(RotKey, $"{rotation.x},{rotation.y},{rotation.z}");
            PlayerPrefs.Save();
        }

        private void LoadOffsets()
        {
            if (PlayerPrefs.HasKey(PosKey) && PlayerPrefs.HasKey(RotKey))
            {
                string[] pos = PlayerPrefs.GetString(PosKey).Split(',');
                string[] rot = PlayerPrefs.GetString(RotKey).Split(',');

                positionOffset = new Vector3(
                    float.Parse(pos[0]),
                    float.Parse(pos[1]),
                    float.Parse(pos[2])
                );

                rotationOffset = new Vector3(
                    float.Parse(rot[0]),
                    float.Parse(rot[1]),
                    float.Parse(rot[2])
                );
            }
        }

        private void SyncOrienterAndUiValues()
        {
            if (orienter == null) return;

            Vector3 uiPos = new Vector3(xValue.Value, yValue.Value, zValue.Value);
            Vector3 uiRot = new Vector3(xRot.Value, yRot.Value, zRot.Value);

            Vector3 orienterPos = orienter.transform.position;
            Vector3 orienterRot = orienter.transform.eulerAngles;

            if (!firstSyncDone)
            {
                // On first detection, align UI with orienter
                xValue.Value = orienterPos.x;
                yValue.Value = orienterPos.y;
                zValue.Value = orienterPos.z;

                xRot.Value = orienterRot.x;
                yRot.Value = orienterRot.y;
                zRot.Value = orienterRot.z;

                lastUiPosition = orienterPos;
                lastUiRotation = orienterRot;
                lastOrienterPosition = orienterPos;
                lastOrienterRotation = orienterRot;

                firstSyncDone = true;
                return;
            }

            bool uiChanged = uiPos != lastUiPosition || uiRot != lastUiRotation;
            bool orienterChanged = orienterPos != lastOrienterPosition || orienterRot != lastOrienterRotation;

            if (uiChanged && !orienterChanged)
            {
                // UI changed → update orienter
                orienter.transform.position = uiPos;
                orienter.transform.eulerAngles = uiRot;
            }
            else if (orienterChanged && !uiChanged)
            {
                // Orienter changed → update UI
                xValue.Value = orienterPos.x;
                yValue.Value = orienterPos.y;
                zValue.Value = orienterPos.z;

                xRot.Value = orienterRot.x;
                yRot.Value = orienterRot.y;
                zRot.Value = orienterRot.z;
            }

            // Save last frame values
            lastUiPosition = new Vector3(xValue.Value, yValue.Value, zValue.Value);
            lastUiRotation = new Vector3(xRot.Value, yRot.Value, zRot.Value);
            lastOrienterPosition = orienter.transform.position;
            lastOrienterRotation = orienter.transform.eulerAngles;

            //debugText.text += $"Orienter Position: {orienter.transform.position}\n" +
            //$"Orienter Rotation: {orienter.transform.eulerAngles}\n" +
            //$"UI Position: {lastUiPosition}\n" +
            //$"UI Rotation: {lastUiRotation}";
        }

        public void ClearSavedOffsets()
        {
            PlayerPrefs.DeleteKey(PosKey);
            PlayerPrefs.DeleteKey(RotKey);
            PlayerPrefs.Save();
        }

        public void ResetSession()
        {
            menu.SetActive(false);
            infoText.gameObject.SetActive(true);

            // Stop watcher
            QRCodesManager.Instance?.StopQRTracking();
            debugText.text += "Resetting session...";
            debugText.text += $"\nClearing {qrCodesObjectsList.Count} QR objects";

            // Destroy scene objects
            foreach (var qr in FindObjectsOfType<QRCode>())
                Destroy(qr.gameObject);

            if (obj != null) Destroy(obj);
            obj = null;

            if (orienter != null) Destroy(orienter);
            orienter = null;

            qrCodesObjectsList.Clear();
            usedQRData.Clear();
            qrCodesFound.Clear();
            qrCodesRequired = null;

            debugText.text += $"\nCleared all QR objects";

            qrFoundThisSession = false;
            firstSyncDone = false;

            // Restart watcher
            QRCodesManager.Instance?.StartQRTracking();
            debugText.text += $"\nRestarted QR tracking";
        }
    }
}