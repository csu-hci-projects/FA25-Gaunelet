using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
public class WaterMotion : MonoBehaviour
{
    [Header("Wave Settings")]
    public float waveHeight = 0.2f;      // Max wave height in center
    public float waveSpeed = 1f;
    public float waveLength = 1f;

    [Header("Ground Settings")]
    public float baseY = 0f;             // Minimum Y position (ground level)

    [Header("Falloff Settings")]
    [Range(0f, 1f)]
    public float falloffRadius = 0.5f;   // Fraction of plane radius for moving waves (0 = none, 1 = full)

    private Mesh mesh;
    private Vector3[] baseVertices;
    private Vector3 center;

    void Start()
    {
        mesh = GetComponent<MeshFilter>().mesh;
        baseVertices = mesh.vertices;
        center = mesh.bounds.center;
    }

    void Update()
    {
        Vector3[] vertices = new Vector3[baseVertices.Length];

        // Determine max distance from center to corner
        float maxDistance = 0f;
        foreach (Vector3 v in baseVertices)
        {
            float dist = Vector2.Distance(new Vector2(v.x, v.z), new Vector2(center.x, center.z));
            if (dist > maxDistance) maxDistance = dist;
        }

        for (int i = 0; i < vertices.Length; i++)
        {
            Vector3 v = baseVertices[i];

            // Distance from center
            float distance = Vector2.Distance(new Vector2(v.x, v.z), new Vector2(center.x, center.z));

            // Circular falloff: 1 at center, 0 at edges beyond falloff radius
            float falloff = Mathf.Clamp01(1f - distance / (maxDistance * falloffRadius));

            // Wave offset
            float yOffset = Mathf.Sin(Time.time * waveSpeed + v.x * waveLength + v.z * waveLength) * waveHeight;

            // Apply falloff and clamp to baseY
            v.y = Mathf.Max(baseY, v.y + yOffset * falloff);

            vertices[i] = v;
        }

        mesh.vertices = vertices;
        mesh.RecalculateNormals();
    }
}
