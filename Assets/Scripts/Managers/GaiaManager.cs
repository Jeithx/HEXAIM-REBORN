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

    // Robot prefabs
    [SerializeField] private GameObject robotPrefab;
    [SerializeField] private GameObject bumperPrefab;

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
            Collider2D[] colliders = Physics2D.OverlapCircleAll(hexData.worldPosition, Hex.HEX_SIZE * 0.4f);

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
            snapshot.specificType = obj.GetComponent<IEnemy>().GetType().Name;

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
            snapshot.specificType = obj.GetComponent<IRobot>().GetType().Name;

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
        //else
        //{
        //    // Bilinmeyen obje tipi
        //    Debug.LogWarning($"Unknown object type for snapshot: {obj.name}");
        //    return null;
        //}

        // Health durumu
        var health = obj.GetComponent<Health>();
        if (health != null)
        {
            snapshot.health = health.CurrentHp;
        }

        if (!obj.name.Contains("Square") && !obj.name.Contains("MapBorders"))
        {
            Debug.Log($"SNAP: {obj.name} -> {snapshot.objectType}/{snapshot.specificType}");

        }

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
            Collider2D[] colliders = Physics2D.OverlapCircleAll(hex.worldPosition, Hex.HEX_SIZE * 0.4f);
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
                    var enemy = obj.GetComponent<IEnemy>();
                    if (enemy != null)
                    {
                        typeMatch = enemy.GetType().Name == snapshot.specificType;
                    }

                    break;
                case "Wall":
                    typeMatch = obj.GetComponent<Wall>() != null;
                    break;
                case "Hay":
                    typeMatch = obj.GetComponent<Hay>() != null;
                    break;
                case "Robot":
                    var robot = obj.GetComponent<IRobot>();
                    if (robot != null)
                    {
                        typeMatch = robot.GetType().Name == snapshot.specificType;
                    }
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

    GameObject CreateObjectFromSnapshot(HexSnapshot.ObjectSnapshot s)
    {
        // 1) Doğru prefab’ı al
        GameObject prefab = GetPrefabForSnapshot(s);

        if (prefab == null)
        {
            Debug.LogWarning($"Prefab atanmadı: {s.objectType}/{s.specificType}");
            return null;
        }

        // 2) Instantiate
        GameObject go = Instantiate(prefab, s.position, s.rotation);
        go.transform.localScale = s.scale;

        // 3) Ek bileşen durumlarını geri yükle
        RestoreComponentStates(go, s);

        // NOT: Enemy’ler GameManager’a kendi Start()’larında kaydolacak
        return go;
    }

    GameObject GetPrefabForSnapshot(HexSnapshot.ObjectSnapshot s)
    {
        switch (s.objectType)
        {
            case "Player": return playerPrefab;
            case "Wall": return wallPrefab;
            //case "Hay": return hayPrefab;
            //case "Hostage": return hostagePrefab;
            case "Gaia": return gaiaPrefab;

            case "Enemy":
                switch (s.specificType)
                {
                    case "EnemyNoGun": return enemyNoGunPrefab;
                    case "Hunter": return hunterPrefab;
                    case "Baretta60": return baretta60Prefab;
                    case "Baretta120": return baretta120Prefab;
                    case "Baretta180": return baretta180Prefab;
                    case "Medic": return medicPrefab;
                    case "SuperEnemy": return superEnemyPrefab;
                    case "Riot": return riotPrefab;
                    default: return enemyPrefab;  // genel düşman
                }

            case "Robot":
                return s.specificType == "Bumper" ? bumperPrefab : robotPrefab;

            default:
                return null;
        }
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
                enemy.hasHeadphones = !snapshot.hasHeadphones;
            }
            else
            {
                // SuperEnemy, Medic, Hunter gibi diğer enemy tipleri için
                var hasHeadphonesField = obj.GetType().GetField("hasHeadphones",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                if (hasHeadphonesField != null)
                {
                    hasHeadphonesField.SetValue(obj, !snapshot.hasHeadphones);
                }
            }
        }

        // Robot specific
        if (snapshot.objectType == "Robot")
        {
            // Reflection ile private field'ı set et
            var robot = obj.GetComponent<BaseRobot>();
            if (robot != null)
            {
                var field = robot.GetType().GetField("hasHeadphones",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (field != null)
                {
                    field.SetValue(robot, !snapshot.hasHeadphones);
                }
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