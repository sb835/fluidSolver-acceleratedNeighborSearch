using System.Collections.Generic;
using UnityEngine;
using CodeMonkey.Utils;
using System;

public class GridScript : MonoBehaviour
{
    private SimulationScript simulation;
    public int width;
    public int height;
    public float cellSize;
    public List<int>[,] grid;
    public Vector2 gridPos;
    public List<int> gridContent;
    public int chooseNeighborSearch;
    public int[] cellCounter;
    public int[] sortedParticleArray;
    public List<int>[] hashTable;
    public int[] displayHashTable;
    public long[,] cellZIndices;
    private int firstPrimeNumber = 73856093;
    private int secondPrimeNumber = 19349663;
    private int hashTableLength;
    public bool parallelSearchActivated;
    public bool drawGridEnabled;
    private bool sortValues;
    public bool randomInitializedParticles;

    private int texResolution;
    private Camera mainCamera;
    // Start is called before the first frame update
    void Start()
    {
        simulation = GameObject.FindGameObjectWithTag("Simulation").GetComponent<SimulationScript>();
        grid = new List<int>[width, height];
        cellCounter = new int[(width * height * 2) + 1];
        hashTable = new List<int>[width * height * 2];
        hashTableLength = hashTable.Length;
        displayHashTable = new int[hashTableLength];

        // Precompute z-indices for all cells
        cellZIndices = new long[width, height];
        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                cellZIndices[i, j] = computeZIndexForCell(i, j);
            }
        }

        // Initialize simulation variables
        texResolution = simulation.texResolution;
        mainCamera = simulation.mainCamera;
    }

    // Update is called once per frame
    void Update()
    {
        sortedParticleArray = simulation.particleArray.ToArray();
        gridContent = grid[(int)gridPos.x, (int)gridPos.y];

        countHashTableEntries();
    }

    // Interleave to 64 bit
    public long interleave_uint32_with_zeros(int input)
    {
        long word = input;
        word = (word ^ (word << 16)) & 0x0000ffff0000ffff;
        word = (word ^ (word << 8)) & 0x00ff00ff00ff00ff;
        word = (word ^ (word << 4)) & 0x0f0f0f0f0f0f0f0f;
        word = (word ^ (word << 2)) & 0x3333333333333333;
        word = (word ^ (word << 1)) & 0x5555555555555555;
        return word;
    }

    // Compute Z-Index for a cell
    public long computeZIndexForCell(int x, int y)
    {
        return interleave_uint32_with_zeros(x) | (interleave_uint32_with_zeros(y) << 1);
    }

    // Compute Z-Index for a position
    // public long computeZIndexForPosition(Vector2 position)
    // {
    //     int CellX = (int)Mathf.Floor(position.x / cellSize);
    //     int CellY = (int)Mathf.Floor(position.y / cellSize);
    //     return interleave_uint32_with_zeros(CellX) | (interleave_uint32_with_zeros(CellY) << 1);
    // }

    // Returns the world coordinates 
    // in the upper right corner for our grid cell
    public Vector2 computeWorldCoords(float x, float y)
    {
        return new Vector2(x, y) * cellSize;
    }

    // Returns the cell position of a world position
    public Vector2 computeCellPosition(Vector2 position)
    {
        int CellX = (int)Mathf.Floor(position.x / cellSize);
        int CellY = (int)Mathf.Floor(position.y / cellSize);
        return new Vector2(CellX, CellY);
    }

    // Returns the cell index of a world position
    // public int computeCellIndex(Vector2 position)
    // {
    //     int CellX = (int)Mathf.Floor(position.x / cellSize);
    //     int CellY = (int)Mathf.Floor(position.y / cellSize);
    //     return CellX + CellY * height;
    // }

    // Returns a unique cell identifier for a cell
    public long computeUniqueCellIndex(int CellX, int CellY)
    {
        return CellX + CellY * height;
    }

    // Returns a hashIndex for a position
    public int computeHashIndex(Vector2 position)
    {
        int CellX = (int)Mathf.Floor(position.x / cellSize);
        int CellY = (int)Mathf.Floor(position.y / cellSize);
        int hash = ((CellX * firstPrimeNumber) ^ (CellY * secondPrimeNumber)) % hashTableLength;
        // Returns negative hash values
        return Math.Abs(hash);
    }

    // Return a hashIndex for a cell
    public int computeHashIndexForCell(int CellX, int CellY)
    {
        int hash = ((CellX * firstPrimeNumber) ^ (CellY * secondPrimeNumber)) % hashTableLength;
        // Returns negative hash values
        return Math.Abs(hash);
    }

    public void DrawGrid()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                // Change to texCoordinates
                Vector2 texOne = computeWorldCoords(x, y);
                Vector2 texTwo = computeWorldCoords(x, y + 1);
                Vector2 texThree = computeWorldCoords(x + 1, y);
                if (drawGridEnabled)
                {
                    if (chooseNeighborSearch == 0)
                    {
                        TextMesh text = UtilsClass.CreateWorldText("(" + x + "," + y + ")", null, computeWorldCoords(x, y) + new Vector2(cellSize, cellSize) * 0.5f, 20, Color.white, TextAnchor.MiddleCenter);
                        text.characterSize = 0.05f;
                    }
                    else if (chooseNeighborSearch == 1)
                    {
                        TextMesh text = UtilsClass.CreateWorldText(computeUniqueCellIndex(x, y).ToString(), null, computeWorldCoords(x, y) + new Vector2(cellSize, cellSize) * 0.5f, 20, Color.white, TextAnchor.MiddleCenter);
                        text.characterSize = 0.05f;
                    }
                    else if (chooseNeighborSearch == 2)
                    {
                        TextMesh text = UtilsClass.CreateWorldText(computeZIndexForCell(x, y).ToString(), null, computeWorldCoords(x, y) + new Vector2(cellSize, cellSize) * 0.5f, 20, Color.white, TextAnchor.MiddleCenter);
                        text.characterSize = 0.05f;
                    }
                    else if (chooseNeighborSearch == 3)
                    {
                        TextMesh text = UtilsClass.CreateWorldText(computeHashIndexForCell(x, y).ToString(), null, computeWorldCoords(x, y) + new Vector2(cellSize, cellSize) * 0.5f, 20, Color.white, TextAnchor.MiddleCenter);
                        text.characterSize = 0.05f;
                    }
                    // TextMesh text = UtilsClass.CreateWorldText(Convert.ToString(computeZIndexForCell(x, y), 2), null, computeWorldCoords(x, y) + new Vector2(cellSize, cellSize) * 0.5f, 20, Color.white, TextAnchor.MiddleCenter);
                }
                Debug.DrawLine(texOne, texTwo, Color.black, 1000f);
                Debug.DrawLine(texOne, texThree, Color.black, 1000f);
            }
        }
        Debug.DrawLine(computeWorldCoords(0, height), computeWorldCoords(width, height), Color.black, 1000f);
        Debug.DrawLine(computeWorldCoords(width, 0), computeWorldCoords(width, height), Color.black, 1000f);
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

    // Clear the hash table
    public void clearHashTable()
    {
        for (int i = 0; i < hashTableLength; i++)
        {
            hashTable[i] = new List<int>();
        }
    }

    // Count hash table entries
    public void countHashTableEntries()
    {
        for (int i = 0; i < hashTableLength; i++)
        {
            displayHashTable[i] = hashTable[i].Count;
        }
    }

    // Clear the cell counter array
    public void clearCellCounter()
    {
        for (int i = 0; i < cellCounter.Length; i++)
        {
            cellCounter[i] = 0;
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
