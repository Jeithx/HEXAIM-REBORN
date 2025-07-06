using UnityEngine;

public class Baretta120 : Enemy
{
    [Header("Baretta120 Settings")]

    [SerializeField] private Sprite barettaHeadphoneSprite; // 2 namlulu + headphone sprite
    [SerializeField] private Transform firePoint1; // İlk namlu için
    [SerializeField] private Transform firePoint2; // İkinci namlu için

    new void Start()
    {
        // Enemy'nin Start'ını çağır
        base.Start();


        // Baretta sprite'ını ayarla
        UpdateBarettaSprite();
    }

    void OnValidate()
    {
        if (Application.isPlaying)
        {
            UpdateBarettaSprite();
        }
    }

    void UpdateBarettaSprite()
    {
        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            // Headphone durumuna göre sprite seç
            if (hasHeadphones && barettaHeadphoneSprite != null)
            {
                spriteRenderer.sprite = barettaHeadphoneSprite;
            }

        }
    }

    // Enemy'nin FireBullet metodunu override et
    protected override void FireBullet()
    {
        if (enemyBulletPrefab == null || firePoint1 == null || firePoint2 == null) return;

        // İlk mermi: Sol namludan, baktığı yön (0°)
        FireBulletFromPoint(firePoint1, 0f);

        // İkinci mermi: Sağ namludan, baktığı yön + 120°
        FireBulletFromPoint(firePoint2, 120f);

    }

    void FireBulletFromPoint(Transform shootPoint, float angleOffset)
    {
        // Mermi oluştur (belirtilen fire point'ten)
        GameObject bullet = Instantiate(enemyBulletPrefab, shootPoint.position, transform.rotation);
        // Owner ayarla
        EnemyBullet enemyBullet = bullet.GetComponent<EnemyBullet>();
        if (enemyBullet != null)
        {
            enemyBullet.SetOwner(gameObject);
        }

        // Yön hesapla: Transform'un rotation'ı + offset
        float currentAngle = transform.eulerAngles.z;
        float targetAngle = currentAngle + angleOffset;

        Vector2 fireDirection = new Vector2(
            Mathf.Sin(targetAngle * Mathf.Deg2Rad),
            Mathf.Cos(targetAngle * Mathf.Deg2Rad)
        );

        // Mermi hızını ayarla
        Rigidbody2D bulletRb = bullet.GetComponent<Rigidbody2D>();
        if (bulletRb != null)
        {
            bulletRb.velocity = fireDirection * bulletSpeed;
        }
    }
}