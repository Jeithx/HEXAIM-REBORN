using UnityEngine;

public class EnemyBullet : BaseBullet
{
    [Header("Enemy Bullet Settings")]
    [SerializeField] private Color bulletColor = Color.red;


    void Start()
    {
        // Sadece görsel farklılık
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.color = bulletColor;
        }
    }

    // Override yok! Base davranışı kullan
}