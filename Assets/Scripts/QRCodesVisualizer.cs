using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System.IO;
using Microsoft.MixedReality.QR;
using Dummiesman;
using TMPro;
using System;
using System.Text.RegularExpressions;

namespace QRTracking
{
    public class QRCodesVisualizer : MonoBehaviour
    {
        public WeeksManager weeksManager;

        public GameObject qrCodePrefab;
        public GameObject objPrefab;
        public Vector3 positionOffset = Vector3.zero;
        public Vector3 rotationOffset = Vector3.zero;

        public TextMeshProUGUI debugTMP;

        private GameObject obj;
        private Vector3 previousPosition;
        private Vector3 previousRotation;

        private SortedDictionary<System.Guid, GameObject> qrCodesObjectsList = new SortedDictionary<Guid, GameObject>();
        private HashSet<string> usedQRData = new HashSet<string>();
        private Queue<ActionData> pendingActions = new Queue<ActionData>();
        private bool clearExisting = false;

        private const string PosKey = "SavedOffsetPosition";
        private const string RotKey = "SavedOffsetRotation";
        private const string ModelHashKey = "CachedModelHash";

        struct ActionData
        {
            public enum Type { Added, Updated, Removed };
            public Type type;
            public Microsoft.MixedReality.QR.QRCode qrCode;

            public ActionData(Type type, Microsoft.MixedReality.QR.QRCode qRCode) : this()
            {
                this.type = type;
                this.qrCode = qRCode;
            }
        }

        void Start()
        {
            ClearAllTrackedQRCodes();
            Debug.Log("QRCodesVisualizer start");

            QRCodesManager.Instance.QRCodesTrackingStateChanged += Instance_QRCodesTrackingStateChanged;
            QRCodesManager.Instance.QRCodeAdded += Instance_QRCodeAdded;
            QRCodesManager.Instance.QRCodeUpdated += Instance_QRCodeUpdated;
            QRCodesManager.Instance.QRCodeRemoved += Instance_QRCodeRemoved;

            if (qrCodePrefab == null)
                throw new Exception("Prefab not assigned");

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

                    if (string.IsNullOrEmpty(qrData)) continue;

                    if (action.type == ActionData.Type.Added || action.type == ActionData.Type.Updated)
                    {
                        if (!usedQRData.Contains(qrData))
                        {
                            var qrCodeObject = Instantiate(qrCodePrefab, Vector3.zero, Quaternion.identity);
                            qrCodeObject.GetComponent<SpatialGraphCoordinateSystem>().Id = action.qrCode.SpatialGraphNodeId;
                            qrCodeObject.GetComponent<QRCode>().qrCode = action.qrCode;

                            qrCodesObjectsList[action.qrCode.Id] = qrCodeObject;
                            usedQRData.Add(qrData);
                        }
                        else
                        {
                            Debug.Log($"Duplicate QR code data ignored: {qrData}");
                        }
                    }
                    else if (action.type == ActionData.Type.Removed)
                    {
                        if (qrCodesObjectsList.ContainsKey(action.qrCode.Id))
                        {
                            var go = qrCodesObjectsList[action.qrCode.Id];
                            string dataToRemove = go.GetComponent<QRCode>().qrCode?.Data;
                            if (dataToRemove != null)
                                usedQRData.Remove(dataToRemove);

                            Destroy(go);
                            qrCodesObjectsList.Remove(action.qrCode.Id);
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
            }
        }

        public void Scan()
        {
            foreach (var pair in qrCodesObjectsList)
            {
                var qr = pair.Value;
                var qrCode = qr.GetComponent<QRCode>();
                string qrData = qrCode.qrCode?.Data;

                var spatial = qr.GetComponent<SpatialGraphCoordinateSystem>();
                if (spatial == null || !spatial.IsLocated || string.IsNullOrEmpty(qrData)) continue;

                if (qrData == "Test1")
                {
                    UpdateReferenceObject(qrCode);
                    break;
                }
                else if (qrData.StartsWith("http"))
                {
                    UpdateDownloadedObject(qrCode);
                    break;
                }
            }
        }

        void Update()
        {
            if(Input.GetKeyDown(KeyCode.Q)) StartCoroutine(DownloadAndReplaceModel(@"https://drive.google.com/file/d/1BTqtfiU7lIGF2tDfYplgi5pX7f9N8SnH/view?usp=sharing"));
            //return;
            HandleEvents();
        }

        private void UpdateReferenceObject(QRCode qr)
        {
            Vector3 localOffsetPos = qr.orienter.transform.position;
            Vector3 localOffsetRot = qr.orienter.transform.eulerAngles;

            if (previousPosition != localOffsetPos || previousRotation != localOffsetRot)
            {
                previousPosition = localOffsetPos;
                previousRotation = localOffsetRot;
                SaveOffsets(localOffsetPos, localOffsetRot);
            }

            if (obj == null)
            {
                obj = Instantiate(objPrefab, transform);
                weeksManager.AddWeeks();
            }

            obj.transform.position = localOffsetPos + positionOffset;
            obj.transform.eulerAngles = localOffsetRot + rotationOffset;
        }

        private void UpdateDownloadedObject(QRCode qr)
        {
            Vector3 localOffsetPos = qr.orienter.transform.position;
            Vector3 localOffsetRot = qr.orienter.transform.eulerAngles;

            if (previousPosition != localOffsetPos || previousRotation != localOffsetRot)
            {
                previousPosition = localOffsetPos;
                previousRotation = localOffsetRot;
                SaveOffsets(localOffsetPos, localOffsetRot);
            }

            string modelUrl = qr.qrCode?.Data;
            if (!string.IsNullOrEmpty(modelUrl))
                StartCoroutine(DownloadAndReplaceModel(modelUrl));
        }

        private IEnumerator DownloadAndReplaceModel(string url)
        {
            print("-A0-");

            string cleanUrl = ConvertGoogleDriveToDirect(url);

            print("Downloading model from: " + cleanUrl);
            print("Model URL: " + url);

            string fileName = "Reference.obj";
            string localPath = Path.Combine(Application.persistentDataPath, fileName);

            print("Local path: " + localPath);
            debugTMP.text += $"-A0 LP {localPath}-";

            UnityWebRequest request = UnityWebRequest.Get(cleanUrl);
            request.timeout = 20;
            yield return request.SendWebRequest();

            debugTMP.text += "-A1-";
            print("-A1-");

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Download failed: " + request.error);
                debugTMP.text += "[Error] Download failed: " + request.error + "\n";
                yield break;
            }

            debugTMP.text += "-A2-";
            print("-A2-");

            string remoteHash = request.GetResponseHeader("ETag") ?? request.GetResponseHeader("Last-Modified") ?? "";
            string savedHash = PlayerPrefs.GetString(ModelHashKey, "");

            if (File.Exists(localPath) && savedHash == remoteHash)
            {
                Debug.Log("Model already exists and hash matches, skipping download.");
                debugTMP.text += "[Info] Model already exists and hash matches\n";
            }
            else
            {
                debugTMP.text += "-A2.1-";
                print("-A2.1-");

                try
                {
                    File.WriteAllBytes(localPath, request.downloadHandler.data);
                    PlayerPrefs.SetString(ModelHashKey, remoteHash);
                    PlayerPrefs.Save();
                }
                catch (Exception e)
                {
                    Debug.LogError("File save error: " + e.Message);
                    debugTMP.text += "[Error] File save failed: " + e.Message + "\n";
                    yield break;
                }

                debugTMP.text += "-A2.2-";
                print("-A2.2-");
            }

            Debug.Log(File.ReadAllText(localPath).Substring(0, 500));
            debugTMP.text += $"-A3-";
            print("-A3-");

            if (obj != null)
                Destroy(obj);

            // Remove mtllib line
            string objText = File.ReadAllText(localPath);
            objText = Regex.Replace(objText, @"^mtllib .*\r?\n?", "", RegexOptions.Multiline);
            File.WriteAllText(localPath, objText);


            GameObject loadedObj;
            try
            {
                loadedObj = new OBJLoader().Load(localPath);
                debugTMP.text += "[Success] Model loaded\n";
                print("[Success] Model loaded");

                if (loadedObj == null)
                {
                    debugTMP.text += "[Error] loadedObj is null\n";
                    yield break;
                }
            }
            catch (Exception e)
            {
                Debug.LogError("Model load failed: " + e.Message);
                debugTMP.text += "[Error] Model load failed: " + e.Message + "\n";
                yield break;
            }

            debugTMP.text += $"MeshFilters: {loadedObj.GetComponentsInChildren<MeshFilter>().Length}\n";
            debugTMP.text += $"Renderers: {loadedObj.GetComponentsInChildren<Renderer>().Length}\n";
            debugTMP.text += $"SkinnedMeshRenderers: {loadedObj.GetComponentsInChildren<SkinnedMeshRenderer>().Length}\n";

            debugTMP.text += "-A4-";
            print("-A4-");

            foreach (var mf in loadedObj.GetComponentsInChildren<MeshFilter>())
            {
                if (mf.sharedMesh != null)
                    mf.sharedMesh = Instantiate(mf.sharedMesh);
            }

            foreach (var smr in loadedObj.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                if (smr.sharedMesh != null)
                    smr.sharedMesh = Instantiate(smr.sharedMesh);
            }

            // 🔧 Fix for missing shader issue
            Shader defaultShader = Shader.Find("Standard");
            if (defaultShader == null)
            {
                debugTMP.text += "[Error] Shader 'Standard' not found\n";
                Debug.LogError("Shader 'Standard' not found");
            }
            else
            {
                foreach (var renderer in loadedObj.GetComponentsInChildren<Renderer>())
                {
                    if (renderer.sharedMaterial == null)
                    {
                        renderer.material = new Material(defaultShader);
                        debugTMP.text += "[Fix] Assigned Standard shader to renderer\n";
                    }
                }
            }

            loadedObj.transform.SetParent(transform);
            loadedObj.transform.position = previousPosition + positionOffset;
            loadedObj.transform.eulerAngles = previousRotation + rotationOffset;

            obj = loadedObj;

            debugTMP.text += "Model loaded and positioned.\n";
            debugTMP.text += "-A5-";
            print("-A5-");

            weeksManager.AddWeeks();
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
            PlayerPrefs.DeleteKey(ModelHashKey);
            PlayerPrefs.Save();
        }

        private void ClearAllTrackedQRCodes()
        {
            foreach (var obj in qrCodesObjectsList.Values)
                Destroy(obj);

            qrCodesObjectsList.Clear();
            usedQRData.Clear();
            clearExisting = false;
        }

        public static string ConvertGoogleDriveToDirect(string originalUrl)
        {
            if (string.IsNullOrEmpty(originalUrl)) return originalUrl;

            if (originalUrl.Contains("/uc?id="))
                return originalUrl; // Already converted

            string marker = "/file/d/";
            if (originalUrl.Contains(marker))
            {
                int idStart = originalUrl.IndexOf(marker) + marker.Length;
                int idEnd = originalUrl.IndexOf('/', idStart);
                if (idEnd == -1) idEnd = originalUrl.Length;

                string fileId = originalUrl.Substring(idStart, idEnd - idStart);
                return $"https://drive.google.com/uc?id={fileId}";
            }

            Debug.LogWarning("Invalid Google Drive link format.");
            return originalUrl;
        }
    }
}
