using UnityEngine;

/// <summary>
/// PlayerController'a eklenecek cursor takip eden laser
/// </summary>
public class PlayerLaser : MonoBehaviour
{
    [Header("Laser Settings")]
    [SerializeField] private LineRenderer lineRenderer;
    [SerializeField] private float maxDistance = Mathf.Infinity;
    [SerializeField] private LayerMask layerMask = ~0;

    public GameObject gunBarrel;
    private Vector3 startPos;

    private HasLaser lastHitLaser = null; // Son çarpılan laser


    private Camera playerCamera;

    void Awake()
    {
        startPos = gunBarrel != null ? gunBarrel.transform.position : transform.position;

        playerCamera = Camera.main;

        // LineRenderer yoksa ekle
        if (lineRenderer == null)
        {
            lineRenderer = GetComponent<LineRenderer>();
            if (lineRenderer == null)
            {
                lineRenderer = gameObject.AddComponent<LineRenderer>();
            }
        }

        SetupLineRenderer();
        SetupLayerMask();
    }

    void Start()
    {
        lineRenderer.enabled = true; // Player laser'ı her zaman açık
    }

    void Update()
    {
        UpdateLaserToCursor();
    }

    void SetupLineRenderer()
    {
        // LineRenderer ayarları Inspector'dan yapılacak
        lineRenderer.positionCount = 2;
        lineRenderer.useWorldSpace = true;
    }

    void SetupLayerMask()
    {
        // Hex layer'ını çıkar
        int hexLayer = LayerMask.NameToLayer("Hexes");
        if (hexLayer != -1)
            layerMask &= ~(1 << hexLayer);
        int hiddenLayer = LayerMask.NameToLayer("Hidden");
        if (hiddenLayer != -1)
            layerMask &= ~(1 << hiddenLayer);
        int deadLayer = LayerMask.NameToLayer("DeadCharacters");
        if (deadLayer != -1)
            layerMask &= ~(1 << deadLayer);
    }

    void UpdateLaserToCursor()
    {
        if (playerCamera == null) return;

        startPos = gunBarrel != null ? gunBarrel.transform.position : transform.position;

        // Mouse pozisyonunu world pozisyonuna çevir
        Vector3 mouseWorldPos = playerCamera.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPos.z = 0;

        Vector3 direction = (mouseWorldPos - startPos).normalized;

        // Raycast yap
        RaycastHit2D hit = Physics2D.Raycast(startPos, direction, maxDistance, layerMask);

        Vector3 endPos;
        HasLaser currentHitLaser = null;

        if (hit.collider != null)
        {
            endPos = hit.point;
            currentHitLaser = hit.collider.GetComponent<HasLaser>();
        }
        else
        {
            endPos = startPos + direction * 1000f;
        }

        // Eğer farklı bir objeye çarpıyorsak
        if (currentHitLaser != lastHitLaser)
        {
            // Önceki laser'ı kapat
            if (lastHitLaser != null)
            {
                lastHitLaser.laser = false;
            }

            // Yeni laser'ı aç
            if (currentHitLaser != null)
            {
                currentHitLaser.laser = true;
            }

            // Son çarpılan laser'ı güncelle
            lastHitLaser = currentHitLaser;
        }

        // Line renderer pozisyonlarını ayarla
        lineRenderer.SetPosition(0, startPos);
        lineRenderer.SetPosition(1, endPos);
    }
}