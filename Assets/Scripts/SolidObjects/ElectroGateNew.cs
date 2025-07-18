// ElectroGateNew.cs
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ElectroGateNew : MonoBehaviour
{
    [Header("Gate Settings")]
    [SerializeField] private bool hasHeadphones = true;
    [SerializeField] private Transform electricityPoint; // Elektrik çıkış noktası
    private LayerMask detectionMask; // Raycast için layer mask
    [SerializeField] private float maxDistance = Mathf.Infinity; // Raycast için maksimum mesafe

    [Header("Damage Settings")]
    [SerializeField] private float damageCheckInterval = 0.1f;
    [SerializeField] private float checkingInterval = 0.2f; // Checking döngüsü aralığı

    private bool isElectricityActive = false;
    private bool isChecking = false;
    private Coroutine checkingCoroutine;
    private Coroutine damageCoroutine;

    private GameObject electroButtonManager;
    private ElectroButtonManager electroButtonManagerScript;

private void Awake()
{
    // Sadece ElectroGate layer'ını detect et
    detectionMask = 1 << LayerMask.NameToLayer("ElectroGate");
    
    // Eğer ElectroGate layer'ı yoksa default layer'ı kullan
    if (detectionMask == 0)
    {
        detectionMask = 1 << 0; // Default layer
    }
    
    Debug.Log($"Gate {gameObject.name} detection mask: {detectionMask}");
}

    void Start()
    {
        electroButtonManager = FindAnyObjectByType<ElectroButtonManager>()?.gameObject;
        electroButtonManagerScript = electroButtonManager?.GetComponent<ElectroButtonManager>();

        // Checking döngüsünü başlat
        StartChecking();
    }

    void OnDestroy()
    {
        StopAllCoroutines();
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
        Debug.Log($"ElectroGate {gameObject.name} checking STARTED");
    }

    void StopChecking()
    {
        if (checkingCoroutine != null)
        {
            StopCoroutine(checkingCoroutine);
            checkingCoroutine = null;
        }

        isChecking = false;
        Debug.Log($"ElectroGate {gameObject.name} checking STOPPED");
    }

    public static bool anyGateInDamageMode = false; // Static değişken

    IEnumerator CheckingLoop()
    {
        while (isChecking)
        {
            bool buttonsActive = electroButtonManagerScript?.IsElectricityActive() ?? false;

            if (buttonsActive && !anyGateInDamageMode)
            {
                if (HasFacingGate())
                {
                    Debug.Log($"ElectroGate {gameObject.name} found facing gate - switching to DAMAGE mode");

                    anyGateInDamageMode = true; // Diğer gate'leri engelle
                    StopChecking();
                    StartDamage();
                    yield break;
                }
            }

            yield return new WaitForSeconds(checkingInterval);
        }
    }

    // === DAMAGE DÖNGÜSÜ ===
    void StartDamage()
    {
        if (damageCoroutine != null)
        {
            StopCoroutine(damageCoroutine);
        }

        isElectricityActive = true;
        damageCoroutine = StartCoroutine(DamageLoop());
        Debug.Log($"ElectroGate {gameObject.name} damage STARTED");
    }

    void StopDamage()
    {
        if (damageCoroutine != null)
        {
            StopCoroutine(damageCoroutine);
            damageCoroutine = null;
        }

        isElectricityActive = false;
        Debug.Log($"ElectroGate {gameObject.name} damage STOPPED");
    }

    IEnumerator DamageLoop()
    {
        WaitForSeconds wait = new WaitForSeconds(damageCheckInterval);

        while (isElectricityActive)
        {
            // 1. Sadece button kontrol et
            bool buttonsActive = electroButtonManagerScript?.IsElectricityActive() ?? false;

            if (!buttonsActive)
            {
                Debug.Log($"ElectroGate {gameObject.name} buttons deactivated - switching to CHECKING mode");
                StopDamage();
                StartChecking();
                yield break;
            }

            // 2. Hasar ver (facing check YOK!)
            ApplyDamage();

            yield return wait;
        }
    }

    // === YARDIMCI METODLAR ===
    bool HasFacingGate()
    {
        Vector2 myDirection = transform.up;
        Vector2 startPos = electricityPoint != null ? electricityPoint.position : transform.position;

        RaycastHit2D hit = Physics2D.Raycast(startPos, myDirection, maxDistance, detectionMask);

        // DEBUG EKLEYİN:
        Debug.Log($"Gate {gameObject.name} raycast direction: {myDirection}, hit: {hit.collider?.name}");
        Debug.DrawRay(startPos, myDirection * maxDistance, Color.red, 2f); // 2 saniye görünür

        if (hit.collider != null && hit.collider.CompareTag("ElectroGate") && hit.collider.gameObject != this)
        {
            ElectroGateNew otherGate = hit.collider.GetComponent<ElectroGateNew>();
            bool facing = IsFacingEachOther(otherGate);

            // DEBUG EKLEYİN:
            Debug.Log($"Gate {gameObject.name} -> {otherGate.gameObject.name}: facing check = {facing}");

            return facing;
        }
        else
        {
            // DEBUG EKLEYİN:
            Debug.Log($"Gate {gameObject.name} raycast missed or wrong tag");
        }

        return false;
    }

    bool IsFacingEachOther(ElectroGateNew other)
    {
        Vector2 myDirection = transform.up.normalized;
        Vector2 otherDirection = other.transform.up.normalized;

        Vector2 toOther = (other.transform.position - transform.position).normalized;
        Vector2 toMe = -toOther;

        // DEBUG EKLEYİN:
        Debug.Log($"Gate {gameObject.name} direction: {myDirection}, to other: {toOther}");
        Debug.Log($"Other gate {other.gameObject.name} direction: {otherDirection}, to me: {toMe}");

        // Tolerance ile dot product kullan
        float tolerance = 0.3f; // Tolerance'ı artırdım
        bool iLookAtHim = Vector2.Dot(myDirection, toOther) > 1f - tolerance;
        bool heLooksAtMe = Vector2.Dot(otherDirection, toMe) > 1f - tolerance;

        // DEBUG EKLEYİN:
        float dot1 = Vector2.Dot(myDirection, toOther);
        float dot2 = Vector2.Dot(otherDirection, toMe);
        Debug.Log($"Dot products: I->Him = {dot1:F3}, He->Me = {dot2:F3}");
        Debug.Log($"Facing checks: I look at him = {iLookAtHim}, He looks at me = {heLooksAtMe}");

        bool areFacingEachOther = iLookAtHim && heLooksAtMe;
        return areFacingEachOther;
    }

    void ApplyDamage()
    {
        Vector2 startPos = electricityPoint != null ? electricityPoint.position : transform.position;
        RaycastHit2D hit = Physics2D.Raycast(startPos, transform.up, maxDistance, detectionMask);

        if (hit.collider != null)
        {
            Health health = hit.collider.GetComponent<Health>();
            if (health != null)
            {
                health.TakeDamage(2);
            }

            BaseBullet bullet = hit.collider.GetComponent<BaseBullet>();
            if (bullet != null)
            {
                Destroy(bullet.gameObject);
            }

            Hay hay = hit.collider.GetComponent<Hay>();
            if (hay != null)
            {
                Destroy(hay.gameObject);
            }
        }
    }

    // === EXTERNAL EVENTS (Diğer sistemler bunları çağıracak) ===

    // Decoy yapıldığında çağrılacak
    public void OnDecoyApplied()
    {
        Debug.Log($"ElectroGate {gameObject.name} received decoy event - restarting checking");
        RestartChecking();
    }

    // Gaia restore yapıldığında çağrılacak
    public void OnGaiaRestore()
    {
        Debug.Log($"ElectroGate {gameObject.name} received gaia restore event - restarting checking");
        RestartChecking();
    }

    // Bazooka ile vurulduğunda çağrılacak
    public void OnBazookaHit()
    {
        Debug.Log($"ElectroGate {gameObject.name} hit by bazooka - restarting checking");
        RestartChecking();
    }

    // Object spawn edildiğinde çağrılacak
    public void OnObjectSpawned()
    {
        Debug.Log($"ElectroGate {gameObject.name} detected object spawn - restarting checking");
        RestartChecking();
    }

    // Genel restart metodu
    void RestartChecking()
    {
        // Damage mode'daysa durdur
        if (isElectricityActive)
        {
            StopDamage();
        }

        // Checking mode'daysa durdur
        if (isChecking)
        {
            StopChecking();
        }

        // Yeniden başlat
        StartChecking();
    }

    // === GETTER'LAR ===
    public bool IsElectricityActive => isElectricityActive;
    public bool IsChecking => isChecking;
}