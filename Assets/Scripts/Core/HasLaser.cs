using UnityEngine;

/// <summary>
/// Karakterlere eklenecek basit laser component'i
/// </summary>
public class HasLaser : MonoBehaviour
{
    [Header("Laser Settings")]
    public bool laser = false; // Bu boolean aktif edilince laser açılır

    [Header("Visual Settings")]
    [SerializeField] private LineRenderer lineRenderer;
    [SerializeField] private LayerMask layerMask = ~0;

    public GameObject gunBarrel;
    private Vector3 startPos;

    private HasLaser lastHitLaser = null; // Son çarpılan laser


    void Awake()
    {
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

        startPos = gunBarrel != null ? gunBarrel.transform.position : transform.position;

        // Başlangıçta laser kapalı
        lineRenderer.enabled = false;
    }

    void Update()
    {
        // Laser aktifse line renderer'ı çalıştır
        if (laser)
        {
            UpdateLaser();
            lineRenderer.enabled = true;
        }
        else
        {
            if (lastHitLaser != null)
            {
                // Eğer son çarpılan laser'da scared animasyonu varsa, onu kapat
                lastHitLaser.gameObject.GetComponentInChildren<EyesAnimator>()?.GetComponent<Animator>()?.SetBool("IsScared", false);
                lastHitLaser.gameObject.GetComponentInChildren<EyesAnimator>()?.GetComponent<Animator>()?.SetTrigger("ScaredOut");
                lastHitLaser.laser = false; // Son çarpılan laser'ı kapat
                lastHitLaser = null;
            }
            lineRenderer.enabled = false;
        }
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
    }

    void UpdateLaser()
    {
        
        startPos = gunBarrel != null ? gunBarrel.transform.position : transform.position;
        Vector3 direction = transform.up; // Karakterin baktığı yön

        // Raycast yap
        RaycastHit2D hit = Physics2D.Raycast(startPos, direction, Mathf.Infinity, layerMask);

        Vector3 endPos;
        if (hit.collider != null)
        {
            endPos = hit.point;

            // Çarpılan objede HasLaser var mı kontrol et
            HasLaser targetLaser = hit.collider.GetComponent<HasLaser>();
            if (targetLaser != null)
            {
                lastHitLaser = targetLaser; // Son çarpılan laser'ı güncelle
                targetLaser.gameObject.GetComponentInChildren<EyesAnimator>()?.GetComponent<Animator>()?.SetBool("IsScared", true);
                targetLaser.laser = true; // Hedef laser'ı aktif et
            }
        }
        else
        {
            // Hiçbir şeye çarpmadıysa çok uzağa git
            endPos = startPos + direction * 1000f;
        }

        // Line renderer pozisyonlarını ayarla
        lineRenderer.SetPosition(0, startPos);
        lineRenderer.SetPosition(1, endPos);

    }
}