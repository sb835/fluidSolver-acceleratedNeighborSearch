using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SortingScript : MonoBehaviour
{
    private SimulationScript simulation;
    public int[] testArray;
    public long[] testCellIndices;

    // Start is called before the first frame update
    void Start()
    {
        simulation = GameObject.FindGameObjectWithTag("Simulation").GetComponent<SimulationScript>();

        // Create random array
    }

    // Update is called once per frame
    void Update()
    {
        // testArray = new int[simulation.numParticles + simulation.numBoundaries];
        // testCellIndices = new long[simulation.numParticles + simulation.numBoundaries];
        // for (int i = 0; i < simulation.numParticles + simulation.numBoundaries; i++)
        // {
        //     testArray[i] = simulation.particleArray[i].particleIndex;
        //     testCellIndices[i] = simulation.particleArray[i].cellIndex;
        // }
        // InsertionSort();
    }
}
