using UnityEngine;

public class Bumper : BaseRobot
{
    [Header("Bumper Settings")]
    [SerializeField] private GameObject bumperBulletPrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField] private float bulletSpeed = 5f;

    [Header("Turn-Based Firing")]
    [SerializeField] private bool shouldFireAtEndOfTurn = false;


    [Header("Karakter’e takılacak Kulaklık Objesi")]
    [SerializeField] private GameObject headphoneObject;

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
    private void Start()
    {
        UpdateHeadphonesVisibility();

    }

    public override void OnBulletHit(BaseBullet bullet)
    {
        if (isDestroyed) return;

        //Debug.Log($"Bumper hit by {bullet.GetType().Name}");

        // Bazooka mermisi ise yok et
        if (bullet.name is "BazookaBullet")
        {
            DestroyRobot();
            return;
        }

        shouldFireAtEndOfTurn = true;

    }

    // TurnManager tarafından çağrılacak
    public bool HasPendingShot()
    {
        return shouldFireAtEndOfTurn && !isDestroyed;
    }

    public void ExecutePendingShot()
    {
        if (shouldFireAtEndOfTurn && !isDestroyed)
        {
            Debug.Log($"Bumper {gameObject.name} executing pending shot!");
            FireBumperBullet();
            shouldFireAtEndOfTurn = false;
        }
    }

    void FireBumperBullet()
    {
        if (bumperBulletPrefab == null || firePoint == null || isDestroyed) return;

        GameObject bullet = Instantiate(bumperBulletPrefab, firePoint.position, transform.rotation);

        BaseBullet bulletScript = bullet.GetComponent<BaseBullet>();
        if (bulletScript != null)
        {
            bulletScript.SetOwner(gameObject);
        }

        Rigidbody2D bulletRb = bullet.GetComponent<Rigidbody2D>();
        if (bulletRb != null)
        {
            Vector2 fireDirection = transform.up;
            bulletRb.velocity = fireDirection * bulletSpeed;
        }

        Debug.Log($"Bumper fired a push bullet!");
    }
}