using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridScript : MonoBehaviour
{
    private SimulationScript simulation;
    public int width;
    public int height;
    public float cellSize;
    public List<int>[,] grid;

    private int texResolution;
    private Camera mainCamera;
    // Start is called before the first frame update
    void Start()
    {
        simulation = GameObject.FindGameObjectWithTag("Simulation").GetComponent<SimulationScript>();
        grid = new List<int>[width, height];

        // Initialize simulation variables
        texResolution = simulation.texResolution;
        mainCamera = simulation.mainCamera;
    }

    // Update is called once per frame
    void Update()
    {

    }

    // Returns the world coordinates 
    // in the upper right corner for our grid cell
    public Vector2 computeWorldCoords(float x, float y)
    {
        return new Vector2(x, y) * cellSize;
    }

    // Returns the cell position of a world position
    public Vector2 computeCellPosition(Vector2 position)
    {
        int CellX = (int)Mathf.Floor((position.x) / cellSize);
        int CellY = (int)Mathf.Floor((position.y) / cellSize);
        return new Vector2(CellX, CellY);
    }

    // Returns the cell index of a world position
    public int computeCellIndex(Vector2 position)
    {
        int CellX = (int)Mathf.Floor((position.x) / cellSize);
        int CellY = (int)Mathf.Floor((position.y) / cellSize);
        return CellX + CellY * 2;
    }

    public void DrawGrid()
    {
        for (float x = 0; x < width; x++)
        {
            for (float y = 0; y < height; y++)
            {
                // Change to texCoordinates
                Vector2 texOne = computeWorldCoords(x, y);
                Vector2 texTwo = computeWorldCoords(x, y + 1);
                Vector2 texThree = computeWorldCoords(x + 1, y);
                Debug.DrawLine(texOne, texTwo, Color.black, 100f);
                Debug.DrawLine(texOne, texThree, Color.black, 100f);
            }
        }
    }

    // Fill the grid with empty lists
    public void emptyGrid()
    {
        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                grid[i, j] = new List<int>();
            }
        }
    }

    public bool isValidCell(Vector2 cell)
    {
        if (cell.x >= 0 && cell.y >= 0 && cell.x < width && cell.y < height)
        {
            return true;
        }
        return false;
    }
}
