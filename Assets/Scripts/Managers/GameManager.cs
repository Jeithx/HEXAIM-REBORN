using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class GameManager : MonoBehaviour
{
    [Header("Game State")]
    [SerializeField] private bool gameEnded = false;
    [SerializeField] private bool levelCompleted = false;
    [SerializeField] private int currentTurn = 0;

    [Header("Level Settings")]
    [SerializeField] private int availableBullets = 10;
    [SerializeField] private int usedBullets = 0;

    [Header("Enemy Tracking")]
    private List<IEnemy> enemies = new List<IEnemy>();
    private int totalEnemies = 0;
    private int aliveEnemies = 0;

    public static GameManager Instance { get; private set; }

    // Events
    public System.Action OnLevelWon;
    public System.Action OnLevelLost;
    public System.Action<int> OnBulletsChanged;
    public System.Action<int> OnTurnChanged;

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
        StartCoroutine(DelayedStart());
    }

    IEnumerator DelayedStart()
    {
        // Diğer sistemlerin başlamasını bekle
        yield return new WaitForSeconds(0.1f);

        OnBulletsChanged?.Invoke(availableBullets);
        OnTurnChanged?.Invoke(currentTurn);

        Debug.Log($"Game started - Turn: {currentTurn}, Bullets: {availableBullets}, Enemies: {aliveEnemies}");
        CheckGameState();
    }

    // Düşman kayıt sistemi
    public void RegisterEnemy(IEnemy enemy)
    {
        if (!enemies.Contains(enemy))
        {
            enemies.Add(enemy);
            aliveEnemies++;
            totalEnemies++;

            // Düşman ölüm event'ini dinle
            enemy.OnEnemyDeath += OnEnemyKilled;

            Debug.Log($"Enemy registered. Total: {totalEnemies}, Alive: {aliveEnemies}");
        }
        else
        {
            Debug.LogWarning($"⚠️ Enemy {enemy} already registered!"); // EKLE
        }
    }

    public void UnregisterEnemy(IEnemy enemy)
    {
        if (enemies.Contains(enemy))
        {
            enemies.Remove(enemy);

            // Eğer düşman hala yaşıyorsa alive count'u azalt
            if (enemy.IsAlive)
            {
                aliveEnemies--;
            }

            // Event listener'ı temizle
            enemy.OnEnemyDeath -= OnEnemyKilled;

            Debug.Log($"Enemy unregistered. Remaining alive: {aliveEnemies}");
        }
    }

    // Düşman öldüğünde çağrılır
    void OnEnemyKilled(IEnemy enemy)
    {
        aliveEnemies--;
        Debug.Log($"Enemy killed! Remaining alive: {aliveEnemies}");

        // Enemy listesinden çıkar (ölü olduğu için)
        enemies.Remove(enemy);

        // Turn sonunda kontrol edilecek
    }

    // Player ateş ettiğinde çağrılacak
    public void OnPlayerFired()
    {
        if (gameEnded) return;

        usedBullets++;
        int remaining = availableBullets - usedBullets;
        OnBulletsChanged?.Invoke(remaining);

        Debug.Log($"Bullets used: {usedBullets}/{availableBullets}");

        // Turn bittiğinde kontrol et
        if (TurnManager.Instance != null)
        {
            StartCoroutine(CheckAfterTurn());
        }
    }

    IEnumerator CheckAfterTurn()
    {
        // Turn bitene kadar bekle
        while (TurnManager.Instance.IsTurnActive())
        {
            yield return new WaitForSeconds(0.1f);
        }

        // Turn bitti, turn sayısını artır
        currentTurn++;
        OnTurnChanged?.Invoke(currentTurn);

        Debug.Log($"=== TURN {currentTurn} ENDED ===");

        // Oyun durumunu kontrol et
        CheckGameState();
    }

    void CheckGameState()
    {
        if (gameEnded) return;

        Debug.Log($"Checking game state - Turn: {currentTurn}, Alive enemies: {aliveEnemies}");

        // Önce LOSE koşullarını kontrol et (kritik)
        if (CheckLoseCondition())
        {
            LoseLevel();
            return;
        }

        // Sonra WIN koşulunu kontrol et
        if (CheckWinCondition())
        {
            WinLevel();
            return;
        }

        Debug.Log("Game continues...");
    }

    bool CheckWinCondition()
    {
        // 1. Tüm düşmanlar öldü mü?
        if (aliveEnemies > 0)
        {
            Debug.Log($"WIN CHECK: Still {aliveEnemies} enemies alive");
            return false;
        }

        // 2. Player yaşıyor mu?
        if (PlayerController.Instance != null)
        {
            Health playerHealth = PlayerController.Instance.GetComponent<Health>();
            if (playerHealth != null && playerHealth.IsDead)
            {
                Debug.Log("WIN CHECK: Player is dead");
                return false;
            }
        }

        // 3. Hostage'lar yaşıyor mu?
        Hostage[] hostages = FindObjectsOfType<Hostage>();
        foreach (Hostage hostage in hostages)
        {
            Health hostageHealth = hostage.GetComponent<Health>();
            if (hostageHealth != null && hostageHealth.IsDead)
            {
                Debug.Log($"WIN CHECK: Hostage {hostage.name} is dead");
                return false;
            }
        }

        Debug.Log("WIN CONDITION MET: All enemies dead, player and hostages alive!");
        return true;
    }

    bool CheckLoseCondition()
    {
        // 1. Mermi bitti mi?
        if (usedBullets >= availableBullets)
        {
            Debug.Log("LOSE: No bullets left!");
            return true;
        }

        // 2. Player öldü mü?
        if (PlayerController.Instance != null)
        {
            Health playerHealth = PlayerController.Instance.GetComponent<Health>();
            if (playerHealth != null && playerHealth.IsDead)
            {
                Debug.Log("LOSE: Player died!");
                return true;
            }
        }

        // 3. Hostage ölü mü ve canlandırılma süresi geçti mi?
        Hostage[] hostages = FindObjectsOfType<Hostage>();
        foreach (Hostage hostage in hostages)
        {
            if (hostage.IsDeadTooLong(currentTurn))
            {
                Debug.Log($"LOSE: Hostage {hostage.name} dead too long!");
                return true;
            }
        }

        return false;
    }

    void WinLevel()
    {
        if (gameEnded) return;

        gameEnded = true;
        levelCompleted = true;

        Debug.Log("🎉 LEVEL WON! 🎉");
        OnLevelWon?.Invoke();

        StartCoroutine(ShowWinMessage());
    }

    void LoseLevel()
    {
        if (gameEnded) return;

        gameEnded = true;
        levelCompleted = false;

        Debug.Log("💀 LEVEL LOST! 💀");
        OnLevelLost?.Invoke();

        StartCoroutine(ShowLoseMessage());
    }

    IEnumerator ShowWinMessage()
    {
        yield return new WaitForSeconds(1f);
        Debug.Log("Press R to restart or continue...");
    }

    IEnumerator ShowLoseMessage()
    {
        yield return new WaitForSeconds(1f);
        Debug.Log("Press R to restart level...");
    }

    // Getter'lar
    public bool IsGameEnded => gameEnded;
    public bool IsLevelCompleted => levelCompleted;
    public int RemainingBullets => availableBullets - usedBullets;
    public int TotalBullets => availableBullets;
    public int CurrentTurn => currentTurn;
    public int AliveEnemies => aliveEnemies;

    // Level settings
    public void SetAvailableBullets(int bullets)
    {
        availableBullets = bullets;
        usedBullets = 0;
        OnBulletsChanged?.Invoke(RemainingBullets);
    }

    // Test metodları
    [ContextMenu("Force Win")]
    void ForceWin() { WinLevel(); }

    [ContextMenu("Force Lose")]
    void ForceLose() { LoseLevel(); }

    [ContextMenu("Show Stats")]
    void ShowStats()
    {
        Debug.Log($"=== GAME STATS ===");
        Debug.Log($"Turn: {currentTurn}");
        Debug.Log($"Bullets: {usedBullets}/{availableBullets}");
        Debug.Log($"Enemies: {aliveEnemies} alive / {totalEnemies} total");
        Debug.Log($"Game ended: {gameEnded}");
    }
}