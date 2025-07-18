// ElectroButtonNew.cs (Güncellenen)
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ElectroButtonNew : MonoBehaviour
{
    public bool isActive = false;
    private ElectroButtonManager manager;

    private void Start()
    {
        manager = FindObjectOfType<ElectroButtonManager>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponent<BaseBullet>() != null)
        {
            Debug.Log($"Button hit by bullet");
            isActive = !isActive; // Toggle button state
            Destroy(other.gameObject); // Mermi yok et
            // Tüm sistemi toggle et
            if (manager != null)
            {
                manager.ToggleElectricity();
            }
        }
    }
}