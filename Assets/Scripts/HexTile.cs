using UnityEngine;

public class HexTile : MonoBehaviour
{
    [Header("Hex Data")]
    [SerializeField] private Hex hexData;

    [Header("Visual Feedback")]
    [SerializeField] private Color normalColor = new Color(0.8f, 0.8f, 0.8f, 0.3f);
    [SerializeField] private Color hoverColor = new Color(1f, 1f, 0.5f, 0.5f);
    [SerializeField] private Color occupiedColor = new Color(1f, 0.5f, 0.5f, 0.5f);

    [Header("Coordinate Display")]
    [SerializeField] private bool showCoordinates = false;
    [SerializeField] private float textSize = 0.0f;
    [SerializeField] private Color textColor = Color.black;

    private SpriteRenderer spriteRenderer;
    private bool isMouseOver = false;
    private TextMesh coordinateText;
    private GameObject textObject; // TextMesh objesini tutmak için

    public Hex HexData => hexData;

    void Start()
    {
        PolygonCollider2D collider = GetComponent<PolygonCollider2D>();
        if (collider != null)
        {
            collider.isTrigger = true; // Trigger yap - fiziksel çarpışma olmaz
        }
    }

    public void Initialize(Hex hex)
    {
        hexData = hex;
        spriteRenderer = GetComponent<SpriteRenderer>();

        CreateCoordinateText();
        UpdateVisuals();
    }

    void CreateCoordinateText()
    {
        if (hexData == null) return;

        // Önce mevcut text objelerini temizle
        CleanupCoordinateText();

        // Sadece showCoordinates true ise oluştur
        if (!showCoordinates) return;

        // TextMesh objesi oluştur
        textObject = new GameObject("CoordinateText");
        textObject.transform.SetParent(transform);
        textObject.transform.localPosition = Vector3.zero;
        textObject.transform.localScale = Vector3.one;

        // TextMesh component ekle
        coordinateText = textObject.AddComponent<TextMesh>();
        coordinateText.text = $"({hexData.q},{hexData.r})";
        coordinateText.fontSize = 20;
        coordinateText.characterSize = textSize;
        coordinateText.anchor = TextAnchor.MiddleCenter;
        coordinateText.alignment = TextAlignment.Center;
        coordinateText.color = textColor;

        // Font ayarla (default font kullan)
        coordinateText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        // MeshRenderer ayarları
        MeshRenderer meshRenderer = textObject.GetComponent<MeshRenderer>();
        if (meshRenderer != null)
        {
            meshRenderer.sortingLayerName = "Default";
            meshRenderer.sortingOrder = 10; // Hex'lerin üstünde görünsün
        }

        // Z pozisyonunu biraz öne al
        textObject.transform.position = new Vector3(
            transform.position.x,
            transform.position.y,
            transform.position.z - 0.1f
        );
    }

    void CleanupCoordinateText()
    {
        // Mevcut text objesini sil
        if (textObject != null)
        {
            if (Application.isPlaying)
                Destroy(textObject);
            else
                DestroyImmediate(textObject);

            textObject = null;
            coordinateText = null;
        }

        // Ek güvenlik: CoordinateText isimli child objelerini bul ve sil
        Transform[] children = GetComponentsInChildren<Transform>();
        foreach (Transform child in children)
        {
            if (child != transform && child.name == "CoordinateText")
            {
                if (Application.isPlaying)
                    Destroy(child.gameObject);
                else
                    DestroyImmediate(child.gameObject);
            }
        }
    }

    void OnMouseEnter()
    {
        isMouseOver = true;
        UpdateVisuals();

        // Debug bilgisi
        //Debug.Log($"Mouse over hex: {hexData}");
    }

    void OnMouseExit()
    {
        isMouseOver = false;
        UpdateVisuals();
    }

    void OnMouseDown()
    {
        // Sol tık - normal hex bilgisi
        if (Input.GetMouseButtonDown(0))
        {
            //Debug.Log($"Hex Coordinates: ({hexData.q}, {hexData.r})");
            //Debug.Log($"Left clicked hex: {hexData} at world position: {hexData.worldPosition}");
        }
    }

    void UpdateVisuals()
    {
        if (spriteRenderer == null) return;

        Color targetColor = normalColor;

        if (hexData.isOccupied)
        {
            targetColor = occupiedColor;
        }
        else if (isMouseOver)
        {
            targetColor = hoverColor;
        }

        spriteRenderer.color = targetColor;
    }

    // Hex'in durumunu güncelle
    public void SetOccupied(bool occupied, GameObject occupyingObject = null)
    {
        hexData.isOccupied = occupied;
        hexData.occupiedBy = occupyingObject;
        UpdateVisuals();
    }

    void OnValidate()
    {
        // Inspector'da değişiklik yapıldığında koordinat metnini güncelle
        if (Application.isPlaying)
        {
            CreateCoordinateText();
        }
    }

    void OnDestroy()
    {
        // Obje yok edilirken text objelerini temizle
        CleanupCoordinateText();
    }

    //void OnDrawGizmosSelected()
    //{
    //    if (hexData != null)
    //    {
    //        // Hex merkezi
    //        Gizmos.color = Color.yellow;
    //        Gizmos.DrawWireCube(hexData.worldPosition, Vector3.one * 0.2f);

    //        // Koordinat metni (Scene view için)
    //        UnityEditor.Handles.color = Color.white;
    //        UnityEditor.Handles.Label(
    //            hexData.worldPosition + Vector3.up * 0.5f,
    //            $"({hexData.q}, {hexData.r})"
    //        );

    //        // Komşuları göster
    //        Gizmos.color = Color.green;
    //        Hex[] neighbors = hexData.GetNeighbors();
    //        foreach (var neighbor in neighbors)
    //        {
    //            Gizmos.DrawLine(hexData.worldPosition, neighbor.worldPosition);
    //            Gizmos.DrawWireSphere(neighbor.worldPosition, 0.05f);
    //        }
    //    }
    //}
}