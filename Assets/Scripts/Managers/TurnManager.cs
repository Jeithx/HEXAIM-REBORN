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
        // Sahnedeki tüm mermiler durana kadar bekle
        while (AreBulletsActive())
        {
            yield return new WaitForSeconds(0.1f); // 0.1 saniye bekle, tekrar kontrol et
        }

        // Tüm mermiler durdu
        EndTurn();
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