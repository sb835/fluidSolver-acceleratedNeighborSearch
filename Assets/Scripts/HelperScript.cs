using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HelperScript : MonoBehaviour
{
    private SimulationScript simulation;
    // Start is called before the first frame update
    void Start()
    {
        simulation = GameObject.FindGameObjectWithTag("Simulation").GetComponent<SimulationScript>();
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void ResetValues()
    {
        simulation.particleArray = new (long cellIndex, int particleIndex)[simulation.numParticles + simulation.numBoundaries];
        simulation.isFluid = new bool[simulation.numParticles + simulation.numBoundaries];
        simulation.positions = new Vector2[simulation.numParticles + simulation.numBoundaries];
        simulation.velocitys = new Vector2[simulation.numParticles + simulation.numBoundaries];
        simulation.colors = new Color[simulation.numParticles + simulation.numBoundaries];
        simulation.densitys = new float[simulation.numParticles + simulation.numBoundaries];
        simulation.pressures = new float[simulation.numParticles + simulation.numBoundaries];
        simulation.forces = new Vector2[simulation.numParticles + simulation.numBoundaries];
        simulation.nPForces = new Vector2[simulation.numParticles + simulation.numBoundaries];

        simulation.neighbors = new int[simulation.numParticles + simulation.numBoundaries];
        int spaceforNeighbors = (simulation.numParticles + simulation.numBoundaries) * simulation.numParticleNeighbors;
        simulation.neighborsParticles = new int[spaceforNeighbors];
        for (int i = 0; i < spaceforNeighbors; i++)
        {
            simulation.neighborsParticles[i] = -1;
        }
    }

    public void initializeParticles(bool reset, int numX, int numY, Vector2 start, float spacing)
    {
        if (reset)
        {
            ResetValues();
            simulation.numParticles = 0;
            simulation.numBoundaries = 0;
        }
        else
        {
            // allocate new memory
            int newFluidParticles = numX * numY;
            int lengthNewArrays = simulation.numParticles + simulation.numBoundaries + newFluidParticles;
            List<(long cellIndex, int particleIndex)> newParticles = new List<(long cellIndex, int particleIndex)>();
            List<Vector2> newPositions = new List<Vector2>();
            List<Vector2> newVelocitys = new List<Vector2>();
            List<Color> newColors = new List<Color>();
            List<float> newDensitys = new List<float>();
            List<float> newPressures = new List<float>();
            List<Vector2> newForces = new List<Vector2>();
            List<Vector2> newNPForces = new List<Vector2>();

            // Insert old fluid particles
            for (int i = 0; i < simulation.numParticles + simulation.numBoundaries; i++)
            {
                if (simulation.isFluid[i])
                {
                    newParticles.Add(simulation.particleArray[i]);
                    newPositions.Add(simulation.positions[i]);
                    newVelocitys.Add(simulation.velocitys[i]);
                    newColors.Add(simulation.colors[i]);
                    newDensitys.Add(simulation.densitys[i]);
                    newPressures.Add(simulation.pressures[i]);
                    newForces.Add(simulation.forces[i]);
                    newNPForces.Add(simulation.nPForces[i]);
                }
            }

            simulation.particleArray = new (long cellIndex, int particleIndex)[lengthNewArrays];
            newParticles.CopyTo(simulation.particleArray, 0);
            simulation.positions = new Vector2[lengthNewArrays];
            newPositions.CopyTo(simulation.positions, 0);
            simulation.velocitys = new Vector2[lengthNewArrays];
            newVelocitys.CopyTo(simulation.velocitys, 0);
            simulation.colors = new Color[lengthNewArrays];
            newColors.CopyTo(simulation.colors, 0);
            simulation.densitys = new float[lengthNewArrays];
            newDensitys.CopyTo(simulation.densitys, 0);
            simulation.pressures = new float[lengthNewArrays];
            newPressures.CopyTo(simulation.pressures, 0);
            simulation.forces = new Vector2[lengthNewArrays];
            newForces.CopyTo(simulation.forces, 0);
            simulation.nPForces = new Vector2[lengthNewArrays];
            newNPForces.CopyTo(simulation.nPForces, 0);

            simulation.numBoundaries = 0;
        }
        for (int x = 0; x < numX; x++)
        {
            float xx = start.x + spacing * x;
            for (int y = 0; y < numY; y++)
            {
                float yy = start.y + spacing * y;
                InitParticle(new Vector2(xx, yy), Color.blue);
            }
        }
    }

    public void initializeBorder(int leftLength, int rightLength, int topLength, int bottomLength, int layers)
    {
        // Box
        // Down
        for (int l = 0; l < layers; l++)
        {
            float x = 0.0f + simulation.particleSize;
            float y = 0.0f + simulation.particleSize + (2 * l) * simulation.particleSize;
            for (int i = 0; i < bottomLength; i++)
            {
                InitBoundaryParticle(new Vector2(x, y));

                x += 2 * simulation.particleSize;
            }
        }

        // Top
        for (int l = 0; l < layers; l++)
        {
            float x = 0.0f - simulation.particleSize;
            float y = (leftLength * 2 * simulation.particleSize) + simulation.particleSize + (2 * l) * simulation.particleSize;
            for (int i = 0; i < topLength; i++)
            {
                InitBoundaryParticle(new Vector2(x, y));

                x += 2 * simulation.particleSize;
            }
        }

        // Left
        for (int l = 0; l < layers; l++)
        {
            float x = 0.0f + simulation.particleSize + (2 * l) * simulation.particleSize;
            float y = 0.0f + 5 * simulation.particleSize;
            for (int i = 0; i < leftLength; i++)
            {
                InitBoundaryParticle(new Vector2(x, y));

                y += 2 * simulation.particleSize;
            }
        }

        // Right
        for (int l = 0; l < layers; l++)
        {
            float x = (bottomLength * 2 * simulation.particleSize) - 3 * simulation.particleSize + (2 * l) * simulation.particleSize;
            float y = 0.0f + 5 * simulation.particleSize;
            for (int i = 0; i < rightLength; i++)
            {
                InitBoundaryParticle(new Vector2(x, y));

                y += 2 * simulation.particleSize;
            }
        }
    }

    public void setLine(Vector2 start, int length, float angle, int layers, int direction = 1)
    {
        for (int l = 0; l < layers; l++)
        {
            float x = start.x;
            float y = start.y;
            y += (2 * l) * simulation.particleSize;
            for (int i = 0; i < length; i++)
            {
                InitBoundaryParticle(new Vector2(x, y));
                x += 2 * direction * simulation.particleSize;
                y += angle;
            }
        }
    }

    public void initializeBorder0(bool doubleBorder)
    {
        // boundaryPositions = new List<Vector2>();
        // boundaryColors = new List<Color>();
        // Left row
        for (float y = simulation.start.y; y < simulation.boundaries.y; y += simulation.particleSize * 2)
        {
            InitBoundaryParticle(new Vector2(simulation.start.x, y));
        }


        // Double
        if (doubleBorder)
        {
            for (float y = simulation.start.y; y < simulation.boundaries.y + 4 * simulation.particleSize; y += simulation.particleSize * 2)
            {
                InitBoundaryParticle(new Vector2(simulation.start.x - 2 * simulation.particleSize, y));
            }
        }

        // Right row
        for (float y = simulation.start.y; y < simulation.boundaries.y; y += simulation.particleSize * 2)
        {
            InitBoundaryParticle(new Vector2(simulation.boundaries.x, y));
        }

        // Double
        if (doubleBorder)
        {
            for (float y = simulation.start.y - 2 * simulation.particleSize; y < simulation.boundaries.y + 2 * simulation.particleSize; y += simulation.particleSize * 2)
            {
                InitBoundaryParticle(new Vector2(simulation.boundaries.x + 2 * simulation.particleSize, y));
            }
        }

        // Bottom row
        for (float x = simulation.start.x; x < simulation.boundaries.x; x += simulation.particleSize * 2)
        {
            InitBoundaryParticle(new Vector2(x, simulation.start.y));
        }

        // Double
        if (doubleBorder)
        {
            for (float x = simulation.start.x - 2 * simulation.particleSize; x < simulation.boundaries.x; x += simulation.particleSize * 2)
            {
                InitBoundaryParticle(new Vector2(x, simulation.start.y - 2 * simulation.particleSize));
            }
        }

        // Top row
        for (float x = simulation.start.x; x < simulation.boundaries.x; x += simulation.particleSize * 2)
        {
            InitBoundaryParticle(new Vector2(x, simulation.boundaries.y + 0.5f * simulation.particleSize));
        }

        // Double
        if (doubleBorder)
        {
            for (float x = simulation.start.x; x < simulation.boundaries.x + 2 * simulation.particleSize; x += simulation.particleSize * 2)
            {
                InitBoundaryParticle(new Vector2(x, simulation.boundaries.y + 2.5f * simulation.particleSize));
            }
        }
    }

    public void initializeBorder2()
    {
        // boundaryPositions = new List<Vector2>();
        // boundaryColors = new List<Color>();

        // Diagonal left
        float x = 0.0f + 4 * simulation.particleSize;
        float y = 11.0f;
        for (int i = 0; i < 55; i++)
        {
            InitBoundaryParticle(new Vector2(x, y));

            x += 2 * simulation.particleSize;
            y -= 2 * simulation.particleSize;
        }

        // Double
        x = 0.0f + 4 * simulation.particleSize;
        y = 11.0f - 2 * simulation.particleSize;
        for (int i = 0; i < 55; i++)
        {
            InitBoundaryParticle(new Vector2(x, y));

            x += 2 * simulation.particleSize;
            y -= 2 * simulation.particleSize;
        }

        // Diagonal right
        x = 20.0f - 6 * simulation.particleSize;
        y = 11.0f;
        for (int i = 0; i < 80; i++)
        {
            InitBoundaryParticle(new Vector2(x, y));

            x -= 2 * simulation.particleSize;
            y -= 0.8f * simulation.particleSize;
        }

        // Double
        x = 20.0f - 6f * simulation.particleSize;
        y = 11.0f - 2f * simulation.particleSize;
        for (int i = 0; i < 80; i++)
        {
            InitBoundaryParticle(new Vector2(x, y));

            x -= 2 * simulation.particleSize;
            y -= 0.8f * simulation.particleSize;
        }

        // Bottom
        x = 3.0f;
        y = 3.0f;
        for (int i = 0; i < 100; i++)
        {
            InitBoundaryParticle(new Vector2(x, y));

            x += 2 * simulation.particleSize;
        }

        // Double
        x = 3.0f;
        y = 3.0f - 2 * simulation.particleSize;
        for (int i = 0; i < 100; i++)
        {
            InitBoundaryParticle(new Vector2(x, y));

            x += 2 * simulation.particleSize;
        }

        // Bottom left
        x = 3.0f;
        y = 3.0f;
        for (int i = 0; i < 18; i++)
        {
            InitBoundaryParticle(new Vector2(x, y));

            x -= 2 * simulation.particleSize;
            y += 2 * simulation.particleSize;
        }

        // Double
        x = 3.0f;
        y = 3.0f - 2 * simulation.particleSize;
        for (int i = 0; i < 18; i++)
        {
            InitBoundaryParticle(new Vector2(x, y));

            x -= 2 * simulation.particleSize;
            y += 2 * simulation.particleSize;
        }

        // Bottom right
        x = 15.0f;
        y = 3.0f;
        for (int i = 0; i < 20; i++)
        {
            InitBoundaryParticle(new Vector2(x, y));

            x += 2 * simulation.particleSize;
            y += 2 * simulation.particleSize;
        }

        // Double
        x = 15.0f;
        y = 3.0f - 2 * simulation.particleSize;
        for (int i = 0; i < 20; i++)
        {
            InitBoundaryParticle(new Vector2(x, y));

            x += 2 * simulation.particleSize;
            y += 2 * simulation.particleSize;
        }

        // Box
        // Down
        x = 0.0f + simulation.particleSize;
        y = 0.0f + simulation.particleSize;
        for (int i = 0; i < 166; i++)
        {
            InitBoundaryParticle(new Vector2(x, y));

            x += 2 * simulation.particleSize;
        }

        // Double
        x = 0.0f + simulation.particleSize;
        y = 0.0f + 3 * simulation.particleSize;
        for (int i = 0; i < 166; i++)
        {
            InitBoundaryParticle(new Vector2(x, y));

            x += 2 * simulation.particleSize;
        }

        // Top
        x = 0.0f - simulation.particleSize;
        y = 20.0f - simulation.particleSize;
        for (int i = 0; i < 166; i++)
        {
            InitBoundaryParticle(new Vector2(x, y));

            x += 2 * simulation.particleSize;
        }

        // Double
        x = 0.0f - simulation.particleSize;
        y = 20.0f - 3 * simulation.particleSize;
        for (int i = 0; i < 166; i++)
        {
            InitBoundaryParticle(new Vector2(x, y));

            x += 2 * simulation.particleSize;
        }

        // Left
        x = 0.0f + simulation.particleSize;
        y = 0.0f + 5 * simulation.particleSize;
        for (int i = 0; i < 163; i++)
        {
            InitBoundaryParticle(new Vector2(x, y));

            y += 2 * simulation.particleSize;
        }

        // Double
        x = 0.0f + 3 * simulation.particleSize;
        y = 0.0f + 5 * simulation.particleSize;
        for (int i = 0; i < 163; i++)
        {
            InitBoundaryParticle(new Vector2(x, y));

            y += 2 * simulation.particleSize;
        }

        // Right
        x = 20.0f - 2 * simulation.particleSize;
        y = 0.0f + 5 * simulation.particleSize;
        for (int i = 0; i < 163; i++)
        {
            InitBoundaryParticle(new Vector2(x, y));

            y += 2 * simulation.particleSize;
        }

        // Double
        x = 20.0f - 4 * simulation.particleSize;
        y = 0.0f + 5 * simulation.particleSize;
        for (int i = 0; i < 163; i++)
        {
            InitBoundaryParticle(new Vector2(x, y));

            y += 2 * simulation.particleSize;
        }

    }

    public void initializeBorderWall(float xOffset, bool exists)
    {
        if (exists)
        {
            for (float y = simulation.start.y + 2 * simulation.particleSize; y < simulation.boundaries.y; y += simulation.particleSize * 2)
            {
                InitBoundaryParticle(new Vector2(simulation.start.x + xOffset, y));

                InitBoundaryParticle(new Vector2(simulation.start.x + xOffset + 2 * simulation.particleSize, y));
            }
        }
    }

    // Initialize particle
    private void InitParticle(Vector2 position, Color color)
    {
        simulation.particleArray[simulation.numParticles] = (0, simulation.numParticles);
        simulation.isFluid[simulation.numParticles] = true;
        simulation.positions[simulation.numParticles] = position;
        simulation.velocitys[simulation.numParticles] = new Vector2(0, 0);
        simulation.colors[simulation.numParticles] = color;
        simulation.neighbors[simulation.numParticles] = simulation.numParticles * simulation.numParticleNeighbors;
        simulation.densitys[simulation.numParticles] = 0.0f;
        simulation.pressures[simulation.numParticles] = 0.0f;
        simulation.forces[simulation.numParticles] = new Vector2(0, 0);
        simulation.nPForces[simulation.numParticles] = new Vector2(0, 0);

        simulation.numParticles++;
    }

    // Initialize boundary particle
    private void InitBoundaryParticle(Vector2 position)
    {
        simulation.particleArray[simulation.numParticles + simulation.numBoundaries] = (0, simulation.numParticles + simulation.numBoundaries);
        simulation.isFluid[simulation.numParticles] = false;
        simulation.positions[simulation.numParticles + simulation.numBoundaries] = position;
        simulation.colors[simulation.numParticles + simulation.numBoundaries] = Color.black;
        simulation.velocitys[simulation.numParticles + simulation.numBoundaries] = new Vector2(0, 0);
        simulation.densitys[simulation.numParticles + simulation.numBoundaries] = 0.0f;
        simulation.pressures[simulation.numParticles + simulation.numBoundaries] = 0.0f;
        simulation.forces[simulation.numParticles + simulation.numBoundaries] = new Vector2(0, 0);
        simulation.nPForces[simulation.numParticles + simulation.numBoundaries] = new Vector2(0, 0);

        simulation.numBoundaries++;
    }


}
