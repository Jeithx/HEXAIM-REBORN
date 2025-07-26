using UnityEngine;

/// <summary>
/// Parent ne kadar dönerse dönsün, bu objenin dünya Z açısını sabit tutar.
/// </summary>
[DisallowMultipleComponent]
public class LockWorldZ : MonoBehaviour
{
    float initialWorldZ;          // sahnedeki başlangıç açısı
    private SpriteRenderer spriteRenderer; // 2D sprite renderer için

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        initialWorldZ = transform.eulerAngles.z;
    }

    private void Update()
    {
        SpriteRenderer sr = GetComponentInParent<SpriteRenderer>();
        Color color = sr.color;
        color.a = spriteRenderer.color.a;

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
