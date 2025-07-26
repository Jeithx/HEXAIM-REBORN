using UnityEngine;

public class CharacterAnimator : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private Animator animator;
    [SerializeField] private SpriteRenderer shineDumpSpriteRenderer;


    void Awake()
    {
        if (animator == null)
            animator = GetComponent<Animator>();
        if (shineDumpSpriteRenderer == null)
            shineDumpSpriteRenderer = GetComponent<SpriteRenderer>();
    }



    public void OnRevive() 
    
    { 
        animator.SetTrigger("Revive");

    }






    // Health.cs için gerekli
    public SpriteRenderer GetHeartSpriteRenderer()
    {
        return shineDumpSpriteRenderer;
    }
}