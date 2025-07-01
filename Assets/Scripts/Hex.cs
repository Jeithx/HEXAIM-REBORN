using UnityEngine;

[System.Serializable]
public class Hex
{
    [Header("Hex Coordinates")]
    public int q; // Axial koordinat q (sütun)
    public int r; // Axial koordinat r (satır)

    [Header("World Position")]
    public Vector3 worldPosition;

    [Header("Hex Properties")]
    public bool isOccupied = false;
    public GameObject occupiedBy = null;

    // Hex boyutları (büyütüldü)
    public static float HEX_SIZE = 0.55f;

    // Constructor
    public Hex(int q, int r)
    {
        this.q = q;
        this.r = r;
        this.worldPosition = HexToWorldPosition(q, r);
    }

    // Axial koordinatları dünya pozisyonuna çevir
    public static Vector3 HexToWorldPosition(int q, int r)
    {
        float x = HEX_SIZE * (3f / 2f * q);
        float y = HEX_SIZE * (Mathf.Sqrt(3f) / 2f * q + Mathf.Sqrt(3f) * r);
        return new Vector3(x, y, 0);
    }

    // Dünya pozisyonunu axial koordinatlara çevir
    public static Hex WorldPositionToHex(Vector3 worldPos)
    {
        float q = (2f / 3f * worldPos.x) / HEX_SIZE;
        float r = (-1f / 3f * worldPos.x + Mathf.Sqrt(3f) / 3f * worldPos.y) / HEX_SIZE;

        return HexRound(q, r);
    }

    // Float koordinatları en yakın hex'e yuvarla
    private static Hex HexRound(float q, float r)
    {
        float s = -q - r;

        int rq = Mathf.RoundToInt(q);
        int rr = Mathf.RoundToInt(r);
        int rs = Mathf.RoundToInt(s);

        float q_diff = Mathf.Abs(rq - q);
        float r_diff = Mathf.Abs(rr - r);
        float s_diff = Mathf.Abs(rs - s);

        if (q_diff > r_diff && q_diff > s_diff)
            rq = -rr - rs;
        else if (r_diff > s_diff)
            rr = -rq - rs;

        return new Hex(rq, rr);
    }

    // 6 komşu hex'i al (0°, 60°, 120°, 180°, 240°, 300°)
    public Hex[] GetNeighbors()
    {
        Hex[] neighbors = new Hex[6];

        // Komşu yönleri (saat yönünde)
        int[,] directions = {
            {1, 0},   // 0° (sağ)
            {0, 1},   // 60° 
            {-1, 1},  // 120°
            {-1, 0},  // 180° (sol)
            {0, -1},  // 240°
            {1, -1}   // 300°
        };

        for (int i = 0; i < 6; i++)
        {
            neighbors[i] = new Hex(q + directions[i, 0], r + directions[i, 1]);
        }

        return neighbors;
    }

    // İki hex arası mesafe
    public int DistanceTo(Hex other)
    {
        return (Mathf.Abs(q - other.q) + Mathf.Abs(q + r - other.q - other.r) + Mathf.Abs(r - other.r)) / 2;
    }

    // Debug için string representation
    public override string ToString()
    {
        return $"Hex({q}, {r})";
    }
}