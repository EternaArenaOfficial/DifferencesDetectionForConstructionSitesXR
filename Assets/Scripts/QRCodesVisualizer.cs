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

        private SortedDictionary<System.Guid, GameObject> qrCodesObjectsList;
        private bool clearExisting = false;

        private HashSet<string> usedQRData = new HashSet<string>();
        private bool qrFoundThisSession = false;

        private bool firstSyncDone = false;

        public TextMeshProUGUI debugText;

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

        void Start()
        {
            Debug.Log("QRCodesVisualizer start");
            qrCodesObjectsList = new SortedDictionary<System.Guid, GameObject>();

            QRCodesManager.Instance.QRCodesTrackingStateChanged += Instance_QRCodesTrackingStateChanged;
            QRCodesManager.Instance.QRCodeAdded += Instance_QRCodeAdded;
            QRCodesManager.Instance.QRCodeUpdated += Instance_QRCodeUpdated;
            QRCodesManager.Instance.QRCodeRemoved += Instance_QRCodeRemoved;

            if (qrCodePrefab == null)
                throw new System.Exception("Prefab not assigned");

            LoadOffsets();
        }

        private void Instance_QRCodesTrackingStateChanged(object sender, bool status)
        {
            if (!status) clearExisting = true;
        }

        private void Instance_QRCodeAdded(object sender, QRCodeEventArgs<Microsoft.MixedReality.QR.QRCode> e)
        {
            lock (pendingActions)
                pendingActions.Enqueue(new ActionData(ActionData.Type.Added, e.Data));
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

                    if (action.type == ActionData.Type.Added)
                    {
                        if (!usedQRData.Contains(qrData))
                        {
                            GameObject qrCodeObject = Instantiate(qrCodePrefab, Vector3.zero, Quaternion.identity);
                            qrCodeObject.GetComponent<SpatialGraphCoordinateSystem>().Id = action.qrCode.SpatialGraphNodeId;
                            qrCodeObject.GetComponent<QRCode>().qrCode = action.qrCode;

                            qrCodesObjectsList.Add(action.qrCode.Id, qrCodeObject);
                            usedQRData.Add(qrData);
                        }
                        else
                        {
                            Debug.Log($"Duplicate QR code data detected and ignored: {qrData}");
                        }
                    }
                    else if (action.type == ActionData.Type.Updated)
                    {
                        if (!qrCodesObjectsList.ContainsKey(action.qrCode.Id))
                        {
                            if (!usedQRData.Contains(qrData))
                            {
                                GameObject qrCodeObject = Instantiate(qrCodePrefab, Vector3.zero, Quaternion.identity);
                                qrCodeObject.GetComponent<SpatialGraphCoordinateSystem>().Id = action.qrCode.SpatialGraphNodeId;
                                qrCodeObject.GetComponent<QRCode>().qrCode = action.qrCode;

                                qrCodesObjectsList.Add(action.qrCode.Id, qrCodeObject);
                                usedQRData.Add(qrData);
                            }
                        }
                    }
                    else if (action.type == ActionData.Type.Removed)
                    {
                        if (qrCodesObjectsList.ContainsKey(action.qrCode.Id))
                        {
                            GameObject go = qrCodesObjectsList[action.qrCode.Id];
                            string dataToRemove = go.GetComponent<QRCode>().qrCode?.Data;

                            if (dataToRemove != null && usedQRData.Contains(dataToRemove))
                                usedQRData.Remove(dataToRemove);

                            Destroy(go);
                            qrCodesObjectsList.Remove(action.qrCode.Id);

                            if (dataToRemove == "Test1")
                            {
                                if (obj != null) Destroy(obj);
                                obj = null;
                                qrFoundThisSession = false;
                            }
                        }
                    }
                }
            }

            if (clearExisting)
            {
                clearExisting = false;
                foreach (var obj in qrCodesObjectsList.Values)
                    Destroy(obj);

                qrCodesObjectsList.Clear();
                usedQRData.Clear();

                if (obj != null) Destroy(obj);
                obj = null;
                qrFoundThisSession = false;
            }
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.W)) weeksManager.AddWeeks();

            HandleEvents();

            bool foundQR = false;

            foreach (var pair in qrCodesObjectsList)
            {
                var qrGO = pair.Value;
                QRCode qrCode = qrGO.GetComponent<QRCode>();
                var qrData = qrCode.qrCode?.Data;

                if (qrData != "Test1") continue;

                var spatial = qrGO.GetComponent<SpatialGraphCoordinateSystem>();
                if (spatial != null && spatial.IsLocated)
                {
                    SyncOrienterAndUiValues(qrCode);
                    UpdateObjectTransform(qrCode); // still needed to position the actual object in the world
                    foundQR = true;
                    qrFoundThisSession = true;
                    break;
                }
            }


            if (!foundQR && obj != null)
            {
                Destroy(obj);
                obj = null;
                qrFoundThisSession = false;
            }
        }

        private void UpdateObjectTransform(QRCode qr)
        {
            if (obj == null)
            {
                obj = Instantiate(objPrefab, Vector3.zero, Quaternion.identity, transform);
                qr.relatedObj = obj;
                weeksManager.AddWeeks();
            }

            if (qr.orienter == null)
            {
                debugText.text = ("qr.orienter is null. Skipping transform update.");
                return;
            }

            Transform orienter = qr.orienter.transform;

            obj.transform.position = orienter.position;
            obj.transform.eulerAngles = orienter.eulerAngles;
            obj.transform.localScale = Vector3.one * 0.001f;
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

        private void SyncOrienterAndUiValues(QRCode qr)
        {
            if (qr.orienter == null) return;

            Transform orienter = qr.orienter.transform;

            Vector3 uiPos = new Vector3(xValue.Value, yValue.Value, zValue.Value);
            Vector3 uiRot = new Vector3(xRot.Value, yRot.Value, zRot.Value);

            Vector3 orienterPos = orienter.position;
            Vector3 orienterRot = orienter.eulerAngles;

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
                orienter.position = uiPos;
                orienter.eulerAngles = uiRot;
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
            lastOrienterPosition = orienter.position;
            lastOrienterRotation = orienter.eulerAngles;
        }

        public void ClearSavedOffsets()
        {
            PlayerPrefs.DeleteKey(PosKey);
            PlayerPrefs.DeleteKey(RotKey);
            PlayerPrefs.Save();
        }
    }
}
