using System.Collections.Generic;
using UnityEngine;

public enum CurrentState
{
    Active,
    ActiveUpToThis,
}

public class WeekPanel : ListElementUi
{
    [Header("Configuration")]
    public GameObject weekObject;
    public MeshRenderer backPlate;
    public Material completedMaterial;
    public Material offMaterial;

    [Header("State")]
    public bool isSelected = true;
    private bool isCompleted = false;

    private Material defaultMaterial;
    private static bool isFiltered = false;

    CurrentState currentState = CurrentState.Active;

    // Store original materials of each renderer in weekObject
    private Dictionary<Renderer, Material[]> originalMaterials = new Dictionary<Renderer, Material[]>();
    private string prefsKey => $"WeekPanel_Completed_{weekObject.name}";

    private void Start()
    {
        if (backPlate != null)
        {
            defaultMaterial = backPlate.material;
        }

        bool wasInactive = false;
        if (weekObject != null && !weekObject.activeSelf)
        {
            wasInactive = true;
            weekObject.SetActive(true);
        }

        CacheOriginalMaterials();

        if (wasInactive && weekObject != null)
        {
            weekObject.SetActive(false);
        }

        SetWeekActive(true);

        isCompleted = PlayerPrefs.GetInt(prefsKey, 0) == 1;
        CompleteWeek(isCompleted);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.J) && transform.GetSiblingIndex() == 0) ToggleCompleteWeek();
    }
    public void ToggleWeekFilter()
    {
        isFiltered = !isFiltered;

        Transform parent = transform.parent;
        if (parent == null) return;

        foreach (Transform child in parent)
        {
            if (child.TryGetComponent<WeekPanel>(out var panel))
            {
                bool shouldBeActive = !isFiltered || (panel == this);
                panel.SetWeekActive(shouldBeActive);
            }
        }

        Debug.Log(isFiltered
            ? $"[WeekPanel] Filter ON — Showing only: {weekObject.name}"
            : "[WeekPanel] Filter OFF — Showing all weeks");
    }

    public void SetWeekActive(bool active)
    {
        isSelected = active;

        if (weekObject != null)
        {
            weekObject.SetActive(active);
        }
    }

    public void ToggleCompleteWeek()
    {
        CompleteWeek(!isCompleted);
    }

    public void CompleteWeek(bool complete)
    {
        isCompleted = complete;

        if (backPlate != null && completedMaterial != null)
        {
            backPlate.material = isCompleted ? completedMaterial : defaultMaterial;
        }

        ApplyWeekObjectVisualState();

        PlayerPrefs.SetInt(prefsKey, isCompleted ? 1 : 0);
        PlayerPrefs.Save();
    }

    private void ApplyWeekObjectVisualState()
    {
        if (weekObject == null || offMaterial == null) return;

        var renderers = weekObject.GetComponentsInChildren<Renderer>(true);

        Debug.Log($"[WeekPanel] Updating {renderers.Length} renderers for {weekObject.name}");

        foreach (Renderer renderer in renderers)
        {
            if (isCompleted)
            {
                if (originalMaterials.TryGetValue(renderer, out Material[] originals))
                {
                    Material[] offMaterials = new Material[originals.Length];
                    for (int i = 0; i < offMaterials.Length; i++)
                    {
                        offMaterials[i] = offMaterial;
                    }
                    renderer.materials = offMaterials;
                }
            }
            else
            {
                if (originalMaterials.TryGetValue(renderer, out Material[] originals))
                {
                    renderer.materials = originals;
                }
            }
        }
    }

    public void ChangeState()
    {
        switch (currentState)
        {
            case CurrentState.ActiveUpToThis:
                FocusThisWeek();
                currentState = CurrentState.Active;
                break;
            default:
                SetWeekRangeUpToThis();
                currentState = CurrentState.ActiveUpToThis;
                break;
        }
    }

    public void SetWeekRangeUpToThis()
    {
        Transform parent = transform.parent;
        if (parent == null) return;

        bool reachedThis = false;

        foreach (Transform child in parent)
        {
            if (child.TryGetComponent<WeekPanel>(out var panel))
            {
                bool shouldBeActive = !reachedThis;
                panel.SetWeekActive(shouldBeActive);

                if (panel == this)
                {
                    panel.SetWeekActive(true); // ensure this one is active
                    reachedThis = true;
                }
            }
        }

        Debug.Log($"[WeekPanel] Showing weeks up to: {weekObject.name}");
    }

    public void FocusThisWeek()
    {
        Transform parent = transform.parent;
        if (parent == null) return;

        foreach (Transform child in parent)
        {
            if (child.TryGetComponent<WeekPanel>(out var panel))
            {
                panel.SetWeekActive(panel == this);
            }
        }
    }

    private void CacheOriginalMaterials()
    {
        if (weekObject == null) return;

        var renderers = weekObject.GetComponentsInChildren<Renderer>(true);

        foreach (Renderer renderer in renderers)
        {
            Material[] originals = renderer.materials;
            Material[] copy = new Material[originals.Length];
            for (int i = 0; i < originals.Length; i++)
            {
                copy[i] = originals[i];
            }

            originalMaterials[renderer] = copy;
        }

        Debug.Log($"[WeekPanel] Cached {originalMaterials.Count} renderers for {weekObject.name}");
    }
}
