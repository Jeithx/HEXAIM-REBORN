using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ElectroButtonManager : MonoBehaviour
{
    [Header("Button Settings")]
    [SerializeField] private List<ElectroButtonNew> allButtons = new List<ElectroButtonNew>();
    [SerializeField] private bool electricityStartsActive = false; // Başlangıç durumu

    private bool isElectricityActive;

    private void Start()
    {
        if (allButtons.Count == 0)
        {
            allButtons.AddRange(FindObjectsOfType<ElectroButtonNew>());
        }

        isElectricityActive = electricityStartsActive;
        Debug.Log($"Electricity system started. Active: {isElectricityActive}");
    }

    public bool IsElectricityActive()
    {
        return isElectricityActive;
    }

    public void ToggleElectricity()
    {
        isElectricityActive = !isElectricityActive;
        Debug.Log($"Electricity toggled. Now active: {isElectricityActive}");

        // Tüm ElectroGate'lere bildir
        NotifyAllGates();
    }

    void NotifyAllGates()
    {
        ElectroGateNew[] gates = FindObjectsOfType<ElectroGateNew>();

        foreach (var gate in gates)
        {
            // Button durumu değişti, gate'lerin yeniden kontrol etmesi gerekiyor
            if (gate != null)
            {
                gate.RestartChecking(); // Genel restart metodunu çağır
            }
        }
    }
}