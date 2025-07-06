using System.Collections.Generic;
using UnityEngine;

public class GridManager : MonoBehaviour
{
    [Header("Grid Settings")]
    [SerializeField] private int gridWidth = 21;  // yatay
    [SerializeField] private int gridHeight = 11; // dikey
    [SerializeField] private GameObject hexPrefab;
    
    [Header("Visual Settings")]
    [SerializeField] private bool showGridLines = true;
    [SerializeField] private Color gridLineColor = Color.white;
    
    // Grid data
    private Dictionary<Vector2Int, Hex> hexGrid = new Dictionary<Vector2Int, Hex>();
    private Dictionary<Vector2Int, GameObject> hexGameObjects = new Dictionary<Vector2Int, GameObject>();
    
    public static GridManager Instance { get; private set; }
    
    void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    void Start()
    {
        GenerateGrid();
        CenterCamera();
    }

    void GenerateGrid()
    {
        ClearGrid();

        if (hexPrefab == null)
        {
            CreateSimpleHexPrefab();
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

                CreateHexAt(q, r);
            }
        }

        //Debug.Log($"Grid oluşturuldu: {hexGrid.Count} hex ({gridWidth}x{gridHeight})");
    }

    void CreateHexAt(int q, int r)
    {
        // Hex data oluştur
        Hex hex = new Hex(q, r);
        Vector2Int key = new Vector2Int(q, r);
        hexGrid[key] = hex;
        
        // GameObject oluştur
        GameObject hexObj = Instantiate(hexPrefab, hex.worldPosition, Quaternion.identity, transform);
        hexObj.name = $"Hex_{q}_{r}";
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
        // Basit altıgen prefab oluştur
        GameObject hex = new GameObject("HexPrefab");

        hex.layer = LayerMask.NameToLayer("Hexes");

        // Sprite Renderer ekle
        SpriteRenderer sr = hex.AddComponent<SpriteRenderer>();
        sr.sprite = CreateHexSprite();
        sr.color = new Color(0.9f, 0.9f, 0.9f, 0.8f); // Daha görünür
        
        // Manuel hex collider oluştur
        PolygonCollider2D collider = hex.AddComponent<PolygonCollider2D>();
        collider.isTrigger = true; // Trigger olarak ayarla
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
        float size = Hex.HEX_SIZE * 0.9f; // Biraz küçült ki aralar gözüksün
        
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
        
        // Altıgen doldur (içini boyar)
        Vector2 center = Vector2.one * textureSize/2;
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
        float q = (2f/3f * point.x) / size;
        float r = (-1f/3f * point.x + Mathf.Sqrt(3f)/3f * point.y) / size;
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
            
            // Sabit orthographic size - tam kaplasın
            if (cam.orthographic)
            {
                cam.orthographicSize = 5.2f; // Sabit değer
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
                DestroyImmediate(hexObj);
        }
        
        hexGrid.Clear();
        hexGameObjects.Clear();
    }

    //public Hex GetNearestHex(Vector3 worldPosition)
    //{
    //    Hex nearestHex = Hex.WorldPositionToHex(worldPosition);
    //    return GetHexAt(nearestHex.q, nearestHex.r);
    //}

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
}