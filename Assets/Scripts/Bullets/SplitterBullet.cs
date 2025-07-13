using UnityEngine;
using System.Collections.Generic;

public class SplitterBullet : BaseBullet
{

    [Header("Prefabs)")]
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private GameObject wallPrefab;
    //[SerializeField] private GameObject hayPrefab;
    //[SerializeField] private GameObject hostagePrefab;
    [SerializeField] private GameObject gaiaPrefab;

    // Enemy prefabs
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private GameObject enemyNoGunPrefab;
    [SerializeField] private GameObject hunterPrefab;
    [SerializeField] private GameObject baretta60Prefab;
    [SerializeField] private GameObject baretta120Prefab;
    [SerializeField] private GameObject baretta180Prefab;
    [SerializeField] private GameObject medicPrefab;
    [SerializeField] private GameObject superEnemyPrefab;
    [SerializeField] private GameObject riotPrefab;
    [SerializeField] private GameObject splitterPrefab;

    // Robot prefabs
    [SerializeField] private GameObject robotPrefab;
    [SerializeField] private GameObject bumperPrefab;

    protected override void DefaultCollisionBehavior(GameObject hitObject)
    {
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

        IRobot robot = hitObject.GetComponent<IRobot>();
        if (robot != null)
        {
            if (!robot.CanTakeDamageFrom(this))
            {
                robot.OnBulletHit(this);
                DestroyBullet();
                return;
            }
        }

        Health health = hitObject.GetComponent<Health>();
        if (health != null)
        {
            HandleSplitting(hitObject, health);


            DestroyBullet();
            return;
        }
    }

    void HandleSplitting(GameObject targetCharacter, Health health)
    {
        Debug.Log($"SplitterBullet: Attempting to split {targetCharacter.name}");

        Rigidbody2D bulletRb = GetComponent<Rigidbody2D>();
        Vector2 bulletDirection = bulletRb.velocity.normalized;
        float bulletAngle = Mathf.Atan2(bulletDirection.y, bulletDirection.x) * Mathf.Rad2Deg;

        Vector2 leftDirection = Quaternion.Euler(0, 0, 90) * bulletDirection;
        Vector2 rightDirection = Quaternion.Euler(0, 0, -90) * bulletDirection;

        Vector3 targetPos = targetCharacter.transform.position;

        Vector3 leftHexWorldPos = GetAdjacentHexPosition(targetPos, leftDirection);
        Vector3 rightHexWorldPos = GetAdjacentHexPosition(targetPos, rightDirection);

        bool leftHexAvailable = IsHexAvailable(leftHexWorldPos);
        bool rightHexAvailable = IsHexAvailable(rightHexWorldPos);

        Debug.Log($"Left hex ({leftHexWorldPos}) available: {leftHexAvailable}");
        Debug.Log($"Right hex ({rightHexWorldPos}) available: {rightHexAvailable}");

        if (leftHexAvailable && rightHexAvailable)
        {
            CreateClone(targetCharacter, leftHexWorldPos);
            CreateClone(targetCharacter, rightHexWorldPos);
            Debug.Log("Created 2 clones - original will die");
            Destroy(targetCharacter);
        }
        else if (leftHexAvailable || rightHexAvailable)
        {
            Vector3 clonePos = leftHexAvailable ? leftHexWorldPos : rightHexWorldPos;
            CreateClone(targetCharacter, clonePos);

            Debug.Log("Created 1 clone - original stays alive");
        }
        else
        {

            health.Heal(1);

            Debug.Log("No available hexes - original gets 2 HP");
        }
    }

    Vector3 GetAdjacentHexPosition(Vector3 centerPos, Vector2 direction)
    {
        // Yön vektörünü hex mesafesi kadar uzat
        float hexDistance = Hex.HEX_SIZE * 1.5f;
        Vector3 targetPos = centerPos + (Vector3)direction * hexDistance;

        // En yakın hex'e snap et
        if (GridManager.Instance != null)
        {
            Hex nearestHex = GridManager.Instance.GetNearestHex(targetPos);
            if (nearestHex != null)
            {
                return nearestHex.worldPosition;
            }
        }

        return targetPos;
    }

    bool IsHexAvailable(Vector3 hexWorldPos)
    {
        // O pozisyonda başka bir karakter var mı?
        Collider2D[] colliders = Physics2D.OverlapCircleAll(hexWorldPos, 0.3f);

        foreach (var collider in colliders)
        {
            GameObject obj = collider.gameObject;

            // Hex tile'ları ignore et
            if (obj.GetComponent<HexTile>() != null) continue;
            if (obj.GetComponent<BaseBullet>() != null) continue;


            // Herhangi bir engel varsa müsait değil
            if (obj.GetComponent<Health>() != null ||
                obj.GetComponent<Wall>() != null ||
                obj.GetComponent<IRobot>() != null)
            {
                return false;
            }
        }

        return true;
    }

    void CreateClone(GameObject original, Vector3 position)
    {
        // Orijinal karakterin tipini belirle ve doğru prefab'ı al
        GameObject prefab = GetPrefabForCharacter(original);

        if (prefab == null)
        {
            Debug.LogError($"Could not find prefab for {original.name}");
            return;
        }

        // Klonu oluştur
        GameObject clone = Instantiate(prefab, position, original.transform.rotation);
        clone.name = original.name + "_Clone";

        // Orijinal karakterin özelliklerini kopyala
        CopyCharacterProperties(original, clone);

        Debug.Log($"Created clone of {original.name} at {position}");
    }

    GameObject GetPrefabForCharacter(GameObject character)
    {

        // Karakter tipine göre prefab döndür
        if (character.GetComponent<PlayerController>() != null)
            return playerPrefab;

        if (character.GetComponent<Enemy>() != null)
            return enemyPrefab;

        if (character.GetComponent<EnemyNoGun>() != null)
            return enemyNoGunPrefab;

        if (character.GetComponent<Hunter>() != null)
            return hunterPrefab;

        if (character.GetComponent<Baretta60>() != null)
            return baretta60Prefab;

        if (character.GetComponent<Baretta120>() != null)
            return baretta120Prefab;

        if (character.GetComponent<Baretta180>() != null)
            return baretta180Prefab;

        if (character.GetComponent<Medic>() != null)
            return medicPrefab;

        if (character.GetComponent<SuperEnemy>() != null)
            return superEnemyPrefab;

        if (character.GetComponent<Riot>() != null)
            return riotPrefab;

        if (character.GetComponent<Splitter>() != null)
            return splitterPrefab;

        if (character.GetComponent<Robot>() != null)
            return robotPrefab;

        if (character.GetComponent<Bumper>() != null)
            return bumperPrefab;

        if (character.GetComponent<Gaia>() != null)
            return gaiaPrefab;

        return null;
    }

    T GetPrivateField<T>(object obj, string fieldName) where T : class
    {
        var field = obj.GetType().GetField(fieldName,
            System.Reflection.BindingFlags.NonPublic |
            System.Reflection.BindingFlags.Instance);
        return field?.GetValue(obj) as T;
    }

    void CopyCharacterProperties(GameObject original, GameObject clone)
    {
        // Health kopyala
        Health originalHealth = original.GetComponent<Health>();
        Health cloneHealth = clone.GetComponent<Health>();
        if (originalHealth != null && cloneHealth != null)
        {
            cloneHealth.SetHealth(originalHealth.CurrentHp);
        }

        // Enemy özellikleri
        Enemy originalEnemy = original.GetComponent<Enemy>();
        Enemy cloneEnemy = clone.GetComponent<Enemy>();
        if (originalEnemy != null && cloneEnemy != null)
        {
            cloneEnemy.hasHeadphones = originalEnemy.hasHeadphones;
        }

        // Diğer enemy tipleri için kulaklık durumu
        CopyHeadphoneState(original, clone);

        // Gaia için restore hex'leri kopyala
        Gaia originalGaia = original.GetComponent<Gaia>();
        Gaia cloneGaia = clone.GetComponent<Gaia>();
        if (originalGaia != null && cloneGaia != null)
        {
            cloneGaia.SetRestoreHexes(originalGaia.GetRestoreHexCoords());
        }
    }

    void CopyHeadphoneState(GameObject original, GameObject clone)
    {
        // Reflection ile hasHeadphones field'ını bul ve kopyala
        var originalType = original.GetType();
        var cloneType = clone.GetType();

        var components = original.GetComponents<MonoBehaviour>();
        foreach (var comp in components)
        {
            var field = comp.GetType().GetField("hasHeadphones",
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Public |
                System.Reflection.BindingFlags.Instance);

            if (field != null)
            {
                bool hasHeadphones = (bool)field.GetValue(comp);

                // Clone'da aynı component'i bul
                var cloneComp = clone.GetComponent(comp.GetType());
                if (cloneComp != null)
                {
                    field.SetValue(cloneComp, hasHeadphones);
                }
                break;
            }
        }
    }
}