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
    private HashSet<IEnemy> registeredEnemies = new HashSet<IEnemy>(); // All enemies ever registered
    private HashSet<IEnemy> aliveEnemies = new HashSet<IEnemy>(); // Currently alive enemies
    private int totalEnemies = 0;

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

    void Update()
    {
        // Debug key to force game state check
        if (Input.GetKeyDown(KeyCode.G))
        {
            Debug.Log("Manual game state check triggered with G key");
            ForceGameStateCheck();
        }
    }

    IEnumerator DelayedStart()
    {
        // Wait for other systems to initialize
        yield return new WaitForSeconds(0.1f);

        OnBulletsChanged?.Invoke(availableBullets);
        OnTurnChanged?.Invoke(currentTurn);

        Debug.Log($"Game started - Turn: {currentTurn}, Bullets: {availableBullets}, Enemies: {aliveEnemies.Count}");
        CheckGameState();
    }

    // Enemy registration system
    public void RegisterEnemy(IEnemy enemy)
    {
        if (enemy == null) return;

        // Check if this enemy was ever registered before
        bool isNewEnemy = !registeredEnemies.Contains(enemy);

        if (isNewEnemy)
        {
            registeredEnemies.Add(enemy);
            totalEnemies++;

            // Subscribe to death event only once
            enemy.OnEnemyDeath += OnEnemyKilled;
            //Debug.Log($"New enemy registered: {enemy}. Total enemies: {totalEnemies}");
        }

        // Add to alive enemies if actually alive
        if (enemy.IsAlive && !aliveEnemies.Contains(enemy))
        {
            aliveEnemies.Add(enemy);
            Debug.Log($"Enemy added to alive list. Alive count: {aliveEnemies.Count}");
        }
        else if (!enemy.IsAlive && aliveEnemies.Contains(enemy))
        {
            // Safety check - remove from alive if somehow registered while dead
            aliveEnemies.Remove(enemy);
            Debug.LogWarning($"Dead enemy was in alive list - removed. Alive count: {aliveEnemies.Count}");
        }
    }

    public void UnregisterEnemy(IEnemy enemy)
    {
        if (enemy == null) return;

        // Remove from alive enemies if present
        if (aliveEnemies.Contains(enemy))
        {
            aliveEnemies.Remove(enemy);
            Debug.Log($"Enemy removed from alive list. Remaining alive: {aliveEnemies.Count}");
        }

        // Remove from registered enemies
        if (registeredEnemies.Contains(enemy))
        {
            registeredEnemies.Remove(enemy);

            // Unsubscribe from events
            enemy.OnEnemyDeath -= OnEnemyKilled;
            Debug.Log($"Enemy unregistered completely: {enemy}");
        }
    }

    // Enemy died
    void OnEnemyKilled(IEnemy enemy)
    {
        if (enemy == null) return;

        if (aliveEnemies.Contains(enemy))
        {
            aliveEnemies.Remove(enemy);
            Debug.Log($"Enemy killed! Remaining: {aliveEnemies.Count}");
        }
    }

    // Enemy revived (called by enemies when they revive)
    public void OnEnemyRevived(IEnemy enemy)
    {
        if (enemy == null) return;

        // Add back to alive enemies if not already there
        if (enemy.IsAlive && !aliveEnemies.Contains(enemy))
        {
            aliveEnemies.Add(enemy);
            Debug.Log($"Enemy revived and added to alive list! Alive count: {aliveEnemies.Count}");
        }
    }

    // Player fired
    public void OnPlayerFired()
    {
        if (gameEnded) return;

        usedBullets++;
        int remaining = availableBullets - usedBullets;
        OnBulletsChanged?.Invoke(remaining);

        Debug.Log($"Bullets used: {usedBullets}/{availableBullets}");

        if (TurnManager.Instance != null)
        {
            StartCoroutine(CheckAfterTurn());
        }
    }

    IEnumerator CheckAfterTurn()
    {
        // Turn bitene kadar bekle, hiçbir kontrol yapma
        while (TurnManager.Instance.IsTurnActive())
        {
            yield return new WaitForSeconds(0.1f);
        }

        // Extra güvenlik - Bumper chain reaction'ı tamamen bitsin
        yield return new WaitForSeconds(0.2f);

        // Turn bitti, şimdi turn sayısını artır
        currentTurn++;
        OnTurnChanged?.Invoke(currentTurn);

        Debug.Log($"=== TURN {currentTurn} ENDED ===");
        ShowStats();

        // SADECE BURADA win/lose kontrolü yap
        CheckGameState();
    }

    void CheckGameState()
    {
        if (gameEnded) return;

        Debug.Log($"Checking game state - Turn: {currentTurn}, Alive enemies: {aliveEnemies.Count}");

        // Check LOSE conditions first (critical)
        if (CheckLoseCondition())
        {
            LoseLevel();
            return;
        }

        // Then check WIN condition
        if (CheckWinCondition())
        {
            WinLevel();
            return;
        }

        Debug.Log("Game continues...");
    }

    bool CheckWinCondition()
    {
        Debug.Log($"=== CHECKING WIN CONDITION ===");

        // 1. All enemies dead?
        if (aliveEnemies.Count > 0)
        {
            Debug.Log($"WIN CHECK: Still {aliveEnemies.Count} enemies alive - NOT WON");
            return false;
        }
        else
        {
            Debug.Log("WIN CHECK: All enemies dead ✓");
        }

        // 2. Player alive?
        if (PlayerController.Instance != null)
        {
            Health playerHealth = PlayerController.Instance.GetComponent<Health>();
            if (playerHealth != null && playerHealth.IsDead)
            {
                Debug.Log("WIN CHECK: Player is dead - NOT WON");
                return false;
            }
            else
            {
                Debug.Log("WIN CHECK: Player is alive ✓");
            }
        }
        else
        {
            Debug.LogWarning("WIN CHECK: No player found!");
        }

        // 3. Hostages alive?
        Hostage[] hostages = FindObjectsOfType<Hostage>();
        if (hostages.Length == 0)
        {
            Debug.Log("WIN CHECK: No hostages in level ✓");
        }
        else
        {
            foreach (Hostage hostage in hostages)
            {
                Health hostageHealth = hostage.GetComponent<Health>();
                if (hostageHealth != null && hostageHealth.IsDead)
                {
                    Debug.Log($"WIN CHECK: Hostage {hostage.name} is dead - NOT WON");
                    return false;
                }
            }
            Debug.Log($"WIN CHECK: All {hostages.Length} hostages alive ✓");
        }

        Debug.Log("🎉 WIN CONDITION MET: All checks passed! 🎉");
        return true;
    }

    bool CheckLoseCondition()
    {
        // 1. No bullets left AND enemies still alive?
        if (usedBullets >= availableBullets && aliveEnemies.Count > 0)
        {
            Debug.Log("LOSE: No bullets left and enemies still alive!");
            return true;
        }

        // 2. Player dead?
        if (PlayerController.Instance != null)
        {
            Health playerHealth = PlayerController.Instance.GetComponent<Health>();
            if (playerHealth != null && playerHealth.IsDead)
            {
                Debug.Log("LOSE: Player died!");
                return true;
            }
        }

        // 3. Hostage dead too long?
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

    // Bu metodu tamamen sil
    // IEnumerator CheckForInstantWin() { ... }

    // Getters
    public bool IsGameEnded => gameEnded;
    public bool IsLevelCompleted => levelCompleted;
    public int RemainingBullets => availableBullets - usedBullets;
    public int TotalBullets => availableBullets;
    public int CurrentTurn => currentTurn;
    public int AliveEnemies => aliveEnemies.Count;

    // Level settings
    public void SetAvailableBullets(int bullets)
    {
        availableBullets = bullets;
        usedBullets = 0;
        OnBulletsChanged?.Invoke(RemainingBullets);
    }

    // Test methods
    [ContextMenu("Force Win")]
    void ForceWin() { WinLevel(); }

    [ContextMenu("Force Lose")]
    void ForceLose() { LoseLevel(); }

    [ContextMenu("Force Game State Check")]
    public void ForceGameStateCheck()
    {
        Debug.Log("=== FORCED GAME STATE CHECK ===");
        CheckGameState();
    }

    [ContextMenu("Show Stats")]
    public void ShowStats()
    {
        Debug.Log($"=== GAME STATS ===");
        //Debug.Log($"Turn: {currentTurn}");
        //Debug.Log($"Bullets: {usedBullets}/{availableBullets}");
        //Debug.Log($"Alive enemies: {aliveEnemies.Count}");
        //Debug.Log($"Registered enemies: {registeredEnemies.Count}");
        //Debug.Log($"Total enemies ever: {totalEnemies}");
        Debug.Log($"Game ended: {gameEnded}");

        // List alive enemies
        if (aliveEnemies.Count > 0)
        {
            Debug.Log("Alive enemies list:");
            foreach (var enemy in aliveEnemies)
            {
                Debug.Log($"  - {enemy} (IsAlive: {enemy.IsAlive})");
            }
        }
    }

    [ContextMenu("Validate Enemy Counts")]
    void ValidateEnemyCounts()
    {
        // Count actual alive enemies in scene
        int actualAliveCount = 0;
        foreach (var enemy in registeredEnemies)
        {
            if (enemy != null && enemy.IsAlive)
            {
                actualAliveCount++;
            }
        }

        Debug.Log($"=== VALIDATION ===");
        Debug.Log($"Tracked alive enemies: {aliveEnemies.Count}");
        Debug.Log($"Actual alive enemies: {actualAliveCount}");

        if (aliveEnemies.Count != actualAliveCount)
        {
            Debug.LogError("MISMATCH DETECTED! Fixing...");

            // Rebuild alive enemies set
            aliveEnemies.Clear();
            foreach (var enemy in registeredEnemies)
            {
                if (enemy != null && enemy.IsAlive)
                {
                    aliveEnemies.Add(enemy);
                }
            }

            Debug.Log($"Fixed. New alive count: {aliveEnemies.Count}");
        }
    }
}