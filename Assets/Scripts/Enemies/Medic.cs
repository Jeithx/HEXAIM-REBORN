using UnityEngine;

public class Medic : MonoBehaviour, IDecoyable,IEnemy
{
    public bool IsAlive => health != null && !health.IsDead;
    public System.Action<IEnemy> OnEnemyDeath { get; set; }

    [Header("Decoy Settings")]
    [SerializeField] private bool hasHeadphones = true;
    [Header("Karakter’e takılacak Kulaklık Objesi")]
    [SerializeField] private GameObject headphoneObject;
    public bool CanBeDecoyed => hasHeadphones && !health.IsDead; // Decoy olabilmesi için hasHeadphones ve IsDead kontrolü

    public void OnDecoyStart() { Debug.Log($"{gameObject.name} decoy started"); }
    public void OnDecoyEnd() { Debug.Log($"{gameObject.name} decoy ended"); }

    [Header("Medic Settings")]
    [SerializeField] private GameObject medicBulletPrefab; // MedicBullet prefab'ı
    [SerializeField] private Transform firePoint;
    [SerializeField] private float bulletSpeed = 5f;

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
    /// <summary>
    /// Kulaklık objesini hasHeadphones’a göre açar/kapatır.
    /// </summary>
    private void UpdateHeadphonesVisibility()
    {
        if (headphoneObject == null) return;
        headphoneObject.SetActive(!hasHeadphones);
    }
    void OnValidate()
    {
        UpdateHeadphonesVisibility();
    }
    void Start()
    {
        UpdateHeadphonesVisibility();

        // Health event'lerini dinle
        if (health != null)
        {
            health.OnDeath += OnMedicDeath_Internal;
            health.OnRevive += OnEnemyRevive;
            health.OnTakeDamage += OnTakeDamage;
        }
        // GameManager'a kaydol
        if (GameManager.Instance != null)
        {
            //Debug.Log($"Registering enemy {gameObject.name}"); 
            GameManager.Instance.RegisterEnemy(this);
        }

    }

    void OnDestroy()
    {
        // Event listener'ı temizle
        if (health != null)
        {
            health.OnDeath -= OnMedicDeath_Internal;
            health.OnRevive -= OnEnemyRevive;
            health.OnTakeDamage -= OnTakeDamage;
        }

        // GameManager'dan çıkar    
        if (GameManager.Instance != null)
        {
            GameManager.Instance.UnregisterEnemy(this);
        }
    }
    void OnEnemyRevive()
    {
        Debug.Log($"{gameObject.name} revived!");
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnEnemyRevived(this);
        }
    }
    // Health script'i TakeDamage çağırdığında bu tetiklenir
    public void OnTakeDamage(int damage)
    {
        if (health.IsDead) return;

        // Hasar aldığında iyileştirme mermisi ateşle
        FireHealingBullet();
    }

    void OnMedicDeath_Internal()
    {
        Debug.Log($"Medic {gameObject.name} died!");
        OnEnemyDeath?.Invoke(this);
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