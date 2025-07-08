using UnityEngine;

public class Hunter : MonoBehaviour, IDecoyable, IEnemy

{
    public bool IsAlive => health != null && !health.IsDead;
    public System.Action<IEnemy> OnEnemyDeath { get; set; }

    [Header("Hunter Settings")]
    [SerializeField] private GameObject hunterBulletPrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField] private float bulletSpeed = 10f;
    [SerializeField] private float sightCheckInterval = 0.1f;

    [Header("Line of Sight")]
    [SerializeField]
    private LayerMask obstacleLayerMask;

    private Health health;
    private bool hasLineOfSight = false;
    private bool hasFired = false;

    void Awake()
    {
        int excludeBits = (1 << LayerMask.NameToLayer("DeadCharacters")) |
                      (1 << LayerMask.NameToLayer("Hexes")) |
                      (1 << LayerMask.NameToLayer("Hunter"));
        obstacleLayerMask &= ~excludeBits;


        health = GetComponent<Health>();

        if (health == null)
        {
            health = gameObject.AddComponent<Health>();
        }
    }

    [Header("Decoy Settings")]
    [SerializeField] private bool hasHeadphones = true;

    public bool CanBeDecoyed => hasHeadphones && !health.IsDead;

    public void OnDecoyStart() { Debug.Log($"{gameObject.name} decoy started"); }
    public void OnDecoyEnd() { Debug.Log($"{gameObject.name} decoy ended"); }
    void Start()
    {
        if (health != null)
        {
            health.OnDeath += OnHunterDeath_Internal;
            health.OnRevive += OnHunterRevive;
        }

        if (GameManager.Instance != null)
        {
            //Debug.Log($"Registering enemy {gameObject.name}");
            GameManager.Instance.RegisterEnemy(this);
        }


        // Seviye başında player'a dön
        LookAtPlayer();

        // Görüş hattı kontrolünü başlat
        InvokeRepeating(nameof(CheckLineOfSight), 0f, sightCheckInterval);
    }

    void OnDestroy()
    {
        // GameManager'dan çıkar
        if (GameManager.Instance != null)
        {
            GameManager.Instance.UnregisterEnemy(this);
        }

        if (health != null)
        {
            health.OnDeath -= OnHunterDeath_Internal;
            health.OnRevive -= OnHunterRevive;
        }

        CancelInvoke();
    }



    void LookAtPlayer()
    {
        // Her seferinde player'ı bul (pozisyon değişebilir)
        PlayerController player = FindObjectOfType<PlayerController>();
        if (player == null) return;

        Vector3 direction = (player.transform.position - transform.position).normalized;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;
        transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);

        //Debug.Log($"Hunter rotated to face player at {player.transform.position}");
    }

    void CheckLineOfSight()
    {
        if (health.IsDead || hasFired) return;

        // Her kontrolde player'ı bul (pozisyon değişebilir)
        PlayerController player = FindObjectOfType<PlayerController>();
        if (player == null) return;

        Vector3 playerPos = player.transform.position;
        Vector3 directionToPlayer = (playerPos - transform.position).normalized;
        float distanceToPlayer = Vector3.Distance(transform.position, playerPos);

        //Debug.Log($"Player layer: {player.gameObject.layer}, LayerMask: {obstacleLayerMask.value}");

        // Raycast ile görüş hattı kontrolü - RANGE YOK, tüm harita
        RaycastHit2D hit = Physics2D.Raycast(
            transform.position,
            directionToPlayer,
            distanceToPlayer,
            obstacleLayerMask
        );

        // DEBUG - Ray sonucu
        if (hit.collider != null)
        {
            //Debug.Log($"Ray hit: {hit.collider.name} on layer {hit.collider.gameObject.layer}");
        }
        else
        {
            Debug.Log("Ray hit nothing - should see player!");
        }

        // Eğer ray player'a ulaştıysa (engel yok)
        if (hit.collider == null || hit.collider.transform == player.transform)
        {
            if (!hasLineOfSight)
            {
                Debug.Log($"Hunter acquired line of sight to player!");
                hasLineOfSight = true;
                FireAtPlayer(player.transform);
            }
        }
        else
        {
            // Engel var
            if (hasLineOfSight)
            {
                Debug.Log($"Hunter lost line of sight to player");
                hasLineOfSight = false;
            }
        }
    }

    void FireAtPlayer(Transform playerTransform)
    {
        if (hunterBulletPrefab == null || firePoint == null || hasFired) return;

        hasFired = true;
        Debug.Log($"Hunter fired at player!");

        // Mermi oluştur
        GameObject bullet = Instantiate(hunterBulletPrefab, firePoint.position, transform.rotation);

        // Owner ayarla
        BaseBullet bulletScript = bullet.GetComponent<BaseBullet>();
        if (bulletScript != null)
        {
            bulletScript.SetOwner(gameObject);
        }

        // Player'a doğru ateş et
        Rigidbody2D bulletRb = bullet.GetComponent<Rigidbody2D>();
        if (bulletRb != null)
        {
            Vector3 direction = (playerTransform.position - firePoint.position).normalized;
            bulletRb.velocity = direction * bulletSpeed;
        }
    }

    void OnHunterDeath_Internal()
    {
        Debug.Log($"Hunter died!");
        OnEnemyDeath?.Invoke(this);
        CancelInvoke(); // Görüş hattı kontrolünü durdur
        hasFired = true; // Artık ateş edemez
        hasLineOfSight = false; // Görüş hattını sıfırla

    }

    void OnHunterRevive()
    {
        // Hunter yeniden canlandığında
        Debug.Log($"Hunter revived!");
        hasFired = false; // Tekrar ateş edebilir
        hasLineOfSight = false; // Görüş hattını sıfırla
        if (GameManager.Instance != null)
        {
            GameManager.Instance.RegisterEnemy(this);
        }
        LookAtPlayer();

        CancelInvoke(nameof(CheckLineOfSight));

        InvokeRepeating(nameof(CheckLineOfSight), 0f, sightCheckInterval);

    }

    // Manuel rotation (level design için)
    public void SetDirection(float angleDegrees)
    {
        transform.rotation = Quaternion.AngleAxis(angleDegrees, Vector3.forward);
    }

    public void SetHexDirection(int directionIndex)
    {
        float angle = directionIndex * 60f;
        SetDirection(angle);
    }
}