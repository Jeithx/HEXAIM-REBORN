﻿using UnityEngine;

public class Health : MonoBehaviour
{
    [Header("Health Settings")]
    [SerializeField] private int maxHp = 2;
    [SerializeField] private int startingHp = 1;
    [SerializeField] private bool isPermanentlyDead = false;

    [Header("Visual Settings")]
    [SerializeField] private float deadAlpha = 0.3f; // Ölünce ne kadar soluk görünecek (0-1 arası)

    private int currentHp;
    private bool isDead = false;
    private SpriteRenderer[] allSpriteRenderers; // Tüm sprite renderer'ları sakla
    private Color[] originalColors; // Orijinal renkleri sakla

    // Events
    public System.Action OnDeath;
    public System.Action OnRevive;
    public System.Action<int> OnHealthChanged;
    public System.Action<int> OnTakeDamage;

    private int aliveLayer;


    void Awake()
    {

        if (this.GetComponent<Gaia>() != null)
        {
            startingHp = 100;
        }
        aliveLayer = gameObject.layer;

        allSpriteRenderers = GetComponentsInChildren<SpriteRenderer>();
        originalColors = new Color[allSpriteRenderers.Length];

        // Orijinal renkleri kaydet
        for (int i = 0; i < allSpriteRenderers.Length; i++)
        {
            originalColors[i] = allSpriteRenderers[i].color;
        }
    }

    private void SetAllSpritesAlpha(float alpha)
    {
        for (int i = 0; i < allSpriteRenderers.Length; i++)
        {
            Color fadeColor = originalColors[i];
            fadeColor.a = alpha;
            allSpriteRenderers[i].color = fadeColor;
        }
    }

    private void RestoreOriginalColors()
    {
        for (int i = 0; i < allSpriteRenderers.Length; i++)
        {
            allSpriteRenderers[i].color = originalColors[i];
        }
    }

    void Start()
    {
        currentHp = startingHp;
        OnHealthChanged?.Invoke(currentHp);
    }

    public void TakeDamage(int damage)
    {
        if (isPermanentlyDead || isDead) return;

        currentHp -= damage;
        currentHp = Mathf.Max(currentHp, 0); // HP negatif olamaz

        OnHealthChanged?.Invoke(currentHp);
        //Debug.Log($"{gameObject.name} took {damage} damage. HP: {currentHp}/{maxHp}");
        OnTakeDamage?.Invoke(damage); // Event'i tetikle

        if (currentHp <= 0)
        {
            Die();
        }
    }

    public void Heal(int healAmount)
    {
        if (isPermanentlyDead) return;

        // Ölüyse canlandır
        if (isDead && currentHp <= 0)
        {
            Revive();
            return; // Revive zaten 1 HP veriyor, ekstra heal ekleme
        }

        // HP'yi artır ama cap'i geçme
        int oldHp = currentHp;
        currentHp = Mathf.Min(currentHp + healAmount, maxHp);

        if (currentHp != oldHp) // Gerçekten heal olduysa
        {
            OnHealthChanged?.Invoke(currentHp);
            Debug.Log($"{gameObject.name} healed {healAmount}. HP: {currentHp}/{maxHp}");
        }
    }

    public void Die()
    {
        if (isDead) return;

        isDead = true;
        currentHp = 0;
        OnDeath?.Invoke();
        //Debug.Log($"{gameObject.name} died!");

        SetAllSpritesAlpha(deadAlpha); // Tüm sprite'ları soluk yap

        gameObject.layer = LayerMask.NameToLayer("DeadCharacters");
        //Debug.Log($"{gameObject.name} moved to DeadCharacters layer");
    }

    public void Revive()
    {
        if (isPermanentlyDead || !isDead) return; // Zaten canlıysa revive etme

        isDead = false;
        currentHp = 1;
        OnRevive?.Invoke();
        OnHealthChanged?.Invoke(currentHp);
        Debug.Log($"{gameObject.name} revived!");

        // Tüm sprite'ları normale döndür
        RestoreOriginalColors();
        gameObject.layer = aliveLayer;


    }

    public void PermanentKill()
    {
        isPermanentlyDead = true;
        isDead = true;
        currentHp = 0;
        OnDeath?.Invoke();
        Debug.Log($"{gameObject.name} permanently killed!");

        // Kalıcı ölüm - objeyi yok et
        Destroy(gameObject);
    }

    // HP'yi belirli bir değere ayarla (debug/test için kullanışlı)
    public void SetHealth(int newHp)
    {
        if (isPermanentlyDead) return;

        int oldHp = currentHp;
        currentHp = Mathf.Clamp(newHp, 0, maxHp);

        if (currentHp != oldHp)
        {
            OnHealthChanged?.Invoke(currentHp);

            if (currentHp <= 0 && !isDead)
            {
                Die();
            }
            else if (currentHp > 0 && isDead)
            {
                Revive();
            }
        }
    }

    // Getter'lar
    public int CurrentHp => currentHp;
    public int MaxHp => maxHp;
    public bool IsDead => isDead;
    public bool IsPermanentlyDead => isPermanentlyDead;
    public bool IsAlive => !isDead && !isPermanentlyDead;

    // HP yüzdesi (UI için kullanışlı)
    public float HpPercentage => maxHp > 0 ? (float)currentHp / maxHp : 0f;
}