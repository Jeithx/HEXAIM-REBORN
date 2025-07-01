using UnityEngine;

public class Wall : MonoBehaviour
{
    // Sadece Bazooka mermisi tarafından yok edilebilir
    public void DestroyByBazooka()
    {
        Destroy(gameObject);
    }
}