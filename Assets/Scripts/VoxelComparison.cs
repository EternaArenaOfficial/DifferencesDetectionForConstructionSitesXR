using System.Collections.Generic;
using UnityEngine;
using Microsoft.MixedReality.Toolkit;
using Microsoft.MixedReality.Toolkit.SpatialAwareness;
using Microsoft.MixedReality.Toolkit.UI;
using System.IO;
using System.Text;
using System.Collections;
using System.Globalization;

public class VoxelComparison : MonoBehaviour
{
    public GameObject referenceContainer;
    public float voxelSize = 0.05f;
    public GameObject debugCubePrefab;
    public GameObject loadingUi;
    public GameObject menu;

    private Dictionary<Transform, (int matched, int total)> objectVoxelStats = new Dictionary<Transform, (int matched, int total)>();

    private string output;
    private string Output
    {
        get => output;
        set
        {
            output = value;
            if(buttonConfigHelper) buttonConfigHelper.MainLabelText = value;
        }
    }

    public ButtonConfigHelper buttonConfigHelper;

    public VerticalContent list;
    public ListElementUi listElement;

    public VerticalContent weeksList;

    public PressableButtonHoloLens2 compareButton;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.V))
        {
            StartCoroutine(CompareVoxelGridsCoroutine());
        }
    }

    public void CompareVoxelGrids()
    {
        StartCoroutine(CompareVoxelGridsCoroutine());
    }

    IEnumerator CompareVoxelGridsCoroutine()
    {
        if (compareButton) compareButton.enabled = false;

        // Show loading UI, hide main UI
        if (loadingUi) loadingUi.SetActive(true);
        if (menu) menu.SetActive(false);
        if (buttonConfigHelper) buttonConfigHelper.gameObject.SetActive(false);

        objectVoxelStats.Clear();

        try
        {
            HashSet<Vector3Int> scannedVoxels = VoxelizeSpatialMesh(referenceContainer.transform);
            yield return StartCoroutine(CompareAndReportCoroutine(referenceContainer.transform, scannedVoxels));

            Debug.Log("\n--- Summary ---");
            Output += "--- Summary ---\n";
            StringBuilder csvBuilder = new StringBuilder();
            csvBuilder.AppendLine("Name,Percentage,Status");

            foreach (var kvp in objectVoxelStats)
            {
                Transform t = kvp.Key;

                if (t == referenceContainer.transform)
                    continue;

                int matched = kvp.Value.matched;
                int total = kvp.Value.total;

                if (!t.gameObject.activeInHierarchy || total == 0)
                    continue;

                float percent = (float)matched / total * 100f;
                string status = percent >= 15f ? "Completed" : "Not Completed";

                bool isDirectChild = t.parent == referenceContainer.transform.GetChild(0);

                if (isDirectChild && weeksList.container.GetChild(t.GetSiblingIndex()).TryGetComponent<WeekPanel>(out var weekPanel))
                {
                    weekPanel.value.text = $"{percent:F1}%";
                }

                Debug.Log($"{t.name} - {status} ({percent:F1}%)");
                Output += $"{t.name} - {status} ({percent:F1}%)\n";

                csvBuilder.AppendLine($"\"{t.name}\",\"{percent.ToString("F1", CultureInfo.InvariantCulture)}%\",\"{status}\"");

                if (list != null && listElement != null)
                {
                    var button = Instantiate(listElement, list.container);
                    button.label.text = t.name;
                    button.value.text = $"({percent:F1}%)";
                    list.AddElement(button.gameObject);
                }

                yield return null; // Yield after each object summary update
            }

            SaveCSV(csvBuilder.ToString());
        }
        finally
        {
            // Hide loading, show main UI
            if (loadingUi) loadingUi.SetActive(false);
            if (menu) menu.SetActive(true);
            if (buttonConfigHelper) buttonConfigHelper.gameObject.SetActive(true);

            if (compareButton) compareButton.enabled = true;
        }
    }

    IEnumerator CompareAndReportCoroutine(Transform current, HashSet<Vector3Int> scannedVoxels)
    {
        int matched = 0, total = 0;

        if (current.gameObject.activeInHierarchy)
        {
            MeshFilter mf = current.GetComponent<MeshFilter>();
            if (mf != null)
            {
                HashSet<Vector3Int> partVoxels = VoxelizeMesh(mf, referenceContainer.transform);
                total += partVoxels.Count;

                int counter = 0;
                foreach (var voxel in partVoxels)
                {
                    if (scannedVoxels.Contains(voxel))
                    {
                        matched++;
                    }
                    else if (debugCubePrefab != null)
                    {
                        Vector3 worldPos = referenceContainer.transform.TransformPoint(VoxelToLocalPosition(voxel));
                        Instantiate(debugCubePrefab, worldPos, Quaternion.identity);
                    }

                    counter++;
                    if (counter % 500 == 0) yield return null; // Yield every 500 voxels
                }
            }
        }

        foreach (Transform child in current)
        {
            yield return StartCoroutine(CompareAndReportCoroutine(child, scannedVoxels));
            if (objectVoxelStats.TryGetValue(child, out var childStats))
            {
                matched += childStats.matched;
                total += childStats.total;
            }
        }

        objectVoxelStats[current] = (matched, total);
    }

    HashSet<Vector3Int> VoxelizeSpatialMesh(Transform referenceSpace)
    {
        HashSet<Vector3Int> allVoxels = new HashSet<Vector3Int>();
        var spatialObserver = CoreServices.SpatialAwarenessSystem.GetObserver<IMixedRealitySpatialAwarenessMeshObserver>();

        if (spatialObserver == null)
        {
            Debug.Log("No spatial mesh observer found.");
            Output = "No spatial mesh observer found.";
            return allVoxels;
        }

        Debug.Log($"Voxelizing {spatialObserver.Meshes.Count} spatial meshes...");
        Output += $"Voxelizing {spatialObserver.Meshes.Count} spatial meshes...\n";

        foreach (var meshObject in spatialObserver.Meshes.Values)
        {
            MeshFilter mf = meshObject.Filter;
            if (mf != null)
            {
                var voxels = VoxelizeMesh(mf, referenceSpace);
                foreach (var v in voxels)
                    allVoxels.Add(v);
            }
        }

        return allVoxels;
    }

    HashSet<Vector3Int> VoxelizeMesh(MeshFilter mf, Transform referenceSpace)
    {
        Mesh mesh = mf.sharedMesh;
        if (!mesh.isReadable)
        {
            Debug.Log($"Mesh on {mf.name} is not readable.");
            return new HashSet<Vector3Int>();
        }

        HashSet<Vector3Int> voxels = new HashSet<Vector3Int>();
        Vector3[] vertices = mesh.vertices;
        int[] triangles = mesh.triangles;

        for (int i = 0; i < triangles.Length; i += 3)
        {
            Vector3 v0 = TransformToReferenceSpace(vertices[triangles[i]], mf.transform, referenceSpace);
            Vector3 v1 = TransformToReferenceSpace(vertices[triangles[i + 1]], mf.transform, referenceSpace);
            Vector3 v2 = TransformToReferenceSpace(vertices[triangles[i + 2]], mf.transform, referenceSpace);

            Bounds triBounds = new Bounds(v0, Vector3.zero);
            triBounds.Encapsulate(v1);
            triBounds.Encapsulate(v2);

            Vector3 min = triBounds.min;
            Vector3 max = triBounds.max;

            for (float x = min.x; x <= max.x; x += voxelSize)
            {
                for (float y = min.y; y <= max.y; y += voxelSize)
                {
                    for (float z = min.z; z <= max.z; z += voxelSize)
                    {
                        Vector3 center = new Vector3(x, y, z);
                        Vector3 half = Vector3.one * (voxelSize * 0.5f);
                        if (TriangleIntersectsBox(center, half, v0, v1, v2))
                        {
                            voxels.Add(WorldToVoxel(center));
                        }
                    }
                }
            }
        }

        return voxels;
    }

    Vector3 TransformToReferenceSpace(Vector3 localVertex, Transform from, Transform referenceSpace)
    {
        Vector3 worldPoint = from.TransformPoint(localVertex);
        return referenceSpace.InverseTransformPoint(worldPoint);
    }

    Vector3Int WorldToVoxel(Vector3 localReferencePos)
    {
        return Vector3Int.RoundToInt(localReferencePos / voxelSize);
    }

    Vector3 VoxelToLocalPosition(Vector3Int voxelCoord)
    {
        return new Vector3(
            voxelCoord.x * voxelSize,
            voxelCoord.y * voxelSize,
            voxelCoord.z * voxelSize
        );
    }

    bool TriangleIntersectsBox(Vector3 boxCenter, Vector3 boxHalfSize, Vector3 v0, Vector3 v1, Vector3 v2)
    {
        v0 -= boxCenter;
        v1 -= boxCenter;
        v2 -= boxCenter;

        Vector3[] triVerts = { v0, v1, v2 };
        Vector3[] boxAxes = { Vector3.right, Vector3.up, Vector3.forward };

        for (int i = 0; i < 3; i++)
        {
            Vector3 edge = triVerts[(i + 1) % 3] - triVerts[i];
            foreach (Vector3 axis in boxAxes)
            {
                Vector3 testAxis = Vector3.Cross(edge, axis);
                if (testAxis.sqrMagnitude < 1e-6f) continue;

                float minTri, maxTri;
                ProjectTriangle(testAxis, triVerts, out minTri, out maxTri);

                float r = boxHalfSize.x * Mathf.Abs(Vector3.Dot(testAxis, Vector3.right)) +
                          boxHalfSize.y * Mathf.Abs(Vector3.Dot(testAxis, Vector3.up)) +
                          boxHalfSize.z * Mathf.Abs(Vector3.Dot(testAxis, Vector3.forward));

                if (minTri > r || maxTri < -r)
                    return false;
            }
        }

        foreach (Vector3 axis in boxAxes)
        {
            float minTri, maxTri;
            ProjectTriangle(axis, triVerts, out minTri, out maxTri);
            if (maxTri < -boxHalfSize[axisToIndex(axis)] || minTri > boxHalfSize[axisToIndex(axis)])
                return false;
        }

        Vector3 normal = Vector3.Cross(v1 - v0, v2 - v0).normalized;
        float minProj, maxProj;
        ProjectTriangle(normal, triVerts, out minProj, out maxProj);

        float rNormal = boxHalfSize.x * Mathf.Abs(Vector3.Dot(normal, Vector3.right)) +
                        boxHalfSize.y * Mathf.Abs(Vector3.Dot(normal, Vector3.up)) +
                        boxHalfSize.z * Mathf.Abs(Vector3.Dot(normal, Vector3.forward));

        if (minProj > rNormal || maxProj < -rNormal)
            return false;

        return true;
    }

    void ProjectTriangle(Vector3 axis, Vector3[] verts, out float min, out float max)
    {
        min = max = Vector3.Dot(axis, verts[0]);
        for (int i = 1; i < 3; i++)
        {
            float proj = Vector3.Dot(axis, verts[i]);
            if (proj < min) min = proj;
            if (proj > max) max = proj;
        }
    }

    int axisToIndex(Vector3 axis)
    {
        if (axis == Vector3.right) return 0;
        if (axis == Vector3.up) return 1;
        return 2;
    }

    void SaveCSV(string csvText)
    {
        string folderPath = Application.persistentDataPath;
        string baseFileName = "VoxelComparisonSummary";
        string filePath = Path.Combine(folderPath, baseFileName + ".csv");

        int counter = 1;
        while (File.Exists(filePath))
        {
            filePath = Path.Combine(folderPath, $"{baseFileName}{counter}.csv");
            counter++;
        }

        try
        {
            File.WriteAllText(filePath, csvText);
            Debug.Log($"CSV saved to: {filePath}");
            Output += $"CSV saved to: {filePath}\n";
        }
        catch (IOException e)
        {
            Debug.LogError("Failed to save CSV: " + e.Message);
            Output += "Failed to save CSV.\n";
        }
    }


    public void ClearConsole()
    {
        Output = "";
        list.Clear();
    }
}
