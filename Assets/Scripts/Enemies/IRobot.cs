using UnityEngine;

// Robot interface - tüm robotlar bu kurallara uyar
public interface IRobot
{
    void OnBulletHit(BaseBullet bullet);
    bool CanTakeDamageFrom(BaseBullet bullet);
    void DestroyRobot();
}


public abstract class BaseRobot : MonoBehaviour, IRobot, IDecoyable
{
    [Header("Robot Settings")]
    [SerializeField] protected bool isDestroyed = false;

    [Header("Decoy Settings")]
    [SerializeField] protected bool hasHeadphones = true; // Robotlar genelde headphone'lu

    // IDecoyable implementation
    public bool CanBeDecoyed => hasHeadphones && !isDestroyed;

    public virtual void OnDecoyStart()
    {
        Debug.Log($"Robot {gameObject.name} decoy started");
        // Robot özel efektleri buraya
    }

    public virtual void OnDecoyEnd()
    {
        Debug.Log($"Robot {gameObject.name} decoy ended");
        // Robot özel efektleri buraya
    }

    // Her robot kendi davranışını implement eder
    public abstract void OnBulletHit(BaseBullet bullet);

    // Genel robot kuralı: Sadece Bazooka'dan yok edilir
    public virtual bool CanTakeDamageFrom(BaseBullet bullet)
    {
        return false; //daha sonradan burya if BazookaBullet ekle ego
    }

    // Robot yok etme
    public virtual void DestroyRobot()
    {
        if (isDestroyed) return;

        isDestroyed = true;
        Debug.Log($"Robot {gameObject.name} destroyed!");

        // GridManager'a bildir
        if (GridManager.Instance != null)
        {
            Hex hex = GridManager.Instance.GetHexAt(transform.position);
            if (hex != null)
            {
                hex.isOccupied = false;
                hex.occupiedBy = null;
            }
        }

        Destroy(gameObject);
    }

    // Ortak rotation metodları
    //public virtual void SetDirection(float angleDegrees)
    //{
    //    transform.rotation = Quaternion.AngleAxis(angleDegrees, Vector3.forward);
    //}

    //public virtual void SetHexDirection(int directionIndex)
    //{
    //    float angle = directionIndex * 60f;
    //    SetDirection(angle);
    //}

    // Robot durumu
    public bool IsDestroyed => isDestroyed;
}