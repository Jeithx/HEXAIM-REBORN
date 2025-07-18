using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ElectroGateNew : MonoBehaviour, IDecoyable
{
    [Header("Gate Settings")]
    [SerializeField] private bool hasHeadphones = true;
    [SerializeField] private Transform electricityPoint;
    private LayerMask detectionMask;
    [SerializeField] private float maxDistance = Mathf.Infinity;

    [Header("Damage Settings")]
    [SerializeField] private float damageCheckInterval = 0.1f;
    [SerializeField] private float checkingInterval = 0.2f;

    [Header("LineRenderer Settings")]
    [SerializeField] private LineRenderer beamRenderer;  // assign via Inspector or create in code
    [SerializeField] private float beamWidth = 0.05f;

    private bool isElectricityActive = false;
    private bool isChecking = false;
    private Coroutine checkingCoroutine;
    private Coroutine damageCoroutine;
    private ElectroGateNew connectedGate = null; // Bağlı olduğu gate

    private GameObject electroButtonManager;
    private ElectroButtonManager electroButtonManagerScript;


    [Header("Karakter’e takılacak Kulaklık Objesi")]
    [SerializeField] private GameObject headphoneObject;
    private void UpdateHeadphonesVisibility()
    {
        if (headphoneObject == null) return;
        headphoneObject.SetActive(!hasHeadphones);
    }
    void OnValidate()
    {
        UpdateHeadphonesVisibility();
    }

    public bool CanBeDecoyed
    {
        get
        {
            bool canDecoy = hasHeadphones;
            //Debug.Log($"{gameObject.name} CanBeDecoyed: hasHeadphones={hasHeadphones}, result={canDecoy}");
            return canDecoy;
        }
    }

    public void OnDecoyStart() { Debug.Log($"{gameObject.name} decoy started"); }
    public void OnDecoyEnd() { Debug.Log($"{gameObject.name} decoy ended"); }

    // Static set yerine instance-based yaklaşım
    private static HashSet<ElectroGateNew> activeGates = new HashSet<ElectroGateNew>();

    private void Awake()
    {
        detectionMask = ~0;
        detectionMask &= ~(1 << LayerMask.NameToLayer("Hexes"));
        detectionMask &= ~(1 << LayerMask.NameToLayer("UI"));
        detectionMask &= ~(1 << LayerMask.NameToLayer("MapBorders"));

        //Debug.Log($"Gate {gameObject.name} detection mask value: {detectionMask.value}");
        
        if (beamRenderer == null)
        {
            beamRenderer = gameObject.AddComponent<LineRenderer>();
            beamRenderer.useWorldSpace = true;
            beamRenderer.startColor = beamRenderer.endColor = Color.blue;
            beamRenderer.startWidth = beamRenderer.endWidth = beamWidth;
            beamRenderer.positionCount = 2;
            beamRenderer.enabled = false;          // hide until needed
        }

    }


    void Start()
    {
        UpdateHeadphonesVisibility();

        electroButtonManager = FindAnyObjectByType<ElectroButtonManager>()?.gameObject;
        electroButtonManagerScript = electroButtonManager?.GetComponent<ElectroButtonManager>();

        StartChecking();
    }
    void LateUpdate()
    {
        if (isElectricityActive && connectedGate != null && beamRenderer.enabled)
        {
            Vector3 from = electricityPoint ? electricityPoint.position : transform.position;
            Vector3 to = connectedGate.electricityPoint
                           ? connectedGate.electricityPoint.position
                           : connectedGate.transform.position;

            beamRenderer.SetPosition(0, from);
            beamRenderer.SetPosition(1, to);
        }
    }

    void OnDestroy()
    {
        StopAllCoroutines();
        // Aktif listeden çıkar
        activeGates.Remove(this);

        // Eğer bağlı gate varsa onun da bağlantısını kes
        if (connectedGate != null)
        {
            connectedGate.DisconnectFromGate();
        }
    }

    // === CHECKING DÖNGÜSÜ ===
    void StartChecking()
    {
        if (checkingCoroutine != null)
        {
            StopCoroutine(checkingCoroutine);
        }

        isChecking = true;
        checkingCoroutine = StartCoroutine(CheckingLoop());
        Debug.Log($"[STATE] ElectroGate {gameObject.name} checking STARTED");
    }

    void StopChecking()
    {
        if (checkingCoroutine != null)
        {
            StopCoroutine(checkingCoroutine);
            checkingCoroutine = null;
        }

        isChecking = false;
        Debug.Log($"[STATE] ElectroGate {gameObject.name} checking STOPPED");
    }

    IEnumerator CheckingLoop()
    {
        int checkCount = 0;
        while (isChecking)
        {
            checkCount++;
            Debug.Log($"[CHECKING LOOP] Gate {gameObject.name} - Check #{checkCount}");

            bool buttonsActive = electroButtonManagerScript?.IsElectricityActive() ?? false;
            Debug.Log($"[CHECKING LOOP] Gate {gameObject.name} - Buttons active: {buttonsActive}");

            if (buttonsActive)
            {
                Debug.Log($"[CHECKING LOOP] Gate {gameObject.name} - Buttons are active, looking for facing gate...");
                ElectroGateNew facingGate = GetFacingGate();

                if (facingGate != null)
                {
                    Debug.Log($"[CHECKING LOOP] Gate {gameObject.name} - Found facing gate: {facingGate.gameObject.name}");

                    // Her iki gate de müsait mi kontrol et
                    bool imFree = !isElectricityActive;
                    bool heFree = !facingGate.isElectricityActive;

                    Debug.Log($"[CHECKING LOOP] Gate {gameObject.name} - I'm free: {imFree}, He's free: {heFree}");

                    if (imFree && heFree)
                    {
                        Debug.Log($"[CHECKING LOOP] Gate {gameObject.name} - BOTH GATES FREE! Starting connection...");

                        // İki gate'i birbirine bağla
                        ConnectToGate(facingGate);
                        facingGate.ConnectToGate(this);

                        // Her ikisini de damage moduna geçir
                        StopChecking();
                        StartDamage();

                        facingGate.StopChecking();
                        facingGate.StartDamage();

                        yield break;
                    }
                    else
                    {
                        Debug.Log($"[CHECKING LOOP] Gate {gameObject.name} - One or both gates are busy, waiting...");
                    }
                }
                else
                {
                    Debug.Log($"[CHECKING LOOP] Gate {gameObject.name} - No facing gate found");
                }
            }
            else
            {
                Debug.Log($"[CHECKING LOOP] Gate {gameObject.name} - Buttons not active, waiting...");
            }

            yield return new WaitForSeconds(checkingInterval);
        }

        Debug.Log($"[CHECKING LOOP] Gate {gameObject.name} - Loop ended after {checkCount} checks");
    }

    // === DAMAGE DÖNGÜSÜ ===
    void StartDamage()
    {
        if (damageCoroutine != null)
        {
            StopCoroutine(damageCoroutine);
        }

        isElectricityActive = true;
        activeGates.Add(this);
        damageCoroutine = StartCoroutine(DamageLoop());
        beamRenderer.enabled = true;
        Debug.Log($"[STATE] ElectroGate {gameObject.name} damage STARTED - Total active gates: {activeGates.Count}");
    }

    void StopDamage()
    {
        if (damageCoroutine != null)
        {
            StopCoroutine(damageCoroutine);
            damageCoroutine = null;
        }

        isElectricityActive = false;
        activeGates.Remove(this);
        beamRenderer.enabled = false;
        Debug.Log($"[STATE] ElectroGate {gameObject.name} damage STOPPED - Total active gates: {activeGates.Count}");
    }

    IEnumerator DamageLoop()
    {
        WaitForSeconds wait = new WaitForSeconds(damageCheckInterval);

        while (isElectricityActive)
        {
            // 1. Elektrik hâlâ açık mı?
            if (!(electroButtonManagerScript?.IsElectricityActive() ?? false))
            {
                Debug.Log($"{name} buttons OFF – restart checking");
                DisconnectAndRestartChecking();
                yield break;
            }

            // 2. Sadece hasar ver
            ApplyDamage();

            yield return wait;
        }
    }

    // === BAĞLANTI YÖNETİMİ ===
    void ConnectToGate(ElectroGateNew otherGate)
    {
        connectedGate = otherGate;
        Debug.Log($"[CONNECTION] Gate {gameObject.name} connected to {otherGate.gameObject.name}");
    }

    void DisconnectFromGate()
    {
        if (connectedGate != null)
        {
            Debug.Log($"[CONNECTION] Gate {gameObject.name} disconnected from {connectedGate.gameObject.name}");
            connectedGate = null;
        }
        else
        {
            Debug.Log($"[CONNECTION] Gate {gameObject.name} was already disconnected");
        }
    }

    void DisconnectAndRestartChecking()
    {
        // Önce bağlı gate'i bilgilendir
        if (connectedGate != null)
        {
            connectedGate.DisconnectFromGate();
            connectedGate.StopDamage();
            connectedGate.StartChecking();
        }

        // Kendi bağlantısını kes
        DisconnectFromGate();
        StopDamage();
        StartChecking();
    }

    // === YARDIMCI METODLAR ===
    ElectroGateNew GetFacingGate()
    {
        Vector2 myDirection = transform.up;
        Vector2 startPos = electricityPoint != null ? electricityPoint.position : transform.position;

        Debug.Log($"[RAYCAST] Gate {gameObject.name} - Starting raycast from {startPos} in direction {myDirection}");

        RaycastHit2D hit = Physics2D.Raycast(startPos, myDirection, maxDistance, detectionMask);

        Debug.DrawRay(startPos, myDirection * maxDistance, Color.red, 0.5f);

        if (hit.collider != null)
        {
            //Debug.Log($"[RAYCAST] Gate {gameObject.name} - Hit object: {hit.collider.name} at distance {hit.distance}");
            //Debug.Log($"[RAYCAST] Gate {gameObject.name} - Hit object tag: {hit.collider.tag}");
            //Debug.Log($"[RAYCAST] Gate {gameObject.name} - Is same object: {hit.collider.gameObject == this.gameObject}");

            if (hit.collider.CompareTag("ElectroGate"))
            {
                Debug.Log($"[RAYCAST] Gate {gameObject.name} - Hit object has ElectroGate tag!");

                if (hit.collider.gameObject != this.gameObject)
                {
                    Debug.Log($"[RAYCAST] Gate {gameObject.name} - Hit object is different gate, checking component...");

                    ElectroGateNew otherGate = hit.collider.GetComponent<ElectroGateNew>();
                    if (otherGate != null)
                    {
                        Debug.Log($"[RAYCAST] Gate {gameObject.name} - Found ElectroGateNew component, checking if facing...");

                        bool facing = IsFacingGate(otherGate);
                        Debug.Log($"[RAYCAST] Gate {gameObject.name} - Facing check result: {facing}");

                        if (facing)
                        {
                            Debug.Log($"[RAYCAST] Gate {gameObject.name} - SUCCESS! Returning facing gate: {otherGate.gameObject.name}");
                            return otherGate;
                        }
                        else
                        {
                            Debug.Log($"[RAYCAST] Gate {gameObject.name} - Gates are not facing each other");
                        }
                    }
                    else
                    {
                        Debug.Log($"[RAYCAST] Gate {gameObject.name} - Hit object has no ElectroGateNew component!");
                    }
                }
                else
                {
                    Debug.Log($"[RAYCAST] Gate {gameObject.name} - Hit myself, ignoring...");
                }
            }
            else
            {
                Debug.Log($"[RAYCAST] Gate {gameObject.name} - Hit object is not an ElectroGate, it's blocking the path");
            }
        }
        else
        {
            Debug.Log($"[RAYCAST] Gate {gameObject.name} - Raycast hit nothing");
        }

        Debug.Log($"[RAYCAST] Gate {gameObject.name} - Returning null (no facing gate found)");
        return null;
    }

    bool IsFacingGate(ElectroGateNew otherGate)
    {
        Vector2 myDirection = transform.up.normalized;
        Vector2 otherDirection = otherGate.transform.up.normalized;

        Vector2 toOther = (otherGate.transform.position - transform.position).normalized;
        Vector2 toMe = -toOther;

        //Debug.Log($"[FACING CHECK] Gate {gameObject.name} -> {otherGate.gameObject.name}");
        //Debug.Log($"[FACING CHECK] My direction: {myDirection}, Other direction: {otherDirection}");
        //Debug.Log($"[FACING CHECK] Direction to other: {toOther}, Direction to me: {toMe}");

        float tolerance = 0.0001f;

        float dotMyToOther = Vector2.Dot(myDirection, toOther);
        float dotOtherToMe = Vector2.Dot(otherDirection, toMe);

        //Debug.Log($"[FACING CHECK] Dot product (me->other): {dotMyToOther:F3}");
        //Debug.Log($"[FACING CHECK] Dot product (other->me): {dotOtherToMe:F3}");
        Debug.Log($"[FACING CHECK] Tolerance threshold: {1f - tolerance:F3}");

        bool iLookAtHim = dotMyToOther > 1f - tolerance;
        bool heLooksAtMe = dotOtherToMe > 1f - tolerance;

        //Debug.Log($"[FACING CHECK] I look at him: {iLookAtHim}");
        //Debug.Log($"[FACING CHECK] He looks at me: {heLooksAtMe}");

        bool result = iLookAtHim && heLooksAtMe;
        //Debug.Log($"[FACING CHECK] Final result: {result}");

        return result;
    }
    void ApplyDamage()
    {
        Vector2 startPos = electricityPoint ? electricityPoint.position : transform.position;
        Vector2 direction = transform.up;

        float beamLength = connectedGate
                           ? Vector2.Distance(startPos, connectedGate.transform.position)
                           : maxDistance;

        // ❶ Trigger’ları da hesaba kat
        bool oldQuery = Physics2D.queriesHitTriggers;
        Physics2D.queriesHitTriggers = true;

        // ❷ Geniş ışın
        float radius = 0.15f;
        RaycastHit2D[] hits = Physics2D.CircleCastAll(startPos, radius, direction, beamLength, detectionMask);

        foreach (var h in hits)
        {
            // ❸ Önce mermileri temizle
            if (h.collider.TryGetComponent<BaseBullet>(out var bullet))
            {
                Destroy(bullet.gameObject);
                Debug.Log($"[DAMAGE] {name} destroyed bullet {bullet.name}");
                continue;                 // aynı karede başka çarpanlara bakmaya devam et
            }

            if (h.collider.TryGetComponent<Health>(out var hp))
            {
                hp.TakeDamage(2);
            }
            if (h.collider.TryGetComponent<Hay>(out var hay))
            {
                Destroy(hay.gameObject);
            }
        }

        Physics2D.queriesHitTriggers = oldQuery;
    }


    // === EXTERNAL EVENTS ===
    public void OnDecoyApplied()
    {
        Debug.Log($"ElectroGate {gameObject.name} received decoy event - restarting checking");
        RestartChecking();
    }

    public void OnGaiaRestore()
    {
        Debug.Log($"ElectroGate {gameObject.name} received gaia restore event - restarting checking");
        RestartChecking();
    }

    public void OnBazookaHit()
    {
        Debug.Log($"ElectroGate {gameObject.name} hit by bazooka - restarting checking");
        RestartChecking();
    }

    public void OnObjectSpawned()
    {
        Debug.Log($"ElectroGate {gameObject.name} detected object spawn - restarting checking");
        RestartChecking();
    }

    public void RestartChecking()
    {
        if (isElectricityActive)
        {
            DisconnectAndRestartChecking();
        }
        else if (isChecking)
        {
            StopChecking();
            StartChecking();
        }
        else
        {
            StartChecking();
        }
    }

    // === GETTER'LAR ===
    public bool IsElectricityActive => isElectricityActive;
    public bool IsChecking => isChecking;
    public ElectroGateNew ConnectedGate => connectedGate;
    public bool IsConnected => connectedGate != null;

    // Debug için
    public static int GetActiveGateCount() => activeGates.Count;
}