using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class Gaia : MonoBehaviour
{
    [Header("Gaia Settings")]
    [SerializeField] private List<Vector2Int> restoreHexCoordinates = new List<Vector2Int>();

    [Header("Visual Settings")]
    [SerializeField] private bool showRestoreArea = true;
    [SerializeField] private Color restoreAreaColor = new Color(0, 1, 1, 0.3f);
    [SerializeField] private float pulseSpeed = 2f;
    [SerializeField] private float pulseAmount = 0.1f;

    //[Header("Hex Selection (Editor)")]
    //[SerializeField] private int hexSelectionRadius = 3;
    //[SerializeField] private bool autoSelectNearbyHexes = false;

    private Health health;
    private float pulseTimer = 0f;

    void Awake()
    {
        health = GetComponent<Health>();
        if (health == null)
        {
            health = gameObject.AddComponent<Health>();
        }
    }

    void Start()
    {
        // Health olaylarını dinle
        if (health != null)
        {
            health.OnDeath += OnGaiaDeath;
            health.OnTakeDamage += OnGaiaDamage;
        }

        // Debug için başlangıçta seçili hex'leri göster
        if (showRestoreArea)
        {
            Debug.Log($"Gaia at {transform.position} will restore {restoreHexCoordinates.Count} hexes");
        }
    }

    void OnDestroy()
    {
        if (health != null)
        {
            health.OnDeath -= OnGaiaDeath;
            health.OnTakeDamage -= OnGaiaDamage;
        }
    }

    void Update()
    {
        // Görsel pulse efekti
        if (showRestoreArea)
        {
            pulseTimer += Time.deltaTime * pulseSpeed;
        }
    }

    void OnGaiaDamage(int damage)
    {
        Debug.Log($"=== GAIA ACTIVATED at {transform.position} ===");

        // Restore işlemini başlat
        if (GaiaManager.Instance != null && restoreHexCoordinates.Count > 0)
        {
            GaiaManager.Instance.RestoreHexes(restoreHexCoordinates, transform.position);
        }
        else
        {
            Debug.LogWarning("GaiaManager not found or no hexes to restore!");
        }

        // Gaia kendini yok eder
        //StartCoroutine(DestroyAfterEffect());
    }

    void OnGaiaDeath()
    {
        Debug.Log($"=== GAIA ACTIVATED at {transform.position} ===");

        // Restore işlemini başlat
        if (GaiaManager.Instance != null && restoreHexCoordinates.Count > 0)
        {
            GaiaManager.Instance.RestoreHexes(restoreHexCoordinates, transform.position);
        }
        else
        {
            Debug.LogWarning("GaiaManager not found or no hexes to restore!");
        }

        // Gaia kendini yok eder
        //StartCoroutine(DestroyAfterEffect());
    }

    //System.Collections.IEnumerator DestroyAfterEffect()
    //{
    //    // Görsel efekt için kısa bir bekleme
    //    yield return new WaitForSeconds(0.1f);

    //    // Not: Eğer Gaia kendisi de restore alanındaysa ve başka bir Gaia 
    //    // onu restore ederse, yeni Gaia oluşturulacak
    //    Destroy(gameObject);
    //}

    // Inspector'dan hex seçimi için yardımcı metod
    //[ContextMenu("Select Nearby Hexes")]
    //void SelectNearbyHexes()
    //{
    //    restoreHexCoordinates.Clear();

    //    if (GridManager.Instance == null)
    //    {
    //        Debug.LogError("GridManager not found! Start the game first.");
    //        return;
    //    }

    //    Gaia'nın bulunduğu hex
    //    Hex centerHex = GridManager.Instance.GetHexAt(transform.position);
    //    if (centerHex == null) return;

    //    Belirtilen yarıçaptaki tüm hex'leri bul
    //    for (int q = -hexSelectionRadius; q <= hexSelectionRadius; q++)
    //    {
    //        for (int r = -hexSelectionRadius; r <= hexSelectionRadius; r++)
    //        {
    //            int s = -q - r;
    //            if (Mathf.Abs(s) <= hexSelectionRadius)
    //            {
    //                Vector2Int coords = new Vector2Int(centerHex.q + q, centerHex.r + r);
    //                Hex hex = GridManager.Instance.GetHexAt(coords.x, coords.y);

    //                if (hex != null)
    //                {
    //                    restoreHexCoordinates.Add(coords);
    //                }
    //            }
    //        }
    //    }

    //    Debug.Log($"Selected {restoreHexCoordinates.Count} hexes around Gaia");
    //}

    [ContextMenu("Clear Hex Selection")]
    void ClearHexSelection()
    {
        restoreHexCoordinates.Clear();
        Debug.Log("Cleared hex selection");
    }

    [ContextMenu("Add Current Hex")]
    void AddCurrentHex()
    {
        if (GridManager.Instance == null)
        {
            Debug.LogError("GridManager not found!");
            return;
        }

        Hex currentHex = GridManager.Instance.GetHexAt(transform.position);
        if (currentHex != null)
        {
            Vector2Int coords = new Vector2Int(currentHex.q, currentHex.r);
            if (!restoreHexCoordinates.Contains(coords))
            {
                restoreHexCoordinates.Add(coords);
                Debug.Log($"Added hex {coords} to restore list");
            }
        }
    }

    // Public getter/setter for snapshot system
    public List<Vector2Int> GetRestoreHexCoords()
    {
        return new List<Vector2Int>(restoreHexCoordinates);
    }

    public void SetRestoreHexes(List<Vector2Int> hexes)
    {
        restoreHexCoordinates = new List<Vector2Int>(hexes);
    }

    void OnDrawGizmos()
    {
        if (!showRestoreArea || restoreHexCoordinates == null) return;

        // Pulse efekti için alpha değeri
        float alpha = restoreAreaColor.a + Mathf.Sin(pulseTimer) * pulseAmount;
        Color gizmoColor = restoreAreaColor;
        gizmoColor.a = Mathf.Clamp01(alpha);

        Gizmos.color = gizmoColor;

        // Her restore hex'i için görsel
        foreach (var coords in restoreHexCoordinates)
        {
            Vector3 hexWorldPos = Hex.HexToWorldPosition(coords.x, coords.y);

            // Hex alanını göster
            DrawHexGizmo(hexWorldPos, Hex.HEX_SIZE * 0.9f);

            // Gaia'dan hex'e çizgi
            Gizmos.color = new Color(restoreAreaColor.r, restoreAreaColor.g, restoreAreaColor.b, 0.2f);
            Gizmos.DrawLine(transform.position, hexWorldPos);
        }

        // Gaia'nın kendisini vurgula
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, 0.3f);
    }

    void DrawHexGizmo(Vector3 center, float size)
    {
        Vector3[] hexPoints = new Vector3[6];

        for (int i = 0; i < 6; i++)
        {
            float angle = i * 60f * Mathf.Deg2Rad;
            hexPoints[i] = center + new Vector3(
                size * Mathf.Cos(angle),
                size * Mathf.Sin(angle),
                0
            );
        }

        // Hex kenarlarını çiz
        for (int i = 0; i < 6; i++)
        {
            Gizmos.DrawLine(hexPoints[i], hexPoints[(i + 1) % 6]);
        }

        // İçini doldur (basit mesh benzeri görünüm)
        for (int i = 0; i < 6; i++)
        {
            Gizmos.DrawLine(center, hexPoints[i]);
        }
    }

    //void OnDrawGizmosSelected()
    //{
    //    // Seçiliyken hex koordinatlarını göster
    //    if (restoreHexCoordinates == null) return;

    //    UnityEditor.Handles.color = Color.white;

    //    foreach (var coords in restoreHexCoordinates)
    //    {
    //        Vector3 hexWorldPos = Hex.HexToWorldPosition(coords.x, coords.y);
    //        UnityEditor.Handles.Label(hexWorldPos + Vector3.up * 0.5f, $"({coords.x},{coords.y})");
    //    }
    //}
}