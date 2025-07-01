﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PiercingBullet : BaseBullet
{
    [Header("Piercing Bullet Settings")]
    [SerializeField] private Color bulletColor = Color.yellow;

    void Start()
    {
        // Piercing mermi görsel ayarları
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.color = bulletColor;
        }
    }

    protected override void DefaultCollisionBehavior(GameObject hitObject)
    {
        // Duvar'a çarparsa dur (sadece duvarlar durdurur)
        if (hitObject.GetComponent<Wall>() != null)
        {
            DestroyBullet();
            return;
        }

        // Karaktere çarparsa hasar ver AMA YOK OLMA!
        Health health = hitObject.GetComponent<Health>();
        if (health != null)
        {
            health.TakeDamage(damage);
            Debug.Log($"PiercingBullet: Hit {hitObject.name} but continuing through!");

            // Mermi yok olmaz, yoluna devam eder!
            // DestroyBullet() çağrılmaz
            return;
        }
    }

    //protected override void OnTriggerEnter2D(Collider2D other)
    //{
    //    // Piercing bullet için trigger collision kullan
    //    // Böylece fiziksel durdurucu etki olmaz
    //    HandleCollision(other.gameObject);
    //}

    //protected override void OnCollisionEnter2D(Collision2D collision)
    //{
    //    // Normal collision'ı da destekle ama genelde trigger kullanılır
    //    HandleCollision(collision.gameObject);
    //}
}