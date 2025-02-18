using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class SpatialHashingScript : MonoBehaviour
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
        spatialGrid.clearHashTable();

        for (int i = 0; i < simulation.numParticles + simulation.numBoundaries; i++)
        {
            int hashIndex = spatialGrid.computeHashIndex(simulation.positions[i]);
            spatialGrid.hashTable[hashIndex][spatialGrid.countHashTable[hashIndex]] = i;
            spatialGrid.countHashTable[hashIndex]++;
        }
    }

    public void query()
    {
        // Find all neighbors for each particle
        if (spatialGrid.parallelSearchActivated)
        {
            Parallel.For(0, spatialGrid.hashTable.Length, i =>
            {
                findNeighborsHashing(i);
            });
        }
        else
        {
            for (int i = 0; i < spatialGrid.hashTable.Length; i++)
            {
                findNeighborsHashing(i);
            }
        }
    }

    private void findNeighborsHashing(int cell)
    {
        // Get all particles in cell
        int[] particles = spatialGrid.hashTable[cell];

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
                        foreach (int p in spatialGrid.hashTable[cellIndex])
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
