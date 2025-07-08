using UnityEngine;

public class Baretta180 : Enemy
{
    [Header("Baretta180 Settings")]

    //[Header("Karakter’e takılacak Kulaklık Objesi")]
    //[SerializeField] private GameObject headphoneObject;
    [SerializeField] private Transform firePoint1; // İlk namlu için
    [SerializeField] private Transform firePoint2; // İkinci namlu için

    new void Start()
    {
        // Enemy'nin Start'ını çağır
        base.Start();


        UpdateBarettaHeadphonesVisibility();
    }

    void OnValidate()
    {
        UpdateBarettaHeadphonesVisibility();
    }

    /// <summary>
    /// Kulaklık objesini hasHeadphones’a göre açar/kapatır.
    /// </summary>
    private void UpdateBarettaHeadphonesVisibility()
    {
        if (headphoneObject == null) return;
        headphoneObject.SetActive(!hasHeadphones);
    }


    // Enemy'nin FireBullet metodunu override et
    protected override void FireBullet()
    {
        if (enemyBulletPrefab == null || firePoint1 == null || firePoint2 == null) return;

        // İlk mermi: Sol namludan, baktığı yön (0°)
        FireBulletFromPoint(firePoint1, 0f);

        // İkinci mermi: Sağ namludan, baktığı yön + 180°
        FireBulletFromPoint(firePoint2, 180f);

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