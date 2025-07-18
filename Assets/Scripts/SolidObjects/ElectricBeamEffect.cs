using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(LineRenderer))]
public class ElectricBeamEffect : MonoBehaviour
{
    [Header("Lightning Settings")]
    [SerializeField] private int segmentCount = 20; // Elektrik segman sayısı
    [SerializeField] private float amplitude = 0.5f; // Elektrik genliği
    [SerializeField] private float frequency = 5f; // Titreşim hızı
    [SerializeField] private float noiseScale = 3f; // Rastgelelik

    [Header("Branch Lightning")]
    [SerializeField] private bool enableBranches = true;
    [SerializeField] private int branchCount = 3;
    [SerializeField] private float branchProbability = 0.3f;
    [SerializeField] private float branchLength = 1f;
    [SerializeField] private LineRenderer branchPrefab;

    [Header("Visual Effects")]
    [SerializeField] private bool pulseWidth = true;
    [SerializeField] private float widthMultiplier = 1.5f;
    [SerializeField] private AnimationCurve widthCurve = AnimationCurve.Linear(0, 1, 1, 1);
    [SerializeField] private Gradient colorGradient;

    [Header("Performance")]
    [SerializeField] private float updateInterval = 0.02f; // 50 FPS

    private LineRenderer mainBeam;
    private List<LineRenderer> branches = new List<LineRenderer>();
    private Vector3[] segmentPositions;
    private float timer = 0f;
    private float updateTimer = 0f;
    private Vector3 startPoint;
    private Vector3 endPoint;

    void Awake()
    {
        mainBeam = GetComponent<LineRenderer>();
        segmentPositions = new Vector3[segmentCount + 1];

        // Gradient ayarla
        if (colorGradient == null || colorGradient.colorKeys.Length == 0)
        {
            colorGradient = new Gradient();
            GradientColorKey[] colorKeys = new GradientColorKey[3];
            colorKeys[0] = new GradientColorKey(Color.white, 0f);
            colorKeys[1] = new GradientColorKey(Color.cyan, 0.5f);
            colorKeys[2] = new GradientColorKey(Color.blue, 1f);

            GradientAlphaKey[] alphaKeys = new GradientAlphaKey[2];
            alphaKeys[0] = new GradientAlphaKey(1f, 0f);
            alphaKeys[1] = new GradientAlphaKey(1f, 1f);

            colorGradient.SetKeys(colorKeys, alphaKeys);
        }
    }

    public void SetPoints(Vector3 start, Vector3 end)
    {
        startPoint = start;
        endPoint = end;
        GenerateLightning();
    }

    void Update()
    {
        timer += Time.deltaTime;
        updateTimer += Time.deltaTime;

        if (updateTimer >= updateInterval)
        {
            updateTimer = 0f;
            GenerateLightning();
            UpdateVisualEffects();
        }
    }

    void GenerateLightning()
    {
        // Ana elektrik yolu
        Vector3 direction = endPoint - startPoint;
        float distance = direction.magnitude;
        direction.Normalize();

        // Yan vektör (elektriğin salınımı için)
        Vector3 perpendicular = Vector3.Cross(direction, Vector3.forward).normalized;

        // Segment pozisyonlarını hesapla
        for (int i = 0; i <= segmentCount; i++)
        {
            float t = i / (float)segmentCount;

            // Temel pozisyon
            Vector3 basePos = Vector3.Lerp(startPoint, endPoint, t);

            // Elektrik offset'i
            float offset = 0f;

            if (i != 0 && i != segmentCount) // İlk ve son nokta sabit
            {
                // Perlin noise ile doğal hareket
                float noise = Mathf.PerlinNoise(
                    timer * frequency + i * noiseScale,
                    timer * frequency * 0.7f
                ) - 0.5f;

                // Ortaya doğru daha fazla salınım
                float enveloppe = Mathf.Sin(t * Mathf.PI);
                offset = noise * amplitude * enveloppe;
            }

            segmentPositions[i] = basePos + perpendicular * offset;
        }

        // Ana beam'i güncelle
        mainBeam.positionCount = segmentCount + 1;
        mainBeam.SetPositions(segmentPositions);

        // Dal elektrikler
        if (enableBranches)
        {
            GenerateBranches();
        }
    }

    void GenerateBranches()
    {
        // Mevcut branch sayısını kontrol et
        while (branches.Count < branchCount)
        {
            CreateNewBranch();
        }

        // Her branch için
        for (int b = 0; b < branchCount; b++)
        {
            if (Random.value > branchProbability)
            {
                branches[b].enabled = false;
                continue;
            }

            branches[b].enabled = true;

            // Rastgele bir segment seç (ilk ve son hariç)
            int segmentIndex = Random.Range(2, segmentCount - 2);
            Vector3 branchStart = segmentPositions[segmentIndex];

            // Rastgele yön
            Vector3 branchDir = Random.onUnitSphere;
            branchDir.z = 0; // 2D için

            // Branch pozisyonları
            int branchSegments = 5;
            Vector3[] branchPositions = new Vector3[branchSegments];

            for (int i = 0; i < branchSegments; i++)
            {
                float t = i / (float)(branchSegments - 1);
                float distance = branchLength * (1f - t * 0.5f); // Uçlara doğru kısalır

                // Küçük rastgele sapma
                Vector3 offset = Random.insideUnitCircle * 0.1f;
                offset.z = 0;

                branchPositions[i] = branchStart + (branchDir + offset).normalized * distance * t;
            }

            branches[b].positionCount = branchSegments;
            branches[b].SetPositions(branchPositions);

            // Branch kalınlığı (ana beam'den ince)
            branches[b].startWidth = mainBeam.startWidth * 0.5f;
            branches[b].endWidth = 0.01f;
        }
    }

    void CreateNewBranch()
    {
        GameObject branchObj = new GameObject($"Branch_{branches.Count}");
        branchObj.transform.SetParent(transform);

        LineRenderer branch = branchObj.AddComponent<LineRenderer>();
        branch.material = mainBeam.material;
        branch.startColor = mainBeam.startColor;
        branch.endColor = mainBeam.endColor;
        branch.sortingOrder = mainBeam.sortingOrder - 1;
        branch.textureMode = LineTextureMode.Stretch;

        branches.Add(branch);
    }

    void UpdateVisualEffects()
    {
        // Genişlik pulse efekti
        if (pulseWidth)
        {
            float pulse = 1f + Mathf.Sin(timer * frequency * 2f) * 0.2f;
            mainBeam.widthMultiplier = widthMultiplier * pulse;

            // Branch'ler için de
            foreach (var branch in branches)
            {
                if (branch.enabled)
                {
                    branch.widthMultiplier = widthMultiplier * pulse * 0.5f;
                }
            }
        }

        // Renk animasyonu
        float colorTime = Mathf.PingPong(timer * 2f, 1f);
        Color currentColor = colorGradient.Evaluate(colorTime);

        mainBeam.startColor = currentColor;
        mainBeam.endColor = currentColor * 0.7f;

        foreach (var branch in branches)
        {
            branch.startColor = currentColor * 0.8f;
            branch.endColor = currentColor * 0.3f;
        }
    }

    void OnDisable()
    {
        // Branch'leri gizle
        foreach (var branch in branches)
        {
            if (branch != null)
                branch.enabled = false;
        }
    }

    void OnEnable()
    {
        // Reset timers
        timer = 0f;
        updateTimer = 0f;
    }
}