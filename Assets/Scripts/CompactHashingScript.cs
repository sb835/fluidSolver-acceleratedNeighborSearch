using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using HPCsharp;
using UnityEngine.AI;

public class CompactHashingScript : MonoBehaviour
{
    private GridScript spatialGrid;
    private SimulationScript simulation;
    public bool startCounting;
    public int count;
    private int usedCells;
    private int compactListLength;
    private int[] countCompactList;
    private List<int> compactListHashs;
    private int[] movingParticles;
    public int numMovingParticles;
    private int sumMovingParticles;
    public int avgMovingParticles;
    private int[] compactHashTable;
    private int numCompactHashTable;
    private int containsDublicates;
    private int[] countHashTable;
    private List<int> oldHashes;
    private Vector2[] cellCoordinates;
    private List<bool> hashCollisions;
    private int counter;
    private int numHashCollsions;
    // Start is called before the first frame update
    void Start()
    {
        spatialGrid = GameObject.FindGameObjectWithTag("Grid").GetComponent<GridScript>();
        simulation = GameObject.FindGameObjectWithTag("Simulation").GetComponent<SimulationScript>();
        cellCoordinates = new Vector2[simulation.numParticles + simulation.numBoundaries];
        movingParticles = new int[simulation.numParticles + simulation.numBoundaries];
        counter = 0;
    }

    // Update is called once per frame
    void Update()
    {
        // // Tests
        // compactListLength = spatialGrid.compactList.Count;
        // numMovingParticles = 0;
        // for (int i = 0; i < movingParticles.Length; i++)
        // {
        //     if (movingParticles[i] >= 0)
        //     {
        //         numMovingParticles++;
        //     }
        // }
        // compactHashTable = spatialGrid.compactHashTable;
        // countHashTable = spatialGrid.countHashTable;
        // countCompactList = new int[spatialGrid.compactList.Count];
        // for (int i = 0; i < spatialGrid.compactList.Count; i++)
        // {
        //     int num = 0;
        //     for (int j = 0; j < spatialGrid.compactList[i].Length; j++)
        //     {
        //         if (spatialGrid.compactList[i][j] != -1)
        //         {
        //             num++;
        //         }
        //     }
        //     countCompactList[i] = num;
        // }
        // containsDublicates = ContainsDuplicates(compactHashTable);
        // numCompactHashTable = 0;
        // for (int i = 0; i < compactHashTable.Length; i++)
        // {
        //     if (compactHashTable[i] != -1)
        //     {
        //         numCompactHashTable++;
        //     }
        // }
        // numHashCollsions = 0;
        // for (int i = 0; i < hashCollisions.Count; i++)
        // {
        //     if (hashCollisions[i])
        //     {
        //         numHashCollsions++;
        //     }
        // }
        // compactListHashs = spatialGrid.compactListHashs;
    }

    public void construction()
    {
        if (counter == 0)
        {
            sortParticles();
            constructionWithSetK();
            if (simulation.moveParticles)
            {
                counter++;
            }
        }
        else
        {
            counter++;
            constructionWithSetKUpdate();
            if (counter == 100)
            {
                counter = 0;
            }
        }
    }

    public void sortParticles()
    {

        // Compute cellIndex for every particle
        Parallel.For(0, simulation.numParticles + simulation.numBoundaries, i =>
        {
            simulation.particleArray[i].cellIndex = spatialGrid.computeZIndexForPosition(simulation.positions[simulation.particleArray[i].particleIndex]);
        });

        // Sort attributes after cell indices
        simulation.particleArray = simulation.particleArray.SortMergeStablePar(simulation.comparer);

        Parallel.For(0, simulation.numParticles + simulation.numBoundaries, i =>
        {
            long index = simulation.particleArray[i].particleIndex;

            // Unsort already sorted particle references
            simulation.particleArray[i].particleIndex = i;

            // Sort particle attibutes
            simulation.sortedIsFluid[i] = simulation.isFluid[index];
            simulation.sortedPositions[i] = simulation.positions[index];
            simulation.sortedVelocitys[i] = simulation.velocitys[index];
            simulation.sortedColors[i] = simulation.colors[index];
            simulation.sortedNeighbors[i] = simulation.neighbors[index];
        });
        simulation.isFluid = new List<bool>(simulation.sortedIsFluid).ToArray();
        simulation.positions = new List<Vector2>(simulation.sortedPositions).ToArray();
        simulation.velocitys = new List<Vector2>(simulation.sortedVelocitys).ToArray();
        simulation.colors = new List<Color>(simulation.sortedColors).ToArray();
        simulation.neighbors = new List<int>(simulation.sortedNeighbors).ToArray();
    }

    public void constructionWithSetK()
    {
        // clear HashTable and countHashTable
        Parallel.For(0, spatialGrid.compactHashTable.Length, i =>
        // for (int i = 0; i < spatialGrid.compactHashTable.Length; i++)
        {
            spatialGrid.compactHashTable[i] = -1;
            spatialGrid.countHashTable[i] = 0;
        });

        // Initialize compact list
        spatialGrid.compactList = new List<int[]>();
        spatialGrid.compactListHashs = new List<int>();
        hashCollisions = new List<bool>();


        // Initialize number of used Cells
        usedCells = 0;

        // Iterate over all particles and insert in compact array
        for (int particle = 0; particle < simulation.numParticles + simulation.numBoundaries; particle++)
        {
            int i = simulation.particleArray[particle].particleIndex;
            // Compute hashIndex
            int hashIndex = spatialGrid.computeHashIndex(simulation.positions[i]);
            if (spatialGrid.compactHashTable[hashIndex] == -1)
            {
                // Insert new cell
                int[] cell = new int[spatialGrid.numHashTableEntries];
                for (int x = 0; x < spatialGrid.numHashTableEntries; x++)
                {
                    cell[x] = -1;
                }
                cell[0] = i;
                spatialGrid.compactList.Add(cell);
                spatialGrid.compactListHashs.Add(hashIndex);
                hashCollisions.Add(false);
                spatialGrid.compactHashTable[hashIndex] = spatialGrid.compactList.Count - 1;
                usedCells++;
                spatialGrid.countHashTable[hashIndex]++;
            }
            else
            {
                // Update existing cell
                int cell = spatialGrid.compactHashTable[hashIndex];
                int cellHash = spatialGrid.compactListHashs[cell];
                spatialGrid.compactList[cell][spatialGrid.countHashTable[cellHash]] = i;
                spatialGrid.countHashTable[cellHash]++;

                // Check for hash collisions
                int oldParticleInCell = spatialGrid.compactList[cell][0];
                if (oldParticleInCell != -1 && spatialGrid.computeCellIndex(simulation.positions[oldParticleInCell]) != spatialGrid.computeCellIndex(simulation.positions[i]))
                {
                    hashCollisions[cell] = true;
                }
            }
        }

        // Compute cellIndex for every particle
        Parallel.For(0, simulation.numParticles + simulation.numBoundaries, i =>
        {
            cellCoordinates[i] = spatialGrid.computeCellPosition(simulation.positions[i]);
        });
    }

    public void constructionWithSetKUpdate()
    {
        Parallel.For(0, simulation.numParticles + simulation.numBoundaries, i =>
        {
            movingParticles[i] = -1;
        });

        // Add particles to moving particles
        Parallel.For(0, simulation.numParticles + simulation.numBoundaries, particle =>
        {
            int i = simulation.particleArray[particle].particleIndex;
            Vector2 cellIndex = spatialGrid.computeCellPosition(simulation.positions[i]);
            if (cellIndex != cellCoordinates[i])
            {
                movingParticles[particle] = i;
            }
        });

        // if (startCounting)
        // {
        //     count++;
        //     numMovingParticles = 0;
        //     for (int i = 0; i < movingParticles.Length; i++)
        //     {
        //         if (movingParticles[i] != -1)
        //         {
        //             numMovingParticles++;
        //         }
        //     }
        //     sumMovingParticles += numMovingParticles;
        //     avgMovingParticles = sumMovingParticles / count;
        // }
        // else
        // {
        //     sumMovingParticles = 0;
        //     count = 0;
        // }

        foreach (int particle in movingParticles)
        {
            if (particle == -1)
            {
                continue;
            }
            // simulation.colors[particle] = Color.red;
            // Remove particle from old cell
            int oldHash = spatialGrid.computeHashIndexForCell((int)cellCoordinates[particle].x, (int)cellCoordinates[particle].y);
            int oldCell = spatialGrid.compactHashTable[oldHash];
            spatialGrid.countHashTable[oldHash]--;
            // Remove particle
            for (int j = 0; j < spatialGrid.numHashTableEntries; j++)
            {
                if (spatialGrid.compactList[oldCell][j] == particle)
                {
                    // Swap empty place with last non-empty place
                    spatialGrid.compactList[oldCell][j] = spatialGrid.compactList[oldCell][spatialGrid.countHashTable[oldHash]];
                    spatialGrid.compactList[oldCell][spatialGrid.countHashTable[oldHash]] = -1;

                    break;
                }

            }
            // Check for hash collisions
            if (hashCollisions[oldCell])
            {
                for (int j = 0; j < spatialGrid.numHashTableEntries; j++)
                {
                    if (spatialGrid.compactList[oldCell][j] == -1)
                    {
                        hashCollisions[oldCell] = false;
                        break;
                    }
                    int oldParticleInCell = spatialGrid.compactList[oldCell][0];
                    int particleBefore = spatialGrid.compactList[oldCell][j];
                    if (spatialGrid.computeCellIndex(simulation.positions[oldParticleInCell]) != spatialGrid.computeCellIndex(simulation.positions[particleBefore]))
                    {
                        break;
                    }
                }
            }

            // Remove empty cell
            if (spatialGrid.compactList[oldCell][0] == -1)
            {
                // Swap empty cell with last cell
                int m = spatialGrid.compactList.Count;
                int hash = spatialGrid.compactListHashs[oldCell];
                if (oldCell >= 0 && oldCell < m)
                {
                    // Debug.Log("Delete cell " + oldCell);
                    int lastIndex = m - 1;
                    int lastHash = spatialGrid.compactListHashs[lastIndex];
                    bool lastHashCollision = hashCollisions[lastIndex];
                    if (oldCell != lastIndex)
                    {
                        int[] lastCell = spatialGrid.compactList[lastIndex];
                        spatialGrid.compactList[oldCell] = lastCell;
                        spatialGrid.compactListHashs[oldCell] = lastHash;
                        hashCollisions[oldCell] = lastHashCollision;
                        // Find right hash
                        // int lastHash = spatialGrid.computeHashIndex(simulation.positions[spatialGrid.compactList[lastIndex][0]]);
                        spatialGrid.compactHashTable[lastHash] = oldCell; // ????
                    }
                    spatialGrid.compactHashTable[hash] = -1;  // Doesnt account for hash collisions
                    // Delete last cell
                    spatialGrid.compactList.RemoveAt(lastIndex);
                    spatialGrid.compactListHashs.RemoveAt(lastIndex);
                    hashCollisions.RemoveAt(lastIndex);
                    usedCells--;
                }
                // // Remove element
                // spatialGrid.compactList.RemoveAt(oldCell);
                // // Add compacthashtable up
                // int hash = spatialGrid.compactListHashs[oldCell];
                // spatialGrid.compactListHashs.RemoveAt(oldCell);
                // spatialGrid.compactHashTable[hash] = -1;
                // for (int i = 0; i < spatialGrid.compactHashTable.Length; i++)
                // {
                //     if (spatialGrid.compactHashTable[i] > oldCell)
                //     {
                //         spatialGrid.compactHashTable[i]--;
                //     }
                // }
                // usedCells--;
            }
            // Add particle to new cell
            // Compute hashIndex
            int hashIndex = spatialGrid.computeHashIndex(simulation.positions[particle]);
            // Update hashIndex
            int newCell = spatialGrid.compactHashTable[hashIndex];
            if (newCell == -1)
            {
                // Insert new cell
                int[] cell = new int[spatialGrid.numHashTableEntries];
                for (int x = 0; x < spatialGrid.numHashTableEntries; x++)
                {
                    cell[x] = -1;
                }
                cell[0] = particle;
                spatialGrid.compactList.Add(cell);
                spatialGrid.compactListHashs.Add(hashIndex);
                hashCollisions.Add(false);
                spatialGrid.compactHashTable[hashIndex] = usedCells;
                usedCells++;
                spatialGrid.countHashTable[hashIndex]++;
            }
            else
            {
                // Update existing cell
                int cell = spatialGrid.compactHashTable[hashIndex];
                int cellHash = spatialGrid.compactListHashs[cell];
                spatialGrid.compactList[cell][spatialGrid.countHashTable[cellHash]] = particle;
                spatialGrid.countHashTable[cellHash]++;

                // Check for hash collisions
                if (!hashCollisions[cell])
                {
                    int oldParticleInCell = spatialGrid.compactList[cell][0];
                    if (oldParticleInCell != -1 && spatialGrid.computeCellIndex(simulation.positions[oldParticleInCell]) != spatialGrid.computeCellIndex(simulation.positions[particle]))
                    {
                        hashCollisions[cell] = true;
                    }
                }
            }
        }

        // Compute cellIndex for every particle
        Parallel.For(0, simulation.numParticles + simulation.numBoundaries, i =>
            {
                cellCoordinates[i] = spatialGrid.computeCellPosition(simulation.positions[i]);
            });
    }

    public void query()
    {
        // Find all neighbors for each particle
        if (spatialGrid.parallelSearchActivated)
        {
            Parallel.For(0, spatialGrid.compactList.Count, i =>
            {
                findNeighborsCompactHashing(i);
            });
        }
        else
        {
            for (int i = 0; i < spatialGrid.compactList.Count; i++)
            {
                findNeighborsCompactHashing(i);
            }
        }
    }

    private void findNeighborsCompactHashing(int cell)
    {
        if (hashCollisions[cell])
        {
            findNeighborsCompactListHashCollisions(cell);
        }
        else
        {
            findNeighborsCompactListNoHashCollisions(cell);
        }
        // findNeighborsCompactListHashCollisions(cell);
    }

    private void findNeighborsCompactListHashCollisions(int cell)
    {
        // Get all particles in cell
        int[] particles = spatialGrid.compactList[cell];

        // Check if cell not empty
        if (particles[0] != -1)
        {
            // Find neighbors for all particles
            foreach (int i in particles)
            {
                if (i == -1)
                {
                    break;
                }
                // Only find neighbors for fluid particles
                if (simulation.isFluid[i])
                {
                    // Clear all neighbors
                    for (int n = 0; n < simulation.numParticleNeighbors; n++)
                    {
                        if (simulation.neighborsParticles[simulation.neighbors[i] + n] == -1)
                        {
                            break;
                        }
                        simulation.neighborsParticles[simulation.neighbors[i] + n] = -1;
                    }
                    // Initialize counter
                    int counter = 0;
                    Vector2 gridCell = spatialGrid.computeCellPosition(simulation.positions[i]);
                    for (int x = -1; x <= 1; x++)
                    {
                        for (int y = -1; y <= 1; y++)
                        {
                            int cellX = (int)gridCell.x + x;
                            int cellY = (int)gridCell.y + y;
                            int cellIndex = spatialGrid.computeHashIndexForCell(cellX, cellY);
                            int posInArray = spatialGrid.compactHashTable[cellIndex];
                            if (posInArray >= 0)
                            {
                                foreach (int p in spatialGrid.compactList[posInArray])
                                {
                                    if (p == -1)
                                    {
                                        break;
                                    }
                                    if (Vector2.Distance(simulation.positions[i], simulation.positions[p]) < simulation.kernelSupportRadius)
                                    {
                                        simulation.neighborsParticles[simulation.neighbors[i] + counter] = p;
                                        counter++;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }

    private void findNeighborsCompactListNoHashCollisions(int cell)
    {
        // Get all particles in cell
        int[] particles = spatialGrid.compactList[cell];

        // Check if cell not empty
        if (particles[0] != -1)
        {
            Vector2 currentCell = spatialGrid.computeCellPosition(simulation.positions[particles[0]]);
            int CellX = (int)currentCell.x;
            int CellY = (int)currentCell.y;
            // Compute all neighboring cells
            spatialGrid.neighboringCellIndices[cell][0] = spatialGrid.computeHashIndexForCell(CellX, CellY);
            spatialGrid.neighboringCellIndices[cell][1] = spatialGrid.computeHashIndexForCell(CellX + 1, CellY);
            spatialGrid.neighboringCellIndices[cell][2] = spatialGrid.computeHashIndexForCell(CellX + 1, CellY + 1);
            spatialGrid.neighboringCellIndices[cell][3] = spatialGrid.computeHashIndexForCell(CellX + 1, CellY - 1);
            spatialGrid.neighboringCellIndices[cell][4] = spatialGrid.computeHashIndexForCell(CellX - 1, CellY);
            spatialGrid.neighboringCellIndices[cell][5] = spatialGrid.computeHashIndexForCell(CellX - 1, CellY + 1);
            spatialGrid.neighboringCellIndices[cell][6] = spatialGrid.computeHashIndexForCell(CellX - 1, CellY - 1);
            spatialGrid.neighboringCellIndices[cell][7] = spatialGrid.computeHashIndexForCell(CellX, CellY + 1);
            spatialGrid.neighboringCellIndices[cell][8] = spatialGrid.computeHashIndexForCell(CellX, CellY - 1);

            // Find neighbors for all particles
            foreach (int i in particles)
            {
                if (i == -1)
                {
                    break;
                }
                // Only find neighbors for fluid particles
                if (simulation.isFluid[i])
                {
                    // Clear all neighbors
                    for (int n = 0; n < simulation.numParticleNeighbors; n++)
                    {
                        if (simulation.neighborsParticles[simulation.neighbors[i] + n] == -1)
                        {
                            break;
                        }
                        simulation.neighborsParticles[simulation.neighbors[i] + n] = -1;
                    }
                    // Initialize counter
                    int counter = 0;
                    // Test all potential neighbors in neighboring cells
                    foreach (int cellIndex in spatialGrid.neighboringCellIndices[cell])
                    {
                        int posInArray = spatialGrid.compactHashTable[cellIndex];
                        if (posInArray >= 0)
                        {
                            foreach (int p in spatialGrid.compactList[posInArray])
                            {
                                if (p == -1)
                                {
                                    break;
                                }
                                if (Vector2.Distance(simulation.positions[i], simulation.positions[p]) < simulation.kernelSupportRadius)
                                {
                                    simulation.neighborsParticles[simulation.neighbors[i] + counter] = p;
                                    counter++;
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
