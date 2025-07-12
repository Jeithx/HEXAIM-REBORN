﻿using UnityEngine;
using UnityEngine.SceneManagement;


public class PlayerController : MonoBehaviour
{
    [Header("Player Settings")]
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField] private float bulletSpeed = 5f;

    //[Header("Player Visual")]
    //[SerializeField] private Color playerColor = Color.blue;
    //[SerializeField] private float playerSize = 0.3f;

    // Player state
    private bool canFire = true;
    private Camera playerCamera;

    void Awake()
    {

        playerCamera = Camera.main;
    }

    private void Start()
    {
        // PlayerManager'a kaydol
        if (PlayerManager.Instance != null)
        {
            PlayerManager.Instance.RegisterPlayer(this);
            Debug.Log("Player registered with PlayerManager!");
        }
    }

    void Update()
    {
        HandleRotation();
        //HandleShooting();

        //// TEST KODLARI (geçici)
        //if (Input.GetKeyDown(KeyCode.Q)) // Q tuşu = hasar al
        //{
        //    GetComponent<Health>()?.TakeDamage(1);
        //}

        //if (Input.GetKeyDown(KeyCode.E)) // E tuşu = iyileş
        //{
        //    GetComponent<Health>()?.Heal(1);
        //}

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

    //void HandleShooting()
    //{
    //    // Sol mouse tuşuna basıldığında ateş et
    //    if (Input.GetMouseButtonDown(0) && canFire)
    //    {
    //        Fire();
    //    }
    //}

    public bool CanFire()
    {
        return canFire && bulletPrefab != null && firePoint != null;
    }

    public void FireTowards(Vector3 targetWorldPos)
    {
        if (!CanFire()) return;

        // Mermi oluştur
        GameObject bullet = Instantiate(bulletPrefab, firePoint.position, Quaternion.identity);

        // Owner ayarla
        PlayerBullet playerBullet = bullet.GetComponent<PlayerBullet>();
        if (playerBullet != null)
        {
            playerBullet.SetOwner(gameObject);
        }

        bullet.SetActive(true);

        // Yön hesapla ve ateş et
        Rigidbody2D bulletRb = bullet.GetComponent<Rigidbody2D>();
        if (bulletRb != null)
        {
            Vector2 fireDirection = (targetWorldPos - firePoint.position).normalized;
            bulletRb.velocity = fireDirection * bulletSpeed;
        }

        Debug.Log($"Player {gameObject.name} fired towards {targetWorldPos}!");
    }

    //void Fire()
    //{
    //    if (bulletPrefab == null || firePoint == null || !canFire) return;

    //    // Mouse pozisyonunu al
    //    Vector3 mouseWorldPos = playerCamera.ScreenToWorldPoint(Input.mousePosition);
    //    mouseWorldPos.z = 0;

    //    // Mermi oluştur (rotation yok)
    //    GameObject bullet = Instantiate(bulletPrefab, firePoint.position, Quaternion.identity);

    //    // Merminin sahibini ayarla
    //    PlayerBullet playerBullet = bullet.GetComponent<PlayerBullet>();
    //    if (playerBullet != null)
    //    {
    //        playerBullet.SetOwner(gameObject);
    //    }

    //    bullet.SetActive(true);

    //    // Bullet script ekle (5 saniye sonra imha için)
    //    //bullet.AddComponent<BulletDestroyer>();

    //    // Mermi yönünü ayarla - mouse'a doğru
    //    Rigidbody2D bulletRb = bullet.GetComponent<Rigidbody2D>();
    //    if (bulletRb != null)
    //    {
    //        Vector2 fireDirection = (mouseWorldPos - firePoint.position).normalized;
    //        bulletRb.velocity = fireDirection * bulletSpeed;
    //    }



    //    if (PlayerManager.Instance != null)
    //    {
    //        PlayerManager.Instance.OnAnyPlayerFired();
    //    }


    //    //Debug.Log("Player fired towards mouse!");
    //}

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

    private void OnDestroy()
    {
        // PlayerManager'dan çık
        if (PlayerManager.Instance != null)
        {
            PlayerManager.Instance.UnregisterPlayer(this);
        }
    }
}