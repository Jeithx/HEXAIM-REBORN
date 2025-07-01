using UnityEngine;

public class DecoyManager : MonoBehaviour
{
    [Header("Decoy Settings")]
    //[SerializeField] private Color decoyHighlightColor = Color.white;
    [SerializeField] private bool showDecoyPreview = true;

    private Camera playerCamera;
    private GameObject targetEnemy;
    private IDecoyable targetDecoyable;
    private bool isDecoyActive = false;
    private Color originalColor;
    private SpriteRenderer targetSpriteRenderer;
    private int currentDirectionIndex = 0; // 0-5 arası (60°'lik adımlar)

    public static DecoyManager Instance { get; private set; }

    // 60°'lik yönler: 0°, 60°, 120°, 180°, 240°, 300°   
    private readonly float[] hexDirections = { 0f, 60f, 120f, 180f, 240f, 300f };

    [SerializeField] private LayerMask decoyTargetMask = ~0;  // Inspector’da hepsi açık gelir

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

        playerCamera = Camera.main;
        // “Hexes” bit’ini maskeden düşür
        int hexLayerBit = 1 << LayerMask.NameToLayer("Hexes");
        decoyTargetMask &= ~hexLayerBit;
    }

    void Update()
    {
        HandleDecoyInput();

        if (isDecoyActive)
        {
            HandleDecoyRotation();
        }
    }

    void HandleDecoyInput()
    {
        // Sağ mouse tuşuna basıldığında
        if (Input.GetMouseButtonDown(1))
        {
            StartDecoy();
        }

        // Sağ mouse tuşu bırakıldığında
        if (Input.GetMouseButtonUp(1))
        {
            EndDecoy();
        }
    }

    void StartDecoy()
    {
        if (isDecoyActive) return;

        // Mouse altındaki objeyi bul
        Vector3 mouseWorldPos = playerCamera.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPos.z = 0;

        GameObject clickedObject = GetObjectUnderMouse(mouseWorldPos);

        if (clickedObject != null)
        {
            Debug.Log($"Clicked on: {clickedObject.name}");

            IDecoyable decoyable = clickedObject.GetComponent<IDecoyable>();

            if (decoyable == null)
            {
                Debug.LogWarning($"{clickedObject.name} has NO IDecoyable component!");
                return;
            }

            Debug.Log($"{clickedObject.name} has IDecoyable. CanBeDecoyed: {decoyable.CanBeDecoyed}");

            if (decoyable.CanBeDecoyed)
            {
                targetEnemy = clickedObject;
                targetDecoyable = decoyable;
                isDecoyActive = true;

                // Mevcut açıya en yakın direction index'i bul
                float currentAngle = targetEnemy.transform.eulerAngles.z;
                currentDirectionIndex = GetNearestDirectionIndex(currentAngle);

                // Görsel feedback
                //targetSpriteRenderer = targetEnemy.GetComponent<SpriteRenderer>();
                //if (targetSpriteRenderer != null)
                //{
                //    originalColor = targetSpriteRenderer.color;
                //    targetSpriteRenderer.color = decoyHighlightColor;
                //}

                // Decoy başlangıç eventi
                targetDecoyable.OnDecoyStart();

                Debug.Log($"Decoy STARTED on {targetEnemy.name}");
            }
            else
            {
                Debug.LogWarning($"Cannot decoy {clickedObject.name} - CanBeDecoyed returned false");

                // Detailed debug
                if (clickedObject.GetComponent<Enemy>() != null)
                {
                    Enemy enemy = clickedObject.GetComponent<Enemy>();
                    Debug.Log($"Enemy hasHeadphones: {enemy.hasHeadphones}, isDead: {enemy.GetComponent<Health>()?.IsDead}");
                }
            }
        }
        else
        {
            Debug.Log("No object clicked!");
        }
    }

    void HandleDecoyRotation()
    {
        if (targetEnemy == null) return;

        // Mouse hareketine göre yön değiştir
        Vector3 mouseWorldPos = playerCamera.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPos.z = 0;

        Vector3 direction = (mouseWorldPos - targetEnemy.transform.position).normalized;
        float mouseAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;

        // En yakın direction index'i bul
        int nearestIndex = GetNearestDirectionIndex(mouseAngle);

        // Sadece değişti ise rotate et (60°'lik adımlar)
        if (nearestIndex != currentDirectionIndex)
        {
            currentDirectionIndex = nearestIndex;
            float targetAngle = hexDirections[currentDirectionIndex];
            targetEnemy.transform.rotation = Quaternion.AngleAxis(targetAngle, Vector3.forward);

            Debug.Log($"Decoy rotated to {targetAngle}° (index {currentDirectionIndex})");
        }
    }

    void EndDecoy()
    {
        if (!isDecoyActive || targetEnemy == null) return;

        // Son pozisyonu uygula
        float finalAngle = hexDirections[currentDirectionIndex];
        targetEnemy.transform.rotation = Quaternion.AngleAxis(finalAngle, Vector3.forward);

        // Görsel feedback'i geri al
        if (targetSpriteRenderer != null)
        {
            targetSpriteRenderer.color = originalColor;
        }

        // Decoy bitiş eventi
        if (targetDecoyable != null)
        {
            targetDecoyable.OnDecoyEnd();
        }

        Debug.Log($"Decoy ended - {targetEnemy.name} final angle: {finalAngle}°");

        // Temizlik
        isDecoyActive = false;
        targetEnemy = null;
        targetDecoyable = null;
        targetSpriteRenderer = null;
    }

    int GetNearestDirectionIndex(float angle)
    {
        // Açıyı 0-360 aralığına normalize et
        angle = angle % 360f;
        if (angle < 0) angle += 360f;

        int nearestIndex = 0;
        float minDifference = Mathf.Abs(Mathf.DeltaAngle(angle, hexDirections[0]));

        for (int i = 1; i < hexDirections.Length; i++)
        {
            float difference = Mathf.Abs(Mathf.DeltaAngle(angle, hexDirections[i]));
            if (difference < minDifference)
            {
                minDifference = difference;
                nearestIndex = i;
            }
        }

        return nearestIndex;
    }

    GameObject GetObjectUnderMouse(Vector3 worldPosition)
    {
        Collider2D hit = Physics2D.OverlapPoint(worldPosition, decoyTargetMask);
        return hit != null ? hit.gameObject : null;
    }

    //void OnDrawGizmos()
    //{
    //    if (isDecoyActive && targetEnemy != null && showDecoyPreview)
    //    {
    //        // Decoy target'ı göster
    //        Gizmos.color = Color.yellow;
    //        Gizmos.DrawWireSphere(targetEnemy.transform.position, 0.6f);

    //        // Mevcut yönü göster
    //        Vector3 direction = Quaternion.AngleAxis(hexDirections[currentDirectionIndex], Vector3.forward) * Vector3.up;
    //        Gizmos.color = Color.green;
    //        Gizmos.DrawLine(targetEnemy.transform.position, targetEnemy.transform.position + direction * 1f);
    //    }
    //}
}