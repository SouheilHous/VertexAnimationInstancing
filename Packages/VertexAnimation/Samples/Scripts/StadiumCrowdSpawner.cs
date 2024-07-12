using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StadiumCrowdSpawner : MonoBehaviour
{
    public List<GameObject> personPrefabs; // List of prefabs of the people to be instantiated
    public BoxCollider boxCollider; // Box Collider defining the initial rectangle
    public int levels; // Number of levels to scale up
    public float characterSpacing = 1f; // Spacing between people
    public float levelSpacing = 2f; // Spacing between levels

    void Start()
    {
        if (boxCollider != null && personPrefabs.Count > 0)
        {
            SpawnCrowd();
        }
        else
        {
            Debug.LogError("BoxCollider is not assigned or prefabs list is empty!");
        }
    }

    void SpawnCrowd()
    {
        Vector3 center = boxCollider.transform.TransformPoint(boxCollider.center);
        Vector3 size = boxCollider.size;

        for (int level = 0; level < levels; level++)
        {
            float offset = level * levelSpacing;

            // Calculate the new corners for the current level
            Vector3 currentBottomLeft = new Vector3(center.x - size.x / 2 - offset, 0, center.z - size.z / 2 - offset);
            Vector3 currentTopRight = new Vector3(center.x + size.x / 2 + offset, 0, center.z + size.z / 2 + offset);

            // Bottom edge
            for (float x = currentBottomLeft.x; x <= currentTopRight.x; x += characterSpacing)
            {
                InstantiatePerson(new Vector3(x, 0, currentBottomLeft.z), center);
            }

            // Top edge
            for (float x = currentBottomLeft.x; x <= currentTopRight.x; x += characterSpacing)
            {
                InstantiatePerson(new Vector3(x, 0, currentTopRight.z), center);
            }

            // Left edge
            for (float z = currentBottomLeft.z + characterSpacing; z < currentTopRight.z; z += characterSpacing) // Avoid corners
            {
                InstantiatePerson(new Vector3(currentBottomLeft.x, 0, z), center);
            }

            // Right edge
            for (float z = currentBottomLeft.z + characterSpacing; z < currentTopRight.z; z += characterSpacing) // Avoid corners
            {
                InstantiatePerson(new Vector3(currentTopRight.x, 0, z), center);
            }
        }
    }

    void InstantiatePerson(Vector3 position, Vector3 center)
    {
        GameObject personPrefab = personPrefabs[Random.Range(0, personPrefabs.Count)];
        GameObject person = Instantiate(personPrefab, position, Quaternion.identity);
        Vector3 direction = center - position;
        direction.y = 0; // Keep only the horizontal direction
        person.transform.rotation = Quaternion.LookRotation(direction);
    }
}
