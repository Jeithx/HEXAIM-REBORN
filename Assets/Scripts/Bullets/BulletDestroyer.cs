using UnityEngine;

public class BulletDestroyer : MonoBehaviour
{
    [SerializeField] private float destroyTime = 5f;

    void Start()
    {
        Destroy(gameObject, destroyTime);
    }
}