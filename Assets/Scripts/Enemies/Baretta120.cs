using UnityEngine;

public class Baretta120 : Enemy
{
    [Header("Baretta120 Settings")]


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

        // İkinci mermi: Sağ namludan, baktığı yön + 120°
        FireBulletFromPoint(firePoint2, 240f);

    }

    void FireBulletFromPoint(Transform shootPoint, float angleOffset)
    {
        // 1. Yönü Hesapla (Yeni ve Güvenilir Yöntem)
        // Karakterin "ileri" yönünü al (Sprite'ınızın üst kısmı ileri bakıyorsa transform.up doğrudur).
        Vector2 baseDirection = transform.up;

        // Belirtilen açı kadar bir dönüş (rotasyon) oluştur.
        Quaternion rotation = Quaternion.Euler(0, 0, angleOffset);

        // Temel yönü oluşturduğun rotasyon ile döndürerek nihai yönü bul.
        Vector2 finalDirection = rotation * baseDirection;

        // 2. Mermiyi Oluştur
        // Merminin başlangıç rotasyonu önemsiz çünkü hızını doğrudan biz ayarlayacağız.
        GameObject bullet = Instantiate(enemyBulletPrefab, shootPoint.position, Quaternion.identity);

        EnemyBullet enemyBullet = bullet.GetComponent<EnemyBullet>();
        if (enemyBullet != null)
        {
            enemyBullet.SetOwner(gameObject);
        }

        // 3. Mermiye Hız Ver
        Rigidbody2D bulletRb = bullet.GetComponent<Rigidbody2D>();
        if (bulletRb != null)
        {
            // Hesaplanan yönde mermiyi fırlat.
            bulletRb.velocity = finalDirection.normalized * bulletSpeed;
        }
    }
}