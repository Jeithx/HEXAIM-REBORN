using UnityEngine;
using UnityEngine.SceneManagement;


public class PlayerController : MonoBehaviour
{
    [Header("Player Settings")]
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField] private float bulletSpeed = 5f;

    [Header("Player Visual")]
    [SerializeField] private Color playerColor = Color.blue;
    [SerializeField] private float playerSize = 0.3f;

    // Player state
    private bool canFire = true;
    private Camera playerCamera;

    public static PlayerController Instance { get; private set; }

    void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

        playerCamera = Camera.main;
    }

    void Start()
    {
        // Player'ı grid ortasına yerleştir
        if (GridManager.Instance != null)
        {
            GridManager.Instance.PlaceObjectOnHex(gameObject, 0, 0);
        }

        // Fire point yoksa oluştur (UP yönünde)
        if (firePoint == null)
        {
            GameObject firePointObj = new GameObject("FirePoint");
            firePointObj.transform.SetParent(transform);
            firePointObj.transform.localPosition = Vector3.up * playerSize;
            firePoint = firePointObj.transform;
        }

        // Bullet prefab yoksa oluştur
        if (bulletPrefab == null)
        {
            CreateBulletPrefab();
        }
    }

    void Update()
    {
        HandleRotation();
        HandleShooting();

        // TEST KODLARI (geçici)
        if (Input.GetKeyDown(KeyCode.Q)) // Q tuşu = hasar al
        {
            GetComponent<Health>()?.TakeDamage(1);
        }

        if (Input.GetKeyDown(KeyCode.E)) // E tuşu = iyileş
        {
            GetComponent<Health>()?.Heal(1);
        }

        if (Input.GetKeyDown(KeyCode.R)) // R tuşu = bölümü sıfırlama kodu, basit birtane, şuanki bölümü alır onu loadlar
        {
            UnityEngine.SceneManagement.Scene active = SceneManager.GetActiveScene();
            SceneManager.LoadSceneAsync(active.name, LoadSceneMode.Single);

        }


    }

    void HandleRotation()
    {
        // Farenin dünya pozisyonunu al
        Vector3 mouseWorldPosition = playerCamera.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPosition.z = 0; // 2D için z=0

        // Oyuncudan fareye olan yönü hesapla
        Vector3 direction = (mouseWorldPosition - transform.position).normalized;

        // Açıyı hesapla - Sprite yukarı bakıyorsa 90 derece offset ekle
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;
        transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
    }

    void HandleShooting()
    {
        // Sol mouse tuşuna basıldığında ateş et
        if (Input.GetMouseButtonDown(0) && canFire)
        {
            Fire();
        }
    }

    void Fire()
    {
        if (bulletPrefab == null || firePoint == null || !canFire) return;

        // Mouse pozisyonunu al
        Vector3 mouseWorldPos = playerCamera.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPos.z = 0;

        // Mermi oluştur (rotation yok)
        GameObject bullet = Instantiate(bulletPrefab, firePoint.position, Quaternion.identity);

        // Merminin sahibini ayarla
        PlayerBullet playerBullet = bullet.GetComponent<PlayerBullet>();
        if (playerBullet != null)
        {
            playerBullet.SetOwner(gameObject);
        }

        bullet.SetActive(true);

        // Bullet script ekle (5 saniye sonra imha için)
        //bullet.AddComponent<BulletDestroyer>();

        // Mermi yönünü ayarla - mouse'a doğru
        Rigidbody2D bulletRb = bullet.GetComponent<Rigidbody2D>();
        if (bulletRb != null)
        {
            Vector2 fireDirection = (mouseWorldPos - firePoint.position).normalized;
            bulletRb.velocity = fireDirection * bulletSpeed;
        }

     

        if (TurnManager.Instance != null)
        {
            TurnManager.Instance.StartTurn();
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnPlayerFired();
        }


        //Debug.Log("Player fired towards mouse!");
    }

    // TurnManager tarafından çağrılacak
    public void EnableFiring()
    {
        canFire = true;
        Debug.Log("Player can fire again!");
    }

    public void DisableFiring()
    {
        canFire = false;
    }

    void CreateBulletPrefab()
    {
        // Basit mermi prefab'ı oluştur
        GameObject bullet = new GameObject("BulletPrefab");

        // Visual (küçük daire)
        SpriteRenderer sr = bullet.AddComponent<SpriteRenderer>();
        sr.sprite = CreateCircleSprite();
        sr.color = Color.yellow;

        // Physics
        Rigidbody2D rb = bullet.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0; // Gravity yok
        rb.drag = 0; // Sürtünme yok

        // Collider
        CircleCollider2D collider = bullet.AddComponent<CircleCollider2D>();
        collider.radius = 0.05f; // Küçük mermi

        bulletPrefab = bullet;

        // Scene'den kaldır (sadece prefab olarak kullan)
        bullet.SetActive(false);
    }

    Sprite CreateCircleSprite()
    {
        // Basit daire sprite oluştur
        int size = 32;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[size * size];

        Vector2 center = Vector2.one * size / 2;
        float radius = size / 4f;

        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                Vector2 point = new Vector2(x, y);
                float distance = Vector2.Distance(point, center);

                if (distance <= radius)
                {
                    pixels[y * size + x] = Color.white;
                }
                else
                {
                    pixels[y * size + x] = Color.clear;
                }
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();

        return Sprite.Create(texture, new Rect(0, 0, size, size), Vector2.one * 0.5f, size);
    }

    void OnDrawGizmos()
    {
        // Ateş yönünü göster - transform.up kullan çünkü sprite yukarı bakıyor
        if (firePoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(firePoint.position, firePoint.position + transform.up * 2f);
        }
    }
}