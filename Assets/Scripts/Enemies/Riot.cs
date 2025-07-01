using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Riot : MonoBehaviour,  IDecoyable

{
    [Header("Decoy Settings")]
    [SerializeField] private bool hasHeadphones = true; // Riot kulaklık kullanıyor
    [SerializeField] private Sprite headphoneSprite; // Kulaklıklı sprite
    public bool CanBeDecoyed => hasHeadphones && !health.IsDead; // Decoy olabilmesi için hasHeadphones ve IsDead kontrolü
    public void OnDecoyStart() { Debug.Log($"{gameObject.name} decoy started"); }
    public void OnDecoyEnd() { Debug.Log($"{gameObject.name} decoy ended"); }

    private Health health;

    void OnValidate()
    {
        if (Application.isPlaying)
        {
            UpdateSprite();
        }
    }
    void UpdateSprite()
    {
        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            if (hasHeadphones && headphoneSprite != null)
            {
                spriteRenderer.sprite = headphoneSprite;
            }
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        UpdateSprite();
    }

    private void Awake()
    {
        health = GetComponent<Health>();

        // Health yoksa ekle
        if (health == null)
        {
            health = gameObject.AddComponent<Health>();
        }
    }
}
