using UnityEngine;

/// <summary>
/// Tüm düşman türleri bu interface'i implement etmelidir.
/// GameManager bu interface'i kullanarak düşmanları takip eder.
/// </summary>
public interface IEnemy
{
    /// <summary>
    /// Bu düşman hala yaşıyor mu?
    /// </summary>
    bool IsAlive { get; }

    /// <summary>
    /// Düşman öldüğünde tetiklenen event
    /// GameManager bunu dinleyecek
    /// </summary>
    System.Action<IEnemy> OnEnemyDeath { get; set; }
}