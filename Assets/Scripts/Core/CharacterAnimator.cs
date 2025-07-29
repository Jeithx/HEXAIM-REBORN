using UnityEngine;

public class CharacterAnimator : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private Animator animator;


    void Awake()
    {
        if (animator == null)
            animator = GetComponent<Animator>();
    }



    public void OnRevive() 
    
    { 
        animator.SetTrigger("Revive");

    }

    public void OnDeath()
    {
        animator.SetTrigger("Death");
    }
}