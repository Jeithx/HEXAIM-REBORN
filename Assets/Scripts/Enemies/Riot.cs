using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Riot : MonoBehaviour,  IDecoyable, IEnemy

{

    // IEnemy implementation
    public bool IsAlive => health != null && !health.IsDead;
    public System.Action<IEnemy> OnEnemyDeath { get; set; }

    [Header("Decoy Settings")]
    [SerializeField] private bool hasHeadphones = true;
    [Header("Karakter’e takılacak Kulaklık Objesi")]
    [SerializeField] private GameObject headphoneObject;

    public bool CanBeDecoyed => hasHeadphones && !health.IsDead; // Decoy olabilmesi için hasHeadphones ve IsDead kontrolü
    public void OnDecoyStart() { Debug.Log($"{gameObject.name} decoy started"); }
    public void OnDecoyEnd() { Debug.Log($"{gameObject.name} decoy ended"); }

    private Health health;

    void OnValidate()
    {
        UpdateHeadphonesVisibility();
    }

    /// <summary>
    /// Kulaklık objesini hasHeadphones’a göre açar/kapatır.
    /// </summary>
    private void UpdateHeadphonesVisibility()
    {
        if (headphoneObject == null) return;
        headphoneObject.SetActive(!hasHeadphones);
    }

    // Start is called before the first frame update
    void Start()
    {
        UpdateHeadphonesVisibility();

        // Health event'lerini dinle
        if (health != null)
        {
            health.OnDeath += OnRiotDeath_Internal;
            health.OnRevive += OnEnemyRevive;
        }

        // GameManager'a kaydol
        if (GameManager.Instance != null)
        {
            //Debug.Log($"Registering enemy {gameObject.name}");
            GameManager.Instance.RegisterEnemy(this);
        }
    }
    private void Awake()
    {
        health = GetComponent<Health>();

        // Health yoksa ekle
        if (health == null)
        {
            health = gameObject.AddComponent<Health>();
        }
    }

    void OnEnemyRevive()
    {
        Debug.Log($"{gameObject.name} revived!");
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnEnemyRevived(this);
        }
    }
    void OnRiotDeath_Internal()
    {
        Debug.Log($"Riot {gameObject.name} died!");
        OnEnemyDeath?.Invoke(this);

    }

    void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.UnregisterEnemy(this);
        }
        // Event listener'ı temizle
        if (health != null)
        {
            health.OnDeath -= OnRiotDeath_Internal;
            health.OnRevive -= OnEnemyRevive;
        }
    }

}
