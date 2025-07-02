using UnityEngine;

public class Enemy : MonoBehaviour, IDecoyable, IEnemy
{
    // IEnemy implementation
    public bool IsAlive => health != null && !health.IsDead;
    public System.Action<IEnemy> OnEnemyDeath { get; set; }

    [Header("Decoy Settings")]
    [SerializeField] public bool hasHeadphones = true;
    [SerializeField] private Sprite headphoneSprite;

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

    [Header("Enemy Settings")]
    [SerializeField] private GameObject enemyBulletPrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField] private float bulletSpeed = 5f;

    [Header("Visual Settings")]
    [SerializeField] private Color enemyColor = Color.red;

    private Health health;

    void Start()
    {
        UpdateSprite();

        // GameManager'a kaydol
        if (GameManager.Instance != null)
        {
            Debug.Log($"Registering enemy {gameObject.name}");
            GameManager.Instance.RegisterEnemy(this);
        }

        // Health event'lerini dinle
        if (health != null)
        {
            health.OnDeath += OnEnemyDeath_Internal;
            health.OnRevive += OnEnemyRevive; // EKLENEN!
            health.OnTakeDamage += OnTakeDamage;
        }

    }
    void Awake()
    {
        health = GetComponent<Health>();
        if (health == null)
        {
            health = gameObject.AddComponent<Health>();
        }
    }
    void OnDestroy()
    {
        // GameManager'dan çıkar
        if (GameManager.Instance != null)
        {
            GameManager.Instance.UnregisterEnemy(this);
        }

        // Event listener'ları temizle
        if (health != null)
        {
            health.OnDeath -= OnEnemyDeath_Internal;
            health.OnRevive -= OnEnemyRevive; // EKLENEN!
            health.OnTakeDamage -= OnTakeDamage;
        }
    }
    void OnEnemyRevive()
    {
        Debug.Log($"Enemy {gameObject.name} revived!");

        // GameManager'a tekrar kaydol
        if (GameManager.Instance != null)
        {
            GameManager.Instance.RegisterEnemy(this);
        }
    }

    void OnEnemyDeath_Internal()
    {
        Debug.Log($"Enemy {gameObject.name} died!");
        OnEnemyDeath?.Invoke(this); // GameManager'a bildir
    }

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

    public void OnTakeDamage(int damage)
    {
        if (health.IsDead) return;

        // Hasar aldığında ateş et
        FireBullet();
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

        // Mermi yönünü ayarla
        Rigidbody2D bulletRb = bullet.GetComponent<Rigidbody2D>();
        if (bulletRb != null)
        {
            Vector2 fireDirection = transform.up;
            bulletRb.velocity = fireDirection * bulletSpeed;
        }

        Debug.Log($"Enemy {gameObject.name} fired a bullet!");
    }
}