using UnityEngine;
using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit;
using Microsoft.MixedReality.Toolkit.Utilities;

public class RaycastHierarchyInfo : MonoBehaviour
{
    public Transform rootContainer;
    public float maxRayDistance = 10f;

    [Tooltip("Material to apply temporarily to the pointed object")]
    public Material highlightMaterial;

    private GameObject lastHitObject = null;
    private Material[] lastHitOriginalMaterials = null;

    void Start()
    {
        if (rootContainer == null)
        {
            Debug.LogError("Root Container is not assigned!");
            return;
        }
        Debug.Log("Starting without colliders, using manual mesh raycast.");
    }

    void Update()
    {
        CastRayFromFingerPointer();
    }

    void CastRayFromFingerPointer()
    {
        var fingerPointer = GetFingerPointer(Handedness.Left);
        if (fingerPointer == null) fingerPointer = GetFingerPointer(Handedness.Right);

        if (fingerPointer == null)
        {
            Debug.LogWarning("Finger pointer not found.");
            ClearHighlight();
            return;
        }

        Vector3 origin = fingerPointer.Position;
        Vector3 direction = fingerPointer.Rotation * Vector3.forward;
        Ray ray = new Ray(origin, direction);

        // Find closest hit by manual ray-mesh intersection
        float closestDistance = maxRayDistance;
        GameObject closestHitObject = null;

        // Traverse all meshes under rootContainer
        foreach (MeshFilter mf in rootContainer.GetComponentsInChildren<MeshFilter>())
        {
            if (!IsMeshValid(mf.sharedMesh)) continue;

            Transform t = mf.transform;
            if (RayIntersectsMesh(ray, mf.sharedMesh, t.localToWorldMatrix, out float hitDistance))
            {
                if (hitDistance < closestDistance)
                {
                    closestDistance = hitDistance;
                    closestHitObject = t.gameObject;
                }
            }
        }

        if (closestHitObject != null)
        {
            HighlightObject(closestHitObject);

            // Log hierarchy info
            string info = $"Hit object: {closestHitObject.name}\nHierarchy (child > root):\n";
            Transform current = closestHitObject.transform;

            while (current != null)
            {
                info = "> " + current.name + "\n" + info;
                current = current.parent;
            }

            Debug.Log($"finger pointer raycast hit:\n{info}");
        }
        else
        {
            ClearHighlight();
            Debug.Log("finger pointer raycast did not hit any object.");
        }
    }

    bool IsMeshValid(Mesh mesh)
    {
        if (mesh == null) return false;
        if (mesh.vertexCount == 0 || mesh.triangles.Length == 0) return false;
        return true;
    }

    bool RayIntersectsMesh(Ray ray, Mesh mesh, Matrix4x4 localToWorld, out float distance)
    {
        distance = float.MaxValue;
        bool hit = false;

        Vector3[] vertices = mesh.vertices;
        int[] triangles = mesh.triangles;

        // Loop over every triangle in the mesh
        for (int i = 0; i < triangles.Length; i += 3)
        {
            Vector3 v0 = localToWorld.MultiplyPoint3x4(vertices[triangles[i]]);
            Vector3 v1 = localToWorld.MultiplyPoint3x4(vertices[triangles[i + 1]]);
            Vector3 v2 = localToWorld.MultiplyPoint3x4(vertices[triangles[i + 2]]);

            if (RayIntersectsTriangle(ray, v0, v1, v2, out float currentDist))
            {
                if (currentDist < distance)
                {
                    distance = currentDist;
                    hit = true;
                }
            }
        }
        return hit;
    }

    bool RayIntersectsTriangle(Ray ray, Vector3 v0, Vector3 v1, Vector3 v2, out float distance)
    {
        distance = 0f;

        Vector3 edge1 = v1 - v0;
        Vector3 edge2 = v2 - v0;
        Vector3 h = Vector3.Cross(ray.direction, edge2);
        float a = Vector3.Dot(edge1, h);

        if (Mathf.Abs(a) < 0.00001f)
            return false; // Ray is parallel to triangle.

        float f = 1.0f / a;
        Vector3 s = ray.origin - v0;
        float u = f * Vector3.Dot(s, h);

        if (u < 0.0f || u > 1.0f)
            return false;

        Vector3 q = Vector3.Cross(s, edge1);
        float v = f * Vector3.Dot(ray.direction, q);

        if (v < 0.0f || u + v > 1.0f)
            return false;

        float t = f * Vector3.Dot(edge2, q);

        if (t > 0.00001f)
        {
            distance = t;
            return true;
        }

        return false;
    }

    void HighlightObject(GameObject obj)
    {
        if (lastHitObject == obj) return; // Already highlighted.

        ClearHighlight();

        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
        if (renderers.Length > 0 && highlightMaterial != null)
        {
            lastHitOriginalMaterials = new Material[renderers.Length];
            for (int i = 0; i < renderers.Length; i++)
            {
                lastHitOriginalMaterials[i] = renderers[i].material;
                renderers[i].material = highlightMaterial;
            }
            lastHitObject = obj;
        }
    }

    void ClearHighlight()
    {
        if (lastHitObject == null) return;

        Renderer[] renderers = lastHitObject.GetComponentsInChildren<Renderer>();
        if (renderers.Length == lastHitOriginalMaterials?.Length)
        {
            for (int i = 0; i < renderers.Length; i++)
            {
                renderers[i].material = lastHitOriginalMaterials[i];
            }
        }

        lastHitObject = null;
        lastHitOriginalMaterials = null;
    }

    IMixedRealityNearPointer GetFingerPointer(Handedness handedness)
    {
        var inputSystem = CoreServices.InputSystem;
        if (inputSystem == null) return null;

        var nearPointers = inputSystem.FocusProvider.GetPointers<IMixedRealityNearPointer>();

        foreach (var pointer in nearPointers)
        {
            if (pointer is PokePointer pokePointer)
            {
                if (pokePointer.Controller != null && pokePointer.Controller.ControllerHandedness == handedness)
                {
                    return pokePointer;
                }
            }
        }

        return null;
    }
}
