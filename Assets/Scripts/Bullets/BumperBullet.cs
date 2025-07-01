using UnityEngine;
using System.Collections;

public class BumperBullet : BaseBullet
{
    [Header("Bumper Bullet Settings")]
    [SerializeField] private Color bulletColor = Color.white;

    void Start()
    {
        // Bumper mermi görsel ayarları
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.color = bulletColor;
        }
    }

    protected override void DefaultCollisionBehavior(GameObject hitObject)
    {
        // Duvar'a çarparsa dur
        if (hitObject.GetComponent<Wall>() != null)
        {
            DestroyBullet();
            return;
        }

        // İtilecek objeyi kontrol et
        if (CanBePushed(hitObject))
        {
            ExecutePush(hitObject);
        }

        DestroyBullet();
    }

    bool CanBePushed(GameObject obj)
    {
        // Health'i olan karakterler itilebilir
        return obj.GetComponent<Health>() != null;
    }

    void ExecutePush(GameObject target)
    {
        // İtme yönünü hesapla (mermi yönü)
        Vector2 pushDirection = GetComponent<Rigidbody2D>().velocity.normalized;

        Debug.Log($"BumperBullet: Calculating push for {target.name}");

        // Push path'ini hesapla
        PushResult pushResult = PushCalculator.CalculatePushPath(
            target.transform.position,
            pushDirection,
            target
        );

        // Push executor component ekle
        PushExecutor executor = target.GetComponent<PushExecutor>();
        if (executor == null)
        {
            executor = target.AddComponent<PushExecutor>();
        }

        // Hesaplanan path'i uygula
        executor.ExecutePush(pushResult);
    }
}

// Push execution component
public class PushExecutor : MonoBehaviour
{
    [Header("Push Animation Settings")]
    [SerializeField] private float moveSpeed = 8f;
    [SerializeField] private AnimationCurve moveCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private bool isPushing = false;

    public void ExecutePush(PushResult pushResult)
    {
        if (isPushing) return;

        Debug.Log($"Executing push for {gameObject.name}: {pushResult.hexPath.Count} steps");

        if (pushResult.willBeDestroyed)
        {
            Debug.Log($"{gameObject.name} will be destroyed by boundary!");
            // Direkt boundary'ye it ve yok et
            StartCoroutine(PushToDestruction(pushResult.finalPosition));
        }
        else if (pushResult.hexPath.Count > 0)
        {
            // Normal push - hex'ler boyunca hareket
            StartCoroutine(PushThroughHexes(pushResult));
        }
        else
        {
            Debug.Log($"{gameObject.name} has nowhere to go - no push needed");
            Destroy(this); // Component'i kaldır
        }
    }

    System.Collections.IEnumerator PushThroughHexes(PushResult pushResult)
    {
        isPushing = true;

        // Her hex'e sırayla git
        foreach (Vector3 hexPos in pushResult.hexPath)
        {
            yield return StartCoroutine(MoveToPosition(hexPos, 0.2f));
        }

        // Final pozisyona git
        if (pushResult.finalPosition != transform.position)
        {
            yield return StartCoroutine(MoveToPosition(pushResult.finalPosition, 0.2f));
        }

        Debug.Log($"{gameObject.name} push completed at {pushResult.finalPosition}");

        isPushing = false;
        Destroy(this); // Component'i temizle
    }

    System.Collections.IEnumerator PushToDestruction(Vector3 finalPos)
    {
        isPushing = true;

        // Boundary'ye git
        yield return StartCoroutine(MoveToPosition(finalPos, 0.4f));

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

        Debug.Log($"{gameObject.name} pushed out of bounds - destroying!");
        Destroy(gameObject);
    }

    System.Collections.IEnumerator MoveToPosition(Vector3 targetPos, float duration)
    {
        Vector3 startPos = transform.position;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            // Animation curve ile smooth movement
            float curveValue = moveCurve.Evaluate(t);
            transform.position = Vector3.Lerp(startPos, targetPos, curveValue);

            yield return null;
        }

        transform.position = targetPos;
    }
}