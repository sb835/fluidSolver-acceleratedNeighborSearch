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
        testArray = new int[simulation.numParticles + simulation.numBoundaries];
        testCellIndices = new long[simulation.numParticles + simulation.numBoundaries];
        for (int i = 0; i < simulation.numParticles + simulation.numBoundaries; i++)
        {
            testArray[i] = simulation.particleArray[i].particleIndex;
            testCellIndices[i] = simulation.particleArray[i].cellIndex;
        }
        // InsertionSort();
    }
    void InsertionSort()
    {
        // Sort particle attributes with insertion sort
        for (int i = 1; i < testArray.Length; i++)
        {
            int j = i; // index of element we are trying to swap
            // bool tempIsFluid; // temporary variable for swapping
            // Vector2 tempPosition;
            // Vector2 tempVelocity;
            int temp = 0;
            long tempCell = 0;
            // int tempNeighbors;

            while (j > 0 && testCellIndices[j - 1] > testCellIndices[j])
            {
                temp = testArray[j];
                testArray[j] = testArray[j - 1];
                testArray[j - 1] = temp;

                // Sort cells too
                tempCell = testCellIndices[j];
                testCellIndices[j] = testCellIndices[j - 1];
                testCellIndices[j - 1] = tempCell;

                j--;
            }
        }
    }
}
