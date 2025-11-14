using UnityEngine;

public class ChangeColorToRed : MonoBehaviour
{
    [Header("Color Settings")]
    [SerializeField] private Color redColor = new Color(1f, 0.3f, 0.3f); // Brighter red
    [SerializeField] private bool changeAllMeshes = true; // Change all child meshes
    [SerializeField] private string specificMeshName = ""; // Leave empty to change all, or specify name like "Body"
    [SerializeField] private string[] excludeNames = new string[] { "GoblinSword" }; // Meshes to skip

    void Start()
    {
        if (changeAllMeshes)
        {
            // Change all SkinnedMeshRenderers
            SkinnedMeshRenderer[] renderers = GetComponentsInChildren<SkinnedMeshRenderer>();
            foreach (SkinnedMeshRenderer renderer in renderers)
            {
                // Skip if in exclude list
                if (ShouldExclude(renderer.gameObject.name)) continue;

                renderer.material = new Material(renderer.material);
                renderer.material.color = redColor;
                Debug.Log($"[ChangeColorToRed] Changed {renderer.gameObject.name} to red");
            }

            // Also change regular MeshRenderers
            MeshRenderer[] meshRenderers = GetComponentsInChildren<MeshRenderer>();
            foreach (MeshRenderer meshRenderer in meshRenderers)
            {
                // Skip if in exclude list
                if (ShouldExclude(meshRenderer.gameObject.name)) continue;

                meshRenderer.material = new Material(meshRenderer.material);
                meshRenderer.material.color = redColor;
                Debug.Log($"[ChangeColorToRed] Changed {meshRenderer.gameObject.name} to red (MeshRenderer)");
            }

            if (renderers.Length == 0 && meshRenderers.Length == 0)
            {
                Debug.LogWarning($"[ChangeColorToRed] No renderers found on {gameObject.name}");
            }
        }
        else
        {
            // Find specific mesh by name
            SkinnedMeshRenderer[] renderers = GetComponentsInChildren<SkinnedMeshRenderer>();
            foreach (SkinnedMeshRenderer renderer in renderers)
            {
                if (ShouldExclude(renderer.gameObject.name)) continue;

                if (string.IsNullOrEmpty(specificMeshName) || renderer.gameObject.name.Contains(specificMeshName))
                {
                    renderer.material = new Material(renderer.material);
                    renderer.material.color = redColor;
                    Debug.Log($"[ChangeColorToRed] Changed {renderer.gameObject.name} to red");
                }
            }
        }
    }

    bool ShouldExclude(string objectName)
    {
        foreach (string excludeName in excludeNames)
        {
            if (objectName.Contains(excludeName))
            {
                Debug.Log($"[ChangeColorToRed] Skipping {objectName} (excluded)");
                return true;
            }
        }
        return false;
    }
}