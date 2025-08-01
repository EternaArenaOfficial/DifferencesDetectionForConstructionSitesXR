using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeeksManager : MonoBehaviour
{
    public GameObject referenceContainer;

    public VerticalContent verticalContent;
    public WeekPanel weekCard;

    public void Update()
    {
        if (Input.GetKeyDown(KeyCode.W))
        {
            AddWeeks();
        }
    }

    public void AddWeeks()
    {
        verticalContent.Clear();

        foreach (Transform t in referenceContainer.transform.GetChild(0))
        {
            WeekPanel weekCardInstance = Instantiate(weekCard);
            weekCardInstance.weekObject = t.gameObject;
            weekCardInstance.label.text = t.gameObject.name;

            verticalContent.AddElement(weekCardInstance.gameObject);
        }
    }
}
