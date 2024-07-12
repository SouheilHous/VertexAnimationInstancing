using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnManager : MonoBehaviour
{
    public enum SpawnShape { Grid, Circle }

    #region Properties

    public GameObject spawnPrefab;
    public SpawnShape spawnShape;
    public int gridWidth;
    public int gridHeight;
    public int targetRotation;
    public int spacing = 1;
    public float radius;

    #endregion

    #region Methods

    void Start()
    {
        if (spawnShape == SpawnShape.Grid)
        {
            SpawnGrid();
        }
        else if (spawnShape == SpawnShape.Circle)
        {
            SpawnCircle();
        }
    }

    void SpawnGrid()
    {
        // Calculate the offset to center the grid at the world origin
        float xOffset = (gridWidth * spacing) / 2f - spacing / 2f;
        float zOffset = (gridHeight * spacing) / 2f - spacing / 2f;

        for (var i = 0; i < gridWidth; i++)
        {
            for (var j = 0; j < gridHeight; j++)
            {
                Vector3 position = new Vector3(i * spacing - xOffset, 0, j * spacing - zOffset);
                Vector3 rotation = new Vector3(0, targetRotation, 0);
                Instantiate(spawnPrefab, position, Quaternion.Euler(rotation));
            }
        }
    }

    void SpawnCircle()
    {
        float currentRadius = 0;
        while (currentRadius <= radius)
        {
            float circumference = 2 * Mathf.PI * currentRadius;
            int numberOfPoints = Mathf.Max(1, Mathf.FloorToInt(circumference / spacing));

            for (int i = 0; i < numberOfPoints; i++)
            {
                float angle = i * 360.0f / numberOfPoints;
                float x = Mathf.Cos(angle * Mathf.Deg2Rad) * currentRadius;
                float z = Mathf.Sin(angle * Mathf.Deg2Rad) * currentRadius;
                Vector3 position = new Vector3(x, 0, z);
                Vector3 rotation = new Vector3(0, Random.Range(0, 360), 0);
                Instantiate(spawnPrefab, position, Quaternion.Euler(rotation));
            }

            currentRadius += spacing;
        }
    }

    #endregion
}
