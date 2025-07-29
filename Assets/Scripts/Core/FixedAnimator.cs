using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class FixedAnimator: MonoBehaviour
{

    [Header("Components")]
    private GameObject fixedPart;
    [SerializeField] private Animator animator;


    void Awake()
    {
        Transform[] allChildren = GetComponentsInChildren<Transform>();
        foreach (Transform child in allChildren)
        {
            if (child.CompareTag("Fixed"))
            {
                fixedPart = child.gameObject;
                break;
            }
        }
        if (animator == null)
        {
            animator=fixedPart.gameObject.GetComponent<Animator>();
        }
            
    }



    public void OnReviveFixed()

    {
        animator.SetTrigger("Revive");

    }

    public void OnDeathFixed()
    {
        animator.SetTrigger("Death");
    }
}

