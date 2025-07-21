using UnityEngine;
using System.Collections.Generic;

// Push sonucu data class
[System.Serializable]
public class PushResult
{
    public List<Vector3> hexPath;
    public Vector3 finalPosition;
    public bool hitObstacle;
    public GameObject obstacleHit;
    public bool willBeDestroyed; // Kamera dışına çıkacak mı?

    public PushResult(List<Vector3> path, Vector3 finalPos, bool obstacle, GameObject obstacleObj, bool destroyed = false)
    {
        hexPath = path;
        finalPosition = finalPos;
        hitObstacle = obstacle;
        obstacleHit = obstacleObj;
        willBeDestroyed = destroyed;
    }
}

public static class PushCalculator
{
    private static Camera gameCamera = Camera.main;

    public static PushResult CalculatePushPath(Vector3 startPos, Vector2 direction, GameObject pushedObject)
    {
        List<Vector3> hexPath = new List<Vector3>();
        Vector3 currentPos = startPos;
        Vector3 finalPosition = startPos;
        bool hitObstacle = false;
        GameObject obstacleHit = null;
        bool willBeDestroyed = false;

        Debug.Log($"Calculating push path for {pushedObject.name} from {startPos} in direction {direction}");

        // Push yönünde hex hex ilerle (max 30 hex)
        for (int i = 0; i < 30; i++)
        {
            Vector3 nextHex = GetNextHexInDirection(currentPos, direction);

            // Kamera sınırlarında mı?
            if (IsOutOfBounds(nextHex))
            {
                Debug.Log($"Push path: Object will be destroyed at bounds");
                finalPosition = nextHex;
                willBeDestroyed = true;
                break;
            }

            // O hex'te engel var mı?
            GameObject obstacle = GetObstacleAt(nextHex, pushedObject);
            if (obstacle != null)
            {
                Debug.Log($"Push path: Hit obstacle {obstacle.name}, stopping at {currentPos}");
                hitObstacle = true;
                obstacleHit = obstacle;
                finalPosition = currentPos; // Önceki hex'te dur
                break;
            }

            hexPath.Add(nextHex);
            finalPosition = nextHex;
            currentPos = nextHex;
        }

        Debug.Log($"Push path calculated: {hexPath.Count} steps, final pos: {finalPosition}");
        return new PushResult(hexPath, finalPosition, hitObstacle, obstacleHit, willBeDestroyed);
    }

    static Vector3 GetNextHexInDirection(Vector3 currentPos, Vector2 direction)
    {
        // Direction'a göre bir sonraki hex pozisyonunu hesapla
        float hexDistance = Hex.HEX_SIZE * 1.5f; // Hex'ler arası mesafe
        Vector3 targetPos = currentPos + (Vector3)direction * hexDistance;

        // En yakın hex pozisyonuna snap
        if (GridManager.Instance != null)
        {
            Hex nearestHex = GridManager.Instance.GetNearestHex(targetPos);
            if (nearestHex != null)
            {
                return nearestHex.worldPosition;
            }
        }

        return targetPos;
    }

    static GameObject GetObstacleAt(Vector3 position, GameObject pushedObject)
    {
        // O pozisyonda engel var mı kontrol et
        Collider2D[] hits = Physics2D.OverlapCircleAll(position, 0.3f);

        foreach (Collider2D hit in hits)
        {
            if (hit.gameObject == pushedObject) continue; // Kendisi değil

            // Duvar
            if (hit.GetComponent<Wall>() != null)
            {
                return hit.gameObject;
            }

            if (hit.GetComponent<Hay>() != null)
            {
                return hit.gameObject;
            }

            // Başka karakter
            if (hit.GetComponent<Health>() != null)
            {
                return hit.gameObject;
            }

            // Robot
            if (hit.GetComponent<IRobot>() != null)
            {
                return hit.gameObject;
            }

            // ShieldBlocker
            if (hit.GetComponent<ShieldBlocker>() != null)
            {
                return hit.gameObject;
            }
        }

        return null; // Engel yok
    }

    static bool IsOutOfBounds(Vector3 worldPosition)
    {
        if (gameCamera == null) gameCamera = Camera.main;
        if (gameCamera == null) return false;

        Vector3 screenPos = gameCamera.WorldToScreenPoint(worldPosition);

        // Ekran dışında mı? (Buffer ile)
        float buffer = 50f;
        return screenPos.x < -buffer ||
               screenPos.x > Screen.width + buffer ||
               screenPos.y < -buffer ||
               screenPos.y > Screen.height + buffer;
    }
}