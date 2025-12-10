using UnityEngine;
using System.Collections.Generic;

public class SpawnOnDestroyedObjects : MonoBehaviour
{
    [Header("Spawning Configuration")]
    [Tooltip("The item prefab that will appear once all targets are destroyed.")]
    public GameObject itemToSpawnPrefab;

    [Header("Targets To Destroy")]
    [Tooltip("Drag the 20 cobwebs (or other destroyable objects) here.")]
    public List<GameObject> targetsToDestroy;

    private bool hasSpawned = false;

    void Start()
    {
        // we assume something is wrong (or unassigned) and prevent the item 
        // from spawning immediately.
        if (targetsToDestroy == null || targetsToDestroy.Count == 0)
        {
            Debug.LogWarning($"[{gameObject.name}] Target list is empty at Start! Item spawn disabled to prevent bugs.");
            hasSpawned = true; // Set to true so Update() never runs the spawn logic
            return;
        }

        // CONFIRMATION: This tells you exactly how many items the script sees.
        Debug.Log($"[{gameObject.name}] Game Started. Waiting for {targetsToDestroy.Count} targets to be destroyed.");
    }

    void Update()
    {
        // 1. If we have already spawned the item, stop checking.
        if (hasSpawned) return;

        // 2. Remove any items from the list that have become null (Destroyed).
        // Note: Disabling an object (SetActive false) does NOT remove it. It must be Destroyed.
        targetsToDestroy.RemoveAll(item => item == null);

        // 3. Check if the list is now empty.
        if (targetsToDestroy.Count == 0)
        {
            SpawnItem();
        }
    }

    void SpawnItem()
    {
        if (itemToSpawnPrefab != null)
        {
            // 1. Define the desired position, setting Y to 2.55 and keeping the Spawner's X and Z.
            Vector3 spawnPosition = transform.position;
            spawnPosition.y = 2.55f;

            // 2. Define the specific rotation (X=0, Y=0, Z=182.207).
            Quaternion spawnRotation = Quaternion.Euler(0f, 0f, 182.207f);

            // 3. Instantiate the item using the new position and rotation.
            Instantiate(itemToSpawnPrefab, spawnPosition, spawnRotation);
            
            hasSpawned = true;
            Debug.Log("All targets destroyed! Spawning " + itemToSpawnPrefab.name);
        }
        else
        {
            Debug.LogError("All targets destroyed, but 'Item To Spawn Prefab' is missing on " + gameObject.name);
        }
    }
}