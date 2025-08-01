using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tab : MonoBehaviour
{
    public GameObject panel;
    public bool isSelected;

    public MeshRenderer backPanel;

    public Material selectedMaterial;
    Material defaultMaterial;

    public bool IsSelected
    {
        get => isSelected;
        set
        {
            isSelected = value;
            backPanel.material = isSelected ? selectedMaterial : defaultMaterial;
            panel.SetActive(isSelected);
        }
    }

    private void Start()
    {
        defaultMaterial = backPanel.material;
        IsSelected = isSelected;
    }

    public void ActivateTab()
    {
        if (IsSelected) return;

        Debug.Log($"Activating tab: {name}");

        foreach (Transform t in transform.parent)
        {
            if (!t.TryGetComponent<Tab>(out Tab currentTab)) continue;
            currentTab.IsSelected = currentTab == this;
        }
    }
}
