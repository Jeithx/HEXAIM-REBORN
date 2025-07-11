using UnityEngine;
using System.Collections.Generic;
using System.Linq;

// Bir hex'in başlangıç durumunu saklar
[System.Serializable]
public class HexSnapshot
{
    public Vector2Int hexCoords;
    public List<ObjectSnapshot> objects = new List<ObjectSnapshot>();

    [System.Serializable]
    public class ObjectSnapshot
    {
        public string prefabName;
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 scale;
        public int health;
        public bool hasHeadphones;
        public string objectType; // Enemy, Wall, Hay, Robot, Player, Gaia, etc.
        public string specificType; // Enemy subtype: Baretta60, Hunter, etc.

        // Component states
        public Dictionary<string, object> componentData = new Dictionary<string, object>();
    }
}

public class GaiaManager : MonoBehaviour
{
    private Dictionary<Vector2Int, HexSnapshot> levelSnapshot = new Dictionary<Vector2Int, HexSnapshot>();

    public static GaiaManager Instance { get; private set; }

    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = true;
    [SerializeField] private Color snapshotGizmoColor = Color.cyan;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // GameManager'dan çağrılacak
    public void TakeLevelSnapshot()
    {
        levelSnapshot.Clear();
        Debug.Log("=== TAKING LEVEL SNAPSHOT ===");

        // Tüm hex'leri dolaş
        foreach (var hexTile in FindObjectsOfType<HexTile>())
        {
            var hexData = hexTile.HexData;
            Vector2Int coords = new Vector2Int(hexData.q, hexData.r);

            HexSnapshot snapshot = new HexSnapshot();
            snapshot.hexCoords = coords;

            // Bu hex'teki tüm objeleri bul
            Collider2D[] colliders = Physics2D.OverlapCircleAll(hexData.worldPosition, Hex.HEX_SIZE * 0.7f);

            foreach (var collider in colliders)
            {
                GameObject obj = collider.gameObject;

                // Hex tile'ın kendisini skip et
                if (obj.GetComponent<HexTile>() != null) continue;

                // Bullet'ları skip et
                if (obj.GetComponent<BaseBullet>() != null) continue;

                // Obje snapshot'ını oluştur
                var objSnapshot = CreateObjectSnapshot(obj);
                if (objSnapshot != null)
                {
                    snapshot.objects.Add(objSnapshot);
                }
            }

            levelSnapshot[coords] = snapshot;
        }

        Debug.Log($"Level snapshot taken: {levelSnapshot.Count} hexes recorded");
    }

    HexSnapshot.ObjectSnapshot CreateObjectSnapshot(GameObject obj)
    {
        var snapshot = new HexSnapshot.ObjectSnapshot();

        snapshot.position = obj.transform.position;
        snapshot.rotation = obj.transform.rotation;
        snapshot.scale = obj.transform.localScale;
        snapshot.prefabName = obj.name.Replace("(Clone)", "").Trim();

        // Object type'ı belirle
        if (obj.GetComponent<PlayerController>() != null)
        {
            snapshot.objectType = "Player";
        }
        else if (obj.GetComponent<IEnemy>() != null)
        {
            snapshot.objectType = "Enemy";
            snapshot.specificType = obj.GetType().Name;

            // Enemy interface'inden hasHeadphones'u almaya çalış
            var enemy = obj.GetComponent<Enemy>();
            if (enemy != null)
            {
                snapshot.hasHeadphones = enemy.hasHeadphones;
            }
            else
            {
                // SuperEnemy, Medic, Hunter gibi diğer tipler için
                var hasHeadphonesField = obj.GetType().GetField("hasHeadphones",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                if (hasHeadphonesField != null)
                {
                    snapshot.hasHeadphones = (bool)hasHeadphonesField.GetValue(obj);
                }
            }
        }
        else if (obj.GetComponent<Wall>() != null)
        {
            snapshot.objectType = "Wall";
        }
        else if (obj.GetComponent<Hay>() != null)
        {
            snapshot.objectType = "Hay";
        }
        else if (obj.GetComponent<IRobot>() != null)
        {
            snapshot.objectType = "Robot";
            snapshot.specificType = obj.GetType().Name;

            // Robot'un headphone durumu
            var robot = obj.GetComponent<BaseRobot>();
            if (robot != null)
            {
                snapshot.componentData["hasHeadphones"] = robot.GetType()
                    .GetField("hasHeadphones", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                    ?.GetValue(robot) ?? true;
            }
        }
        else if (obj.GetComponent<Hostage>() != null)
        {
            snapshot.objectType = "Hostage";
        }
        else if (obj.GetComponent<Gaia>() != null)
        {
            snapshot.objectType = "Gaia";

            // Gaia'nın restore hex'lerini de kaydet
            var gaia = obj.GetComponent<Gaia>();
            snapshot.componentData["restoreHexes"] = gaia.GetRestoreHexCoords();
        }
        else
        {
            // Bilinmeyen obje tipi
            Debug.LogWarning($"Unknown object type for snapshot: {obj.name}");
            return null;
        }

        // Health durumu
        var health = obj.GetComponent<Health>();
        if (health != null)
        {
            snapshot.health = health.CurrentHp;
        }

        Debug.Log($"Snapshot created for {snapshot.objectType} at {snapshot.position}");
        return snapshot;
    }

    public void RestoreHexes(List<Vector2Int> hexCoords, Vector3 gaiaPosition)
    {
        Debug.Log($"=== RESTORING {hexCoords.Count} HEXES ===");

        // Önce bu hex'lerdeki mevcut objeleri topla
        Dictionary<GameObject, bool> objectsToProcess = new Dictionary<GameObject, bool>();

        foreach (var coords in hexCoords)
        {
            Hex hex = GridManager.Instance.GetHexAt(coords.x, coords.y);
            if (hex == null) continue;

            // Bu hex'teki objeleri bul
            Collider2D[] colliders = Physics2D.OverlapCircleAll(hex.worldPosition, Hex.HEX_SIZE * 0.7f);
            foreach (var collider in colliders)
            {
                GameObject obj = collider.gameObject;
                if (obj.GetComponent<HexTile>() != null) continue;
                if (obj.GetComponent<BaseBullet>() != null) continue;

                objectsToProcess[obj] = false; // Henüz işlenmedi
            }
        }

        // Her hex için restore işlemi
        foreach (var coords in hexCoords)
        {
            if (!levelSnapshot.ContainsKey(coords))
            {
                Debug.LogWarning($"No snapshot found for hex {coords}");
                continue;
            }

            HexSnapshot snapshot = levelSnapshot[coords];

            // Bu hex'te olması gereken objeleri restore et
            foreach (var objSnapshot in snapshot.objects)
            {
                RestoreObject(objSnapshot, coords, objectsToProcess);
            }
        }

        // İşlenmemiş objeleri yok et (snapshot'ta olmayan objeler)
        foreach (var kvp in objectsToProcess)
        {
            if (!kvp.Value && kvp.Key != null)
            {
                Debug.Log($"Destroying object not in snapshot: {kvp.Key.name}");
                Destroy(kvp.Key);
            }
        }

        Debug.Log("=== RESTORE COMPLETE ===");

        // GameManager'a enemy sayısını validate ettir
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ValidateEnemyCounts();
        }
    }

    void RestoreObject(HexSnapshot.ObjectSnapshot snapshot, Vector2Int hexCoords, Dictionary<GameObject, bool> processedObjects)
    {
        // Bu pozisyonda zaten var olan objeyi bul
        GameObject existingObj = FindExistingObject(snapshot);

        if (existingObj != null)
        {
            // Obje var, pozisyon/rotation'ı güncelle
            Debug.Log($"Updating existing {snapshot.objectType} to original state");

            existingObj.transform.position = snapshot.position;
            existingObj.transform.rotation = snapshot.rotation;
            existingObj.transform.localScale = snapshot.scale;

            // Health'i restore et
            var health = existingObj.GetComponent<Health>();
            if (health != null)
            {
                // Ölüyse canlandır
                if (health.IsDead && snapshot.health > 0)
                {
                    health.Revive();
                    // Orijinal HP'yi ayarla
                    if (snapshot.health > 1)
                    {
                        health.SetHealth(snapshot.health);
                    }
                }
                else if (!health.IsDead)
                {
                    // Canlıysa sadece HP'yi güncelle
                    health.SetHealth(snapshot.health);
                }
            }

            // Component state'lerini restore et
            RestoreComponentStates(existingObj, snapshot);

            // ÖNEMLİ: İşlendi olarak işaretle
            if (processedObjects.ContainsKey(existingObj))
            {
                processedObjects[existingObj] = true;
            }
            else
            {
                // Eğer listede yoksa ekle ve işaretle
                processedObjects[existingObj] = true;
            }
        }
        else
        {
            // Obje yok, yeniden oluştur
            Debug.Log($"Creating new {snapshot.objectType} at {snapshot.position}");

            GameObject newObj = CreateObjectFromSnapshot(snapshot);
            if (newObj != null)
            {
                // İşlendi olarak işaretle
                processedObjects[newObj] = true;
            }
        }
    }

    GameObject FindExistingObject(HexSnapshot.ObjectSnapshot snapshot)
    {
        // Snapshot pozisyonunda aynı tipte obje var mı?
        Collider2D[] colliders = Physics2D.OverlapCircleAll(snapshot.position, 0.3f);

        foreach (var collider in colliders)
        {
            GameObject obj = collider.gameObject;

            // Tip kontrolü
            bool typeMatch = false;

            switch (snapshot.objectType)
            {
                case "Player":
                    typeMatch = obj.GetComponent<PlayerController>() != null;
                    break;
                case "Enemy":
                    // Enemy tipini ve specific type'ı kontrol et
                    if (obj.GetComponent<Enemy>() != null || obj.GetComponent<IEnemy>() != null)
                    {
                        // Specific type kontrolü - GetType().Name ile karşılaştır
                        typeMatch = obj.GetType().Name == snapshot.specificType;
                    }
                    break;
                case "Wall":
                    typeMatch = obj.GetComponent<Wall>() != null;
                    break;
                case "Hay":
                    typeMatch = obj.GetComponent<Hay>() != null;
                    break;
                case "Robot":
                    typeMatch = obj.GetComponent<IRobot>() != null && obj.GetType().Name == snapshot.specificType;
                    break;
                case "Hostage":
                    typeMatch = obj.GetComponent<Hostage>() != null;
                    break;
                case "Gaia":
                    typeMatch = obj.GetComponent<Gaia>() != null;
                    break;
            }

            if (typeMatch)
            {
                return obj;
            }
        }

        return null;
    }

    GameObject CreateObjectFromSnapshot(HexSnapshot.ObjectSnapshot snapshot)
    {
        GameObject newObj = null;

        // Object tipine göre oluştur
        switch (snapshot.objectType)
        {
            case "Player":
                // Player prefab'ını bul veya basit bir player oluştur
                newObj = new GameObject("Player");
                newObj.AddComponent<PlayerController>();
                newObj.AddComponent<Health>();
                // Basit görsel
                var playerSr = newObj.AddComponent<SpriteRenderer>();
                playerSr.color = Color.blue;
                playerSr.sprite = CreateSimpleSprite();
                playerSr.sortingOrder = 10;
                break;

            case "Enemy":
                newObj = CreateEnemyFromType(snapshot.specificType);
                break;

            case "Wall":
                newObj = new GameObject("Wall");
                newObj.AddComponent<Wall>();
                newObj.AddComponent<BoxCollider2D>();
                var wallSr = newObj.AddComponent<SpriteRenderer>();
                wallSr.color = Color.gray;
                wallSr.sprite = CreateSimpleSprite();
                break;

            case "Hay":
                newObj = new GameObject("Hay");
                newObj.AddComponent<Hay>();
                newObj.AddComponent<BoxCollider2D>();
                var haySr = newObj.AddComponent<SpriteRenderer>();
                haySr.color = new Color(0.9f, 0.8f, 0.4f);
                haySr.sprite = CreateSimpleSprite();
                break;

            case "Robot":
                newObj = CreateRobotFromType(snapshot.specificType);
                break;

            case "Hostage":
                newObj = new GameObject("Hostage");
                newObj.AddComponent<Hostage>();
                newObj.AddComponent<Health>();
                var hostageSr = newObj.AddComponent<SpriteRenderer>();
                hostageSr.color = Color.green;
                hostageSr.sprite = CreateSimpleSprite();
                break;

            case "Gaia":
                newObj = new GameObject("Gaia");
                newObj.AddComponent<Gaia>();
                newObj.AddComponent<Health>();
                var gaiaSr = newObj.AddComponent<SpriteRenderer>();
                gaiaSr.color = Color.cyan;
                gaiaSr.sprite = CreateSimpleSprite();
                break;
        }

        if (newObj != null)
        {
            newObj.transform.position = snapshot.position;
            newObj.transform.rotation = snapshot.rotation;
            newObj.transform.localScale = snapshot.scale;

            // Component state'lerini restore et
            RestoreComponentStates(newObj, snapshot);

            // Enemy ise GameManager'a hemen register et (Start() beklemeden)
            if (snapshot.objectType == "Enemy")
            {
                var enemy = newObj.GetComponent<IEnemy>();
                if (enemy != null && GameManager.Instance != null)
                {
                    Debug.Log($"Manually registering restored enemy: {newObj.name}");
                    GameManager.Instance.RegisterEnemy(enemy);
                }
            }
        }

        return newObj;
    }

    GameObject CreateEnemyFromType(string typeName)
    {
        GameObject enemy = new GameObject(typeName);

        // Type'a göre component ekle
        switch (typeName)
        {
            case "Enemy":
                enemy.AddComponent<Enemy>();
                break;
            case "EnemyNoGun":
                enemy.AddComponent<EnemyNoGun>();
                break;
            case "Baretta60":
                enemy.AddComponent<Baretta60>();
                break;
            case "Baretta120":
                enemy.AddComponent<Baretta120>();
                break;
            case "Baretta180":
                enemy.AddComponent<Baretta180>();
                break;
            case "Hunter":
                enemy.AddComponent<Hunter>();
                break;
            case "Medic":
                enemy.AddComponent<Medic>();
                break;
            case "SuperEnemy":
                enemy.AddComponent<SuperEnemy>();
                break;
            case "Riot":
                enemy.AddComponent<Riot>();
                break;
            default:
                enemy.AddComponent<Enemy>();
                break;
        }

        // Ortak component'ler
        enemy.AddComponent<Health>();
        enemy.AddComponent<CircleCollider2D>();
        var sr = enemy.AddComponent<SpriteRenderer>();
        sr.color = Color.red;
        sr.sprite = CreateSimpleSprite();
        sr.sortingOrder = 5;

        return enemy;
    }

    GameObject CreateRobotFromType(string typeName)
    {
        GameObject robot = new GameObject(typeName);

        switch (typeName)
        {
            case "Robot":
                robot.AddComponent<Robot>();
                break;
            case "Bumper":
                robot.AddComponent<Bumper>();
                break;
            default:
                robot.AddComponent<Robot>();
                break;
        }

        // Robot görseli
        robot.AddComponent<CircleCollider2D>();
        var sr = robot.AddComponent<SpriteRenderer>();
        sr.color = Color.magenta;
        sr.sprite = CreateSimpleSprite();
        sr.sortingOrder = 5;

        return robot;
    }

    Sprite CreateSimpleSprite()
    {
        // Basit kare sprite
        Texture2D tex = new Texture2D(32, 32);
        Color[] pixels = new Color[32 * 32];
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = Color.white;
        }
        tex.SetPixels(pixels);
        tex.Apply();

        return Sprite.Create(tex, new Rect(0, 0, 32, 32), Vector2.one * 0.5f, 32);
    }

    void RestoreComponentStates(GameObject obj, HexSnapshot.ObjectSnapshot snapshot)
    {
        // Health restore
        var health = obj.GetComponent<Health>();
        if (health != null && snapshot.health > 0)
        {
            health.SetHealth(snapshot.health);
        }

        // Enemy specific
        if (snapshot.objectType == "Enemy")
        {
            var enemy = obj.GetComponent<Enemy>();
            if (enemy != null)
            {
                enemy.hasHeadphones = snapshot.hasHeadphones;
            }
            else
            {
                // SuperEnemy, Medic, Hunter gibi diğer enemy tipleri için
                var hasHeadphonesField = obj.GetType().GetField("hasHeadphones",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                if (hasHeadphonesField != null)
                {
                    hasHeadphonesField.SetValue(obj, snapshot.hasHeadphones);
                }
            }
        }

        // Robot specific
        if (snapshot.objectType == "Robot" && snapshot.componentData.ContainsKey("hasHeadphones"))
        {
            // Reflection ile private field'ı set et
            var robot = obj.GetComponent<BaseRobot>();
            if (robot != null)
            {
                var field = robot.GetType().GetField("hasHeadphones",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                field?.SetValue(robot, snapshot.componentData["hasHeadphones"]);
            }
        }

        // Gaia specific
        if (snapshot.objectType == "Gaia" && snapshot.componentData.ContainsKey("restoreHexes"))
        {
            var gaia = obj.GetComponent<Gaia>();
            if (gaia != null)
            {
                // Restore hex listesini set et
                var hexList = snapshot.componentData["restoreHexes"] as List<Vector2Int>;
                if (hexList != null)
                {
                    gaia.SetRestoreHexes(hexList);
                }
            }
        }
    }

    void OnDrawGizmos()
    {
        if (!showDebugInfo || levelSnapshot == null) return;

        Gizmos.color = snapshotGizmoColor;

        foreach (var snapshot in levelSnapshot.Values)
        {
            foreach (var obj in snapshot.objects)
            {
                // Snapshot'taki objeleri küçük küp olarak göster
                Gizmos.DrawWireCube(obj.position, Vector3.one * 0.2f);
            }
        }
    }
}