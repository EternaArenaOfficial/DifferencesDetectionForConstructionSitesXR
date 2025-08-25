using Microsoft.MixedReality.Toolkit;
using Microsoft.MixedReality.Toolkit.SpatialAwareness;
using System.Collections.Generic;
using UnityEngine;

public class WorldMesh : MonoBehaviour
{
    private void Update()
    {
        if(Input.GetKeyDown(KeyCode.M))
        {
            print("Toggling Mesh Visibility");
            ToggleAllMeshes();
        }
    }

    public void SetAllMeshes(SpatialAwarenessMeshDisplayOptions option)
    {
        var observers = CoreServices.SpatialAwarenessSystem
            .GetObservers<IMixedRealitySpatialAwarenessMeshObserver>();

        foreach (var observer in observers)
        {
            observer.DisplayOption = option;
            Debug.Log($"Set mesh observer {observer.Name} to {option}");
        }
    }

    public void ToggleAllMeshes()
    {
        var observers = CoreServices.SpatialAwarenessSystem
            .GetObservers<IMixedRealitySpatialAwarenessMeshObserver>();

        foreach (var observer in observers)
        {
            if (observer.DisplayOption == SpatialAwarenessMeshDisplayOptions.Visible)
            {
                observer.DisplayOption = SpatialAwarenessMeshDisplayOptions.None;
            }
            else
            {
                observer.DisplayOption = SpatialAwarenessMeshDisplayOptions.Visible;
            }

            Debug.Log($"Toggled mesh observer {observer.Name} to {observer.DisplayOption}");
        }
    }
}
