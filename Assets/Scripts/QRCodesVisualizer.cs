using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Microsoft.MixedReality.QR;

namespace QRTracking
{
    public class QRCodesVisualizer : MonoBehaviour
    {
        public WeeksManager weeksManager;

        public GameObject qrCodePrefab;

        private GameObject obj;
        public GameObject objPrefab;

        // Manual adjustment
        public Vector3 positionOffset = new Vector3(0.0f, 0.0f, 0.0f);
        public Vector3 rotationOffset = new Vector3(0.0f, 0.0f, 0.0f);

        private Vector3 previousPosition;
        private Vector3 previousRotation;

        private bool objPlaced = false;

        private SortedDictionary<System.Guid, GameObject> qrCodesObjectsList;
        private bool clearExisting = false;

        // Track already instantiated QR code data strings to avoid duplicates
        private HashSet<string> usedQRData = new HashSet<string>();

        public UiValue xValue, yValue, zValue;

        private const string PosKey = "SavedOffsetPosition";
        private const string RotKey = "SavedOffsetRotation";

        struct ActionData
        {
            public enum Type
            {
                Added,
                Updated,
                Removed
            };
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
        }

        // Use this for initialization
        void Start()
        {
            Debug.Log("QRCodesVisualizer start");
            qrCodesObjectsList = new SortedDictionary<System.Guid, GameObject>();

            QRCodesManager.Instance.QRCodesTrackingStateChanged += Instance_QRCodesTrackingStateChanged;
            QRCodesManager.Instance.QRCodeAdded += Instance_QRCodeAdded;
            QRCodesManager.Instance.QRCodeUpdated += Instance_QRCodeUpdated;
            QRCodesManager.Instance.QRCodeRemoved += Instance_QRCodeRemoved;

            if (qrCodePrefab == null)
            {
                throw new System.Exception("Prefab not assigned");
            }

            LoadOffsets();
        }


        private void Instance_QRCodesTrackingStateChanged(object sender, bool status)
        {
            if (!status)
            {
                clearExisting = true;
            }
        }

        private void Instance_QRCodeAdded(object sender, QRCodeEventArgs<Microsoft.MixedReality.QR.QRCode> e)
        {
            Debug.Log("QRCodesVisualizer Instance_QRCodeAdded");

            lock (pendingActions)
            {
                pendingActions.Enqueue(new ActionData(ActionData.Type.Added, e.Data));
            }
        }

        private void Instance_QRCodeUpdated(object sender, QRCodeEventArgs<Microsoft.MixedReality.QR.QRCode> e)
        {
            Debug.Log("QRCodesVisualizer Instance_QRCodeUpdated");

            lock (pendingActions)
            {
                pendingActions.Enqueue(new ActionData(ActionData.Type.Updated, e.Data));
            }
        }

        private void Instance_QRCodeRemoved(object sender, QRCodeEventArgs<Microsoft.MixedReality.QR.QRCode> e)
        {
            Debug.Log("QRCodesVisualizer Instance_QRCodeRemoved");

            lock (pendingActions)
            {
                pendingActions.Enqueue(new ActionData(ActionData.Type.Removed, e.Data));
            }
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
                        // Check if QR code data already used; skip if yes
                        if (!usedQRData.Contains(qrData))
                        {
                            GameObject qrCodeObject = Instantiate(qrCodePrefab, Vector3.zero, Quaternion.identity);
                            qrCodeObject.GetComponent<SpatialGraphCoordinateSystem>().Id = action.qrCode.SpatialGraphNodeId;
                            qrCodeObject.GetComponent<QRCode>().qrCode = action.qrCode;

                            qrCodesObjectsList.Add(action.qrCode.Id, qrCodeObject);
                            usedQRData.Add(qrData);  // Track this data
                        }
                        else
                        {
                            Debug.Log($"Duplicate QR code data detected and ignored: {qrData}");
                        }
                    }
                    else if (action.type == ActionData.Type.Updated)
                    {
                        // If the QR code ID is not tracked but data already used, skip
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
                            else
                            {
                                Debug.Log($"Duplicate QR code data detected on update and ignored: {qrData}");
                            }
                        }
                    }
                    else if (action.type == ActionData.Type.Removed)
                    {
                        if (qrCodesObjectsList.ContainsKey(action.qrCode.Id))
                        {
                            // Remove from dictionary and free the data tracking
                            GameObject go = qrCodesObjectsList[action.qrCode.Id];
                            string dataToRemove = go.GetComponent<QRCode>().qrCode?.Data;

                            if (dataToRemove != null && usedQRData.Contains(dataToRemove))
                            {
                                usedQRData.Remove(dataToRemove);
                            }

                            Destroy(go);
                            qrCodesObjectsList.Remove(action.qrCode.Id);
                        }
                    }
                }
            }

            if (clearExisting)
            {
                clearExisting = false;
                foreach (var obj in qrCodesObjectsList)
                {
                    Destroy(obj.Value);
                }
                qrCodesObjectsList.Clear();
                usedQRData.Clear();
            }
        }

        // Update is called once per frame
        void Update()
        {
            HandleEvents();

            foreach (var pair in qrCodesObjectsList)
            {
                var qr = pair.Value;
                QRCode qrCode = qr.GetComponent<QRCode>();
                var qrData = qrCode.qrCode?.Data;

                if (qrData != "Test1") continue;

                var spatial = qr.GetComponent<SpatialGraphCoordinateSystem>();
                if (spatial != null && spatial.IsLocated)
                {
                    UpdateObjectTransform(qrCode);
                    break;  // Use only the first located QR code with data "Test1"
                }
            }
        }

        private void UpdateObjectTransform(QRCode qr)
        {
            if (obj == null)
            {
                obj = Instantiate(objPrefab, transform);
                weeksManager.AddWeeks();
            }

            // Apply offsets from orienter's transform
            Vector3 localOffsetPos = qr.orienter.transform.position;
            Vector3 localOffsetRot = qr.orienter.transform.eulerAngles;

            if(previousPosition != qr.orienter.transform.position || previousRotation != qr.orienter.transform.eulerAngles)
            {
                previousPosition = qr.orienter.transform.position;
                previousRotation = qr.orienter.transform.eulerAngles;

                SaveOffsets(localOffsetPos,localOffsetRot);
            }

            obj.transform.position = localOffsetPos;
            obj.transform.eulerAngles = localOffsetRot;
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

        public void ClearSavedOffsets()
        {
            PlayerPrefs.DeleteKey(PosKey);
            PlayerPrefs.DeleteKey(RotKey);
            PlayerPrefs.Save();
        }
    }
}
