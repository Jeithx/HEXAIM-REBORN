using UnityEngine;

public class BaseBullet : MonoBehaviour
{
    [Header("Base Bullet Settings")]
    [SerializeField] private float destroyTime = 5f;
    [SerializeField] protected int damage = 1;

    protected GameObject owner;

    public virtual void SetOwner(GameObject newOwner)
    {
        owner = newOwner;
    }

    protected virtual void Awake()
    {
        Destroy(gameObject, destroyTime);
    }

    protected virtual void OnTriggerEnter2D(Collider2D other)
    {
        HandleCollision(other.gameObject);
    }

    protected virtual void OnCollisionEnter2D(Collision2D collision)
    {
        HandleCollision(collision.gameObject);
    }

    protected virtual void HandleCollision(GameObject hitObject)
    {

        if (hitObject == owner) {
            Debug.Log("player kendisine vuramaz");
        return; }

        // Hex'lere çarparsa geçsin
        if (hitObject.GetComponent<HexTile>() != null)
        {
            return;
        }

        // Kalkan kontrolü
        ShieldBlocker shield = hitObject.GetComponent<ShieldBlocker>();
        if (shield != null)
        {
            // Çarpışma noktasını hesapla
            Vector2 contact = transform.position;


            if (shield.IsBlocked(contact))
            {
                Debug.Log($"Bullet blocked by {hitObject.name}'s shield!");
                DestroyBullet();
                return;
            }
        }

        // Eğer çarpışma bir kalkan tarafından engellenmediyse

        // Her mermi türü bu metodu override edebilir
        DefaultCollisionBehavior(hitObject);
    }

    protected virtual void DefaultCollisionBehavior(GameObject hitObject)
    {
        // Duvar'a çarparsa dur
        if (hitObject.GetComponent<Wall>() != null)
        {
            DestroyBullet();
            return;
        }

        if (hitObject.GetComponent<Hay>() != null)
        {
            Destroy(hitObject.gameObject);
            DestroyBullet();
            return;
        }

        IRobot robot = hitObject.GetComponent<IRobot>();
        if (robot != null)
        {


            // Bazooka değilse mermi yok olur
            if (!robot.CanTakeDamageFrom(this))
            {
                // Robot'a mermi çarptı
                robot.OnBulletHit(this);
                DestroyBullet();
                return;
            }
            else
            {
                // Bazooka ise robot yok olur
                robot.DestroyRobot();
                DestroyBullet();
                return;
            }
        }

        // Karaktere çarparsa hasar ver
        Health health = hitObject.GetComponent<Health>();
        if (health != null)
        {
            health.TakeDamage(damage);
            DestroyBullet();
            return;
        }
    }

    protected virtual void DestroyBullet()
    {
        Destroy(gameObject);
    }

}