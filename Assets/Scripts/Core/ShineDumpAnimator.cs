using UnityEngine;

public class ShineDumpAnimator : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private Animator animator;
    [SerializeField] private SpriteRenderer shineDumpSpriteRenderer;


    void Awake()
    {
        if (this.transform.localScale != Vector3.zero)
        {
            this.transform.localScale = Vector3.zero;
        }
        if (animator == null)
            animator = GetComponent<Animator>();
        if (shineDumpSpriteRenderer == null)
            shineDumpSpriteRenderer = GetComponent<SpriteRenderer>();
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



    // Health.cs için gerekli
    public SpriteRenderer GetHeartSpriteRenderer()
    {
        return shineDumpSpriteRenderer;
    }
}