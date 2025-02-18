using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class BasicGridScript : MonoBehaviour
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
        // clear the grid
        // iterate one time over all grid cells
        spatialGrid.emptyGrid();

        // Add the number of each particle in the respective grid cell
        // iterate one time over all particles
        for (int particle = 0; particle < simulation.numParticles + simulation.numBoundaries; particle++)
        {
            int i = simulation.particleArray[particle].particleIndex;
            Vector2 gridCoords = spatialGrid.computeCellPosition(simulation.positions[i]);
            if (spatialGrid.isValidCell(gridCoords))
            {
                spatialGrid.grid[(int)gridCoords.x, (int)gridCoords.y].Add(i);
            }
        }
    }

    public void query()
    {
        // Find all neighbors for each particle
        if (spatialGrid.parallelSearchActivated)
        {
            Parallel.For(0, simulation.numParticles + simulation.numBoundaries, particle =>
        {
            int i = simulation.particleArray[particle].particleIndex;
            if (simulation.isFluid[i])
            {
                findNeighbors(i);
            }
        });
        }
        else
        {
            for (int particle = 0; particle < simulation.numParticles + simulation.numBoundaries; particle++)
            {
                int i = simulation.particleArray[particle].particleIndex;
                if (simulation.isFluid[i])
                {
                    findNeighbors(i);
                }
            };
        }
    }

    private void findNeighbors(int i)
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
                if (spatialGrid.isValidCell(new Vector2(cellX, cellY)))
                {
                    List<int> potentialNeighbors = spatialGrid.grid[cellX, cellY];
                    for (int j = 0; j < potentialNeighbors.Count; j++)
                    {
                        if (Vector2.Distance(simulation.positions[i], simulation.positions[potentialNeighbors[j]]) < simulation.kernelSupportRadius)
                        {
                            // n.Add(potentialNeighbors[j]);
                            simulation.neighborsParticles[simulation.neighbors[i] + counter] = potentialNeighbors[j];
                            counter++;
                        }
                    }
                }
            }
        }
        // neighbors[i] = n;
    }
}
