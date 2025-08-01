using Microsoft.MixedReality.Toolkit.Experimental.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UiValue : MonoBehaviour
{
    private float value = 0.0f;
    public float Value
    {
        get => value;
        set
        {
            this.value = value;
            if (onValueChanged != null)
            {
                onValueChanged.Invoke();
            }
        }
    }

    public MRTKTMPInputField inputField;

    public Action onValueChanged;

    private void Start()
    {
        onValueChanged += RefreshText;
    }


    void RefreshText()
    {
        inputField.text = Value.ToString("F2");
    }


    public void UpdateValue()
    {
        if (inputField != null)
        {
            float.TryParse(inputField.text, out value);
            Value = value;
        }
    }

    public void AddToValue()
    {
        Value += Value * .1f;
    }

    public void RemoveFromValue()
    {
        Value -= Value * .1f;
    }
}
