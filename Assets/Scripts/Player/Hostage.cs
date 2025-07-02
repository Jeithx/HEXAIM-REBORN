using UnityEngine;

public class Hostage : MonoBehaviour
{
    [Header("Hostage Settings")]
    [SerializeField] private Color hostageColor = Color.cyan;

    [Header("Death Tracking")]
    [SerializeField] private int deathTurn = -1; // Hangi turda öldü (-1 = hiç ölmedi)

    private Health health;

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
        // Hostage görsel ayarları
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.color = hostageColor;
        }

        // Health event'lerini dinle
        if (health != null)
        {
            health.OnDeath += OnHostageDeath;
            health.OnRevive += OnHostageRevive;
        }
    }

    void OnDestroy()
    {
        if (health != null)
        {
            health.OnDeath -= OnHostageDeath;
            health.OnRevive -= OnHostageRevive;
        }
    }

    void OnHostageDeath()
    {
        // Öldüğü turu kaydet
        if (GameManager.Instance != null)
        {
            deathTurn = GameManager.Instance.CurrentTurn;
        }

        Debug.LogWarning($"⚠️ HOSTAGE {gameObject.name} DIED in turn {deathTurn}! ⚠️");
    }

    void OnHostageRevive()
    {
        // Canlandığında ölüm turunu sıfırla
        deathTurn = -1;
        Debug.Log($"✅ Hostage {gameObject.name} was revived!");
    }

    // GameManager bu metodu çağıracak
    public bool IsDeadTooLong(int currentTurn)
    {
        // Eğer hiç ölmemişse problem yok
        if (deathTurn == -1 || !health.IsDead)
        {
            return false;
        }

        // Ölüm turu + 2 = son şans turu
        int deadlineTurn = deathTurn + 2;

        // Eğer mevcut tur >= deadline ise çok geç!
        bool tooLate = currentTurn >= deadlineTurn;

        if (tooLate)
        {
            Debug.LogError($"Hostage {gameObject.name} died in turn {deathTurn}, now turn {currentTurn} >= {deadlineTurn} - TOO LATE!");
        }

        return tooLate;
    }

    // Debug bilgisi
    public int DeathTurn => deathTurn;
}