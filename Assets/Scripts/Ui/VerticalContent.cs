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


    void Start()
    {
        onElementAdded += OrderContent;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.UpArrow)) SetScrollValue(scrollValue + 0.05f);
        if (Input.GetKeyDown(KeyCode.DownArrow)) SetScrollValue(scrollValue - 0.05f);
        if (Input.GetKeyDown(KeyCode.LeftArrow)) SetNextSortMode();
        UpdateScrollOffset();

        if (Input.GetKeyDown(KeyCode.P))
        {
            onElementAdded.Invoke();
        }

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
                // Do nothing — keep original order from the hierarchy
                break;
        }

        // Apply new sibling index based on sorted list
        for (int i = 0; i < children.Count; i++)
        {
            children[i].SetSiblingIndex(i);
        }

        // Layout elements
        totalContentHeight = 0f;
        float y = 0f;

        foreach (Transform child in container)
        {
            if (child.TryGetComponent<ListElementUi>(out var element))
            {
                child.localPosition = new Vector3(0, -y, 0); // Lay out from top down
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
        print($"Sort mode set to: {sortMode}");
    }

    public void Clear()
    {
        foreach (Transform child in container)
        {
            Destroy(child.gameObject);
        }
    }
}
