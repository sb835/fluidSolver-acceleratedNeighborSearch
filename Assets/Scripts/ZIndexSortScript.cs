using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using HPCsharp;

public class ZIndexSortScript : MonoBehaviour
{
    private GridScript spatialGrid;
    private SimulationScript simulation;
    // Start is called before the first frame update
    void Start()
    {
        spatialGrid = GameObject.FindGameObjectWithTag("Grid").GetComponent<GridScript>();
        simulation = GameObject.FindGameObjectWithTag("Simulation").GetComponent<SimulationScript>();
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void construction()
    {
        if (simulation.moveParticles || !simulation.firstSortingDone)
        {
            simulation.firstSortingDone = true;
            if (simulation.constructionCounter == spatialGrid.sortingInterval)
            {
                constructionMergeSortAttributes();
                simulation.constructionCounter = 0;
            }
            else
            {
                constructionMergeSortReferences();
                simulation.constructionCounter++;
            }
        }
    }

    public void query()
    {
        // Find all neighbors for each particle
        if (spatialGrid.parallelSearchActivated)
        {
            Parallel.For(0, spatialGrid.cellCounter.Length - 1, i =>
            {
                findNeighborsZIndexSort(i);
            });
        }
        else
        {
            for (int particle = 0; particle < simulation.numParticles + simulation.numBoundaries; particle++)
            {
                int i = simulation.particleArray[particle].particleIndex;
                findNeighborsZIndexSort(i);
            }
        }
    }

    public void constructionMergeSortReferences()
    {
        // Clear cell counter
        spatialGrid.clearCellCounter(-1);

        // Compute cellIndex for every particle
        Parallel.For(0, simulation.numParticles + simulation.numBoundaries, i =>
        {
            simulation.particleArray[i].cellIndex = spatialGrid.computeZIndexForPosition(simulation.positions[simulation.particleArray[i].particleIndex]);
        });

        simulation.particleArray = simulation.particleArray.SortMergeStablePar(simulation.comparer);

        // Put particle references in cellCounter
        if (spatialGrid.isValidCellIndex(simulation.particleArray[0].cellIndex))
        {
            spatialGrid.cellCounter[simulation.particleArray[0].cellIndex] = 0;
        }
        Parallel.For(1, simulation.numParticles + simulation.numBoundaries, i =>
        {
            int j = i - 1;
            if (simulation.particleArray[i].cellIndex != simulation.particleArray[j].cellIndex)
            {
                if (spatialGrid.isValidCellIndex(simulation.particleArray[i].cellIndex))
                {
                    spatialGrid.cellCounter[simulation.particleArray[i].cellIndex] = i;
                }
            }
        });

        // Fill empyt cells with number of next cell
        int cellCounterLength = spatialGrid.cellCounter.Length;
        int cellBefore = simulation.numParticles + simulation.numBoundaries;
        for (int i = cellCounterLength - 1; i > 0; i--)
        {
            if (spatialGrid.cellCounter[i] == -1)
            {
                spatialGrid.cellCounter[i] = cellBefore;
            }
            else
            {
                cellBefore = spatialGrid.cellCounter[i];
            }
        }
        spatialGrid.cellCounter[0] = 0;
    }

    public void constructionMergeSortAttributes()
    {
        // Clear cell counter
        spatialGrid.clearCellCounter(-1);

        // Compute cellIndex for every particle
        Parallel.For(0, simulation.numParticles + simulation.numBoundaries, i =>
        {
            simulation.particleArray[i].cellIndex = spatialGrid.computeZIndexForPosition(simulation.positions[simulation.particleArray[i].particleIndex]);
        });

        // Sort attributes after cell attributes
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

        // Put particle references in cellCounter
        if (spatialGrid.isValidCellIndex(simulation.particleArray[0].cellIndex))
        {
            spatialGrid.cellCounter[simulation.particleArray[0].cellIndex] = 0;
        }
        Parallel.For(1, simulation.numParticles + simulation.numBoundaries, i =>
        {
            int j = i - 1;
            if (simulation.particleArray[i].cellIndex != simulation.particleArray[j].cellIndex)
            {
                if (spatialGrid.isValidCellIndex(simulation.particleArray[i].cellIndex))
                {
                    spatialGrid.cellCounter[simulation.particleArray[i].cellIndex] = i;
                }
            }
        });

        // Fill empyt cells with number of next cell
        int cellCounterLength = spatialGrid.cellCounter.Length;
        int cellBefore = 0;
        for (int i = cellCounterLength - 1; i > 0; i--)
        {
            if (spatialGrid.cellCounter[i] == -1)
            {
                spatialGrid.cellCounter[i] = cellBefore;
            }
            else
            {
                cellBefore = spatialGrid.cellCounter[i];
            }
        }
        spatialGrid.cellCounter[0] = 0;
    }

    void findNeighborsZIndexSort(int c)
    {
        // Check if cell is empty otherwise skip
        int cellStart = spatialGrid.cellCounter[c];
        int cellEnd = spatialGrid.cellCounter[c + 1];
        if (cellStart < cellEnd)
        {
            Vector2 currentCell = spatialGrid.computeCellPosition(simulation.positions[simulation.particleArray[cellStart].particleIndex]);
            int CellX = (int)currentCell.x;
            int CellY = (int)currentCell.y;
            // Compute all neighboring cells
            spatialGrid.neighboringCellIndices[c][0] = spatialGrid.computeZIndexForCell(CellX, CellY);
            spatialGrid.neighboringCellIndices[c][1] = spatialGrid.computeZIndexForCell(CellX + 1, CellY);
            spatialGrid.neighboringCellIndices[c][2] = spatialGrid.computeZIndexForCell(CellX + 1, CellY + 1);
            spatialGrid.neighboringCellIndices[c][3] = spatialGrid.computeZIndexForCell(CellX + 1, CellY - 1);
            spatialGrid.neighboringCellIndices[c][4] = spatialGrid.computeZIndexForCell(CellX - 1, CellY);
            spatialGrid.neighboringCellIndices[c][5] = spatialGrid.computeZIndexForCell(CellX - 1, CellY + 1);
            spatialGrid.neighboringCellIndices[c][6] = spatialGrid.computeZIndexForCell(CellX - 1, CellY - 1);
            spatialGrid.neighboringCellIndices[c][7] = spatialGrid.computeZIndexForCell(CellX, CellY + 1);
            spatialGrid.neighboringCellIndices[c][8] = spatialGrid.computeZIndexForCell(CellX, CellY - 1);
            // Iterate over all particles in cell c
            for (int ii = cellStart; ii < cellEnd; ii++)
            {
                int i = simulation.particleArray[ii].particleIndex;
                // Only look for neighbors if i is a fluid particle
                if (simulation.isFluid[i])
                {
                    // Clear all neighbors
                    for (int n = 0; n < simulation.numParticleNeighbors; n++)
                    {
                        simulation.neighborsParticles[simulation.neighbors[i] + n] = -1;
                    }
                    // Initialize counter
                    int counter = 0;
                    // Test all potential neighbors in neighboring cells
                    foreach (int cell in spatialGrid.neighboringCellIndices[c])
                    {
                        if (cell >= 0 && cell < spatialGrid.cellCounter.Length)
                        {
                            int cellStart2 = spatialGrid.cellCounter[cell];
                            int cellEnd2 = spatialGrid.cellCounter[cell + 1];
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
}