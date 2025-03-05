using System.Collections.Generic;
using UnityEngine;
using CodeMonkey.Utils;
using System;
using System.Threading.Tasks;

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
    public int[] cellLinkedListParticles;
    public long[] cellLinkedListCellIndices;
    public (long cellIndex, int particleIndex)[] sortedParticleArray;
    public int[][] hashTable;
    public int[] compactHashTable;
    public List<int[]> compactList;
    public List<int> compactListHashs;
    public List<List<int>> compactList2;
    public int[] numParticlesCompactArray;
    public int[] countHashTable;
    public int[] displayCountHashTable;
    public int numHashTableEntries;
    // public int[] displayHashTable;
    public long[,] cellZIndices;
    private int firstPrimeNumber = 73856093;
    private int secondPrimeNumber = 83492791;
    public int hashTableLength;
    public int sortingInterval;
    public bool parallelSearchActivated;
    public bool drawGridEnabled;
    public bool randomInitializedParticles;
    public long[][] neighboringCellIndices;
    public int numParticles;
    // Start is called before the first frame update
    void Start()
    {
        simulation = GameObject.FindGameObjectWithTag("Simulation").GetComponent<SimulationScript>();
        // Decide size of grid according to the test scene
        if (simulation.tests == 0)
        {
            width = 50;
            height = 50;
            numParticles = 1225;
        }
        if (simulation.tests == 1)
        {
            width = 50;
            height = 50;
            numParticles = 1225;
        }
        else if (simulation.tests == 3)
        {
            drawGridEnabled = false;
            width = 200;
            height = 200;
            numParticles = 40000;
        }
        else if (simulation.tests == 4)
        {
            drawGridEnabled = false;
            width = 250;
            height = 250;
            simulation.stiffness = 300;
            numParticles = 160000;
        }
        else if (simulation.tests == 5)
        {
            drawGridEnabled = false;
            width = 600;
            height = 600;
            numParticles = 1000000;
        }
        else if (simulation.tests == 6)
        {
            drawGridEnabled = false;
            width = 1300;
            height = 1300;
            numParticles = 4000000;
        }
        else if (simulation.tests == 7)
        {
            drawGridEnabled = false;
            width = 1700;
            height = 1700;
            numParticles = 9000000;
        }
        else if (simulation.tests == 8)
        {
            drawGridEnabled = false;
            width = 2100;
            height = 2100;
            numParticles = 16000000;
        }
        else if (simulation.tests == 9)
        {
            drawGridEnabled = false;
            width = 2500;
            height = 2500;
            numParticles = 25000000;
        }
        else if (simulation.tests == 10)
        {
            drawGridEnabled = false;
            width = 2800;
            height = 2800;
            numParticles = 26832000;
        }
        else if (simulation.tests == 11)
        {
            drawGridEnabled = false;
            width = 1300;
            height = 1300;
            numParticles = 1035000;
        }
        else if (simulation.tests == 12)
        {
            drawGridEnabled = false;
            width = 1000;
            height = 1000;
            numParticles = 1400000;
        }
        else if (simulation.tests == 13)
        {
            drawGridEnabled = false;
            width = 2800;
            height = 2800;
            numParticles = 34200000;
        }
        grid = new List<int>[width, height];
        if (chooseNeighborSearch == 1)
        {
            cellCounter = new int[(width * height * 2) + 1];
        }
        else if (chooseNeighborSearch == 2)
        {
            cellCounter = new int[(width * height * 2) + 1];
        }
        if (chooseNeighborSearch == 3)
        {
            hashTable = new int[numParticles * 4][];
            hashTableLength = hashTable.Length;
            for (int i = 0; i < hashTableLength; i++)
            {
                hashTable[i] = new int[numHashTableEntries];
                for (int j = 0; j < numHashTableEntries; j++)
                {
                    hashTable[i][j] = -1;
                }
            }
            countHashTable = new int[hashTableLength];
            displayCountHashTable = new int[hashTableLength];
        }

        if (chooseNeighborSearch == 4)
        {
            // compact hashing
            compactHashTable = new int[numParticles * 4];
            hashTableLength = compactHashTable.Length;
            for (int i = 0; i < compactHashTable.Length; i++)
            {
                compactHashTable[i] = -1;
            }
            countHashTable = new int[hashTableLength];
        }

        neighboringCellIndices = new long[numParticles * 4][];
        for (int i = 0; i < numParticles * 4; i++)
        {
            neighboringCellIndices[i] = new long[9];
        }
    }

    // Update is called once per frame
    void Update()
    {
        // sortedParticleArray = simulation.particleArray;
        // gridContent = grid[(int)gridPos.x, (int)gridPos.y];
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
    public long computeZIndexForPosition(Vector2 position)
    {
        int CellX = (int)Mathf.Floor(position.x / cellSize);
        int CellY = (int)Mathf.Floor(position.y / cellSize);
        return interleave_uint32_with_zeros(CellX) | (interleave_uint32_with_zeros(CellY) << 1);
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
        int CellX = (int)Mathf.Floor(position.x / cellSize);
        int CellY = (int)Mathf.Floor(position.y / cellSize);
        return new Vector2(CellX, CellY);
    }

    // Returns the cell index of a world position
    public int computeCellIndex(Vector2 position)
    {
        int CellX = (int)Mathf.Floor(position.x / cellSize);
        int CellY = (int)Mathf.Floor(position.y / cellSize);
        return CellX + CellY * height;
    }

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

    // public void DrawGrid()
    // {
    //     for (int x = 0; x < width; x++)
    //     {
    //         for (int y = 0; y < height; y++)
    //         {
    //             // Change to texCoordinates
    //             Vector2 texOne = computeWorldCoords(x, y);
    //             Vector2 texTwo = computeWorldCoords(x, y + 1);
    //             Vector2 texThree = computeWorldCoords(x + 1, y);
    //             if (drawGridEnabled)
    //             {
    //                 // TextMesh text = UtilsClass.CreateWorldText(computeUniqueCellIndex(x, y).ToString(), null, computeWorldCoords(x, y) + new Vector2(cellSize, cellSize) * 0.5f, 20, Color.white, TextAnchor.MiddleCenter);
    //                 // text.characterSize = 0.05f;
    //                 if (chooseNeighborSearch == 0)
    //                 {
    //                     TextMesh text = UtilsClass.CreateWorldText("(" + x + "," + y + ")", null, computeWorldCoords(x, y) + new Vector2(cellSize, cellSize) * 0.5f, 20, Color.white, TextAnchor.MiddleCenter);
    //                     text.characterSize = 0.05f;
    //                 }
    //                 else if (chooseNeighborSearch == 1)
    //                 {
    //                     TextMesh text = UtilsClass.CreateWorldText(computeUniqueCellIndex(x, y).ToString(), null, computeWorldCoords(x, y) + new Vector2(cellSize, cellSize) * 0.5f, 20, Color.white, TextAnchor.MiddleCenter);
    //                     text.characterSize = 0.05f;
    //                 }
    //                 else if (chooseNeighborSearch == 2)
    //                 {
    //                     TextMesh text = UtilsClass.CreateWorldText(computeZIndexForCell(x, y).ToString(), null, computeWorldCoords(x, y) + new Vector2(cellSize, cellSize) * 0.5f, 20, Color.white, TextAnchor.MiddleCenter);
    //                     text.characterSize = 0.05f;
    //                 }
    //                 else if (chooseNeighborSearch == 3)
    //                 {
    //                     TextMesh text = UtilsClass.CreateWorldText(computeHashIndexForCell(x, y).ToString(), null, computeWorldCoords(x, y) + new Vector2(cellSize, cellSize) * 0.5f, 20, Color.white, TextAnchor.MiddleCenter);
    //                     text.characterSize = 0.05f;
    //                 }
    //                 else if (chooseNeighborSearch == 4)
    //                 {
    //                     TextMesh text = UtilsClass.CreateWorldText(computeHashIndexForCell(x, y).ToString(), null, computeWorldCoords(x, y) + new Vector2(cellSize, cellSize) * 0.5f, 20, Color.white, TextAnchor.MiddleCenter);
    //                     text.characterSize = 0.05f;
    //                 }
    //                 else if (chooseNeighborSearch == 5)
    //                 {
    //                     TextMesh text = UtilsClass.CreateWorldText(computeUniqueCellIndex(x, y).ToString(), null, computeWorldCoords(x, y) + new Vector2(cellSize, cellSize) * 0.5f, 20, Color.white, TextAnchor.MiddleCenter);
    //                     text.characterSize = 0.05f;
    //                 }
    //             }
    //             Debug.DrawLine(texOne, texTwo, Color.black, 1000f);
    //             Debug.DrawLine(texOne, texThree, Color.black, 1000f);
    //         }
    //     }
    //     Debug.DrawLine(computeWorldCoords(0, height), computeWorldCoords(width, height), Color.black, 1000f);
    //     Debug.DrawLine(computeWorldCoords(width, 0), computeWorldCoords(width, height), Color.black, 1000f);
    // }

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
            for (int j = 0; j < numHashTableEntries; j++)
            {
                if (hashTable[i][j] == -1) break;
                hashTable[i][j] = -1;
            }
        }
        for (int i = 0; i < hashTableLength; i++)
        {
            countHashTable[i] = 0;
        }
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
                    // TextMesh text = UtilsClass.CreateWorldText(computeUniqueCellIndex(x, y).ToString(), null, computeWorldCoords(x, y) + new Vector2(cellSize, cellSize) * 0.5f, 20, Color.white, TextAnchor.MiddleCenter);
                    // text.characterSize = 0.05f;
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
                    else if (chooseNeighborSearch == 4)
                    {
                        TextMesh text = UtilsClass.CreateWorldText(computeHashIndexForCell(x, y).ToString(), null, computeWorldCoords(x, y) + new Vector2(cellSize, cellSize) * 0.5f, 20, Color.white, TextAnchor.MiddleCenter);
                        text.characterSize = 0.05f;
                    }
                    else if (chooseNeighborSearch == 5)
                    {
                        TextMesh text = UtilsClass.CreateWorldText(computeUniqueCellIndex(x, y).ToString(), null, computeWorldCoords(x, y) + new Vector2(cellSize, cellSize) * 0.5f, 20, Color.white, TextAnchor.MiddleCenter);
                        text.characterSize = 0.05f;
                    }
                }
                Debug.DrawLine(texOne, texTwo, Color.black, 1000f);
                Debug.DrawLine(texOne, texThree, Color.black, 1000f);
            }
        }
        Debug.DrawLine(computeWorldCoords(0, height), computeWorldCoords(width, height), Color.black, 1000f);
        Debug.DrawLine(computeWorldCoords(width, 0), computeWorldCoords(width, height), Color.black, 1000f);
    }

    // Clear the cell counter array
    public void clearCellCounter(int clearNumber)
    {
        for (int i = 0; i < cellCounter.Length; i++)
        {
            cellCounter[i] = clearNumber;
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

    public bool isValidCellIndex(long cellIndex)
    {
        if (cellIndex >= 0 && cellIndex < cellCounter.Length - 2)
        {
            return true;
        }
        else
        {
            return false;
        }
    }
}
