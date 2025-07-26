using UnityEngine;

public class HeartAnimator : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private Animator animator;
    [SerializeField] private SpriteRenderer heartSpriteRenderer;


    void Awake()
    {
        if (this.transform.localScale != Vector3.zero)
        {
            this.transform.localScale = Vector3.zero;
        }
        if (animator == null)
            animator = GetComponent<Animator>();
        if (heartSpriteRenderer == null)
            heartSpriteRenderer = GetComponent<SpriteRenderer>();
    }

    public void OnDamageTaken(int newHP, int damageAmount)
    {
        // 2 HP'den 1 HP'ye düştüyse damage animasyonu oynat sonra disable et
        if (newHP == 1)
        {
            animator.SetTrigger("Damage");
        }

    }

    public void OnHealed()
    {
        // Heal animasyonu tetikle
        animator.SetTrigger("Heal");
    }

    public void PlayDenyAnimation()
    {
        Debug.Log("Playing deny animation");
        // Deny animasyonu tetikle (max HP'de heal alırsa)
        animator.SetTrigger("Deny");
    }


    // Health.cs için gerekli
    public SpriteRenderer GetHeartSpriteRenderer()
    {
        return heartSpriteRenderer;
    }
}