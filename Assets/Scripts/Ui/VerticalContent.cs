using Microsoft.MixedReality.Toolkit.UI;
using System;
using System.Collections.Generic;
using UnityEngine;

public enum SortMode
{
    Hierarchy,
    Name,
    Value
}

public class VerticalContent : MonoBehaviour
{
    public float spacing;
    public float containerHeight;

    [Range(0f, 1f)]
    public float scrollValue = 0f;

    public Action onElementAdded;
    public Transform container;

    private float totalContentHeight = 0f;

    public SortMode sortMode = SortMode.Hierarchy;
    public PinchSlider pinchSlider; // PinchSlider for scroll control

    private bool isUpdatingFromSlider = false;

    void Start()
    {
        onElementAdded += OrderContent;

        if (pinchSlider != null)
        {
            pinchSlider.OnValueUpdated.AddListener(OnSliderValueUpdated);
        }
        else
        {
            Debug.LogWarning("PinchSlider reference is not assigned.");
        }
    }

    private void OnSliderValueUpdated(SliderEventData eventData)
    {
        isUpdatingFromSlider = true;
        SetScrollValue(eventData.NewValue);
        isUpdatingFromSlider = false;
    }

    private void Update()
    {
        UpdateScrollOffset();

        // Hide elements outside visible area
        foreach (Transform child in container)
        {
            if (container.parent != null)
            {
                Vector3 localPos = container.parent.InverseTransformPoint(child.position);
                child.gameObject.SetActive(Mathf.Abs(localPos.y) < containerHeight);
            }
        }
    }

    void OrderContent()
    {
        bool wasAtBottom = Mathf.Approximately(scrollValue, 1f);

        // Collect and optionally sort children
        List<Transform> children = new List<Transform>();
        foreach (Transform child in container)
        {
            if (child.TryGetComponent<ListElementUi>(out _))
            {
                children.Add(child);
            }
        }

        switch (sortMode)
        {
            case SortMode.Name:
                children.Sort((a, b) => string.Compare(
                    a.GetComponent<ListElementUi>().label.text,
                    b.GetComponent<ListElementUi>().label.text,
                    StringComparison.OrdinalIgnoreCase));
                break;

            case SortMode.Value:
                children.Sort((a, b) =>
                {
                    var aText = a.GetComponent<ListElementUi>().value.text;
                    var bText = b.GetComponent<ListElementUi>().value.text;

                    float.TryParse(aText, out float aVal);
                    float.TryParse(bText, out float bVal);

                    return aVal.CompareTo(bVal);
                });
                break;

            case SortMode.Hierarchy:
            default:
                // Keep original order
                break;
        }

        // Apply new sibling index
        for (int i = 0; i < children.Count; i++)
        {
            children[i].SetSiblingIndex(i);
        }

        // Layout elements top-down
        totalContentHeight = 0f;
        float y = 0f;

        foreach (Transform child in container)
        {
            if (child.TryGetComponent<ListElementUi>(out var element))
            {
                child.localPosition = new Vector3(0, -y, 0);
                y += element.height + spacing;
                totalContentHeight += element.height + spacing;
            }
        }

        if (wasAtBottom)
        {
            scrollValue = 1f;
        }

        UpdateScrollOffset();
    }

    public void AddElement(GameObject element)
    {
        element.transform.SetParent(container, false);
        onElementAdded?.Invoke();
    }

    public void SetScrollValue(float value)
    {
        scrollValue = Mathf.Clamp01(value);
        UpdateScrollOffset();

        if (!isUpdatingFromSlider && pinchSlider != null && !Mathf.Approximately(pinchSlider.SliderValue, scrollValue))
        {
            pinchSlider.SliderValue = scrollValue;
        }
    }

    public void AddToScrollValue(float value)
    {
        SetScrollValue(scrollValue + value);
    }

    private void UpdateScrollOffset()
    {
        float scrollRange = Mathf.Max(0f, totalContentHeight - containerHeight);
        container.localPosition = new Vector3(0, scrollValue * scrollRange, 0);
    }

    public void SetSortMode(SortMode mode)
    {
        sortMode = mode;
        OrderContent();
    }

    public void SetNextSortMode()
    {
        SetSortMode((SortMode)(((int)sortMode + 1) % Enum.GetValues(typeof(SortMode)).Length));
        Debug.Log($"Sort mode set to: {sortMode}");
    }

    public void Clear()
    {
        foreach (Transform child in container)
        {
            Destroy(child.gameObject);
        }
    }
}
