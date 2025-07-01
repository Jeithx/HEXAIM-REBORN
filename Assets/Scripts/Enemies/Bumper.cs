using UnityEngine;

public class Bumper : BaseRobot
{
    [Header("Bumper Settings")]
    [SerializeField] private GameObject bumperBulletPrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField] private float bulletSpeed = 5f;

    [Header("Visual Settings")]
    [SerializeField] private Color bumperColor = Color.black;

    void Start()
    {
        // Fire point yoksa oluştur
        if (firePoint == null)
        {
            GameObject firePointObj = new GameObject("BumperFirePoint");
            firePointObj.transform.SetParent(transform);
            firePointObj.transform.localPosition = Vector3.up * 0.3f;
            firePoint = firePointObj.transform;
        }
    }

    // Robot'a mermi çarptığında
    public override void OnBulletHit(BaseBullet bullet)
    {
        if (isDestroyed) return;

        Debug.Log($"Bumper hit by {bullet.GetType().Name}");

        // Bazooka mermisi ise yok et
        if (bullet.name is "BazookaBullet")
        {
            DestroyRobot();
            return;
        }

        // Normal mermi ise ateş et
        FireBumperBullet();
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