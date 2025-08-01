﻿using UnityEngine;

public class MedicBullet : BaseBullet
{
    [Header("Medic Bullet Settings")]
    [SerializeField] private Color bulletColor = Color.green;
    [SerializeField] private int healAmount = 1;

    void Start()
    {
        // Medic mermisi görsel ayarları
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.color = bulletColor;
        }
    }

    protected override void DefaultCollisionBehavior(GameObject hitObject)
    {
        // Duvar'a çarparsa dur
        if (hitObject.GetComponent<Wall>() != null)
        {
            DestroyBullet();
            return;
        }

        if (hitObject.GetComponent<Hay>() != null)
        {
            Destroy(hitObject.gameObject);
            DestroyBullet();
            return;
        }

        // Karaktere çarparsa iyileştir
        Health health = hitObject.GetComponent<Health>();
        if (health != null)
        {
                // İyileştir
                health.Heal(healAmount);


            DestroyBullet();
            return;
        }
    }



}