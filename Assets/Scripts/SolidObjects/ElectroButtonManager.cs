using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ElectroButtonManager: MonoBehaviour
{

    [Header("Button Settings")]
    [SerializeField] private List<ElectroButtonNew> allButtons = new List<ElectroButtonNew>();


    private void Start()
    {
        if (allButtons.Count == 0)
        {
            allButtons.AddRange(FindObjectsOfType<ElectroButtonNew>());
        }
    }


    public bool ShouldCheckConnections()
    {
        return AreAllButtonsAreActive();
    }

    private bool AreAllButtonsAreActive()
    {
        foreach (ElectroButtonNew button in allButtons)
        {
            if (button.isActive)
            {
                Debug.Log("buttons are active");
                return true;
            }
        }
        Debug.Log("All buttons are inactive");
        return false;

    }
}
