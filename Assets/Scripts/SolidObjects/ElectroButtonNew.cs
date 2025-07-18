using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ElectroButtonNew : MonoBehaviour
{
    public bool isActive = false;

    // Start is called before the first frame update
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponent<BaseBullet>() != null)
        {
            Debug.Log($"Button hit by bullet from {other.name}");
            isActive = !isActive; // Toggle state on bullet hit
        }
    }
}
