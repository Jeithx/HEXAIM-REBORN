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
        if (!showCoordinates || hexData == null) return;

        // TextMesh objesi oluştur
        GameObject textObj = new GameObject("CoordinateText");
        textObj.transform.SetParent(transform);
        textObj.transform.localPosition = Vector3.zero;
        textObj.transform.localScale = Vector3.one;

        // TextMesh component ekle
        coordinateText = textObj.AddComponent<TextMesh>();
        coordinateText.text = $"({hexData.q},{hexData.r})";
        coordinateText.fontSize = 20;
        coordinateText.characterSize = textSize;
        coordinateText.anchor = TextAnchor.MiddleCenter;
        coordinateText.alignment = TextAlignment.Center;
        coordinateText.color = textColor;

        // Font ayarla (default font kullan)
        coordinateText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        // MeshRenderer ayarları
        MeshRenderer meshRenderer = textObj.GetComponent<MeshRenderer>();
        if (meshRenderer != null)
        {
            meshRenderer.sortingLayerName = "Default";
            meshRenderer.sortingOrder = 10; // Hex'lerin üstünde görünsün
        }

        // Z pozisyonunu biraz öne al
        textObj.transform.position = new Vector3(
            transform.position.x,
            transform.position.y,
            transform.position.z - 0.1f
        );
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
        if (Application.isPlaying && coordinateText != null)
        {
            coordinateText.gameObject.SetActive(showCoordinates);
        }
    }

    void OnDrawGizmosSelected()
    {
        if (hexData != null)
        {
            // Hex merkezi
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(hexData.worldPosition, Vector3.one * 0.2f);

            // Koordinat metni (Scene view için)
            UnityEditor.Handles.color = Color.white;
            UnityEditor.Handles.Label(
                hexData.worldPosition + Vector3.up * 0.5f,
                $"({hexData.q}, {hexData.r})"
            );

            // Komşuları göster
            Gizmos.color = Color.green;
            Hex[] neighbors = hexData.GetNeighbors();
            foreach (var neighbor in neighbors)
            {
                Gizmos.DrawLine(hexData.worldPosition, neighbor.worldPosition);
                Gizmos.DrawWireSphere(neighbor.worldPosition, 0.05f);
            }
        }
    }
}