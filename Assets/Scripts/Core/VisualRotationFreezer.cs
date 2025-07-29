using UnityEngine;

/// <summary>
/// Parent ne kadar dönerse dönsün, bu objenin dünya Z açısını sabit tutar.
/// </summary>
[DisallowMultipleComponent]
public class LockWorldZ : MonoBehaviour
{
    float initialWorldZ;          // sahnedeki başlangıç açısı


    void Awake()
    {
        initialWorldZ = transform.eulerAngles.z;
    }


    void LateUpdate()             // parent'lar döndükten sonra çalışır
    {
        if (transform.parent == null) return;

        float parentZ = transform.parent.eulerAngles.z;
        float desiredZ = initialWorldZ;          // istersen sabit 0f yapabilirsin
        float localZ = desiredZ - parentZ;     // world = parent + local

        transform.localRotation = Quaternion.Euler(0f, 0f, localZ);
    }
}
