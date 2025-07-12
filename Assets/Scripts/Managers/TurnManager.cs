using UnityEngine;
using System.Collections;

public class TurnManager : MonoBehaviour
{
    [Header("Turn Settings")]
    [SerializeField] private bool isTurnActive = false;
    [SerializeField] private bool processingBumpers = false;

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
        if (PlayerManager.Instance != null)
        {
            PlayerManager.Instance.DisableFiring();
        }

        // Mermiler durana kadar bekle
        StartCoroutine(WaitForBulletsToStop());
    }

    IEnumerator WaitForBulletsToStop()
    {
        // Sahnedeki tüm mermiler durana kadar bekle
        while (AreBulletsActive())
        {
            yield return new WaitForSeconds(0.1f);
        }

        // Mermiler durdu, şimdi Bumper'ları kontrol et
        yield return StartCoroutine(ProcessBumperShots());

        // Her şey bitti
        EndTurn();
    }

    IEnumerator ProcessBumperShots()
    {
        processingBumpers = true;
        Debug.Log("=== Processing Bumper shots ===");

        // Infinite loop protection
        int maxIterations = 20;
        int iteration = 0;

        while (iteration < maxIterations)
        {
            iteration++;

            // Ateş edecek Bumper'ları bul
            Bumper[] allBumpers = FindObjectsOfType<Bumper>();
            bool anyBumperFired = false;

            foreach (Bumper bumper in allBumpers)
            {
                if (bumper.HasPendingShot())
                {
                    bumper.ExecutePendingShot();
                    anyBumperFired = true;
                }
            }

            // Hiçbir Bumper ateş etmediyse döngüden çık
            if (!anyBumperFired)
            {
                Debug.Log("No more Bumpers to fire - chain reaction ended");
                break;
            }

            Debug.Log($"Bumper iteration {iteration} - waiting for new bullets to stop");

            // Yeni ateş edilen mermiler durana kadar bekle
            while (AreBulletsActive())
            {
                yield return new WaitForSeconds(0.1f);
            }

            // Kısa bir extra bekleme - robot chain reaction'ları için
            yield return new WaitForSeconds(0.1f);

            Debug.Log($"Bumper iteration {iteration} completed");
        }

        if (iteration >= maxIterations)
        {
            Debug.LogWarning("Bumper chain reaction hit max iterations - forcing end");
        }

        processingBumpers = false;
        Debug.Log("=== Bumper processing completed ===");
    }

    bool AreBulletsActive()
    {
        // Sahnedeki tüm BaseBullet türevlerini say
        BaseBullet[] bullets = FindObjectsOfType<BaseBullet>();
        return bullets.Length > 0;
    }

    void EndTurn()
    {
        isTurnActive = false;
        Debug.Log("Turn ended - player can fire again");

        // PlayerController.Instance yerine PlayerManager kullan
        if (PlayerManager.Instance != null)
        {
            PlayerManager.Instance.EnableFiring();
        }

        // GameManager'a turn bittiğini bildir
        if (GameManager.Instance != null)
        {
            StartCoroutine(NotifyGameManagerAfterDelay());
        }
    }

    IEnumerator NotifyGameManagerAfterDelay()
    {
        // Kısa bir bekleme - diğer sistemlerin settle olması için
        yield return new WaitForSeconds(0.1f);

        // Şimdi GameManager turn bitti diye kontrol edebilir
        Debug.Log("Notifying GameManager - turn truly ended");
    }

    public bool IsTurnActive()
    {
        return isTurnActive || processingBumpers;
    }
    public bool IsProcessingBumpers()
    {
        return processingBumpers;
    }
}