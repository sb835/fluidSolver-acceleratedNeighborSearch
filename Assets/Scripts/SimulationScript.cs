using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.IO;
using System.Diagnostics;
using System.Threading.Tasks;
using System;
using Unity.VisualScripting;
using UnityEngine.Scripting;

public class SimulationScript : MonoBehaviour
{
    public bool moveParticles = false;
    public bool colorParticles = false;
    public bool countAvgDensity = false;
    public bool countCFLConditions = false;
    public bool resetParticles = true;
    public float borderOffset = 3.0f;
    public int tests = 1;
    private double sumConstructionTime = 0;
    private double sumQueryTime = 0;
    public int tS;
    public int maxTS;
    public List<float> averageDensity;
    public List<float> cflConditions;
    public List<double> queryTimes;
    public Vector2 amountParticles;
    public Vector2 particlePosition;
    public float particleStartSpacing;
    public float speedColor = 4.0f;
    public float alpha;
    public (int particleIndex, long cellIndex)[] particleHandles;
    public int[] particleArray;
    public Vector2[] positions;
    public Vector2[] velocitys;
    public Color[] colors;
    public int[] numNeighbors;
    public int[] neighbors;
    public int[] neighborsParticles;
    public float[] densitys;
    public float[] pressures;
    public Vector2[] forces;
    public Vector2[] nPForces;
    public Vector2 maxVelocity = new Vector2(2, 9);
    public float timeStepMultiplyer = 0.9f;
    public float timeStep;
    public float stiffness = 100.0f;
    public float v;
    public int numParticles;
    public int numBoundaries;
    public int numParticleNeighbors;
    public float particleSize;
    public float particleMass;
    public float particleSpacing;
    public float particleVolume;
    public float startDensity = 1.5f;
    public int texResolution = 2048;
    private float quadWidth;
    private float quadHeight;
    public float kernelSupportRadius;
    public (int particleIndex, int cellIndex)[] sortedHandles;
    public int[] sortedParticles;
    public Vector2[] sortedPositions;
    public Vector2[] sortedVelocitys;
    public Color[] sortedColors;
    public int[] sortedNeighbors;
    public float[] sortedDensitys;
    public float[] sortedPressures;
    public Vector2[] sortedForces;
    public Vector2[] sortedNPForces;
    private DrawCirclesScript drawCirclesScript;
    private GridScript spatialGrid;
    private MeasurementsScript measurements;
    public Camera mainCamera;
    private Vector2 mouse;
    private Vector2 start;
    private Vector2 boundaries;
    private float previousBorderOffset;
    private bool firstSortingDone;
    public long[,] cellZIndices;
    public bool doubleBorder;

    private double sumSimulationStep;
    private double sumDrawingTime;
    private double sumUpdateTime;
    private double sumDensityTime;
    private double sumPressureTime;
    private double sumNPForcesTime;
    private double sumPressureForcesTime;
    private double sumMoveParticlesTime;
    private int watchCounter;
    private double densityTime;
    private double pressureTime;
    private double NPForcesTime;
    private double pressureForcesTime;
    private double moveParticlesTime;
    private int constructionCounter;



    // Forces
    public float gravity;


    // Not meant to be seen
    public int currentParticle;
    public Vector2 currentGridCell;
    // Start is called before the first frame update
    void Start()
    {
        // Initialize all values
        drawCirclesScript = GameObject.FindGameObjectWithTag("DrawCircle").GetComponent<DrawCirclesScript>();
        spatialGrid = GameObject.FindGameObjectWithTag("Grid").GetComponent<GridScript>();
        measurements = GameObject.FindGameObjectWithTag("Measurements").GetComponent<MeasurementsScript>();


        particleSpacing = spatialGrid.cellSize / 2;
        particleVolume = particleSpacing * particleSpacing;
        particleMass = startDensity * particleVolume;
        particleSize = particleMass;
        kernelSupportRadius = particleSpacing * 2;
        firstSortingDone = false;

        watchCounter = 1;
        constructionCounter = 0;


        // Precompute z-indices for all cells
        cellZIndices = new long[spatialGrid.width, spatialGrid.height];
        for (int i = 0; i < spatialGrid.width; i++)
        {
            for (int j = 0; j < spatialGrid.height; j++)
            {
                cellZIndices[i, j] = spatialGrid.computeZIndexForCell(i, j);
            }
        }

        // Create array of particle CellIndices
        for (int i = 0; i < numParticles + numBoundaries; i++)
        {
            spatialGrid.cellKeys[i] = spatialGrid.computeCellIndex(positions[i]);
        }

        // Define boundaries of our simulation domain
        start = new Vector2(1, 1);
        boundaries = new Vector2(16, 9);

        numParticles = (int)amountParticles.x * (int)amountParticles.y;
        if (tests == 0)
        {
            numBoundaries = 910;
        }
        else if (tests == 1)
        {
            numBoundaries = 1862;
        }
        else if (tests == 2)
        {
            numBoundaries = 166 * 8;
        }
        else if (tests == 3)
        {
            numBoundaries = 4 * 332 + 4 * 623;
        }
        else if (tests == 4)
        {
            numBoundaries = 8 * 800;
        }
        else if (tests == 5)
        {
            numBoundaries = 4 * 1700 + 4 * 2000;
        }

        else if (tests == 6)
        {
            numBoundaries = 8 * 4000;
        }

        else if (tests == 7)
        {
            numBoundaries = 8 * 6000;
        }

        spatialGrid.cellKeys = new int[numParticles + numBoundaries];

        particleHandles = new (int particleIndex, long cellIndex)[numParticles + numBoundaries];
        particleArray = new int[numParticles + numBoundaries];
        positions = new Vector2[numParticles + numBoundaries];
        velocitys = new Vector2[numParticles + numBoundaries];
        colors = new Color[numParticles + numBoundaries];
        densitys = new float[numParticles + numBoundaries];
        pressures = new float[numParticles + numBoundaries];
        forces = new Vector2[numParticles + numBoundaries];
        nPForces = new Vector2[numParticles + numBoundaries];

        neighbors = new int[numParticles + numBoundaries];
        int spaceforNeighbors = (numParticles + numBoundaries) * numParticleNeighbors;
        neighborsParticles = new int[spaceforNeighbors];
        for (int i = 0; i < spaceforNeighbors; i++)
        {
            neighborsParticles[i] = -1;
        }

        initializeParticles(resetParticles, (int)amountParticles.x, (int)amountParticles.y, particlePosition, particleStartSpacing);

        // Reorder Particles
        if (spatialGrid.randomInitializedParticles)
        {
            ShuffleParticlesRandom();
        }

        // Initialize Grid

        spatialGrid.emptyGrid();
        spatialGrid.clearCellCounter();
        spatialGrid.clearHashTable();

        //Draw grid
        spatialGrid.DrawGrid();

        // Initialize boundary particles
        if (tests == 0)
        {
            initializeBorder0(doubleBorder);
            initializeBorderWall(borderOffset, doubleBorder);
            mainCamera.orthographicSize = 5;
            mainCamera.transform.position = new Vector3(8.91f, 5, -10);

            drawCirclesScript.transform.position = new Vector3(50, 50, 0);
            drawCirclesScript.transform.localScale = new Vector3(100, 100, 0);
        }
        else if (tests == 1)
        {
            initializeBorder2();
            mainCamera.orthographicSize = 10;
            mainCamera.transform.position = new Vector3(12, 10, -10);

            drawCirclesScript.transform.position = new Vector3(50, 50, 0);
            drawCirclesScript.transform.localScale = new Vector3(100, 100, 0);
        }

        else if (tests == 2)
        {
            initializeBorder(166, 166, 166, 166, true);
            mainCamera.orthographicSize = 10;
            mainCamera.transform.position = new Vector3(12, 10, -10);

            drawCirclesScript.transform.position = new Vector3(50, 50, 0);
            drawCirclesScript.transform.localScale = new Vector3(100, 100, 0);
        }

        else if (tests == 3)
        {
            initializeBorder(332, 332, 623, 623, true);
            mainCamera.orthographicSize = 23;
            mainCamera.transform.position = new Vector3(41f, 23, -10);

            drawCirclesScript.transform.position = new Vector3(50, 50, 0);
            drawCirclesScript.transform.localScale = new Vector3(100, 100, 0);
        }

        else if (tests == 4)
        {
            initializeBorder(800, 800, 800, 800, true);
            mainCamera.orthographicSize = 40;
            mainCamera.transform.position = new Vector3(50, 40, -10);

            drawCirclesScript.transform.position = new Vector3(50, 50, 0);
            drawCirclesScript.transform.localScale = new Vector3(100, 100, 0);
        }

        else if (tests == 5)
        {
            initializeBorder(1700, 1700, 2000, 2000, true);
            mainCamera.orthographicSize = 120;
            mainCamera.transform.position = new Vector3(155, 120, -10);

            drawCirclesScript.transform.position = new Vector3(250, 250, 0);
            drawCirclesScript.transform.localScale = new Vector3(500, 500, 0);
        }

        else if (tests == 6)
        {
            initializeBorder(4000, 4000, 4000, 4000, true);
            mainCamera.orthographicSize = 220;
            mainCamera.transform.position = new Vector3(280, 220, -10);

            drawCirclesScript.transform.position = new Vector3(250, 250, 0);
            drawCirclesScript.transform.localScale = new Vector3(500, 500, 0);
        }

        else if (tests == 7)
        {
            initializeBorder(6000, 6000, 6000, 6000, true);
            mainCamera.orthographicSize = 220;
            mainCamera.transform.position = new Vector3(280, 220, -10);

            drawCirclesScript.transform.position = new Vector3(350, 350, 0);
            drawCirclesScript.transform.localScale = new Vector3(700, 700, 0);
        }

        texResolution = drawCirclesScript.texResolution;
        quadWidth = drawCirclesScript.transform.localScale.x;
        quadHeight = drawCirclesScript.transform.localScale.y;

        // Inform the shader about the total amount of drawn particles
        drawCirclesScript.total = numParticles + numBoundaries;

        averageDensity = new List<float>();

        cflConditions = new List<float>();

        queryTimes = new List<double>();

        previousBorderOffset = borderOffset;

        sortedHandles = new (int particleIndex, int cellIndex)[numParticles + numBoundaries];
        sortedParticles = new int[numParticles + numBoundaries];
        sortedPositions = new Vector2[numParticles + numBoundaries];
        sortedVelocitys = new Vector2[numParticles + numBoundaries];
        sortedColors = new Color[numParticles + numBoundaries];
        sortedNeighbors = new int[numParticles + numBoundaries];
        sortedDensitys = new float[numParticles + numBoundaries];
        sortedPressures = new float[numParticles + numBoundaries];
        sortedForces = new Vector2[numParticles + numBoundaries];
        sortedNPForces = new Vector2[numParticles + numBoundaries];

    }
    void ResetValues()
    {
        particleHandles = new (int particleIndex, long cellIndex)[numParticles + numBoundaries];
        particleArray = new int[numParticles + numBoundaries];
        positions = new Vector2[numParticles + numBoundaries];
        velocitys = new Vector2[numParticles + numBoundaries];
        colors = new Color[numParticles + numBoundaries];
        densitys = new float[numParticles + numBoundaries];
        pressures = new float[numParticles + numBoundaries];
        forces = new Vector2[numParticles + numBoundaries];
        nPForces = new Vector2[numParticles + numBoundaries];

        neighbors = new int[numParticles + numBoundaries];
        int spaceforNeighbors = (numParticles + numBoundaries) * numParticleNeighbors;
        neighborsParticles = new int[spaceforNeighbors];
        for (int i = 0; i < spaceforNeighbors; i++)
        {
            neighborsParticles[i] = -1;
        }
    }

    void initializeParticles(bool reset, int numX, int numY, Vector2 start, float spacing)
    {
        if (reset)
        {
            ResetValues();
            numParticles = 0;
            numBoundaries = 0;
        }
        else
        {
            // allocate new memory
            int newFluidParticles = numX * numY;
            int lengthNewArrays = numParticles + numBoundaries + newFluidParticles;
            List<int> newParticles = new List<int>();
            List<Vector2> newPositions = new List<Vector2>();
            List<Vector2> newVelocitys = new List<Vector2>();
            List<Color> newColors = new List<Color>();
            List<float> newDensitys = new List<float>();
            List<float> newPressures = new List<float>();
            List<Vector2> newForces = new List<Vector2>();
            List<Vector2> newNPForces = new List<Vector2>();

            // Insert old fluid particles
            for (int j = 0; j < numParticles + numBoundaries; j++)
            {
                int i = particleHandles[j].particleIndex;
                if (particleArray[i] < numParticles)
                {
                    newParticles.Add(particleArray[i]);
                    newPositions.Add(positions[i]);
                    newVelocitys.Add(velocitys[i]);
                    newColors.Add(colors[i]);
                    newDensitys.Add(densitys[i]);
                    newPressures.Add(pressures[i]);
                    newForces.Add(forces[i]);
                    newNPForces.Add(nPForces[i]);
                }
            }

            particleArray = new int[lengthNewArrays];
            newParticles.CopyTo(particleArray, 0);
            positions = new Vector2[lengthNewArrays];
            newPositions.CopyTo(positions, 0);
            velocitys = new Vector2[lengthNewArrays];
            newVelocitys.CopyTo(velocitys, 0);
            colors = new Color[lengthNewArrays];
            newColors.CopyTo(colors, 0);
            densitys = new float[lengthNewArrays];
            newDensitys.CopyTo(densitys, 0);
            pressures = new float[lengthNewArrays];
            newPressures.CopyTo(pressures, 0);
            forces = new Vector2[lengthNewArrays];
            newForces.CopyTo(forces, 0);
            nPForces = new Vector2[lengthNewArrays];
            newNPForces.CopyTo(nPForces, 0);

            numBoundaries = 0;
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

    void initializeBorder(int leftLength, int rightLength, int topLength, int bottomLength, bool doubleBorder)
    {
        // Box
        // Down
        float x = 0.0f + particleSize;
        float y = 0.0f + particleSize;
        for (int i = 0; i < bottomLength; i++)
        {
            InitBoundaryParticle(new Vector2(x, y));

            x += 2 * particleSize;
        }

        // Double
        if (doubleBorder)
        {
            x = 0.0f + particleSize;
            y = 0.0f + 3 * particleSize;
            for (int i = 0; i < bottomLength; i++)
            {
                InitBoundaryParticle(new Vector2(x, y));

                x += 2 * particleSize;
            }
        }

        // Top
        x = 0.0f - particleSize;
        y = (leftLength * 2 * particleSize) + particleSize;
        for (int i = 0; i < topLength; i++)
        {
            InitBoundaryParticle(new Vector2(x, y));

            x += 2 * particleSize;
        }

        // Double
        if (doubleBorder)
        {
            x = 0.0f - particleSize;
            y = (leftLength * 2 * particleSize) + 3 * particleSize;
            for (int i = 0; i < topLength; i++)
            {
                InitBoundaryParticle(new Vector2(x, y));

                x += 2 * particleSize;
            }
        }

        // Left
        x = 0.0f + particleSize;
        y = 0.0f + 5 * particleSize;
        for (int i = 0; i < leftLength; i++)
        {
            InitBoundaryParticle(new Vector2(x, y));

            y += 2 * particleSize;
        }

        // Double
        if (doubleBorder)
        {
            x = 0.0f + 3 * particleSize;
            y = 0.0f + 5 * particleSize;
            for (int i = 0; i < leftLength; i++)
            {
                InitBoundaryParticle(new Vector2(x, y));

                y += 2 * particleSize;
            }
        }

        // Right
        x = (bottomLength * 2 * particleSize) - 3 * particleSize;
        y = 0.0f + 5 * particleSize;
        for (int i = 0; i < rightLength; i++)
        {
            InitBoundaryParticle(new Vector2(x, y));

            y += 2 * particleSize;
        }

        // Double
        if (doubleBorder)
        {
            x = (bottomLength * 2 * particleSize) - particleSize;
            y = 0.0f + 5 * particleSize;
            for (int i = 0; i < rightLength; i++)
            {
                InitBoundaryParticle(new Vector2(x, y));

                y += 2 * particleSize;
            }
        }
    }

    void initializeBorder0(bool doubleBorder)
    {
        // boundaryPositions = new List<Vector2>();
        // boundaryColors = new List<Color>();
        // Left row
        for (float y = start.y; y < boundaries.y; y += particleSize * 2)
        {
            InitBoundaryParticle(new Vector2(start.x, y));
        }


        // Double
        if (doubleBorder)
        {
            for (float y = start.y; y < boundaries.y + 4 * particleSize; y += particleSize * 2)
            {
                InitBoundaryParticle(new Vector2(start.x - 2 * particleSize, y));
            }
        }

        // Right row
        for (float y = start.y; y < boundaries.y; y += particleSize * 2)
        {
            InitBoundaryParticle(new Vector2(boundaries.x, y));
        }

        // Double
        if (doubleBorder)
        {
            for (float y = start.y - 2 * particleSize; y < boundaries.y + 2 * particleSize; y += particleSize * 2)
            {
                InitBoundaryParticle(new Vector2(boundaries.x + 2 * particleSize, y));
            }
        }

        // Bottom row
        for (float x = start.x; x < boundaries.x; x += particleSize * 2)
        {
            InitBoundaryParticle(new Vector2(x, start.y));
        }

        // Double
        if (doubleBorder)
        {
            for (float x = start.x - 2 * particleSize; x < boundaries.x; x += particleSize * 2)
            {
                InitBoundaryParticle(new Vector2(x, start.y - 2 * particleSize));
            }
        }

        // Top row
        for (float x = start.x; x < boundaries.x; x += particleSize * 2)
        {
            InitBoundaryParticle(new Vector2(x, boundaries.y + 0.5f * particleSize));
        }

        // Double
        if (doubleBorder)
        {
            for (float x = start.x; x < boundaries.x + 2 * particleSize; x += particleSize * 2)
            {
                InitBoundaryParticle(new Vector2(x, boundaries.y + 2.5f * particleSize));
            }
        }
    }

    void initializeBorder2()
    {
        // boundaryPositions = new List<Vector2>();
        // boundaryColors = new List<Color>();

        // Diagonal left
        float x = 0.0f + 4 * particleSize;
        float y = 11.0f;
        for (int i = 0; i < 55; i++)
        {
            InitBoundaryParticle(new Vector2(x, y));

            x += 2 * particleSize;
            y -= 2 * particleSize;
        }

        // Double
        x = 0.0f + 4 * particleSize;
        y = 11.0f - 2 * particleSize;
        for (int i = 0; i < 55; i++)
        {
            InitBoundaryParticle(new Vector2(x, y));

            x += 2 * particleSize;
            y -= 2 * particleSize;
        }

        // Diagonal right
        x = 20.0f - 6 * particleSize;
        y = 11.0f;
        for (int i = 0; i < 80; i++)
        {
            InitBoundaryParticle(new Vector2(x, y));

            x -= 2 * particleSize;
            y -= 0.8f * particleSize;
        }

        // Double
        x = 20.0f - 6f * particleSize;
        y = 11.0f - 2f * particleSize;
        for (int i = 0; i < 80; i++)
        {
            InitBoundaryParticle(new Vector2(x, y));

            x -= 2 * particleSize;
            y -= 0.8f * particleSize;
        }

        // Bottom
        x = 3.0f;
        y = 3.0f;
        for (int i = 0; i < 100; i++)
        {
            InitBoundaryParticle(new Vector2(x, y));

            x += 2 * particleSize;
        }

        // Double
        x = 3.0f;
        y = 3.0f - 2 * particleSize;
        for (int i = 0; i < 100; i++)
        {
            InitBoundaryParticle(new Vector2(x, y));

            x += 2 * particleSize;
        }

        // Bottom left
        x = 3.0f;
        y = 3.0f;
        for (int i = 0; i < 18; i++)
        {
            InitBoundaryParticle(new Vector2(x, y));

            x -= 2 * particleSize;
            y += 2 * particleSize;
        }

        // Double
        x = 3.0f;
        y = 3.0f - 2 * particleSize;
        for (int i = 0; i < 18; i++)
        {
            InitBoundaryParticle(new Vector2(x, y));

            x -= 2 * particleSize;
            y += 2 * particleSize;
        }

        // Bottom right
        x = 15.0f;
        y = 3.0f;
        for (int i = 0; i < 20; i++)
        {
            InitBoundaryParticle(new Vector2(x, y));

            x += 2 * particleSize;
            y += 2 * particleSize;
        }

        // Double
        x = 15.0f;
        y = 3.0f - 2 * particleSize;
        for (int i = 0; i < 20; i++)
        {
            InitBoundaryParticle(new Vector2(x, y));

            x += 2 * particleSize;
            y += 2 * particleSize;
        }

        // Box
        // Down
        x = 0.0f + particleSize;
        y = 0.0f + particleSize;
        for (int i = 0; i < 166; i++)
        {
            InitBoundaryParticle(new Vector2(x, y));

            x += 2 * particleSize;
        }

        // Double
        x = 0.0f + particleSize;
        y = 0.0f + 3 * particleSize;
        for (int i = 0; i < 166; i++)
        {
            InitBoundaryParticle(new Vector2(x, y));

            x += 2 * particleSize;
        }

        // Top
        x = 0.0f - particleSize;
        y = 20.0f - particleSize;
        for (int i = 0; i < 166; i++)
        {
            InitBoundaryParticle(new Vector2(x, y));

            x += 2 * particleSize;
        }

        // Double
        x = 0.0f - particleSize;
        y = 20.0f - 3 * particleSize;
        for (int i = 0; i < 166; i++)
        {
            InitBoundaryParticle(new Vector2(x, y));

            x += 2 * particleSize;
        }

        // Left
        x = 0.0f + particleSize;
        y = 0.0f + 5 * particleSize;
        for (int i = 0; i < 163; i++)
        {
            InitBoundaryParticle(new Vector2(x, y));

            y += 2 * particleSize;
        }

        // Double
        x = 0.0f + 3 * particleSize;
        y = 0.0f + 5 * particleSize;
        for (int i = 0; i < 163; i++)
        {
            InitBoundaryParticle(new Vector2(x, y));

            y += 2 * particleSize;
        }

        // Right
        x = 20.0f - 2 * particleSize;
        y = 0.0f + 5 * particleSize;
        for (int i = 0; i < 163; i++)
        {
            InitBoundaryParticle(new Vector2(x, y));

            y += 2 * particleSize;
        }

        // Double
        x = 20.0f - 4 * particleSize;
        y = 0.0f + 5 * particleSize;
        for (int i = 0; i < 163; i++)
        {
            InitBoundaryParticle(new Vector2(x, y));

            y += 2 * particleSize;
        }

    }
    void initializeBorderWall(float xOffset, bool exists)
    {
        if (exists)
        {
            for (float y = start.y + 2 * particleSize; y < boundaries.y; y += particleSize * 2)
            {
                InitBoundaryParticle(new Vector2(start.x + xOffset, y));

                InitBoundaryParticle(new Vector2(start.x + xOffset + 2 * particleSize, y));
            }
        }
    }

    // Everything that has to do with Neighbor search comes in this place

    // Construction methods

    void constructionBasic()
    {
        // clear the grid
        // iterate one time over all grid cells
        spatialGrid.emptyGrid();

        // Add the number of each particle in the respective grid cell
        // iterate one time over all particles
        for (int j = 0; j < numParticles + numBoundaries; j++)
        {
            int i = particleHandles[j].particleIndex;
            Vector2 gridCoords = spatialGrid.computeCellPosition(positions[i]);
            if (spatialGrid.isValidCell(gridCoords))
            {
                spatialGrid.grid[(int)gridCoords.x, (int)gridCoords.y].Add(i);
            }
        }
    }

    void constructionIndexSort2()
    {
        // sort particle attributes
        Array.Sort(spatialGrid.cellKeys, particleArray);
        Array.Sort(spatialGrid.cellKeys, positions);
        Array.Sort(spatialGrid.cellKeys, velocitys);
        Array.Sort(spatialGrid.cellKeys, colors);
        Array.Sort(spatialGrid.cellKeys, neighbors);
        Array.Sort(spatialGrid.cellKeys, densitys);
        Array.Sort(spatialGrid.cellKeys, pressures);
        Array.Sort(spatialGrid.cellKeys, forces);
        Array.Sort(spatialGrid.cellKeys, nPForces);

        // Create array of particle CellIndices
        // for (int i = 0; i < numParticles + numBoundaries; i++)
        // {
        //     spatialGrid.cellKeys[i] = spatialGrid.computeCellIndex(positions[i]);
        // }
    }
    void constructionIndexSort()
    {
        // // Create handles
        // for (int i = 0; i < numParticles + numBoundaries; i++)
        // {
        //     particleHandles[i].particleIndex = i;
        //     particleHandles[i].cellIndex = spatialGrid.computeCellIndex(positions[i]);
        // }

        // // Sort handles
        // Array.Sort(particleHandles);


        if (moveParticles || !firstSortingDone)
        {
            constructionCounter = 0;
            firstSortingDone = true;
            // Clear cell counter
            spatialGrid.clearCellCounter();

            // Compute cell indices for particles and increment counter in C
            for (int j = 0; j < numParticles + numBoundaries; j++)
            {
                int i = particleHandles[j].particleIndex;
                Vector2 gridCoords = spatialGrid.computeCellPosition(positions[i]);
                if (spatialGrid.isValidCell(gridCoords))
                {
                    long cellIndex = spatialGrid.computeUniqueCellIndex((int)gridCoords.x, (int)gridCoords.y);
                    spatialGrid.cellCounter[cellIndex]++;
                }
            }

            // Accumulate counters
            int accum = 0;
            for (int i = 0; i < spatialGrid.cellCounter.Length; i++)
            {
                accum += spatialGrid.cellCounter[i];
                spatialGrid.cellCounter[i] = accum;
            }


            // Create sorted particle array
            for (int j = 0; j < numParticles + numBoundaries; j++)
            {
                int i = particleHandles[j].particleIndex;
                Vector2 gridCoords = spatialGrid.computeCellPosition(positions[i]);
                if (spatialGrid.isValidCell(gridCoords))
                {
                    long cellIndex = spatialGrid.computeUniqueCellIndex((int)gridCoords.x, (int)gridCoords.y);
                    int index = spatialGrid.cellCounter[cellIndex] - 1;

                    sortedParticles[index] = particleArray[i];
                    sortedPositions[index] = positions[i];
                    sortedVelocitys[index] = velocitys[i];
                    sortedColors[index] = colors[i];
                    sortedNeighbors[index] = neighbors[i];
                    sortedDensitys[index] = densitys[i];
                    sortedPressures[index] = pressures[i];
                    sortedForces[index] = forces[i];
                    sortedNPForces[index] = nPForces[i];
                    spatialGrid.cellCounter[cellIndex] -= 1;
                }
            }

            particleArray = new List<int>(sortedParticles).ToArray();
            positions = new List<Vector2>(sortedPositions).ToArray();
            velocitys = new List<Vector2>(sortedVelocitys).ToArray();
            colors = new List<Color>(sortedColors).ToArray();
            neighbors = new List<int>(sortedNeighbors).ToArray();
            densitys = new List<float>(sortedDensitys).ToArray();
            pressures = new List<float>(sortedPressures).ToArray();
            forces = new List<Vector2>(sortedForces).ToArray();
            nPForces = new List<Vector2>(sortedNPForces).ToArray();
        }
    }

    void constructionZIndexSort()
    {
        // // Create handles
        // for (int i = 0; i < numParticles + numBoundaries; i++)
        // {
        //     particleHandles[i].particleIndex = i;
        //     particleHandles[i].cellIndex = spatialGrid.computeZIndexForPosition(positions[i]);
        // }

        // // Sort handles
        // Array.Sort(particleHandles);

        if (moveParticles || !firstSortingDone)
        {
            // constructionCounter++;
            // if (constructionCounter == 5)
            // {
            // constructionCounter = 0;
            firstSortingDone = true;
            // Clear cell counter
            spatialGrid.clearCellCounter();

            // Compute cell indices for particles and increment counter in C
            for (int j = 0; j < numParticles + numBoundaries; j++)
            {
                int i = particleHandles[j].particleIndex;
                Vector2 gridCoords = spatialGrid.computeCellPosition(positions[i]);
                if (spatialGrid.isValidCell(gridCoords))
                {
                    long cellIndex = spatialGrid.computeZIndexForCell((int)gridCoords.x, (int)gridCoords.y);
                    spatialGrid.cellCounter[cellIndex]++;
                }
            }

            // Accumulate counters
            int accum = 0;
            for (int i = 0; i < spatialGrid.cellCounter.Length; i++)
            {
                accum += spatialGrid.cellCounter[i];
                spatialGrid.cellCounter[i] = accum;
            }

            // Create sorted particle array
            // For fluid particle
            for (int j = 0; j < numParticles + numBoundaries; j++)
            {
                int i = particleHandles[j].particleIndex;
                Vector2 gridCoords = spatialGrid.computeCellPosition(positions[i]);
                if (spatialGrid.isValidCell(gridCoords))
                {
                    long cellIndex = spatialGrid.computeZIndexForCell((int)gridCoords.x, (int)gridCoords.y);
                    int index = spatialGrid.cellCounter[cellIndex] - 1;
                    sortedParticles[index] = particleArray[i];
                    sortedPositions[index] = positions[i];
                    sortedVelocitys[index] = velocitys[i];
                    sortedColors[index] = colors[i];
                    sortedNeighbors[index] = neighbors[i];
                    sortedDensitys[index] = densitys[i];
                    sortedPressures[index] = pressures[i];
                    sortedForces[index] = forces[i];
                    sortedNPForces[index] = nPForces[i];
                    spatialGrid.cellCounter[cellIndex] -= 1;
                }
            }

            particleArray = new List<int>(sortedParticles).ToArray();
            positions = new List<Vector2>(sortedPositions).ToArray();
            velocitys = new List<Vector2>(sortedVelocitys).ToArray();
            colors = new List<Color>(sortedColors).ToArray();
            neighbors = new List<int>(sortedNeighbors).ToArray();
            densitys = new List<float>(sortedDensitys).ToArray();
            pressures = new List<float>(sortedPressures).ToArray();
            forces = new List<Vector2>(sortedForces).ToArray();
            nPForces = new List<Vector2>(sortedNPForces).ToArray();
        }
        // }
    }
    void constructionSpatialHashing()
    {
        // clear the hash table
        // iterate one time over all grid cells
        spatialGrid.clearHashTable();

        // Add the number of each particle in the respective grid cell
        // iterate one time over all particles
        for (int i = 0; i < numParticles + numBoundaries; i++)
        {
            int hashIndex = spatialGrid.computeHashIndex(positions[i]);
            spatialGrid.hashTable[hashIndex][spatialGrid.countHashTable[hashIndex]] = i;
            spatialGrid.countHashTable[hashIndex]++;
        }
    }

    void constructionCompactHashing()
    {
        // // Create handles
        // for (int i = 0; i < numParticles + numBoundaries; i++)
        // {
        //     particleHandles[i].particleIndex = i;
        //     particleHandles[i].cellIndex = spatialGrid.computeZIndexForPosition(positions[i]);
        // }

        // // Sort handles
        // Array.Sort(particleHandles);

        // clear countHashTable
        for (int i = 0; i < spatialGrid.countHashTable.Length; i++)
        {
            spatialGrid.countHashTable[i] = 0;
        }
        // Compute number of filled cells
        for (int j = 0; j < numParticles + numBoundaries; j++)
        {
            int i = particleHandles[j].particleIndex;
            int hashIndex = spatialGrid.computeHashIndex(positions[i]);
            spatialGrid.countHashTable[hashIndex]++;
        }
        int usedCells = 0;
        for (int i = 0; i < spatialGrid.countHashTable.Length; i++)
        {
            if (spatialGrid.countHashTable[i] > 0)
            {
                usedCells++;
            }
        }
        //Initialize compact array
        spatialGrid.compactArray = new int[usedCells][];
        for (int i = 0; i < usedCells; i++)
        {
            spatialGrid.compactArray[i] = new int[spatialGrid.numHashTableEntries];
            for (int j = 0; j < spatialGrid.numHashTableEntries; j++)
            {
                spatialGrid.compactArray[i][j] = -1;
            }
        }
        spatialGrid.compactHashTable = new int[spatialGrid.width * spatialGrid.height * 2];
        for (int i = 0; i < spatialGrid.compactHashTable.Length; i++)
        {
            spatialGrid.compactHashTable[i] = -1;
        }

        // Count compact array entries
        int compactArrayEntries = 0;
        // Construct array that counts already inserted particles
        spatialGrid.numParticlesCompactArray = new int[usedCells];
        // Insert all particles in compact array

        for (int j = 0; j < numParticles + numBoundaries; j++)
        {
            int i = particleHandles[j].particleIndex;
            // Compute hash index of particle
            int hashIndex = spatialGrid.computeHashIndex(positions[i]);
            int compactCell = spatialGrid.compactHashTable[hashIndex];
            // Check if cell at that hash index is empty
            if (compactCell == -1)
            {
                // Add reference to cell array into hash table
                spatialGrid.compactHashTable[hashIndex] = compactArrayEntries;
                // Insert particle at reference
                compactCell = spatialGrid.compactHashTable[hashIndex];
                spatialGrid.compactArray[compactCell][spatialGrid.numParticlesCompactArray[compactCell]] = i; //<----
                spatialGrid.numParticlesCompactArray[compactCell]++;
                compactArrayEntries++;
            }
            else
            {
                spatialGrid.compactArray[compactCell][spatialGrid.numParticlesCompactArray[compactCell]] = i;
                spatialGrid.numParticlesCompactArray[compactCell]++;
            }
        }
    }
    // Query Methods
    // query means quering through our potential neighbors and finding real neighbors

    void quadraticSearch()
    {
        for (int i = 0; i < numParticles + numBoundaries; i++)
        {
            if (particleArray[i] < numParticles)
            {
                findNeighborsQuadraticSearch(i);
            }
        };
    }

    void queryGrid()
    {
        // Find all neighbors for each particle
        if (spatialGrid.parallelSearchActivated)
        {
            Parallel.For(0, numParticles + numBoundaries, j =>
            {
                int i = particleHandles[j].particleIndex;
                if (particleArray[i] < numParticles)
                {
                    findNeighbors(i);
                }
            });
        }
        else
        {
            for (int i = 0; i < numParticles + numBoundaries; i++)
            {
                if (particleArray[i] < numParticles)
                {
                    findNeighbors(i);
                }
            };
        }
    }
    void queryIndexSort()
    {
        // Find all neighbors for each particle
        if (spatialGrid.parallelSearchActivated)
        {
            //Limiting the maximum degree of parallelism to 8
            Parallel.For(0, numParticles + numBoundaries, j =>
            {
                int i = particleHandles[j].particleIndex;
                if (particleArray[i] < numParticles)
                {
                    findNeighborsIndexSort(i);
                }
            });
        }
        else
        {
            for (int i = 0; i < numParticles + numBoundaries; i++)
            {
                if (particleArray[i] < numParticles)
                {
                    findNeighborsIndexSort(i);
                }
            };
        }
    }

    void queryIndexSort2()
    {
        // Find all neighbors for each particle
        if (spatialGrid.parallelSearchActivated)
        {
            Parallel.For(0, spatialGrid.cellCounter.Length - 1, i =>
            {
                findNeighborsIndexSort2(i);
            });
        }
        else
        {
            for (int i = 0; i < spatialGrid.cellCounter.Length - 1; i++)
            {
                findNeighborsIndexSort2(i);
            };
        }
    }

    void queryZIndexSort()
    {
        // Find all neighbors for each particle
        if (spatialGrid.parallelSearchActivated)
        {
            Parallel.For(0, numParticles + numBoundaries, j =>
            {
                int i = particleHandles[j].particleIndex;
                if (particleArray[i] < numParticles)
                {
                    findNeighborsZIndexSort(i);
                }
            });
        }
        else
        {
            for (int i = 0; i < numParticles + numBoundaries; i++)
            {
                if (particleArray[i] < numParticles)
                {
                    findNeighborsZIndexSort(i);
                }
            };
        }
    }

    void queryHashTable()
    {
        // Find all neighbors for each particle
        if (spatialGrid.parallelSearchActivated)
        {
            Parallel.For(0, numParticles + numBoundaries, i =>
            {
                if (particleArray[i] < numParticles)
                {
                    findNeighborsSpatialHashing(i);
                }
            });
        }
        else
        {
            for (int i = 0; i < numParticles + numBoundaries; i++)
            {
                if (particleArray[i] < numParticles)
                {
                    findNeighborsSpatialHashing(i);
                }
            };
        }
    }

    void queryHashTable2()
    {
        // Find all neighbors for each particle
        if (spatialGrid.parallelSearchActivated)
        {
            Parallel.For(0, spatialGrid.hashTable.Length, i =>
            {
                findNeighborsSpatialHashing2(i);
            });
        }
        else
        {
            for (int i = 0; i < spatialGrid.hashTable.Length; i++)
            {
                findNeighborsSpatialHashing2(i);
            };
        }
    }

    void queryCompactHashTable()
    {
        // Find all neighbors for each particle
        if (spatialGrid.parallelSearchActivated)
        {
            Parallel.For(0, spatialGrid.compactArray.Length, j =>
            {
                int i = particleHandles[j].particleIndex;
                findNeighborsCompactHashing(i);
            });
        }
        else
        {
            for (int i = 0; i < spatialGrid.compactArray.Length; i++)
            {
                findNeighborsCompactHashing(i);
            };
        }
    }

    // Neighbor search

    private void findNeighborsQuadraticSearch(int i)
    {
        // Clear all neighbors
        for (int n = 0; n < numParticleNeighbors; n++)
        {
            neighborsParticles[neighbors[i] + n] = -1;
        }
        // Initialize counter
        int counter = 0;
        for (int j = 0; j < numParticles + numBoundaries; j++)
        {
            if (Vector2.Distance(positions[i], positions[j]) < kernelSupportRadius)
            {
                // n.Add(j);
                neighborsParticles[neighbors[i] + counter] = j;
                counter++;
            }
        }
        // neighbors[i] = n;
    }

    private void findNeighbors(int i)
    {
        // Clear all neighbors
        for (int n = 0; n < numParticleNeighbors; n++)
        {
            neighborsParticles[neighbors[i] + n] = -1;
        }
        // Initialize counter
        int counter = 0;
        Vector2 gridCell = spatialGrid.computeCellPosition(positions[i]);
        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                int cellX = (int)gridCell.x + x;
                int cellY = (int)gridCell.y + y;
                if (spatialGrid.isValidCell(new Vector2(cellX, cellY)))
                {
                    // foreach (int p in spatialGrid.grid[cellX, cellY])
                    // {
                    //     if (Vector2.Distance(positions[i], positions[p]) < kernelSupportRadius)
                    //     {
                    //         n.Add(p);
                    //     }
                    // }
                    List<int> potentialNeighbors = spatialGrid.grid[cellX, cellY];
                    for (int j = 0; j < potentialNeighbors.Count; j++)
                    {
                        if (Vector2.Distance(positions[i], positions[potentialNeighbors[j]]) < kernelSupportRadius)
                        {
                            // n.Add(potentialNeighbors[j]);
                            neighborsParticles[neighbors[i] + counter] = potentialNeighbors[j];
                            counter++;
                        }
                    }
                }
            }
        }
        // neighbors[i] = n;
    }

    void findNeighborsIndexSort(int i)
    {
        // Clear all neighbors
        for (int n = 0; n < numParticleNeighbors; n++)
        {
            neighborsParticles[neighbors[i] + n] = -1;
        }
        // Initialize counter
        int counter = 0;
        Vector2 gridCell = spatialGrid.computeCellPosition(positions[i]);
        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                int cellX = (int)gridCell.x + x;
                int cellY = (int)gridCell.y + y;
                if (spatialGrid.isValidCell(new Vector2(cellX, cellY)))
                {
                    long cellIndex = spatialGrid.computeUniqueCellIndex(cellX, cellY);
                    int cellStart = spatialGrid.cellCounter[cellIndex];
                    int cellEnd = spatialGrid.cellCounter[cellIndex + 1];
                    for (int j = cellStart; j < cellEnd; j++)
                    {
                        if (Vector2.Distance(positions[i], positions[j]) < kernelSupportRadius)
                        {
                            // n.Add(j);
                            neighborsParticles[neighbors[i] + counter] = j;
                            counter++;
                        }
                    }
                }
            }
        }

        // // Clear all neighbors
        // for (int n = 0; n < numParticleNeighbors; n++)
        // {
        //     neighborsParticles[neighbors[i] + n] = -1;
        // }
        // // Initialize counter
        // int counter = 0;
        // int cellIndex = spatialGrid.computeCellIndex(positions[i]);
        // int[] neighborcells = new int[9];
        // neighborcells[0] = cellIndex;
        // neighborcells[1] = cellIndex - 1;
        // neighborcells[2] = cellIndex + 1;
        // neighborcells[3] = cellIndex + spatialGrid.height;
        // neighborcells[4] = cellIndex + spatialGrid.height - 1;
        // neighborcells[5] = cellIndex + spatialGrid.height + 1;
        // neighborcells[6] = cellIndex - spatialGrid.height;
        // neighborcells[7] = cellIndex - spatialGrid.height - 1;
        // neighborcells[8] = cellIndex - spatialGrid.height + 1;
        // foreach (int cell in neighborcells)
        // {
        //     if (cell > 0 && cell < spatialGrid.cellCounter.Length)
        //     {
        //         int cellStart = spatialGrid.cellCounter[cell];
        //         int cellEnd = spatialGrid.cellCounter[cell + 1];
        //         for (int j = cellStart; j < cellEnd; j++)
        //         {
        //             if (Vector2.Distance(positions[i], positions[j]) < kernelSupportRadius)
        //             {
        //                 neighborsParticles[neighbors[i] + counter] = j;
        //                 counter++;
        //             }
        //         }
        //     }
        // }
    }

    // Finds all neighbors for all particles in cell c
    void findNeighborsIndexSort2(int c)
    {
        // Check if particles in cell otherwise skip
        int cellStart = spatialGrid.cellCounter[c];
        int cellEnd = spatialGrid.cellCounter[c + 1];
        if (cellStart < cellEnd)
        {
            // Compute all neighboring cells
            int[] neighborcells = new int[9];
            neighborcells[0] = c;
            neighborcells[1] = c - 1;
            neighborcells[2] = c + 1;
            neighborcells[3] = c + spatialGrid.height;
            neighborcells[4] = c + spatialGrid.height - 1;
            neighborcells[5] = c + spatialGrid.height + 1;
            neighborcells[6] = c - spatialGrid.height;
            neighborcells[7] = c - spatialGrid.height - 1;
            neighborcells[8] = c - spatialGrid.height + 1;
            // Iterate over all particles in cell c
            for (int i = cellStart; i < cellEnd; i++)
            {
                // Only look for neighbors if i is a fluid particle
                if (particleArray[i] < numParticles)
                {
                    // Clear all neighbors
                    for (int n = 0; n < numParticleNeighbors; n++)
                    {
                        neighborsParticles[neighbors[i] + n] = -1;
                    }
                    // Initialize counter
                    int counter = 0;
                    // Test all potential neighbors in neighboring cells
                    foreach (int cell in neighborcells)
                    {
                        if (cell >= 0 && cell < spatialGrid.cellCounter.Length)
                        {
                            int cellStart2 = spatialGrid.cellCounter[cell];
                            int cellEnd2 = spatialGrid.cellCounter[cell + 1];
                            for (int j = cellStart2; j < cellEnd2; j++)
                            {
                                if (Vector2.Distance(positions[i], positions[j]) < kernelSupportRadius)
                                {
                                    neighborsParticles[neighbors[i] + counter] = j;
                                    counter++;
                                }
                            }
                        }
                    }
                }
            }
        }
    }

    void findNeighborsZIndexSort(int i)
    {
        // Clear all neighbors
        for (int n = 0; n < numParticleNeighbors; n++)
        {
            neighborsParticles[neighbors[i] + n] = -1;
        }
        // Initialize counter
        int counter = 0;
        Vector2 gridCell = spatialGrid.computeCellPosition(positions[i]);
        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                int cellX = (int)gridCell.x + x;
                int cellY = (int)gridCell.y + y;
                if (spatialGrid.isValidCell(new Vector2(cellX, cellY)))
                {
                    long cellIndex = spatialGrid.computeZIndexForCell(cellX, cellY);
                    // long cellIndex = cellZIndices[cellX, cellY];
                    int cellStart = spatialGrid.cellCounter[cellIndex];
                    int cellEnd = spatialGrid.cellCounter[cellIndex + 1];
                    for (int j = cellStart; j < cellEnd; j++)
                    {
                        if (Vector2.Distance(positions[i], positions[j]) < kernelSupportRadius)
                        {
                            // n.Add(j);
                            neighborsParticles[neighbors[i] + counter] = j;
                            counter++;
                        }
                    }
                }
            }
        }

        // neighbors[i] = n;
    }

    private void findNeighborsSpatialHashing(int i)
    {
        // Clear all neighbors
        for (int n = 0; n < numParticleNeighbors; n++)
        {
            neighborsParticles[neighbors[i] + n] = -1;
        }
        // Initialize counter
        int counter = 0;
        Vector2 gridCell = spatialGrid.computeCellPosition(positions[i]);
        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                int cellX = (int)gridCell.x + x;
                int cellY = (int)gridCell.y + y;
                if (spatialGrid.isValidCell(new Vector2(cellX, cellY)))
                {
                    int cellIndex = spatialGrid.computeHashIndexForCell(cellX, cellY);
                    foreach (int p in spatialGrid.hashTable[cellIndex])
                    {
                        if (p >= 0)
                        {
                            if (Vector2.Distance(positions[i], positions[p]) < kernelSupportRadius)
                            {
                                neighborsParticles[neighbors[i] + counter] = p;
                                counter++;
                            }
                        }
                    }
                }
            }
        }
    }

    private void findNeighborsSpatialHashing2(int cell)
    {
        // Get all particles in cell
        int[] particles = spatialGrid.hashTable[cell];

        // Find neighbors for all particles
        foreach (int i in particles)
        {
            // Check if valid particle
            if (i >= 0)
                // Only find neighbors for fluid particles
                if (particleArray[i] < numParticles)
                {
                    // Clear all neighbors
                    for (int n = 0; n < numParticleNeighbors; n++)
                    {
                        neighborsParticles[neighbors[i] + n] = -1;
                    }
                    // Initialize counter
                    int counter = 0;
                    Vector2 gridCell = spatialGrid.computeCellPosition(positions[i]);
                    for (int x = -1; x <= 1; x++)
                    {
                        for (int y = -1; y <= 1; y++)
                        {
                            int cellX = (int)gridCell.x + x;
                            int cellY = (int)gridCell.y + y;
                            if (spatialGrid.isValidCell(new Vector2(cellX, cellY)))
                            {
                                int cellIndex = spatialGrid.computeHashIndexForCell(cellX, cellY);
                                foreach (int p in spatialGrid.hashTable[cellIndex])
                                {
                                    if (p >= 0)
                                    {
                                        if (Vector2.Distance(positions[i], positions[p]) < kernelSupportRadius)
                                        {
                                            neighborsParticles[neighbors[i] + counter] = p;
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

    private void findNeighborsCompactHashing(int cell)
    {
        // Get all particles in cell
        int[] particles = spatialGrid.compactArray[cell];

        // Find neighbors for all particles
        foreach (int i in particles)
        {
            // Check if valid particle
            if (i >= 0)
                // Only find neighbors for fluid particles
                if (particleArray[i] < numParticles)
                {
                    // Clear all neighbors
                    for (int n = 0; n < numParticleNeighbors; n++)
                    {
                        neighborsParticles[neighbors[i] + n] = -1;
                    }
                    // Initialize counter
                    int counter = 0;
                    Vector2 gridCell = spatialGrid.computeCellPosition(positions[i]);
                    for (int x = -1; x <= 1; x++)
                    {
                        for (int y = -1; y <= 1; y++)
                        {
                            int cellX = (int)gridCell.x + x;
                            int cellY = (int)gridCell.y + y;
                            if (spatialGrid.isValidCell(new Vector2(cellX, cellY)))
                            {
                                int cellIndex = spatialGrid.computeHashIndexForCell(cellX, cellY); // 3096
                                // Get position in compact array
                                int posInArray = spatialGrid.compactHashTable[cellIndex]; // 80
                                // Check if cell inside array
                                if (posInArray >= 0 && posInArray < spatialGrid.compactArray.Length)
                                {
                                    foreach (int p in spatialGrid.compactArray[posInArray])
                                    {
                                        if (p >= 0)
                                        {
                                            if (Vector2.Distance(positions[i], positions[p]) < kernelSupportRadius)
                                            {
                                                neighborsParticles[neighbors[i] + counter] = p;
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
    }

    // // Return int List without boundary particles
    // List<int> removeBoundarysInt(List<int> particles)
    // {
    //     List<int> newList = new List<int>();
    //     for (int i = 0; i < numParticles + numBoundaries; i++)
    //     {
    //         if (particleArray[i] < numParticles)
    //         {
    //             newList.Add(particles[i]);
    //         }
    //     }
    //     return newList;
    // }

    // // Return Vector2 List without boundary particles
    // List<Vector2> removeBoundarysVector2(List<Vector2> particles)
    // {
    //     List<Vector2> newList = new List<Vector2>();
    //     for (int i = 0; i < numParticles + numBoundaries; i++)
    //     {
    //         if (particleArray[i] < numParticles)
    //         {
    //             newList.Add(particles[i]);
    //         }
    //     }
    //     return newList;
    // }

    // // Return Vector2 floats without boundary particles
    // List<float> removeBoundarysFloat(List<float> particles)
    // {
    //     List<float> newList = new List<float>();
    //     for (int i = 0; i < numParticles + numBoundaries; i++)
    //     {
    //         if (particleArray[i] < numParticles)
    //         {
    //             newList.Add(particles[i]);
    //         }
    //     }
    //     return newList;
    // }

    // // Return Vector2 colors without boundary particles
    // List<Color> removeBoundarysColors(List<Color> particles)
    // {
    //     List<Color> newList = new List<Color>();
    //     for (int i = 0; i < numParticles + numBoundaries; i++)
    //     {
    //         if (particleArray[i] < numParticles)
    //         {
    //             newList.Add(particles[i]);
    //         }
    //     }
    //     return newList;
    // }
    // // Update is called once per frame
    void Update()
    {
        Stopwatch watch3 = new Stopwatch();
        watch3.Start();
        if (previousBorderOffset != borderOffset && tests == 0)
        {
            // // particleArray.RemoveRange(numParticles, numBoundaries);
            // positions = removeBoundarysVector2(positions);
            // // positions.RemoveRange(numParticles, numBoundaries);
            // colors = removeBoundarysColors(colors);
            // // colors.RemoveRange(numParticles, numBoundaries);
            // velocitys = removeBoundarysVector2(velocitys);
            // // velocitys.RemoveRange(numParticles, numBoundaries);
            // densitys = removeBoundarysFloat(densitys);
            // // densitys.RemoveRange(numParticles, numBoundaries);
            // pressures = removeBoundarysFloat(pressures);
            // // pressures.RemoveRange(numParticles, numBoundaries);
            // forces = removeBoundarysVector2(forces);
            // // forces.RemoveRange(numParticles, numBoundaries);
            // nPForces = removeBoundarysVector2(nPForces);
            // // nPForces.RemoveRange(numParticles, numBoundaries);
            // // Initialize boundary particles
            // particleArray = removeBoundarysInt(particleArray);
            // numBoundaries = 0;
            // initializeBorder(doubleBorder);
            // initializeBorderWall(borderOffset, doubleBorder);

            // Inform the shader about the total amount of drawn particles
            drawCirclesScript.total = numParticles + numBoundaries;

            previousBorderOffset = borderOffset;
        }

        // Start Stop Simulation
        if (Input.GetKeyDown(KeyCode.Space))
        {
            moveParticles = !moveParticles;
        }

        // Restart
        if (Input.GetKeyDown(KeyCode.Backspace))
        {
            firstSortingDone = false;
            moveParticles = false;
            Vector3 screenPosition = Input.mousePosition;
            screenPosition.z = Camera.main.nearClipPlane + 1;
            particlePosition = Camera.main.ScreenToWorldPoint(screenPosition);

            initializeParticles(resetParticles, (int)amountParticles.x, (int)amountParticles.y, particlePosition, particleStartSpacing);

            // Initialize grid
            spatialGrid.emptyGrid();
            spatialGrid.clearCellCounter();
            spatialGrid.clearHashTable();


            // spatialGrid.DrawGrid();

            // Initialize boundary particles
            if (tests == 0)
            {
                initializeBorder0(doubleBorder);
                initializeBorderWall(borderOffset, doubleBorder);
                mainCamera.orthographicSize = 5;
                mainCamera.transform.position = new Vector3(8.91f, 5, -10);
            }
            else if (tests == 1)
            {
                initializeBorder2();
                mainCamera.orthographicSize = 10;
                mainCamera.transform.position = new Vector3(12, 10, -10);
            }

            else if (tests == 2)
            {
                initializeBorder(166, 166, 166, 166, true);
                mainCamera.orthographicSize = 10;
                mainCamera.transform.position = new Vector3(12, 10, -10);
            }

            else if (tests == 3)
            {
                initializeBorder(332, 332, 623, 623, true);
                mainCamera.orthographicSize = 23;
                mainCamera.transform.position = new Vector3(41f, 23, -10);
            }

            else if (tests == 4)
            {
                initializeBorder(800, 800, 800, 800, true);
                mainCamera.orthographicSize = 40;
                mainCamera.transform.position = new Vector3(50, 40, -10);
            }

            else if (tests == 5)
            {
                initializeBorder(1700, 1700, 1700, 1700, true);
                mainCamera.orthographicSize = 100;
                mainCamera.transform.position = new Vector3(100, 100, -10);
            }

            averageDensity = new List<float>();

            cflConditions = new List<float>();

            queryTimes = new List<double>();

            // Inform the shader about the total amount of drawn particles
            drawCirclesScript.total = numParticles + numBoundaries;

            // IMPORTANT!!!!
            sortedParticles = new int[numParticles + numBoundaries];
            sortedPositions = new Vector2[numParticles + numBoundaries];
            sortedVelocitys = new Vector2[numParticles + numBoundaries];
            sortedColors = new Color[numParticles + numBoundaries];
            sortedNeighbors = new int[numParticles + numBoundaries];
            sortedDensitys = new float[numParticles + numBoundaries];
            sortedPressures = new float[numParticles + numBoundaries];
            sortedForces = new Vector2[numParticles + numBoundaries];
            sortedNPForces = new Vector2[numParticles + numBoundaries];
        }

        // Clean particles
        if (Input.GetKeyDown(KeyCode.C))
        {
            ResetValues();
            numParticles = 0;
            numBoundaries = 0;
        }

        if (Input.GetKey(KeyCode.S))
        {
            Vector3 screenPosition = Input.mousePosition;
            screenPosition.z = Camera.main.nearClipPlane + 1;
            mouseForceOut(Camera.main.ScreenToWorldPoint(screenPosition), 3f, 2.5f);
        }

        if (Input.GetKey(KeyCode.D))
        {
            Vector3 screenPosition = Input.mousePosition;
            screenPosition.z = Camera.main.nearClipPlane + 1;
            mouseForceIn(Camera.main.ScreenToWorldPoint(screenPosition), 3f, 2.5f);
        }

        // Mouse input
        if (Input.GetMouseButton(0))
        {
            ReturnMouse();
        }
        // Update particle size
        particleMass = startDensity * particleVolume;
        particleSize = particleMass;

        // Check for max velocity
        maxVelocity = new Vector2(0, 0);
        for (int j = 0; j < numParticles + numBoundaries; j++)
        {
            int i = particleHandles[j].particleIndex;
            if (particleArray[i] < numParticles)
            {
                if (velocitys[i].magnitude > maxVelocity.magnitude && velocitys[i].y > -15 && velocitys[i].magnitude > 0)
                {
                    maxVelocity = velocitys[i];
                }
            }
        }

        // Add cflNumber
        if (countCFLConditions && moveParticles)
        {
            cflConditions.Add(timeStepMultiplyer * (particleSpacing / maxVelocity.magnitude));
            if (cflConditions.Count >= 2000)
            {
                using (StreamWriter writetext = new StreamWriter("C:\\Users\\User\\graphData.txt"))
                {
                    foreach (float data in cflConditions)
                    {
                        writetext.WriteLine(data);
                    }
                }
                countCFLConditions = false;
            }
        }

        if (countAvgDensity && moveParticles)
        {
            // Add average density
            float avgDensity = 0.0f;
            for (int j = 0; j < numParticles + numBoundaries; j++)
            {
                int i = particleHandles[j].particleIndex;
                if (particleArray[i] < numParticles)
                {
                    avgDensity += densitys[i];
                }
            }
            averageDensity.Add(avgDensity / numParticles);

            if (averageDensity.Count >= 500)
            {
                using (StreamWriter writetext = new StreamWriter("C:\\Users\\User\\graphData.txt"))
                {
                    foreach (float data in averageDensity)
                    {
                        writetext.WriteLine(data);
                    }
                }
                countAvgDensity = false;
            }
        }

        if (queryTimes.Count >= maxTS)
        {
            using (StreamWriter writetext = new StreamWriter("C:\\Users\\skril\\indexsort.txt"))
            {
                foreach (double data in queryTimes)
                {
                    writetext.WriteLine(data);
                }
            }
            queryTimes.Clear();
        }

        if (moveParticles)
        {
            tS++;
            measurements.timeStep = tS;
            if (tS == maxTS)
            {
                moveParticles = !moveParticles;
            }
        }

        // Calculate time step
        Stopwatch watch = new Stopwatch();
        watch.Start();
        SimulationStep(timeStep);
        watch.Stop();
        if (moveParticles)
        {
            measurements.simulationStep = watch.Elapsed.TotalMilliseconds;
            sumSimulationStep += measurements.simulationStep;
            measurements.averageSimulationStep = sumSimulationStep / watchCounter;
        }
        Stopwatch watch2 = new Stopwatch();
        watch2.Start();
        DrawParticles();
        watch2.Stop();
        if (moveParticles)
        {
            measurements.drawingTime = watch2.Elapsed.TotalMilliseconds;
            sumDrawingTime += measurements.drawingTime;
            measurements.averageDrawingTime = sumDrawingTime / watchCounter;
        }


        watch3.Stop();
        if (moveParticles)
        {
            measurements.updateTime = watch3.Elapsed.TotalMilliseconds;
            sumUpdateTime += measurements.updateTime;
            measurements.averageUpdateTime = sumUpdateTime / watchCounter;

            watchCounter++;
        }

    }
    void SimulationStep(float deltaTime)
    {
        if (spatialGrid.chooseNeighborSearch == -1)
        {
            Stopwatch v1 = new Stopwatch();
            v1.Start();
            quadraticSearch();
            v1.Stop();
            if (moveParticles)
            {
                measurements.constructionTime = v1.Elapsed.TotalMilliseconds;
                sumConstructionTime += measurements.constructionTime;
                measurements.averageConstructionTime = sumConstructionTime / tS;
            }
        }
        else if (spatialGrid.chooseNeighborSearch == 0)
        {
            // Measure construction time
            Stopwatch v1 = new Stopwatch();
            v1.Start();
            constructionBasic();
            v1.Stop();
            if (moveParticles)
            {
                measurements.constructionTime = v1.Elapsed.TotalMilliseconds;
                sumConstructionTime += measurements.constructionTime;
                measurements.averageConstructionTime = sumConstructionTime / tS;
            }

            // Measure query time
            Stopwatch v2 = new Stopwatch();
            v2.Start();
            queryGrid();
            v2.Stop();
            if (moveParticles)
            {
                measurements.queryTime = v2.Elapsed.TotalMilliseconds;
                queryTimes.Add(measurements.queryTime);
                sumQueryTime += measurements.queryTime;
                measurements.averageQueryTime = sumQueryTime / tS;

                measurements.averageTotalTime = measurements.averageConstructionTime + measurements.averageQueryTime;
            }

        }
        else if (spatialGrid.chooseNeighborSearch == 1)
        {
            // Measure construction time
            Stopwatch v1 = new Stopwatch();
            v1.Start();
            constructionIndexSort();
            // constructionCompactHashing();
            v1.Stop();
            if (moveParticles)
            {
                measurements.constructionTime = v1.Elapsed.TotalMilliseconds;
                sumConstructionTime += measurements.constructionTime;
                measurements.averageConstructionTime = sumConstructionTime / tS;
            }

            // Measure query time
            Stopwatch v2 = new Stopwatch();
            v2.Start();
            queryIndexSort();
            v2.Stop();
            if (moveParticles)
            {
                measurements.queryTime = v2.Elapsed.TotalMilliseconds;
                queryTimes.Add(measurements.queryTime);
                sumQueryTime += measurements.queryTime;
                measurements.averageQueryTime = sumQueryTime / tS;

                measurements.averageTotalTime = measurements.averageConstructionTime + measurements.averageQueryTime;
            }
        }
        else if (spatialGrid.chooseNeighborSearch == 2)
        {
            // Measure construction time
            Stopwatch v1 = new Stopwatch();
            v1.Start();
            constructionZIndexSort();
            v1.Stop();
            if (moveParticles)
            {
                measurements.constructionTime = v1.Elapsed.TotalMilliseconds;
                sumConstructionTime += measurements.constructionTime;
                measurements.averageConstructionTime = sumConstructionTime / tS;
            }

            // Measure query time
            Stopwatch v2 = new Stopwatch();
            v2.Start();
            queryZIndexSort();
            v2.Stop();
            if (moveParticles)
            {
                measurements.queryTime = v2.Elapsed.TotalMilliseconds;
                queryTimes.Add(measurements.queryTime);
                sumQueryTime += measurements.queryTime;
                measurements.averageQueryTime = sumQueryTime / tS;

                measurements.averageTotalTime = measurements.averageConstructionTime + measurements.averageQueryTime;
            }
        }

        else if (spatialGrid.chooseNeighborSearch == 3)
        {
            // Measure construction time
            Stopwatch v1 = new Stopwatch();
            v1.Start();
            constructionSpatialHashing();
            v1.Stop();
            if (moveParticles)
            {
                measurements.constructionTime = v1.Elapsed.TotalMilliseconds;
                sumConstructionTime += measurements.constructionTime;
                measurements.averageConstructionTime = sumConstructionTime / tS;
            }

            // Measure query time
            Stopwatch v2 = new Stopwatch();
            v2.Start();
            queryHashTable2();
            v2.Stop();
            if (moveParticles)
            {
                measurements.queryTime = v2.Elapsed.TotalMilliseconds;
                queryTimes.Add(measurements.queryTime);
                sumQueryTime += measurements.queryTime;
                measurements.averageQueryTime = sumQueryTime / tS;

                measurements.averageTotalTime = measurements.averageConstructionTime + measurements.averageQueryTime;
            }
        }

        else if (spatialGrid.chooseNeighborSearch == 4)
        {
            // Measure construction time
            Stopwatch v1 = new Stopwatch();
            v1.Start();
            constructionCompactHashing();
            v1.Stop();
            if (moveParticles)
            {
                measurements.constructionTime = v1.Elapsed.TotalMilliseconds;
                sumConstructionTime += measurements.constructionTime;
                measurements.averageConstructionTime = sumConstructionTime / tS;
            }

            // Measure query time
            Stopwatch v2 = new Stopwatch();
            v2.Start();
            queryCompactHashTable();
            v2.Stop();
            if (moveParticles)
            {
                measurements.queryTime = v2.Elapsed.TotalMilliseconds;
                queryTimes.Add(measurements.queryTime);
                sumQueryTime += measurements.queryTime;
                measurements.averageQueryTime = sumQueryTime / tS;

                measurements.averageTotalTime = measurements.averageConstructionTime + measurements.averageQueryTime;
            }
        }

        // Change color according to velocity
        if (colorParticles)
        {
            for (int j = 0; j < numParticles + numBoundaries; j++)
            {
                int i = particleHandles[j].particleIndex;
                if (particleArray[i] < numParticles)
                {
                    float speed = velocitys[i].magnitude / speedColor;
                    colors[i] = Color.Lerp(Color.blue, Color.red, speed);
                }
            }
        }


        // Compute densitys
        Stopwatch watchDensity = new Stopwatch();
        watchDensity.Start();
        Parallel.For(0, numParticles + numBoundaries, j =>
        {
            int i = particleHandles[j].particleIndex;
            if (particleArray[i] < numParticles)
            {
                computeDensity(i);
            }
        });

        watchDensity.Stop();
        if (moveParticles)
        {
            densityTime = watchDensity.Elapsed.TotalMilliseconds;
            sumDensityTime += densityTime;
            measurements.averageDensityTime = sumDensityTime / tS;
        }

        Stopwatch watchPressure = new Stopwatch();
        watchPressure.Start();
        // Compute pressures
        Parallel.For(0, numParticles + numBoundaries, j =>
        {
            int i = particleHandles[j].particleIndex;
            if (particleArray[i] < numParticles)
            {
                pressures[i] = Mathf.Max(stiffness * ((densitys[i] / startDensity) - 1), 0);
            }
        });
        watchPressure.Stop();
        if (moveParticles)
        {
            pressureTime = watchPressure.Elapsed.TotalMilliseconds;
            sumPressureTime += pressureTime;
            measurements.averagePressureTime = sumPressureTime / tS;
        }

        // Compute non-pressure accelerations
        Stopwatch watchNPForces = new Stopwatch();
        watchNPForces.Start();
        Parallel.For(0, numParticles + numBoundaries, j =>
        {
            int i = particleHandles[j].particleIndex;
            if (particleArray[i] < numParticles)
            {
                Vector2 v = computeViscosityAcceleration(i);
                Vector2 g = new Vector2(0, gravity);
                nPForces[i] = v + g;
            }
        });
        watchNPForces.Stop();
        if (moveParticles)
        {
            NPForcesTime = watchNPForces.Elapsed.TotalMilliseconds;
            sumNPForcesTime += NPForcesTime;
            measurements.averageNPForcesTime = sumNPForcesTime / tS;
        }

        // Compute pressure forces
        Stopwatch watchPressureForces = new Stopwatch();
        watchPressureForces.Start();
        Parallel.For(0, numParticles + numBoundaries, j =>
        {
            int i = particleHandles[j].particleIndex;
            if (particleArray[i] < numParticles)
            {
                computePressureAcceleration(i);
            }
        });
        watchPressureForces.Stop();
        if (moveParticles)
        {
            pressureForcesTime = watchPressureForces.Elapsed.TotalMilliseconds;
            sumPressureForcesTime += pressureForcesTime;
            measurements.averagePressureForcesTime = sumPressureForcesTime / tS;
        }

        // Update particle
        Stopwatch watchmoveParticles = new Stopwatch();
        watchmoveParticles.Start();
        Parallel.For(0, numParticles + numBoundaries, j =>
        {
            int i = particleHandles[j].particleIndex;
            if (particleArray[i] < numParticles)
            {
                Vector2 acceleration = nPForces[i] + forces[i];
                if (moveParticles)
                {
                    MoveParticles(acceleration, i, deltaTime);
                }
            }
        });
        watchmoveParticles.Stop();
        if (moveParticles)
        {
            moveParticlesTime = watchmoveParticles.Elapsed.TotalMilliseconds;
            sumMoveParticlesTime += moveParticlesTime;
            measurements.averageMoveParticlesTime = sumMoveParticlesTime / tS;
        }
    }

    //Move particles
    private void MoveParticles(Vector2 acceleration, int particle, float deltaTime)
    {
        velocitys[particle] += acceleration * deltaTime;
        positions[particle] = positions[particle] + velocitys[particle] * deltaTime;
    }

    // Draw all Particles at Position with their respective color
    private void DrawParticles()
    {
        drawCirclesScript.DrawCirclesAtPositions(convertPositions(positions), colors, particleSize * (texResolution / quadWidth));
        drawCirclesScript.DispatchKernel(Mathf.Max(positions.Length / 256, 1)); //64 for 4 mill
    }

    // Convert positions to texCoords
    private Vector2[] convertPositions(Vector2[] pos)
    {
        Vector2[] results = new Vector2[pos.Length];
        for (int i = 0; i < pos.Length; i++)
        {
            results[i] = pos[i] * (texResolution / quadWidth);
        }
        return results;
    }

    // Initialize particle
    private void InitParticle(Vector2 position, Color color)
    {
        particleHandles[numParticles] = (numParticles, 0);
        particleArray[numParticles] = numParticles;
        positions[numParticles] = position;
        velocitys[numParticles] = new Vector2(0, 0);
        colors[numParticles] = color;
        neighbors[numParticles] = numParticles * numParticleNeighbors;
        densitys[numParticles] = 0.0f;
        pressures[numParticles] = 0.0f;
        forces[numParticles] = new Vector2(0, 0);
        nPForces[numParticles] = new Vector2(0, 0);

        numParticles++;
    }

    // Initialize boundary particle
    private void InitBoundaryParticle(Vector2 position)
    {
        particleHandles[numParticles + numBoundaries] = (numParticles + numBoundaries, 0);
        particleArray[numParticles + numBoundaries] = numParticles + numBoundaries;
        positions[numParticles + numBoundaries] = position;
        colors[numParticles + numBoundaries] = Color.black;
        velocitys[numParticles + numBoundaries] = new Vector2(0, 0);
        densitys[numParticles + numBoundaries] = 0.0f;
        pressures[numParticles + numBoundaries] = 0.0f;
        forces[numParticles + numBoundaries] = new Vector2(0, 0);
        nPForces[numParticles + numBoundaries] = new Vector2(0, 0);

        numBoundaries++;
    }

    private void ShuffleParticlesRandom()
    {
        System.Random rnd = new System.Random();
        // Iterate through all Particles
        for (int i = 0; i < numParticles + numBoundaries; i++)
        {
            // Choose a random Particle
            int rndParticle = rnd.Next(0, numParticles + numBoundaries);
            // Swap Particle attributes
            int tmpParticleReference = particleArray[i];
            Vector2 tmpParticlePosition = positions[i];
            particleArray[i] = particleArray[rndParticle];
            positions[i] = positions[rndParticle];
            particleArray[rndParticle] = tmpParticleReference;
            positions[rndParticle] = tmpParticlePosition;
        }
    }
    // Return mouse position
    private void ReturnMouse()
    {
        Vector3 screenPosition = Input.mousePosition;
        screenPosition.z = Camera.main.nearClipPlane + 1;
        mouse = Camera.main.ScreenToWorldPoint(screenPosition);

        for (int j = 0; j < numParticles + numBoundaries; j++)
        {
            int i = particleHandles[j].particleIndex;
            if (particleArray[i] < numParticles)
            {
                if (Vector2.Distance(positions[i], mouse) <= particleSize)
                {
                    currentParticle = i;
                    currentGridCell = spatialGrid.computeCellPosition(positions[i]);
                }
            }
        }
    }

    // Compute viscosity acceleration of particle i
    private Vector2 computeViscosityAcceleration(int i)
    {
        Vector2 viscosity = new Vector2(0, 0);
        for (int n = 0; n < numParticleNeighbors; n++)
        {
            int num = neighborsParticles[neighbors[i] + n];
            if (num >= 0)
            {
                if (particleArray[num] < numParticles)
                {
                    Vector2 xij = positions[i] - positions[num];
                    Vector2 vij = velocitys[i] - velocitys[num];
                    Vector2 formula = vij * xij / ((xij * xij) + new Vector2(0.01f * particleSpacing * particleSpacing, 0.01f * particleSpacing * particleSpacing));
                    Vector2 gradient = smoothingKernelDerivative(positions[i], positions[num], particleSpacing);
                    viscosity += particleMass / densitys[num] * formula * gradient;
                }
            }
        }
        return 2 * v * viscosity;
    }

    // Compute density for each particle
    private void computeDensity(int i)
    {
        float result = 0.0f;
        for (int n = 0; n < numParticleNeighbors; n++)
        {
            int num = neighborsParticles[neighbors[i] + n];
            if (num >= 0)
            {
                result += particleMass * smoothingKernel(positions[i], positions[num], particleSpacing);
            }
        }
        densitys[i] = result;
    }

    // Compute pressure acceleration
    private void computePressureAcceleration(int i)
    {
        Vector2 result = new Vector2(0, 0);
        for (int n = 0; n < numParticleNeighbors; n++)
        {
            int num = neighborsParticles[neighbors[i] + n];
            if (num >= 0)
            {
                if (particleArray[num] < numParticles)
                {
                    Vector2 gradient = smoothingKernelDerivative(positions[i], positions[num], particleSpacing);
                    float formula = (pressures[i] / (densitys[i] * densitys[i])) + (pressures[num] / (densitys[num] * densitys[num]));
                    result += particleMass * formula * gradient;
                }
                else
                {
                    Vector2 gradient = smoothingKernelDerivative(positions[i], positions[num], particleSpacing);
                    float formula = (pressures[i] / (densitys[i] * densitys[i])) + (pressures[i] / (densitys[i] * densitys[i]));
                    result += particleMass * formula * gradient;
                }
            }
        }
        forces[i] = -result;

        // Vector2 result2 = new Vector2(0, 0);
        // foreach (int num in boundaryNeighbors[particle])
        // {
        //     Vector2 gradient = smoothingKernelDerivative(positions[particle], positions[num], particleSpacing);
        //     float formula = (pressures[particle] / (densitys[particle] * densitys[particle])) + (pressures[particle] / (densitys[particle] * densitys[particle]));
        //     result2 += particleMass * formula * gradient;
        // }
        // forces[particle] = -result - result2;
    }

    // My smoothing Kernel implemented as a cubic spline kernel
    public float smoothingKernel(Vector2 xi, Vector2 xj, float h)
    {
        float d = Vector2.Distance(xi, xj) / h;
        float t1 = Mathf.Max(1 - d, 0);
        float t2 = Mathf.Max(2 - d, 0);
        return alpha * (t2 * t2 * t2 - 4 * t1 * t1 * t1);
    }

    // My kernel Gradient
    public Vector2 smoothingKernelDerivative(Vector2 xi, Vector2 xj, float h)
    {
        float d = Vector2.Distance(xi, xj) / h;
        float t1 = Mathf.Max(1 - d, 0);
        float t2 = Mathf.Max(2 - d, 0);
        if (Vector2.Distance(xi, xj) == 0)
        {
            return new Vector2(0, 0);
        }
        return alpha * (xi - xj) / (Vector2.Distance(xi, xj) * h) * (-3 * t2 * t2 + 12 * t1 * t1);
    }



    // Testing 1: Neighbor search

    // Color all neighbors in one color
    public void colorNeighbors(int j, Color color)
    {
        int i = particleHandles[j].particleIndex;
        if (particleArray[i] < numParticles)
        {
            for (int n = 0; n < numParticleNeighbors; n++)
            {
                int num = neighborsParticles[neighbors[i] + n];
                if (num >= 0)
                {
                    if (particleArray[num] < numParticles)
                    {
                        colors[num] = color;
                    }
                }
            }
        }
    }

    public void colorBoundaryNeighbors(int j, Color color)
    {
        int i = particleHandles[j].particleIndex;
        if (particleArray[i] < numParticles)
        {
            for (int n = 0; n < numParticleNeighbors; n++)
            {
                int num = neighborsParticles[neighbors[i] + n];
                if (num >= 0)
                {
                    if (particleArray[num] >= numParticles)
                    {

                        colors[num] = color;
                    }
                }
            }
        }
    }

    // Testing Mouse Controls

    // Push particles away from the cursor
    void mouseForceOut(Vector2 mousePos, float radius, float strength)
    {
        for (int j = 0; j < numParticles + numBoundaries; j++)
        {
            int i = particleHandles[j].particleIndex;
            if (particleArray[i] < numParticles)
            {
                if (Vector2.Distance(mousePos, positions[i]) < radius)
                {
                    Vector2 direction = mousePos - positions[i];
                    direction = direction.normalized;
                    velocitys[i] = -direction * strength;
                }
            }
        }
    }

    // Pull particles to the cursor
    void mouseForceIn(Vector2 mousePos, float radius, float strength)
    {
        for (int j = 0; j < numParticles + numBoundaries; j++)
        {
            int i = particleHandles[j].particleIndex;
            if (particleArray[i] < numBoundaries)
            {
                if (Vector2.Distance(mousePos, positions[i]) < radius)
                {
                    Vector2 direction = mousePos - positions[i];
                    direction = direction.normalized;
                    velocitys[i] = direction * strength;
                }
            }
        }
    }
}
