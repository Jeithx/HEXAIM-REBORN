using UnityEngine;

public class EnemyNoGun : MonoBehaviour, IEnemy
{
    // IEnemy implementation
    public bool IsAlive => health != null && !health.IsDead;
    public System.Action<IEnemy> OnEnemyDeath { get; set; }


    private Health health;

    protected void Start()
    {

        // GameManager'a kaydol
        if (GameManager.Instance != null)
        {
            //Debug.Log($"Registering enemy {gameObject.name}");
            GameManager.Instance.RegisterEnemy(this);
        }

        // Health event'lerini dinle
        if (health != null)
        {
            health.OnDeath += OnEnemyDeath_Internal;
            health.OnRevive += OnEnemyRevive; // EKLENEN!
        }

    }
    void Awake()
    {
        health = GetComponent<Health>();
        if (health == null)
        {
            health = gameObject.AddComponent<Health>();
        }
    }
    void OnDestroy()
    {
        // GameManager'dan çıkar
        if (GameManager.Instance != null)
        {
            GameManager.Instance.UnregisterEnemy(this);
        }

        // Event listener'ları temizle
        if (health != null)
        {
            health.OnDeath -= OnEnemyDeath_Internal;
            health.OnRevive -= OnEnemyRevive;
        }
    }
    void OnEnemyRevive()
    {
        Debug.Log($"Enemy {gameObject.name} revived!");

        // GameManager'a tekrar kaydol
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnEnemyRevived(this);
        }
    }

    void OnEnemyDeath_Internal()
    {
        Debug.Log($"Enemy {gameObject.name} died!");
        OnEnemyDeath?.Invoke(this); // GameManager'a bildir
    }

}