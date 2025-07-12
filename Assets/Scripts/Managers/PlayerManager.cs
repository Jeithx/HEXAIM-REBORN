using System.Collections.Generic;
using UnityEngine;

public class PlayerManager : MonoBehaviour
{
    public static PlayerManager Instance { get; private set; }

    private List<PlayerController> allPlayers = new List<PlayerController>();
    private bool canAnyPlayerFire = true;
    private Camera playerCamera;


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

        playerCamera = Camera.main;

    }

    public void RegisterPlayer(PlayerController player)
    {
        if (!allPlayers.Contains(player))
        {
            allPlayers.Add(player);
        }
    }

    public void UnregisterPlayer(PlayerController player)
    {
        allPlayers.Remove(player);
    }

    // Tüm sistemler bu metodları çağıracak
    public void EnableFiring()
    {
        canAnyPlayerFire = true;
        foreach (var player in allPlayers)
        {
            player.EnableFiring();
        }
    }

    public void DisableFiring()
    {
        canAnyPlayerFire = false;
        foreach (var player in allPlayers)
        {
            player.DisableFiring();
        }
    }
    private void Update()
    {
        if (Input.GetMouseButtonDown(0) && canAnyPlayerFire)
        {
            // Mouse sol tuşuna basıldığında ateş et
            FireAllPlayers();
        }
    }

    void FireAllPlayers()
    {
        if (allPlayers.Count == 0) return;

        Vector3 mouseWorldPos = playerCamera.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPos.z = 0;

        bool anyPlayerFired = false;

        foreach (var player in allPlayers)
        {
            if (player != null && player.CanFire())
            {
                player.FireTowards(mouseWorldPos);
                anyPlayerFired = true;
            }
        }

        if(anyPlayerFired)
        {
            OnAnyPlayerFired();
        }
    }

    public void OnAnyPlayerFired()
    {


        if (TurnManager.Instance != null)
        {
            TurnManager.Instance.StartTurn();
        }

        // Sadece bir kez GameManager'a bildir
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnPlayerFired();
        }
    }

    // Health check için
    public bool AreAllPlayersDead()
    {
        if (allPlayers.Count == 0) return false; // Player yoksa ölü sayılmaz

        foreach (var player in allPlayers)
        {
            if (player != null)
            {
                Health health = player.GetComponent<Health>();
                if (health == null || !health.IsDead)
                {
                    // En az bir player canlı bulundu
                    return false;
                }
            }
        }

        // Tüm playerlar ölü
        return true;
    }
    public int GetAlivePlayerCount()
    {
        int aliveCount = 0;
        foreach (var player in allPlayers)
        {
            if (player != null)
            {
                Health health = player.GetComponent<Health>();
                if (health != null && !health.IsDead)
                {
                    aliveCount++;
                }
            }
        }
        return aliveCount;
    }

    // Debug için
    public int GetTotalPlayerCount()
    {
        return allPlayers.Count;
    }
    // Public getter
    public List<PlayerController> GetAllPlayers() => new List<PlayerController>(allPlayers);
}