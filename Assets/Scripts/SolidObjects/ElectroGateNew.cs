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

    private bool isActive = false;

    private bool isChecking = true;

    private GameObject electroButtonManager;
    private ElectroButtonManager electroButtonManagerScript;

    private void Awake()
    {
        if (detectionMask == 0)
        {
            detectionMask = ~0; // Tüm layerlar
            detectionMask &= ~(1 << LayerMask.NameToLayer("Hexes"));
            detectionMask &= ~(1 << LayerMask.NameToLayer("UI"));
        }
    }

    void Start()
    {
        electroButtonManager = FindAnyObjectByType<ElectroButtonManager>()?.gameObject;
        electroButtonManagerScript = electroButtonManager?.GetComponent<ElectroButtonManager>();

        isChecking = true;
    }

    // Update is called once per frame
    void Update()
    {
        if (isChecking && electroButtonManagerScript.ShouldCheckConnections())
        {
            Debug.Log("Checking for binding in " + gameObject.name);
            CheckForBinding();
        }
    }

    void CheckForBinding()
    {
        Vector2 myDirection = transform.up;
        Vector2 startPos = electricityPoint != null ? electricityPoint.position : transform.position;

        RaycastHit2D hit = Physics2D.Raycast(startPos, myDirection, maxDistance, detectionMask);
        Debug.DrawRay(startPos, myDirection * maxDistance, Color.red);
        Debug.Log($"Raycast hit? {hit.collider != null}" +"RaycastObjType= " + hit.collider.name);

        if (hit.collider != null && hit.collider.CompareTag("ElectroGate") && hit.collider.gameObject != this)
        {
           ElectroGateNew otherGate = hit.collider.GetComponent<ElectroGateNew>();
            // Karşılıklı bakıyor mu kontrol et
            if (IsFacingEachOther(otherGate))
            {
                StartCoroutine(DamageRoutine());
                
                return;
            }
        }
    }

    bool IsFacingEachOther(ElectroGateNew other)
    {
        Vector2 myDirection = transform.up.normalized;
        Vector2 otherDirection = other.transform.up.normalized;

        Vector2 toOther = (other.transform.position - transform.position).normalized;
        Vector2 toMe = -toOther;

        // İkisi de birebir karşılıklı mı bakıyor?
        bool iLookAtHim = myDirection == toOther;
        bool heLooksAtMe = otherDirection == toMe;

        bool areFacingEachOther;
        areFacingEachOther = iLookAtHim && heLooksAtMe;
        Debug.Log("Burada yazacak " +areFacingEachOther);
        return areFacingEachOther;
    }

    IEnumerator DamageRoutine()
    {
        Debug.Log("Damage routine started for " + gameObject.name);
        isChecking = false; // Hasar kontrolü başlatıldıktan sonra durdur
        WaitForSeconds wait = new WaitForSeconds(damageCheckInterval);

        while (isActive && isChecking)
        {
            // Elektrik çıkış noktasından raycast at
            Vector2 startPos = electricityPoint != null ? electricityPoint.position : transform.position;
            RaycastHit2D hit = Physics2D.Raycast(startPos, transform.up, detectionMask);

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

            yield return wait;
        }

    }
}
