using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Demo usage, spawning a large number of active characters
/// </summary>
public class WarInstance : MonoBehaviour
{
    public GameObject characterPrefab; // The running character prefab
    public int army1Seed;
    public int army2Seed;
    public float speed;
    public float initialDistance;
    public int spacing = 1;
    public int gridWidth;
    public int gridHeight;

    private GameObject army1Parent;
    private GameObject army2Parent;

    void Start()
    {
        // Create army parent objects
        army1Parent = new GameObject("Army1");
        army2Parent = new GameObject("Army2");

        // Seed the random generators
        Random.InitState(army1Seed);
        GenerateArmy(army1Parent, -initialDistance / 2, 90);

        Random.InitState(army2Seed);
        GenerateArmy(army2Parent, initialDistance / 2, -90);
    }

    void Update()
    {
        // Move the army parents towards each other
        army1Parent.transform.Translate(Vector3.right * speed * Time.deltaTime);
        army2Parent.transform.Translate(Vector3.left * speed * Time.deltaTime);
    }

    void GenerateArmy(GameObject armyParent, float startX, int targetRotation)
    {
        for (var i = 0; i < gridWidth * spacing; i += spacing)
        {
            for (var j = 0; j < gridHeight * spacing; j += spacing)
            {
                Vector3 position = new Vector3(startX + i, 0, j - (gridHeight / 2.0f) * spacing);
                Vector3 rotation = new Vector3(0, targetRotation, 0);
                Instantiate(characterPrefab, position, Quaternion.Euler(rotation), armyParent.transform);
            }
        }
    }
}
