using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using HPCsharp;
using System;
using System.Linq;

public class CellLinkedListScript : MonoBehaviour
{
    private GridScript spatialGrid;
    private SimulationScript simulation;
    public int[] cellLinkedListParticle;
    public long[] cellLinkedListCellIndex;
    public int[] markers;
    public long[] scans;
    public float timeStep;
    // Start is called before the first frame update
    void Start()
    {
        spatialGrid = GameObject.FindGameObjectWithTag("Grid").GetComponent<GridScript>();
        simulation = GameObject.FindGameObjectWithTag("Simulation").GetComponent<SimulationScript>();
    }

    // Update is called once per frame
    void Update()
    {
        // cellLinkedListParticle = spatialGrid.cellLinkedListParticles;
        // cellLinkedListCellIndex = spatialGrid.cellLinkedListCellIndices;
        // markers = simulation.markers;
        // scans = simulation.scans;
        // timeStep = simulation.tS;
    }

    public void construction()
    {
        if (simulation.moveParticles || !simulation.firstSortingDone)
        {
            simulation.firstSortingDone = true;
            if (simulation.constructionCounter == spatialGrid.sortingInterval)
            {
                constructionAttributes();
                simulation.constructionCounter = 0;
            }
            else
            {
                constructionReferences();
                simulation.constructionCounter++;
            }
        }
    }

    public void constructionAttributes()
    {
        // Compute cellIndex for every particle
        Parallel.For(0, simulation.numParticles + simulation.numBoundaries, i =>
        {
            simulation.particleArray[i].cellIndex = spatialGrid.computeCellIndex(simulation.positions[simulation.particleArray[i].particleIndex]);
        });

        // Sort particleArray after cellIndex
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

        // Create marker and scan values
        long previousCell = -1;
        int accumScan = 0;
        for (int i = 0; i < simulation.numParticles + simulation.numBoundaries; i++)
        {
            if (simulation.particleArray[i].cellIndex != previousCell)
            {
                simulation.markers[i] = 1;
                previousCell = simulation.particleArray[i].cellIndex;
                accumScan++;
                simulation.scans[i] = accumScan;
            }
            else
            {
                simulation.markers[i] = 0;
                simulation.scans[i] = previousCell;
            }
        }

        // Initialize compact linked list
        spatialGrid.cellLinkedListParticles = new int[accumScan + 1];
        spatialGrid.cellLinkedListCellIndices = new long[accumScan + 1];
        Parallel.For(0, simulation.numParticles + simulation.numBoundaries, i =>
        {
            if (simulation.markers[i] == 1)
            {
                spatialGrid.cellLinkedListParticles[simulation.scans[i] - 1] = i; // Store refernce to particleArray
                spatialGrid.cellLinkedListCellIndices[simulation.scans[i] - 1] = simulation.particleArray[i].cellIndex;
            }
        });

        // Fill last cell with -1 that stands for number of particles in order to compute last cell
        spatialGrid.cellLinkedListParticles[accumScan] = simulation.numParticles + simulation.numBoundaries;
        spatialGrid.cellLinkedListCellIndices[accumScan] = -1;
    }

    public void constructionReferences()
    {
        // Compute cellIndex for every particle
        Parallel.For(0, simulation.numParticles + simulation.numBoundaries, i =>
        {
            simulation.particleArray[i].cellIndex = spatialGrid.computeCellIndex(simulation.positions[simulation.particleArray[i].particleIndex]);
        });

        // Sort particleArray after cellIndex
        simulation.particleArray = simulation.particleArray.SortMergeStablePar(simulation.comparer);

        // Create marker and scan values
        long previousCell = -1;
        int accumScan = 0;
        for (int i = 0; i < simulation.numParticles + simulation.numBoundaries; i++)
        {
            if (simulation.particleArray[i].cellIndex != previousCell)
            {
                simulation.markers[i] = 1;
                previousCell = simulation.particleArray[i].cellIndex;
                accumScan++;
                simulation.scans[i] = accumScan;
            }
            else
            {
                simulation.markers[i] = 0;
                simulation.scans[i] = previousCell;
            }
        }

        // Initialize compact linked list
        spatialGrid.cellLinkedListParticles = new int[accumScan + 1];
        spatialGrid.cellLinkedListCellIndices = new long[accumScan + 1];
        Parallel.For(0, simulation.numParticles + simulation.numBoundaries, i =>
        {
            if (simulation.markers[i] == 1)
            {
                spatialGrid.cellLinkedListParticles[simulation.scans[i] - 1] = i; // Store refernce to particleArray
                spatialGrid.cellLinkedListCellIndices[simulation.scans[i] - 1] = simulation.particleArray[i].cellIndex;
            }
        });

        // Fill last cell with -1 that stands for number of particles in order to compute last cell
        spatialGrid.cellLinkedListParticles[accumScan] = simulation.numParticles + simulation.numBoundaries;
        spatialGrid.cellLinkedListCellIndices[accumScan] = -1;
    }

    public void query()
    {
        // Find all neighbors for each particle
        if (spatialGrid.parallelSearchActivated)
        {
            Parallel.For(0, spatialGrid.cellLinkedListParticles.Length - 1, i =>
            {
                findNeighborsCellLinkedList(i);
            });
        }
        else
        {
            for (int i = 0; i < spatialGrid.cellLinkedListParticles.Length - 1; i++)
            {
                findNeighborsCellLinkedList(i);
            }
        }
    }

    void findNeighborsCellLinkedList(int c)
    {
        long cellIndex = spatialGrid.cellLinkedListCellIndices[c];
        // Compute all neighboring cells
        spatialGrid.neighboringCellIndices[c][0] = c;
        spatialGrid.neighboringCellIndices[c][1] = c + 1;
        spatialGrid.neighboringCellIndices[c][2] = c - 1;
        int cUp = Array.BinarySearch(spatialGrid.cellLinkedListCellIndices, cellIndex + spatialGrid.width);
        spatialGrid.neighboringCellIndices[c][3] = cUp;
        spatialGrid.neighboringCellIndices[c][4] = cUp + 1;
        spatialGrid.neighboringCellIndices[c][5] = cUp - 1;
        int cDown = Array.BinarySearch(spatialGrid.cellLinkedListCellIndices, cellIndex - spatialGrid.width);
        spatialGrid.neighboringCellIndices[c][6] = cDown;
        spatialGrid.neighboringCellIndices[c][7] = cDown + 1;
        spatialGrid.neighboringCellIndices[c][8] = cDown - 1;

        // Vector2 currentCell = spatialGrid.computeCellPosition(simulation.positions[simulation.particleArray[spatialGrid.cellLinkedListParticles[c]].particleIndex]);
        // int CellX = (int)currentCell.x;
        // int CellY = (int)currentCell.y;
        // // Compute all neighboring cells
        // spatialGrid.neighboringCellIndices[c][0] = Array.BinarySearch(spatialGrid.cellLinkedListCellIndices, spatialGrid.computeZIndexForCell(CellX, CellY));
        // spatialGrid.neighboringCellIndices[c][1] = Array.BinarySearch(spatialGrid.cellLinkedListCellIndices, spatialGrid.computeZIndexForCell(CellX + 1, CellY));
        // spatialGrid.neighboringCellIndices[c][2] = Array.BinarySearch(spatialGrid.cellLinkedListCellIndices, spatialGrid.computeZIndexForCell(CellX + 1, CellY + 1));
        // spatialGrid.neighboringCellIndices[c][3] = Array.BinarySearch(spatialGrid.cellLinkedListCellIndices, spatialGrid.computeZIndexForCell(CellX + 1, CellY - 1));
        // spatialGrid.neighboringCellIndices[c][4] = Array.BinarySearch(spatialGrid.cellLinkedListCellIndices, spatialGrid.computeZIndexForCell(CellX - 1, CellY));
        // spatialGrid.neighboringCellIndices[c][5] = Array.BinarySearch(spatialGrid.cellLinkedListCellIndices, spatialGrid.computeZIndexForCell(CellX - 1, CellY + 1));
        // spatialGrid.neighboringCellIndices[c][6] = Array.BinarySearch(spatialGrid.cellLinkedListCellIndices, spatialGrid.computeZIndexForCell(CellX - 1, CellY - 1));
        // spatialGrid.neighboringCellIndices[c][7] = Array.BinarySearch(spatialGrid.cellLinkedListCellIndices, spatialGrid.computeZIndexForCell(CellX, CellY + 1));
        // spatialGrid.neighboringCellIndices[c][8] = Array.BinarySearch(spatialGrid.cellLinkedListCellIndices, spatialGrid.computeUniqueCellIndex(CellX, CellY - 1));

        // Iterate over all particles in cell i
        int cellStart = spatialGrid.cellLinkedListParticles[c];
        int cellEnd = spatialGrid.cellLinkedListParticles[c + 1];
        for (int ii = cellStart; ii < cellEnd; ii++)
        {
            int i = simulation.particleArray[ii].particleIndex;
            // Only look for neighbors if i is a fluid particle
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
                foreach (int index in spatialGrid.neighboringCellIndices[c])
                {
                    // Lookup index
                    if (index >= 0 && index < spatialGrid.cellLinkedListParticles.Length - 1)
                    {
                        int cellStart2 = spatialGrid.cellLinkedListParticles[index];
                        int cellEnd2 = spatialGrid.cellLinkedListParticles[index + 1];
                        for (int j = cellStart2; j < cellEnd2; j++)
                        {
                            if (Vector2.Distance(simulation.positions[i], simulation.positions[simulation.particleArray[j].particleIndex]) < simulation.kernelSupportRadius)
                            {
                                simulation.neighborsParticles[simulation.neighbors[i] + counter] = simulation.particleArray[j].particleIndex;
                                counter++;
                            }
                        }
                    }
                }
            }
        }
    }
}
