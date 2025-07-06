using UnityEngine;

public class Robot : BaseRobot
{
    [Header("Robot Settings")]
    [SerializeField] private GameObject robotBulletPrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField] private float bulletSpeed = 5f;



    // Robot'a mermi çarptığında
    public override void OnBulletHit(BaseBullet bullet)
    {
        if (isDestroyed) return;

        Debug.Log($"Robot hit by {bullet.GetType().Name}");

        // Bazooka mermisi ise yok et
        if (bullet.name is "BazookaBullet")
        {
            DestroyRobot();
            return;
        }

        // Normal mermi ise ateş et
        FireBullet();
    }

    void FireBullet()
    {
        if (robotBulletPrefab == null || firePoint == null || isDestroyed) return;

        GameObject bullet = Instantiate(robotBulletPrefab, firePoint.position, transform.rotation);

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