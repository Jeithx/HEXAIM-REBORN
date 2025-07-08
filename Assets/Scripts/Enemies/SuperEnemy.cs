using UnityEngine;

public class SuperEnemy : MonoBehaviour, IDecoyable, IEnemy
{
    public bool IsAlive => health != null && !health.IsDead;
    public System.Action<IEnemy> OnEnemyDeath { get; set; }


    [Header("Decoy Settings")]
    [SerializeField] private bool hasHeadphones = true;
    [SerializeField] private Sprite headphoneSprite; // Kulaklıklı sprite
    public bool CanBeDecoyed => hasHeadphones && !health.IsDead;
    [Header("SuperEnemy Settings")]
    [SerializeField] private GameObject piercingBulletPrefab; // PiercingBullet prefab'ı
    [SerializeField] private Transform firePoint;
    [SerializeField] private float bulletSpeed = 5f;

    private Health health;

    public void OnDecoyStart()
    {
        Debug.Log($"{gameObject.name} decoy started");
        // Görsel efekt eklenebilir
    }

    public void OnDecoyEnd()
    {
        Debug.Log($"{gameObject.name} decoy ended");
        // Görsel efekt temizle
    }
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
            health.OnDeath += OnSuperEnemyDeath_Internal;
            health.OnRevive += OnEnemyRevive;
            health.OnTakeDamage += OnTakeDamage;
        }

        if (GameManager.Instance != null)
        {
            //Debug.Log($"Registering enemy {gameObject.name}");
            GameManager.Instance.RegisterEnemy(this);
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

    void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.UnregisterEnemy(this);
        }
        // Event listener'ı temizle
        if (health != null)
        {
            health.OnDeath -= OnSuperEnemyDeath_Internal;
            health.OnRevive -= OnEnemyRevive;
            health.OnTakeDamage -= OnTakeDamage;
        }
    }

    // Health script'i TakeDamage çağırdığında bu tetiklenir
    public void OnTakeDamage(int damage)
    {
        if (health.IsDead) return;

        // Hasar aldığında piercing mermi ateşle
        FirePiercingBullet();
    }
    void OnEnemyRevive()
    {
        Debug.Log($"{gameObject.name} revived!");
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnEnemyRevived(this);
        }
    }

    void OnSuperEnemyDeath_Internal()
    {
        //Debug.Log($"SuperEnemy {gameObject.name} died!");
        OnEnemyDeath?.Invoke(this);

    }

    void FirePiercingBullet()
    {
        if (piercingBulletPrefab == null || firePoint == null) return;

        // Mermi oluştur
        GameObject bullet = Instantiate(piercingBulletPrefab, firePoint.position, transform.rotation);

        // Owner'ı ayarla (kendine ateş etmesin)
        BaseBullet bulletScript = bullet.GetComponent<BaseBullet>();
        if (bulletScript != null)
        {
            bulletScript.SetOwner(gameObject);
        }

        // Mermi yönünü ayarla (SuperEnemy'nin baktığı yöne)
        Rigidbody2D bulletRb = bullet.GetComponent<Rigidbody2D>();
        if (bulletRb != null)
        {
            Vector2 fireDirection = transform.up; // SuperEnemy'nin baktığı yön
            bulletRb.velocity = fireDirection * bulletSpeed;
        }

        //Debug.Log($"SuperEnemy {gameObject.name} fired a piercing bullet!");
    }

}