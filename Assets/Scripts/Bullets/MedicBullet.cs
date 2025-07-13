using UnityEngine;

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
            // GDD kuralı: HP zaten 2 ise mermi etki etmez
            if (health.CurrentHp >= health.MaxHp)
            {
                // Özel animasyon/efekt buraya eklenebilir
                Debug.Log($"MedicBullet: {hitObject.name} already at max HP! No effect.");
                ShowNoEffectAnimation();
            }
            else
            {
                // İyileştir
                health.Heal(healAmount);
                Debug.Log($"MedicBullet: Healed {hitObject.name} for {healAmount} HP");
            }

            DestroyBullet();
            return;
        }
    }

    void ShowNoEffectAnimation()
    {
        // GDD: "Bu durum özel bir animasyonla belirtilir"
        // Basit örnek - gerçek animasyon sistemi eklendiğinde değiştirilir
        Debug.Log("* No Effect Animation Played *");
    }

}