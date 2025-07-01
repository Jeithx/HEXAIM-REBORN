using UnityEngine;

public class Medic : MonoBehaviour, IDecoyable
{
    [Header("Decoy Settings")]
    [SerializeField] private bool hasHeadphones = true; // Medic kulaklık kullanıyor
    [SerializeField] private Sprite headphoneSprite; // Kulaklıklı sprite
    public bool CanBeDecoyed => hasHeadphones && !health.IsDead; // Decoy olabilmesi için hasHeadphones ve IsDead kontrolü

    public void OnDecoyStart() { Debug.Log($"{gameObject.name} decoy started"); }
    public void OnDecoyEnd() { Debug.Log($"{gameObject.name} decoy ended"); }

    [Header("Medic Settings")]
    [SerializeField] private GameObject medicBulletPrefab; // MedicBullet prefab'ı
    [SerializeField] private Transform firePoint;
    [SerializeField] private float bulletSpeed = 5f;

    [Header("Visual Settings")]
    [SerializeField] private Color medicColor = Color.green;

    private Health health;

    void Awake()
    {
        health = GetComponent<Health>();

        // Health yoksa ekle
        if (health == null)
        {
            health = gameObject.AddComponent<Health>();
        }
    }
    void UpdateSprite()
    {
        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            if (hasHeadphones && headphoneSprite != null)
            {
                spriteRenderer.sprite = headphoneSprite;
            }
        }
    }
    void OnValidate()
    {
        if (Application.isPlaying)
        {
            UpdateSprite();
        }
    }
    void Start()
    {
        UpdateSprite();
        // Health event'lerini dinle
        if (health != null)
        {
            health.OnDeath += OnMedicDeath;
            health.OnTakeDamage += OnTakeDamage;
        }

    }

    void OnDestroy()
    {
        // Event listener'ı temizle
        if (health != null)
        {
            health.OnDeath -= OnMedicDeath;
            health.OnTakeDamage -= OnTakeDamage;
        }
    }

    // Health script'i TakeDamage çağırdığında bu tetiklenir
    public void OnTakeDamage(int damage)
    {
        if (health.IsDead) return;

        // Hasar aldığında iyileştirme mermisi ateşle
        FireHealingBullet();
    }

    void OnMedicDeath()
    {
        Debug.Log($"Medic {gameObject.name} died!");
    }

    void FireHealingBullet()
    {
        if (medicBulletPrefab == null || firePoint == null) return;

        // Mermi oluştur
        GameObject bullet = Instantiate(medicBulletPrefab, firePoint.position, transform.rotation);

        // Owner'ı ayarla (kendine iyileştirme yapmasın)
        BaseBullet bulletScript = bullet.GetComponent<BaseBullet>();
        if (bulletScript != null)
        {
            bulletScript.SetOwner(gameObject);
        }

        // Mermi yönünü ayarla (Medic'in baktığı yöne)
        Rigidbody2D bulletRb = bullet.GetComponent<Rigidbody2D>();
        if (bulletRb != null)
        {
            Vector2 fireDirection = transform.up; // Medic'in baktığı yön
            bulletRb.velocity = fireDirection * bulletSpeed;
        }

        Debug.Log($"Medic {gameObject.name} fired a healing bullet!");
    }

}