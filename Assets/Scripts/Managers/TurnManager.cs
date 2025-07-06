using UnityEngine;
using System.Collections;

public class TurnManager : MonoBehaviour
{
    [Header("Turn Settings")]
    [SerializeField] private bool isTurnActive = false;

    public static TurnManager Instance { get; private set; }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        // Oyun başında player ateş edebilir
        isTurnActive = false;
    }

    // Player ateş ettiğinde çağır
    public void StartTurn()
    {
        if (isTurnActive) return; // Zaten tur aktifse dur

        isTurnActive = true;
        Debug.Log("Turn started - waiting for bullets to stop");

        // Player'ın ateş etmesini engelle
        if (PlayerController.Instance != null)
        {
            PlayerController.Instance.DisableFiring();
        }

        // Mermiler durana kadar bekle
        StartCoroutine(WaitForBulletsToStop());
    }

    IEnumerator WaitForBulletsToStop()
    {
        float waitTime = 0f;

        while (AreBulletsActive())
        {
            waitTime += 0.1f;
            yield return new WaitForSeconds(0.1f);

            // Her saniyede bir debug log at
            if (waitTime % 1f < 0.1f)
            {
                Debug.Log($"Waiting for bullets... Time: {waitTime:F1}s");
            }
        }

        Debug.Log($"All bullets stopped after {waitTime:F1} seconds");
        EndTurn();
    }

    bool AreBulletsActive()
    {
        // Tüm BaseBullet türevlerini bul
        BaseBullet[] bullets = FindObjectsOfType<BaseBullet>();

        if (bullets.Length > 0)
        {
            //Debug.Log($"Active bullets: {bullets.Length}");
            return true;
        }

        return false;
    }

    void EndTurn()
    {
        isTurnActive = false;
        Debug.Log("Turn ended - player can fire again");

        // Player tekrar ateş edebilir
        if (PlayerController.Instance != null)
        {
            PlayerController.Instance.EnableFiring();
        }
    }

    public bool IsTurnActive()
    {
        return isTurnActive;
    }
}