using UnityEngine;

public class Enemy : MonoBehaviour, IDecoyable
{
    [Header("Decoy Settings")]
    [SerializeField] public bool hasHeadphones = true; // Enemy kulaklık kullanıyor
    [SerializeField] private Sprite headphoneSprite; // Kulaklıklı sprite
    public bool CanBeDecoyed
    {
        get
        {
            bool canDecoy = hasHeadphones && !health.IsDead;
            Debug.Log($"{gameObject.name} CanBeDecoyed: hasHeadphones={hasHeadphones}, isDead={health.IsDead}, result={canDecoy}");
            return canDecoy;
        }
    }
    public void OnDecoyStart() { Debug.Log($"{gameObject.name} decoy started"); }
    public void OnDecoyEnd() { Debug.Log($"{gameObject.name} decoy ended"); }
    void OnValidate()
    {
        if (Application.isPlaying)
        {
            UpdateSprite();
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
    [Header("Enemy Settings")]
    [SerializeField] private GameObject enemyBulletPrefab; // EnemyBullet prefab'ı
    [SerializeField] private Transform firePoint;
    [SerializeField] private float bulletSpeed = 5f;

    [Header("Visual Settings")]
    [SerializeField] private Color enemyColor = Color.red;

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

    void Start()
    {
        UpdateSprite();
        // Health event'lerini dinle
        if (health != null)
        {
            health.OnDeath += OnEnemyDeath;
            health.OnTakeDamage += OnTakeDamage;
        }

        // Fire point yoksa oluştur
        if (firePoint == null)
        {
            GameObject firePointObj = new GameObject("EnemyFirePoint");
            firePointObj.transform.SetParent(transform);
            firePointObj.transform.localPosition = Vector3.up * 0.3f;
            firePoint = firePointObj.transform;
        }
    }

    void OnDestroy()
    {
        // Event listener'ı temizle
        if (health != null)
        {
            health.OnDeath -= OnEnemyDeath;
            health.OnTakeDamage -= OnTakeDamage;
        }
    }

    // Health script'i TakeDamage çağırdığında bu tetiklenir
    public void OnTakeDamage(int damage)
    {
        if (health.IsDead) return;

        // Hasar aldığında ateş et
        FireBullet();
    }

    void OnEnemyDeath()
    {
        Debug.Log($"Enemy {gameObject.name} died!");
    }

    void FireBullet()
    {
        if (enemyBulletPrefab == null || firePoint == null) return;

        // Mermi oluştur
        GameObject bullet = Instantiate(enemyBulletPrefab, firePoint.position, transform.rotation);

        // Merminin sahibini ayarla
        EnemyBullet enemyBullet = bullet.GetComponent<EnemyBullet>();
        if (enemyBullet != null)
        {
            enemyBullet.SetOwner(gameObject);
        }

        // Mermi yönünü ayarla (Enemy'nin baktığı yöne)
        Rigidbody2D bulletRb = bullet.GetComponent<Rigidbody2D>();
        if (bulletRb != null)
        {
            Vector2 fireDirection = transform.up; // Enemy'nin baktığı yön
            bulletRb.velocity = fireDirection * bulletSpeed;
        }

        Debug.Log($"Enemy {gameObject.name} fired a bullet!");
    }

}