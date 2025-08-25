using UnityEngine;
using System;
using TMPro;
using System.Globalization;

public class UiValue : MonoBehaviour
{
    private float value = 0.0f;
    public float Value
    {
        get => value;
        set
        {
            this.value = value;
            onValueChanged?.Invoke();
        }
    }

    public Action onValueChanged;

    public TextMeshPro tmp;               // Main UI display of the value
    public TextMeshProUGUI debugText;     // Optional debug text

    private TouchScreenKeyboard keyboard;
    private bool keyboardWasOpen = false;

    void Awake()
    {
        onValueChanged += RefreshText;
        RefreshText();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            AddToValue(-.1f);
        }
        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            AddToValue(.1f);
        }

        if (keyboard != null && keyboardWasOpen)
        {
            string rawText = keyboard.text;
            string sanitized = rawText.Replace(',', '.');

            if (!string.IsNullOrEmpty(sanitized))
            {
                if (float.TryParse(sanitized, NumberStyles.Float, CultureInfo.InvariantCulture, out float parsedValue))
                {
                    // Live update of display without assigning Value
                    tmp.text = parsedValue.ToString("F2");
                    debugText.text = $"Typing valid: {parsedValue}";
                }
                else
                {
                    debugText.text = $"Typing invalid: {sanitized}";
                }
            }

            if (keyboard.done)
            {
                if (float.TryParse(sanitized, NumberStyles.Float, CultureInfo.InvariantCulture, out float finalValue))
                {
                    Value = finalValue;
                    debugText.text += $"\nConfirmed value: {Value}";
                }
                else
                {
                    debugText.text += $"\nInvalid input on confirm: {sanitized}";
                }

                keyboardWasOpen = false;
                keyboard = null;
            }
        }
    }

    public void OpenKeyboard()
    {
        keyboard = TouchScreenKeyboard.Open(
            "", // Empty input to start fresh
            TouchScreenKeyboardType.DecimalPad, // Allows decimal point
            false, // autocorrection
            false, // multiline
            false, // secure
            false, // alert
            "Enter new value" // placeholder
        );

        keyboardWasOpen = true;
    }

    void RefreshText()
    {
        Debug.Log("Refreshed value: " + Value.ToString("F2"));
        tmp.text = Value.ToString("F2");
    }

    public void AddToValue(float amount)
    {
        print("Adding " + amount + " to value. Current value: " + Value);
        Value += amount;
    }
}
