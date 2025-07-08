using UnityEngine;

public class HexTile : MonoBehaviour
{
    [Header("Hex Data")]
    [SerializeField] private Hex hexData;

    [Header("Visual Feedback")]
    [SerializeField] private Color normalColor = new Color(0.8f, 0.8f, 0.8f, 0.3f);
    [SerializeField] private Color hoverColor = new Color(1f, 1f, 0.5f, 0.5f);
    [SerializeField] private Color occupiedColor = new Color(1f, 0.5f, 0.5f, 0.5f);

    private SpriteRenderer spriteRenderer;
    private bool isMouseOver = false;

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
        UpdateVisuals();
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

    void OnDrawGizmosSelected()
    {
        if (hexData != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(hexData.worldPosition, Vector3.one * 0.2f);

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