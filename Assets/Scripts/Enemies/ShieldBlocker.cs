using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class ShieldBlocker : MonoBehaviour
{
    [Header("Shield Settings")]
    [Tooltip("Kalkan açısı (derece)")]
    [Range(60, 180)] public float coneAngle = 90f;

    [Header("Health Integration")]
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

    void Start()
    {
        // Health event'lerini dinle
        if (health != null)
        {
            health.OnDeath += OnDeath;
        }
    }

    void OnDestroy()
    {
        if (health != null)
        {
            health.OnDeath -= OnDeath;
        }
    }

    // Sap yukarı bakıyor → local up = kalkan yönü
    Vector2 LocalShieldDir => Vector2.up;

    /// Dünya uzayında kalkan vektörü
    public Vector2 ShieldDirWorld
    {
        get
        {
            var dir = transform.TransformDirection(LocalShieldDir);
            return dir.normalized;
        }
    }

    /// Verilen temas noktası bloklanıyor mu?
    public bool IsBlocked(Vector2 hitPoint)
    {
        if (health != null && health.IsDead) return false; // Ölüyse kalkan çalışmaz

        Vector2 toHit = (hitPoint - (Vector2)transform.position).normalized;
        float angle = Vector2.Angle(ShieldDirWorld, toHit);
        return angle <= coneAngle * 0.5f+5f;
    }

    // GDD: "Kalkanı aktifken Medic mermisiyle can alamaz"
    public bool CanReceiveHealing()
    {
        // Sadece ölüyken heal alabilir
        return health != null && health.IsDead;
    }

    void OnDeath()
    {
        Debug.Log($"ShieldBlocker {gameObject.name} died - shield no longer active!");
    }

    // Manual rotation for level design
    public void SetDirection(float angleDegrees)
    {
        transform.rotation = Quaternion.AngleAxis(angleDegrees, Vector3.forward);
    }

    public void SetHexDirection(int directionIndex)
    {
        // 0=0°, 1=60°, 2=120°, 3=180°, 4=240°, 5=300°
        float angle = directionIndex * 60f;
        SetDirection(angle);
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (health != null && health.IsDead) return; // Ölüyse gizmo gösterme

        Gizmos.color = Color.cyan;
        Vector3 dir = (Vector3)ShieldDirWorld;
        float half = coneAngle * 0.5f;
        int steps = 20;

        Vector3 prev = transform.position + Quaternion.Euler(0, 0, -half) * dir * 0.8f;
        for (int i = 1; i <= steps; i++)
        {
            float t = -half + (coneAngle / steps) * i;
            Vector3 next = transform.position + Quaternion.Euler(0, 0, t) * dir * 0.8f;
            Gizmos.DrawLine(prev, next);
            prev = next;
        }
        // Orta çizgi
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.position, transform.position + dir * 0.8f);
    }
#endif
}