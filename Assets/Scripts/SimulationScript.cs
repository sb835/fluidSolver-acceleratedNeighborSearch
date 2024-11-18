using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.IO;
using System.Diagnostics;
using System.Threading.Tasks;

// Start

public class SimulationScript : MonoBehaviour
{
    public bool moveParticles = false;
    public bool colorParticles = false;
    public bool countAvgDensity = false;
    public bool countCFLConditions = false;
    public bool resetParticles = false;
    public float borderOffset = 3.0f;
    public int tests = 1;
    private int counter = 0;
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
    public List<int> particleArray;
    public List<Vector2> positions;
    public List<Vector2> velocitys;
    public List<Color> colors;
    public int[] numNeighbors;
    public List<int>[] neighbors;
    public List<float> densitys;
    public List<float> pressures;
    public List<Vector2> forces;
    public List<Vector2> nPForces;
    public Vector2 maxVelocity = new Vector2(2, 9);
    public float timeStepMultiplyer = 0.9f;
    public float timeStep;
    public float stiffness = 100.0f;
    public float v;
    public int numParticles;
    public int numBoundaries;
    public float particleSize;
    public float particleMass;
    public float particleSpacing;
    public float particleVolume;
    public float startDensity = 1.5f;
    public int texResolution = 2048;
    public float kernelSupportRadius;
    public double constructionTime;
    public double queryTime;
    public double averageConstructionTime;
    public double averageQueryTime;
    public double averageTotalTime;
    public int[] sortedParticles;
    public Vector2[] sortedPositions;
    public Vector2[] sortedVelocitys;
    public Color[] sortedColors;
    public List<int>[] sortedNeighbors;
    public float[] sortedDensitys;
    public float[] sortedPressures;
    public Vector2[] sortedForces;
    public Vector2[] sortedNPForces;
    private DrawCirclesScript drawCirclesScript;
    private GridScript spatialGrid;
    public Camera mainCamera;
    private Vector2 mouse;
    private Vector2 start;
    private Vector2 boundaries;
    private float previousBorderOffset;
    private bool firstSortingDone;
    public long[,] cellZIndices;
    public bool doubleBorder;


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
        texResolution = drawCirclesScript.texResolution;
        particleSpacing = spatialGrid.cellSize / 2;
        particleVolume = particleSpacing * particleSpacing;
        particleMass = startDensity * particleVolume;
        particleSize = particleMass;
        kernelSupportRadius = particleSpacing * 2;
        firstSortingDone = false;

        // Precompute z-indices for all cells
        cellZIndices = new long[spatialGrid.width, spatialGrid.height];
        for (int i = 0; i < spatialGrid.width; i++)
        {
            for (int j = 0; j < spatialGrid.height; j++)
            {
                cellZIndices[i, j] = spatialGrid.computeZIndexForCell(i, j);
            }
        }

        // Define boundaries of our simulation domain
        start = new Vector2(1, 1);
        boundaries = new Vector2(16, 9);

        initializeParticles(resetParticles, (int)amountParticles.x, (int)amountParticles.y, particlePosition, particleStartSpacing);

        // Initialize Grid

        spatialGrid.emptyGrid();
        spatialGrid.clearCellCounter();
        spatialGrid.clearHashTable();

        //Draw grid
        spatialGrid.DrawGrid();

        // Initialize boundary particles
        if (tests == 0)
        {
            initializeBorder(doubleBorder);
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
            initializeBorder3(doubleBorder);
            mainCamera.orthographicSize = 10;
            mainCamera.transform.position = new Vector3(12, 10, -10);
        }

        neighbors = new List<int>[numParticles + numBoundaries];
        numNeighbors = new int[numParticles + numBoundaries];

        // Inform the shader about the total amount of drawn particles
        drawCirclesScript.total = numParticles + numBoundaries;

        averageDensity = new List<float>();

        cflConditions = new List<float>();

        queryTimes = new List<double>();

        previousBorderOffset = borderOffset;

        // IMPORTANT!!!!
        // sortedParticles = new List<int>(particleArray);
        // sortedPositions = new List<Vector2>(positions);
        // sortedVelocitys = new List<Vector2>(velocitys);
        // sortedColors = new List<Color>(colors);
        // sortedNeighbors = new List<List<int>>(neighbors).ToArray();
        // sortedDensitys = new List<float>(densitys);
        // sortedPressures = new List<float>(pressures);
        // sortedForces = new List<Vector2>(forces);
        // sortedNPForces = new List<Vector2>(nPForces);

        sortedParticles = new int[numParticles + numBoundaries];
        sortedPositions = new Vector2[numParticles + numBoundaries];
        sortedVelocitys = new Vector2[numParticles + numBoundaries];
        sortedColors = new Color[numParticles + numBoundaries];
        sortedNeighbors = new List<int>[numParticles + numBoundaries];
        sortedDensitys = new float[numParticles + numBoundaries];
        sortedPressures = new float[numParticles + numBoundaries];
        sortedForces = new Vector2[numParticles + numBoundaries];
        sortedNPForces = new Vector2[numParticles + numBoundaries];

    }
    void ResetValues()
    {
        particleArray = new List<int>();
        positions = new List<Vector2>();
        velocitys = new List<Vector2>();
        colors = new List<Color>();
        densitys = new List<float>();
        pressures = new List<float>();
        forces = new List<Vector2>();
        nPForces = new List<Vector2>();
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
            // particleArray.RemoveRange(numParticles, numBoundaries);
            positions = removeBoundarysVector2(positions);
            // positions.RemoveRange(numParticles, numBoundaries);
            colors = removeBoundarysColors(colors);
            // colors.RemoveRange(numParticles, numBoundaries);
            velocitys = removeBoundarysVector2(velocitys);
            // velocitys.RemoveRange(numParticles, numBoundaries);
            densitys = removeBoundarysFloat(densitys);
            // densitys.RemoveRange(numParticles, numBoundaries);
            pressures = removeBoundarysFloat(pressures);
            // pressures.RemoveRange(numParticles, numBoundaries);
            forces = removeBoundarysVector2(forces);
            // forces.RemoveRange(numParticles, numBoundaries);
            nPForces = removeBoundarysVector2(nPForces);
            // nPForces.RemoveRange(numParticles, numBoundaries);
            // Initialize boundary particles
            particleArray = removeBoundarysInt(particleArray);
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
    void initializeBorder(bool doubleBorder)
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

    void initializeBorder3(bool doubleBorder)
    {
        // Box
        // Down
        float x = 0.0f + particleSize;
        float y = 0.0f + particleSize;
        for (int i = 0; i < 166; i++)
        {
            InitBoundaryParticle(new Vector2(x, y));

            x += 2 * particleSize;
        }

        // Double
        if (doubleBorder)
        {
            x = 0.0f + particleSize;
            y = 0.0f + 3 * particleSize;
            for (int i = 0; i < 166; i++)
            {
                InitBoundaryParticle(new Vector2(x, y));

                x += 2 * particleSize;
            }
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
        if (doubleBorder)
        {
            x = 0.0f - particleSize;
            y = 20.0f - 3 * particleSize;
            for (int i = 0; i < 166; i++)
            {
                InitBoundaryParticle(new Vector2(x, y));

                x += 2 * particleSize;
            }
        }

        // Left
        x = 0.0f + particleSize;
        y = 0.0f + 4 * particleSize;
        for (int i = 0; i < 164; i++)
        {
            InitBoundaryParticle(new Vector2(x, y));

            y += 2 * particleSize;
        }

        // Double
        if (doubleBorder)
        {
            x = 0.0f + 3 * particleSize;
            y = 0.0f + 5 * particleSize;
            for (int i = 0; i < 163; i++)
            {
                InitBoundaryParticle(new Vector2(x, y));

                y += 2 * particleSize;
            }
        }

        // Right
        x = 20.0f - 2 * particleSize;
        y = 0.0f + 4 * particleSize;
        for (int i = 0; i < 164; i++)
        {
            InitBoundaryParticle(new Vector2(x, y));

            y += 2 * particleSize;
        }

        // Double
        if (doubleBorder)
        {
            x = 20.0f - 4 * particleSize;
            y = 0.0f + 5 * particleSize;
            for (int i = 0; i < 163; i++)
            {
                InitBoundaryParticle(new Vector2(x, y));

                y += 2 * particleSize;
            }
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
        for (int i = 0; i < numParticles + numBoundaries; i++)
        {
            Vector2 gridCoords = spatialGrid.computeCellPosition(positions[i]);
            if (spatialGrid.isValidCell(gridCoords))
            {
                spatialGrid.grid[(int)gridCoords.x, (int)gridCoords.y].Add(i);
            }
        }
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
            spatialGrid.hashTable[hashIndex].Add(i);
        }
    }

    void constructionIndexSort()
    {
        if (moveParticles || !firstSortingDone)
        {
            firstSortingDone = true;
            // Clear cell counter
            spatialGrid.clearCellCounter();

            // Compute cell indices for particles and increment counter in C
            for (int i = 0; i < numParticles + numBoundaries; i++)
            {
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
            // For fluid particle
            for (int i = 0; i < numParticles + numBoundaries; i++)
            {
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

            particleArray = new List<int>(sortedParticles);
            positions = new List<Vector2>(sortedPositions);
            velocitys = new List<Vector2>(sortedVelocitys);
            colors = new List<Color>(sortedColors);
            neighbors = new List<List<int>>(sortedNeighbors).ToArray();
            densitys = new List<float>(sortedDensitys);
            pressures = new List<float>(sortedPressures);
            forces = new List<Vector2>(sortedForces);
            nPForces = new List<Vector2>(sortedNPForces);
        }
    }

    void constructionZIndexSort()
    {
        if (moveParticles || !firstSortingDone)
        {
            firstSortingDone = true;
            // Clear cell counter
            spatialGrid.clearCellCounter();

            // Compute cell indices for particles and increment counter in C
            for (int i = 0; i < numParticles + numBoundaries; i++)
            {
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
            for (int i = 0; i < numParticles + numBoundaries; i++)
            {
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

            particleArray = new List<int>(sortedParticles);
            positions = new List<Vector2>(sortedPositions);
            velocitys = new List<Vector2>(sortedVelocitys);
            colors = new List<Color>(sortedColors);
            neighbors = new List<List<int>>(sortedNeighbors).ToArray();
            densitys = new List<float>(sortedDensitys);
            pressures = new List<float>(sortedPressures);
            forces = new List<Vector2>(sortedForces);
            nPForces = new List<Vector2>(sortedNPForces);
        }
    }
    // Query Methods
    // query means quering through our potential neighbors and finding real neighbors
    void queryGrid()
    {
        // Find all neighbors for each particle
        if (spatialGrid.parallelSearchActivated)
        {
            //Limiting the maximum degree of parallelism to 8
            var options = new ParallelOptions()
            {
                MaxDegreeOfParallelism = 8
            };
            Parallel.For(0, numParticles + numBoundaries, options, i =>
            {
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
            var options = new ParallelOptions()
            {
                MaxDegreeOfParallelism = 8
            };
            Parallel.For(0, numParticles + numBoundaries, options, i =>
            {
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

    void queryZIndexSort()
    {
        // Find all neighbors for each particle
        if (spatialGrid.parallelSearchActivated)
        {
            //Limiting the maximum degree of parallelism to 8
            var options = new ParallelOptions()
            {
                MaxDegreeOfParallelism = 8
            };
            Parallel.For(0, numParticles + numBoundaries, options, i =>
            {
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
            //Limiting the maximum degree of parallelism to 8
            var options = new ParallelOptions()
            {
                MaxDegreeOfParallelism = 8
            };
            Parallel.For(0, numParticles + numBoundaries, options, i =>
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

    // Neighbor search
    private void findNeighbors(int i)
    {
        List<int> n = new List<int>();
        Vector2 gridCell = spatialGrid.computeCellPosition(positions[i]);
        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                int cellX = (int)gridCell.x + x;
                int cellY = (int)gridCell.y + y;
                if (spatialGrid.isValidCell(new Vector2(cellX, cellY)))
                {
                    foreach (int p in spatialGrid.grid[cellX, cellY])
                    {
                        if (Vector2.Distance(positions[i], positions[p]) < kernelSupportRadius)
                        {
                            n.Add(p);
                        }
                    }
                }
            }
        }
        neighbors[i] = n;
    }

    void findNeighborsIndexSort(int i)
    {
        List<int> n = new List<int>();
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
                            n.Add(j);
                        }
                    }
                }
            }
        }

        neighbors[i] = n;
    }

    void findNeighborsZIndexSort(int i)
    {
        List<int> n = new List<int>();
        Vector2 gridCell = spatialGrid.computeCellPosition(positions[i]);
        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                int cellX = (int)gridCell.x + x;
                int cellY = (int)gridCell.y + y;
                if (spatialGrid.isValidCell(new Vector2(cellX, cellY)))
                {
                    // long cellIndex = spatialGrid.computeZIndexForCell(cellX, cellY);
                    long cellIndex = cellZIndices[cellX, cellY];
                    int cellStart = spatialGrid.cellCounter[cellIndex];
                    int cellEnd = spatialGrid.cellCounter[cellIndex + 1];
                    for (int j = cellStart; j < cellEnd; j++)
                    {
                        if (Vector2.Distance(positions[i], positions[j]) < kernelSupportRadius)
                        {
                            n.Add(j);
                        }
                    }
                }
            }
        }

        neighbors[i] = n;
    }

    private void findNeighborsSpatialHashing(int i)
    {
        List<int> n = new List<int>();
        List<int> hashIndices = new List<int>();
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
                    if (!hashIndices.Contains(cellIndex))
                    {
                        hashIndices.Add(cellIndex);

                        foreach (int p in spatialGrid.hashTable[cellIndex])
                        {
                            if (Vector2.Distance(positions[i], positions[p]) < kernelSupportRadius)
                            {
                                n.Add(p);
                            }
                        }
                    }
                }
            }
        }
        neighbors[i] = n;
    }

    // Return int List without boundary particles
    List<int> removeBoundarysInt(List<int> particles)
    {
        List<int> newList = new List<int>();
        for (int i = 0; i < numParticles + numBoundaries; i++)
        {
            if (particleArray[i] < numParticles)
            {
                newList.Add(particles[i]);
            }
        }
        return newList;
    }

    // Return Vector2 List without boundary particles
    List<Vector2> removeBoundarysVector2(List<Vector2> particles)
    {
        List<Vector2> newList = new List<Vector2>();
        for (int i = 0; i < numParticles + numBoundaries; i++)
        {
            if (particleArray[i] < numParticles)
            {
                newList.Add(particles[i]);
            }
        }
        return newList;
    }

    // Return Vector2 floats without boundary particles
    List<float> removeBoundarysFloat(List<float> particles)
    {
        List<float> newList = new List<float>();
        for (int i = 0; i < numParticles + numBoundaries; i++)
        {
            if (particleArray[i] < numParticles)
            {
                newList.Add(particles[i]);
            }
        }
        return newList;
    }

    // Return Vector2 colors without boundary particles
    List<Color> removeBoundarysColors(List<Color> particles)
    {
        List<Color> newList = new List<Color>();
        for (int i = 0; i < numParticles + numBoundaries; i++)
        {
            if (particleArray[i] < numParticles)
            {
                newList.Add(particles[i]);
            }
        }
        return newList;
    }
    // Update is called once per frame
    void Update()
    {
        if (previousBorderOffset != borderOffset && tests == 0)
        {
            // particleArray.RemoveRange(numParticles, numBoundaries);
            positions = removeBoundarysVector2(positions);
            // positions.RemoveRange(numParticles, numBoundaries);
            colors = removeBoundarysColors(colors);
            // colors.RemoveRange(numParticles, numBoundaries);
            velocitys = removeBoundarysVector2(velocitys);
            // velocitys.RemoveRange(numParticles, numBoundaries);
            densitys = removeBoundarysFloat(densitys);
            // densitys.RemoveRange(numParticles, numBoundaries);
            pressures = removeBoundarysFloat(pressures);
            // pressures.RemoveRange(numParticles, numBoundaries);
            forces = removeBoundarysVector2(forces);
            // forces.RemoveRange(numParticles, numBoundaries);
            nPForces = removeBoundarysVector2(nPForces);
            // nPForces.RemoveRange(numParticles, numBoundaries);
            // Initialize boundary particles
            particleArray = removeBoundarysInt(particleArray);
            numBoundaries = 0;
            initializeBorder(doubleBorder);
            initializeBorderWall(borderOffset, doubleBorder);

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
                initializeBorder(doubleBorder);
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
                initializeBorder3(doubleBorder);
                mainCamera.orthographicSize = 10;
                mainCamera.transform.position = new Vector3(12, 10, -10);
            }

            averageDensity = new List<float>();

            cflConditions = new List<float>();

            queryTimes = new List<double>();

            // Inform the shader about the total amount of drawn particles
            drawCirclesScript.total = numParticles + numBoundaries;

            neighbors = new List<int>[numParticles + numBoundaries];
            numNeighbors = new int[numParticles + numBoundaries];

            // IMPORTANT!!!!
            sortedParticles = new int[numParticles + numBoundaries];
            sortedPositions = new Vector2[numParticles + numBoundaries];
            sortedVelocitys = new Vector2[numParticles + numBoundaries];
            sortedColors = new Color[numParticles + numBoundaries];
            sortedNeighbors = new List<int>[numParticles + numBoundaries];
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
            mouseForceOut(Camera.main.ScreenToWorldPoint(screenPosition), 2f, 1.5f);
        }

        if (Input.GetKey(KeyCode.D))
        {
            Vector3 screenPosition = Input.mousePosition;
            screenPosition.z = Camera.main.nearClipPlane + 1;
            mouseForceIn(Camera.main.ScreenToWorldPoint(screenPosition), 2f, 1.5f);
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
        for (int i = 0; i < numParticles + numBoundaries; i++)
        {
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
            for (int i = 0; i < numParticles + numBoundaries; i++)
            {
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

        // Calculate time step
        SimulationStep(timeStep);
        DrawParticles();

        if (moveParticles)
        {
            tS++;
            if (tS == maxTS)
            {
                moveParticles = !moveParticles;
            }
        }

    }
    void SimulationStep(float deltaTime)
    {
        if (moveParticles)
        {
            counter++;
        }

        if (spatialGrid.chooseNeighborSearch == 0)
        {
            // Measure construction time
            var v1 = new Stopwatch();
            v1.Start();
            constructionBasic();
            v1.Stop();
            if (moveParticles)
            {
                constructionTime = v1.Elapsed.TotalMilliseconds;
                sumConstructionTime += constructionTime;
                averageConstructionTime = sumConstructionTime / counter;
            }

            // Measure query time
            var v2 = new Stopwatch();
            v2.Start();
            queryGrid();
            v2.Stop();
            if (moveParticles)
            {
                queryTime = v2.Elapsed.TotalMilliseconds;
                queryTimes.Add(queryTime);
                sumQueryTime += queryTime;
                averageQueryTime = sumQueryTime / counter;

                averageTotalTime = averageConstructionTime + averageQueryTime;
            }

        }
        else if (spatialGrid.chooseNeighborSearch == 1)
        {
            // Measure construction time
            var v1 = new Stopwatch();
            v1.Start();
            constructionIndexSort();
            v1.Stop();
            if (moveParticles)
            {
                constructionTime = v1.Elapsed.TotalMilliseconds;
                sumConstructionTime += constructionTime;
                averageConstructionTime = sumConstructionTime / counter;
            }

            // Measure query time
            var v2 = new Stopwatch();
            v2.Start();
            queryIndexSort();
            v2.Stop();
            if (moveParticles)
            {
                queryTime = v2.Elapsed.TotalMilliseconds;
                queryTimes.Add(queryTime);
                sumQueryTime += queryTime;
                averageQueryTime = sumQueryTime / counter;

                averageTotalTime = averageConstructionTime + averageQueryTime;
            }
        }
        else if (spatialGrid.chooseNeighborSearch == 2)
        {
            // Measure construction time
            var v1 = new Stopwatch();
            v1.Start();
            constructionZIndexSort();
            v1.Stop();
            if (moveParticles)
            {
                constructionTime = v1.Elapsed.TotalMilliseconds;
                sumConstructionTime += constructionTime;
                averageConstructionTime = sumConstructionTime / counter;
            }

            // Measure query time
            var v2 = new Stopwatch();
            v2.Start();
            queryZIndexSort();
            v2.Stop();
            if (moveParticles)
            {
                queryTime = v2.Elapsed.TotalMilliseconds;
                queryTimes.Add(queryTime);
                sumQueryTime += queryTime;
                averageQueryTime = sumQueryTime / counter;

                averageTotalTime = averageConstructionTime + averageQueryTime;
            }
        }

        else if (spatialGrid.chooseNeighborSearch == 3)
        {
            // Measure construction time
            var v1 = new Stopwatch();
            v1.Start();
            constructionSpatialHashing();
            v1.Stop();
            if (moveParticles)
            {
                constructionTime = v1.Elapsed.TotalMilliseconds;
                sumConstructionTime += constructionTime;
                averageConstructionTime = sumConstructionTime / counter;
            }

            // Measure query time
            var v2 = new Stopwatch();
            v2.Start();
            queryHashTable();
            v2.Stop();
            if (moveParticles)
            {
                queryTime = v2.Elapsed.TotalMilliseconds;
                queryTimes.Add(queryTime);
                sumQueryTime += queryTime;
                averageQueryTime = sumQueryTime / counter;

                averageTotalTime = averageConstructionTime + averageQueryTime;
            }
        }

        // Compute num neighbors
        for (int x = 0; x < numParticles + numBoundaries; x++)
        {
            if (particleArray[x] < numParticles)
            {
                numNeighbors[x] = neighbors[x].Count;
            }
        }

        // Change color according to velocity
        if (colorParticles)
        {
            for (int i = 0; i < numParticles + numBoundaries; i++)
            {
                if (particleArray[i] < numParticles)
                {
                    float speed = velocitys[i].magnitude / speedColor;
                    colors[i] = Color.Lerp(Color.blue, Color.red, speed);
                }
            }
        }

        // Compute non-pressure accelerations
        for (int i = 0; i < numParticles + numBoundaries; i++)
        {
            if (particleArray[i] < numParticles)
            {
                Vector2 v = computeViscosityAcceleration(i);
                Vector2 g = new Vector2(0, gravity);
                nPForces[i] = v + g;
            }
        }

        // Compute densitys
        for (int i = 0; i < numParticles + numBoundaries; i++)
        {
            if (particleArray[i] < numParticles)
            {
                computeDensity(i);
            }
        }
        // Compute pressures
        for (int i = 0; i < numParticles + numBoundaries; i++)
        {
            if (particleArray[i] < numParticles)
            {
                pressures[i] = Mathf.Max(stiffness * ((densitys[i] / startDensity) - 1), 0);
            }
        }

        // Compute pressure forces
        for (int i = 0; i < numParticles + numBoundaries; i++)
        {
            if (particleArray[i] < numParticles)
            {
                computePressureAcceleration(i);
            }
        }

        // Update particle
        for (int i = 0; i < numParticles + numBoundaries; i++)
        {
            if (particleArray[i] < numParticles)
            {
                Vector2 acceleration = new Vector2(0, 0);
                acceleration = nPForces[i] + forces[i];
                if (moveParticles)
                {
                    MoveParticles(acceleration, i, deltaTime);
                }
            }
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
        Vector2[] particlePositions = positions.ToArray();

        Color[] particleColors = colors.ToArray();

        drawCirclesScript.DrawCirclesAtPositions(convertPositions(particlePositions), particleColors, particleSize * 102.4f);
        drawCirclesScript.DispatchKernel(Mathf.Max(particlePositions.Length / 16, 1));
    }

    // Convert positions to texCoords
    private Vector2[] convertPositions(Vector2[] pos)
    {
        Vector2[] results = new Vector2[pos.Length];
        for (int i = 0; i < pos.Length; i++)
        {
            results[i] = pos[i] * 102.4f;
        }
        return results;
    }

    // Initialize particle
    private void InitParticle(Vector2 position, Color color)
    {
        particleArray.Add(numParticles);

        positions.Add(position);
        velocitys.Add(new Vector2(0, 0));
        colors.Add(color);
        densitys.Add(0.0f);
        pressures.Add(0.0f);
        forces.Add(new Vector2(0, 0));
        nPForces.Add(new Vector2(0, 0));

        numParticles++;
    }

    // Initialize boundary particle
    private void InitBoundaryParticle(Vector2 position)
    {
        particleArray.Add(numParticles + numBoundaries);

        positions.Add(position);
        colors.Add(Color.black);
        velocitys.Add(new Vector2(0, 0));
        densitys.Add(0.0f);
        pressures.Add(0.0f);
        forces.Add(new Vector2(0, 0));
        nPForces.Add(new Vector2(0, 0));

        numBoundaries++;
    }


    // Return mouse position
    private void ReturnMouse()
    {
        Vector3 screenPosition = Input.mousePosition;
        screenPosition.z = Camera.main.nearClipPlane + 1;
        mouse = Camera.main.ScreenToWorldPoint(screenPosition);

        for (int i = 0; i < numParticles + numBoundaries; i++)
        {
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
        foreach (int num in neighbors[i])
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
        return 2 * v * viscosity;
    }

    // Compute density for each particle
    private void computeDensity(int i)
    {
        float result = 0.0f;
        foreach (int num in neighbors[i])
        {
            result += particleMass * smoothingKernel(positions[i], positions[num], particleSpacing);
        }

        // foreach (int num in boundaryNeighbors[particle])
        // {
        //     result += particleMass * smoothingKernel(positions[particle], positions[num], particleSpacing);
        // }
        densitys[i] = result;
    }

    // Compute pressure acceleration
    private void computePressureAcceleration(int i)
    {
        Vector2 result = new Vector2(0, 0);
        foreach (int num in neighbors[i])
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
    public void colorNeighbors(int i, Color color)
    {
        if (particleArray[i] < numParticles)
        {
            foreach (int num in neighbors[i])
            {
                if (particleArray[num] < numParticles)
                {
                    colors[num] = color;
                }
            }
        }
    }

    public void colorBoundaryNeighbors(int i, Color color)
    {
        if (particleArray[i] < numParticles)
        {
            foreach (int num in neighbors[i])
            {
                if (particleArray[num] >= numParticles)
                {

                    colors[num] = color;
                }
            }
        }
    }

    // Testing Mouse Controls

    // Push particles away from the cursor
    void mouseForceOut(Vector2 mousePos, float radius, float strength)
    {
        for (int i = 0; i < numParticles + numBoundaries; i++)
        {
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
        for (int i = 0; i < numParticles + numBoundaries; i++)
        {
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
