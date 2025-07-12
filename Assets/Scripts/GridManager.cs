using System.Collections.Generic;
using UnityEngine;

//[ExecuteInEditMode] // <-- Editörde de çalışmasını sağlar
public class GridManager : MonoBehaviour
{
    [Header("Grid Settings")]
    [SerializeField] private int gridWidth = 21;  // yatay
    [SerializeField] private int gridHeight = 11; // dikey
    [SerializeField] private GameObject hexPrefab;

    [Header("Visual Settings")]
    [SerializeField] private bool showGridLines = true;
    [SerializeField] private Color gridLineColor = Color.white;

    [Header("Editor Settings")]
    [SerializeField] private bool generateInEditor = true;
    [SerializeField] private bool autoRegenerate = true;

    // Grid data
    private Dictionary<Vector2Int, Hex> hexGrid = new Dictionary<Vector2Int, Hex>();
    private Dictionary<Vector2Int, GameObject> hexGameObjects = new Dictionary<Vector2Int, GameObject>();

    public static GridManager Instance { get; private set; }

    void Awake()
    {
        // Play mode'da singleton
        if (Application.isPlaying)
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
                return;
            }
        }
    }

    void Start()
    {
        if (Application.isPlaying)
        {
            // Layer kontrolü
            if (LayerMask.NameToLayer("Hexes") == -1)
            {
                Debug.LogWarning("'Hexes' layer not found! Please create a layer named 'Hexes' in Edit > Project Settings > Tags and Layers");
            }

            // ÖNEMLİ: Önce mevcut hex'leri kontrol et
            //CheckForExistingHexes();

            // Sadece hex'ler yoksa oluştur
            if (hexGrid.Count == 0)
            {
                Debug.Log("No existing hexes found - generating new grid");
                GenerateGrid();
            }


            CenterCamera();
        }
    }
    // YENİ METOD: Sahnedeki mevcut hex'leri bul ve kaydet
    //void CheckForExistingHexes()
    //{
    //    // Sahnedeki tüm HexTile'ları bul
    //    HexTile[] existingHexTiles = FindObjectsOfType<HexTile>();

    //    Debug.Log($"Found {existingHexTiles.Length} existing HexTiles in scene");

    //    foreach (HexTile hexTile in existingHexTiles)
    //    {
    //        if (hexTile.HexData != null)
    //        {
    //            Hex hex = hexTile.HexData;
    //            Vector2Int key = new Vector2Int(hex.q, hex.r);

    //            // Grid'e kaydet
    //            hexGrid[key] = hex;
    //            hexGameObjects[key] = hexTile.gameObject;

    //            //Debug.Log($"Registered existing hex at ({hex.q}, {hex.r})");
    //        }
    //    }
    //}
    void OnEnable()
    {
        // Editörde otomatik grid oluştur
        if (!Application.isPlaying && generateInEditor)
        {
            GenerateGrid();
        }
    }

    void OnDisable()
    {
        // Editörde grid'i temizle
        if (!Application.isPlaying)
        {
            ClearGrid();
        }
    }

    // Inspector'da değer değiştiğinde
    void OnValidate()
    {
        if (!Application.isPlaying && generateInEditor && autoRegenerate)
        {
            // Küçük bir delay ile regenerate et
#if UNITY_EDITOR
            UnityEditor.EditorApplication.delayCall -= DelayedRegenerate;
            UnityEditor.EditorApplication.delayCall += DelayedRegenerate;
#endif
        }
    }

#if UNITY_EDITOR
    void DelayedRegenerate()
    {
        if (this != null && gameObject != null)
        {
            ClearGrid();
            GenerateGrid();
        }
    }
#endif

    void GenerateGrid()
    {
        ClearGrid();

        // Sadece play mode'da prefab oluştur
        if (hexPrefab == null && Application.isPlaying)
        {
            CreateSimpleHexPrefab();
        }

        Transform gridParent = transform;

        // Editörde ayrı bir parent oluştur
        if (!Application.isPlaying)
        {
            GameObject editorGrid = GameObject.Find("EditorGrid");
            if (editorGrid == null)
            {
                editorGrid = new GameObject("EditorGrid");
                editorGrid.transform.SetParent(transform);
            }
            gridParent = editorGrid.transform;
        }

        // Offset-based rectangular hex grid
        for (int row = 0; row < gridHeight; row++)
        {
            for (int col = 0; col < gridWidth; col++)
            {
                // Offset coordinates to axial coordinates
                int q, r;

                // Even-row offset (even rows shift right)
                if (row % 2 == 0) // Even row
                {
                    q = col - gridWidth / 2;
                    r = row - gridHeight / 2;
                }
                else // Odd row
                {
                    q = col - gridWidth / 2;
                    r = row - gridHeight / 2;
                }

                CreateHexAt(q, r, gridParent);
            }
        }

        Debug.Log($"Grid oluşturuldu: {hexGrid.Count} hex ({gridWidth}x{gridHeight}) - Editor Mode: {!Application.isPlaying}");
    }

    void CreateHexAt(int q, int r, Transform parent = null)
    {
        // Hex data oluştur
        Hex hex = new Hex(q, r);
        Vector2Int key = new Vector2Int(q, r);
        hexGrid[key] = hex;

        // GameObject oluştur
        GameObject hexObj;

        // Prefab yoksa veya geçersizse yeni GameObject oluştur
        if (hexPrefab == null || (!Application.isPlaying &&
#if UNITY_EDITOR
            UnityEditor.PrefabUtility.GetPrefabAssetType(hexPrefab) == UnityEditor.PrefabAssetType.NotAPrefab
#else
            false
#endif
            ))
        {
            // Basit GameObject oluştur
            hexObj = new GameObject($"Hex_{q}_{r}");
            hexObj.transform.position = hex.worldPosition;
            hexObj.transform.rotation = Quaternion.identity;
            hexObj.transform.SetParent(parent ?? transform);

            // Hex layer'ı ayarla (layer yoksa default layer kullan)
            int hexLayer = LayerMask.NameToLayer("Hexes");
            if (hexLayer != -1)
            {
                hexObj.layer = hexLayer;
            }

            // Components ekle
            SpriteRenderer sr = hexObj.AddComponent<SpriteRenderer>();
            Sprite hexSprite = CreateHexSprite();
            if (hexSprite != null)
            {
                sr.sprite = hexSprite;
            }
            sr.color = new Color(0.9f, 0.9f, 0.9f, Application.isPlaying ? 0.8f : 0.3f);
            sr.sortingOrder = -10; // Hex'ler en arkada

            PolygonCollider2D collider = hexObj.AddComponent<PolygonCollider2D>();
            collider.isTrigger = true;
            Vector2[] hexColliderPoints = new Vector2[6];
            for (int i = 0; i < 6; i++)
            {
                float angle = i * 60f * Mathf.Deg2Rad;
                hexColliderPoints[i] = new Vector2(
                    Hex.HEX_SIZE * 0.9f * Mathf.Cos(angle),
                    Hex.HEX_SIZE * 0.9f * Mathf.Sin(angle)
                );
            }
            collider.points = hexColliderPoints;
        }
        else
        {
            // Prefab var, normal instantiate
            if (Application.isPlaying)
            {
                hexObj = Instantiate(hexPrefab, hex.worldPosition, Quaternion.identity, parent ?? transform);
            }
            else
            {
#if UNITY_EDITOR
                // Editörde prefab kullan
                if (UnityEditor.PrefabUtility.GetPrefabAssetType(hexPrefab) != UnityEditor.PrefabAssetType.NotAPrefab)
                {
                    hexObj = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(hexPrefab, parent ?? transform);
                    hexObj.transform.position = hex.worldPosition;
                    hexObj.transform.rotation = Quaternion.identity;
                }
                else
                {
                    hexObj = Instantiate(hexPrefab, hex.worldPosition, Quaternion.identity, parent ?? transform);
                }
#else
                hexObj = Instantiate(hexPrefab, hex.worldPosition, Quaternion.identity, parent ?? transform);
#endif
            }
            hexObj.name = $"Hex_{q}_{r}";

            // Editörde hex'leri yarı saydam yap
            if (!Application.isPlaying)
            {
                SpriteRenderer sr = hexObj.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    Color c = sr.color;
                    c.a = 0.3f;
                    sr.color = c;
                }
            }
        }

        hexGameObjects[key] = hexObj;

        // HexTile component ekle
        HexTile hexTile = hexObj.GetComponent<HexTile>();
        if (hexTile == null)
        {
            hexTile = hexObj.AddComponent<HexTile>();
        }
        hexTile.Initialize(hex);
    }

    void CreateSimpleHexPrefab()
    {
        // Not: Editörde runtime'da prefab oluşturamayız, 
        // bu yüzden bu metod artık sadece sprite oluşturmak için kullanılacak

        // Eğer play mode'daysa eski yöntemi kullan
        if (Application.isPlaying)
        {
            GameObject hex = new GameObject("HexPrefab");

            // Layer kontrolü
            int hexLayer = LayerMask.NameToLayer("Hexes");
            if (hexLayer != -1)
            {
                hex.layer = hexLayer;
            }

            // Sprite Renderer ekle
            SpriteRenderer sr = hex.AddComponent<SpriteRenderer>();
            Sprite hexSprite = CreateHexSprite();
            if (hexSprite != null)
            {
                sr.sprite = hexSprite;
            }
            sr.color = new Color(0.9f, 0.9f, 0.9f, 0.8f);
            sr.sortingOrder = -10; // Hex'ler en arkada

            // Manuel hex collider oluştur
            PolygonCollider2D collider = hex.AddComponent<PolygonCollider2D>();
            collider.isTrigger = true;
            Vector2[] hexColliderPoints = new Vector2[6];
            for (int i = 0; i < 6; i++)
            {
                float angle = i * 60f * Mathf.Deg2Rad;
                hexColliderPoints[i] = new Vector2(
                    Hex.HEX_SIZE * 0.9f * Mathf.Cos(angle),
                    Hex.HEX_SIZE * 0.9f * Mathf.Sin(angle)
                );
            }
            collider.points = hexColliderPoints;

            hexPrefab = hex;
            hex.SetActive(false);
        }
        // Editörde prefab null bırak, CreateHexAt her hex'i manuel oluşturacak
    }

    public Hex GetNearestHex(Vector3 worldPosition)
    {
        Hex nearestHex = Hex.WorldPositionToHex(worldPosition);
        return GetHexAt(nearestHex.q, nearestHex.r);
    }

    Sprite CreateHexSprite()
    {
        // Altıgen şekli oluştur
        Vector2[] hexPoints = new Vector2[6];
        float size = Hex.HEX_SIZE * 0.9f;

        for (int i = 0; i < 6; i++)
        {
            float angle = i * 60f * Mathf.Deg2Rad;
            hexPoints[i] = new Vector2(
                size * Mathf.Cos(angle),
                size * Mathf.Sin(angle)
            );
        }

        // Texture oluştur
        int textureSize = 128;
        Texture2D texture = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[textureSize * textureSize];

        // Şeffaf yap
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = Color.clear;
        }

        // Altıgen doldur
        Vector2 center = Vector2.one * textureSize / 2;
        for (int x = 0; x < textureSize; x++)
        {
            for (int y = 0; y < textureSize; y++)
            {
                Vector2 point = new Vector2(x, y) - center;
                if (IsPointInHex(point, size * textureSize / (Hex.HEX_SIZE * 2)))
                {
                    pixels[y * textureSize + x] = new Color(0.8f, 0.8f, 0.8f, 0.3f);
                }
            }
        }

        // Kenarları çiz
        for (int i = 0; i < 6; i++)
        {
            Vector2 start = hexPoints[i] * textureSize / (Hex.HEX_SIZE * 2) + center;
            Vector2 end = hexPoints[(i + 1) % 6] * textureSize / (Hex.HEX_SIZE * 2) + center;
            DrawThickLine(pixels, textureSize, start, end, Color.white, 2);
        }

        texture.SetPixels(pixels);
        texture.Apply();

        return Sprite.Create(texture, new Rect(0, 0, textureSize, textureSize), Vector2.one * 0.5f, textureSize / (Hex.HEX_SIZE * 2));
    }

    bool IsPointInHex(Vector2 point, float size)
    {
        float q = (2f / 3f * point.x) / size;
        float r = (-1f / 3f * point.x + Mathf.Sqrt(3f) / 3f * point.y) / size;
        float s = -q - r;

        return Mathf.Abs(q) <= 1 && Mathf.Abs(r) <= 1 && Mathf.Abs(s) <= 1;
    }

    public Vector3 GetHexWorldPosition(int q, int r)
    {
        return Hex.HexToWorldPosition(q, r);
    }

    void DrawThickLine(Color[] pixels, int width, Vector2 start, Vector2 end, Color color, int thickness)
    {
        Vector2 dir = (end - start).normalized;
        Vector2 perpendicular = new Vector2(-dir.y, dir.x);
        float distance = Vector2.Distance(start, end);

        for (float t = 0; t <= distance; t += 0.5f)
        {
            Vector2 center = start + dir * t;

            for (int i = -thickness; i <= thickness; i++)
            {
                Vector2 point = center + perpendicular * i;
                int x = Mathf.RoundToInt(point.x);
                int y = Mathf.RoundToInt(point.y);

                if (x >= 0 && x < width && y >= 0 && y < width)
                {
                    pixels[y * width + x] = color;
                }
            }
        }
    }

    void CenterCamera()
    {
        Camera cam = Camera.main;
        if (cam != null)
        {
            // Grid'in ortasına kamerayı getir
            cam.transform.position = new Vector3(0, 0, -10);

            // Sabit orthographic size
            if (cam.orthographic)
            {
                cam.orthographicSize = 5.2f;
            }
        }
    }

    // Grid'den hex al
    public Hex GetHexAt(int q, int r)
    {
        Vector2Int key = new Vector2Int(q, r);
        return hexGrid.ContainsKey(key) ? hexGrid[key] : null;
    }

    public Hex GetHexAt(Vector3 worldPosition)
    {
        return Hex.WorldPositionToHex(worldPosition);
    }

    // GameObject'i hex'e yerleştir
    public bool PlaceObjectOnHex(GameObject obj, int q, int r)
    {
        Hex hex = GetHexAt(q, r);
        if (hex != null && !hex.isOccupied)
        {
            hex.isOccupied = true;
            hex.occupiedBy = obj;
            obj.transform.position = hex.worldPosition;
            return true;
        }
        return false;
    }

    void ClearGrid()
    {
        foreach (var hexObj in hexGameObjects.Values)
        {
            if (hexObj != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(hexObj);
                }
                else
                {
                    DestroyImmediate(hexObj);
                }
            }
        }

        hexGrid.Clear();
        hexGameObjects.Clear();

        // Editörde EditorGrid parent'ı da temizle
        if (!Application.isPlaying)
        {
            GameObject editorGrid = GameObject.Find("EditorGrid");
            if (editorGrid != null)
            {
                DestroyImmediate(editorGrid);
            }
        }
    }

    void OnDrawGizmos()
    {
        if (showGridLines && hexGrid != null)
        {
            Gizmos.color = gridLineColor;
            foreach (var hex in hexGrid.Values)
            {
                // Hex merkezini çiz
                Gizmos.DrawWireSphere(hex.worldPosition, 0.1f);
            }
        }
    }

    // Editör butonları için
    [ContextMenu("Regenerate Grid")]
    public void RegenerateGrid()
    {
        ClearGrid();
        GenerateGrid();
    }

    [ContextMenu("Clear Grid")]
    public void ClearGridManual()
    {
        ClearGrid();
    }
}