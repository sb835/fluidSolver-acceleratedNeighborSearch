using System.Collections.Generic;
using UnityEngine;
using System.Diagnostics;
using System.Threading.Tasks;
public class SimulationScript : MonoBehaviour
{
    public bool moveParticles = false;
    public bool colorParticles = false;
    public bool colorSpatialLocality = false;
    public bool countAvgDensity = false;
    public bool countCFLConditions = false;
    public bool resetParticles = true;
    public float borderOffset = 3.0f;
    public int tests = 1;
    private double sumConstructionTime = 0;
    private double sumQueryTime = 0;
    public int tS;
    public int maxTS;
    public int constructionCounter;
    public Vector2 amountParticles;
    public Vector2 particlePosition;
    public float particleStartSpacing;
    public float speedColor = 4.0f;
    public float alpha;
    public (long cellIndex, int particleIndex)[] particleArray;
    public bool[] isFluid;
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
    public (long cellIndex, int particleIndex)[] sortedParticles;
    public bool[] sortedIsFluid;
    public Vector2[] sortedPositions;
    public Vector2[] sortedVelocitys;
    public Color[] sortedColors;
    public int[] sortedNeighbors;
    public float[] sortedDensitys;
    public float[] sortedPressures;
    public Vector2[] sortedForces;
    public Vector2[] sortedNPForces;
    private HelperScript helper;
    private DrawCirclesScript drawCirclesScript;
    private GridScript spatialGrid;
    private MeasurementsScript measurements;
    private BasicGridScript basicGrid;
    private IndexSortScript indexSort;
    private ZIndexSortScript zIndexSort;
    private SpatialHashingScript spatialHashing;
    private CompactHashingScript compactHashing;
    private CellLinkedListScript cellLinkedList;
    public Camera mainCamera;
    private Vector2 mouse;
    public Vector2 start;
    public Vector2 boundaries;
    private float previousBorderOffset;
    public bool firstSortingDone;
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

    // Used for compact hashing
    private int[] spatialCellsCompactArray;
    private int[] movingParticles;
    public Comparer<(long cellIndex, int particleIndex)> comparer;
    public int[] markers;
    public long[] scans;



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
        helper = GameObject.FindGameObjectWithTag("Helper").GetComponent<HelperScript>();
        basicGrid = GameObject.FindGameObjectWithTag("BasicGrid").GetComponent<BasicGridScript>();
        indexSort = GameObject.FindGameObjectWithTag("IndexSort").GetComponent<IndexSortScript>();
        zIndexSort = GameObject.FindGameObjectWithTag("ZIndexSort").GetComponent<ZIndexSortScript>();
        spatialHashing = GameObject.FindGameObjectWithTag("SpatialHashing").GetComponent<SpatialHashingScript>();
        compactHashing = GameObject.FindGameObjectWithTag("CompactHashing").GetComponent<CompactHashingScript>();
        cellLinkedList = GameObject.FindGameObjectWithTag("CellLinkedList").GetComponent<CellLinkedListScript>();


        particleSpacing = spatialGrid.cellSize / 2;
        particleVolume = particleSpacing * particleSpacing;
        particleMass = startDensity * particleVolume;
        particleSize = particleMass;
        kernelSupportRadius = particleSpacing * 2;
        firstSortingDone = false;

        watchCounter = 1;
        constructionCounter = 100;

        // Define boundaries of our simulation domain
        start = new Vector2(1, 1);
        boundaries = new Vector2(16, 9);

        if (tests == 0)
        {
            numBoundaries = 0;
            amountParticles = new Vector2(5, 5);
            particlePosition = new Vector2(0.5f, 1.1f);
        }
        else if (tests == 1)
        {
            numBoundaries = 0;
        }
        else if (tests == 2)
        {
            numBoundaries = 166 * 8;
        }
        else if (tests == 3)
        {
            numBoundaries = 4 * 332 + 4 * 623;
            amountParticles = new Vector2(190, 190);
        }
        else if (tests == 4)
        {
            numBoundaries = 8 * 800;
            amountParticles = new Vector2(400, 400);
        }
        else if (tests == 5)
        {
            numBoundaries = 4 * 1700 + 4 * 2000;
            amountParticles = new Vector2(1000, 1000);
        }

        else if (tests == 6)
        {
            numBoundaries = 8 * 4000;
            amountParticles = new Vector2(2000, 2000);
        }

        else if (tests == 7)
        {
            numBoundaries = 8 * 6000;
            amountParticles = new Vector2(3000, 3000);
        }
        else if (tests == 8)
        {
            numBoundaries = 0;
            amountParticles = new Vector2(4000, 4000);
        }
        else if (tests == 9)
        {
            numBoundaries = 0;
            amountParticles = new Vector2(5000, 5000);
        }
        else if (tests == 10)
        {
            numBoundaries = 0;
            amountParticles = new Vector2(5100, 5100);
        }

        numParticles = (int)amountParticles.x * (int)amountParticles.y;

        particleArray = new (long, int)[numParticles + numBoundaries];
        isFluid = new bool[numParticles + numBoundaries];
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

        helper.initializeParticles(resetParticles, (int)amountParticles.x, (int)amountParticles.y, particlePosition, particleStartSpacing);

        // Reorder Particles
        if (spatialGrid.randomInitializedParticles)
        {
            ShuffleParticlesRandom();
        }

        // Initialize Grid

        spatialGrid.emptyGrid();
        spatialGrid.clearCellCounter(0);
        spatialGrid.clearHashTable();

        //Draw grid
        spatialGrid.DrawGrid();

        // Initialize boundary particles
        if (tests == 0)
        {
            // helper.initializeBorder0(doubleBorder);
            // helper.initializeBorderWall(borderOffset, doubleBorder);
            mainCamera.orthographicSize = 5;
            mainCamera.transform.position = new Vector3(8.91f, 5, -10);

            drawCirclesScript.transform.position = new Vector3(50, 50, 0);
            drawCirclesScript.transform.localScale = new Vector3(100, 100, 0);
        }
        else if (tests == 1)
        {
            // helper.initializeBorder2();
            mainCamera.orthographicSize = 1;
            mainCamera.transform.position = new Vector3(1.78f, 1, -10);

            drawCirclesScript.transform.position = new Vector3(50, 50, 0);
            drawCirclesScript.transform.localScale = new Vector3(100, 100, 0);
        }

        else if (tests == 2)
        {
            helper.initializeBorder(166, 166, 166, 166, true);
            mainCamera.orthographicSize = 10;
            mainCamera.transform.position = new Vector3(12, 10, -10);

            drawCirclesScript.transform.position = new Vector3(50, 50, 0);
            drawCirclesScript.transform.localScale = new Vector3(100, 100, 0);
        }

        else if (tests == 3)
        {
            helper.initializeBorder(332, 332, 623, 623, true);
            mainCamera.orthographicSize = 23;
            mainCamera.transform.position = new Vector3(41f, 23, -10);

            drawCirclesScript.transform.position = new Vector3(50, 50, 0);
            drawCirclesScript.transform.localScale = new Vector3(100, 100, 0);
        }

        else if (tests == 4)
        {
            helper.initializeBorder(800, 800, 800, 800, true);
            mainCamera.orthographicSize = 40;
            mainCamera.transform.position = new Vector3(50, 40, -10);

            drawCirclesScript.transform.position = new Vector3(50, 50, 0);
            drawCirclesScript.transform.localScale = new Vector3(100, 100, 0);
        }

        else if (tests == 5)
        {
            helper.initializeBorder(1700, 1700, 2000, 2000, true);
            mainCamera.orthographicSize = 120;
            mainCamera.transform.position = new Vector3(155, 120, -10);

            drawCirclesScript.transform.position = new Vector3(250, 250, 0);
            drawCirclesScript.transform.localScale = new Vector3(500, 500, 0);
        }

        else if (tests == 6)
        {
            helper.initializeBorder(4000, 4000, 4000, 4000, true);
            mainCamera.orthographicSize = 220;
            mainCamera.transform.position = new Vector3(280, 220, -10);

            drawCirclesScript.transform.position = new Vector3(250, 250, 0);
            drawCirclesScript.transform.localScale = new Vector3(500, 500, 0);
        }

        else if (tests == 7)
        {
            helper.initializeBorder(6000, 6000, 6000, 6000, true);
            mainCamera.orthographicSize = 220;
            mainCamera.transform.position = new Vector3(280, 220, -10);

            drawCirclesScript.transform.position = new Vector3(350, 350, 0);
            drawCirclesScript.transform.localScale = new Vector3(700, 700, 0);
        }
        else if (tests == 8)
        {
            // helper.initializeBorder(6000, 6000, 6000, 6000, true);
            mainCamera.orthographicSize = 220;
            mainCamera.transform.position = new Vector3(280, 220, -10);

            drawCirclesScript.transform.position = new Vector3(500, 500, 0);
            drawCirclesScript.transform.localScale = new Vector3(1000, 1000, 0);
        }
        else if (tests == 9)
        {
            mainCamera.orthographicSize = 220;
            mainCamera.transform.position = new Vector3(280, 220, -10);

            drawCirclesScript.transform.position = new Vector3(500, 500, 0);
            drawCirclesScript.transform.localScale = new Vector3(1000, 1000, 0);
        }
        else if (tests == 10)
        {
            mainCamera.orthographicSize = 220;
            mainCamera.transform.position = new Vector3(280, 220, -10);

            drawCirclesScript.transform.position = new Vector3(500, 500, 0);
            drawCirclesScript.transform.localScale = new Vector3(1000, 1000, 0);
        }

        texResolution = drawCirclesScript.texResolution;
        quadWidth = drawCirclesScript.transform.localScale.x;
        quadHeight = drawCirclesScript.transform.localScale.y;

        // Inform the shader about the total amount of drawn particles
        drawCirclesScript.total = numParticles + numBoundaries;

        previousBorderOffset = borderOffset;

        sortedParticles = new (long, int)[numParticles + numBoundaries];
        sortedIsFluid = new bool[numParticles + numBoundaries];
        sortedPositions = new Vector2[numParticles + numBoundaries];
        sortedVelocitys = new Vector2[numParticles + numBoundaries];
        sortedColors = new Color[numParticles + numBoundaries];
        sortedNeighbors = new int[numParticles + numBoundaries];
        sortedDensitys = new float[numParticles + numBoundaries];
        sortedPressures = new float[numParticles + numBoundaries];
        sortedForces = new Vector2[numParticles + numBoundaries];
        sortedNPForces = new Vector2[numParticles + numBoundaries];

        spatialCellsCompactArray = new int[numParticles + numBoundaries];
        for (int i = 0; i < numParticles + numBoundaries; i++)
        {
            spatialCellsCompactArray[i] = -1;
        }
        movingParticles = new int[numParticles + numBoundaries];

        // Erzeuge einen IComparer<(long, int)>, der nur nach cellIndex sortiert:
        comparer = Comparer<(long cellIndex, int particleIndex)>.Create(
            (a, b) => a.cellIndex.CompareTo(b.cellIndex)
        );

        markers = new int[numParticles + numBoundaries];
        scans = new long[numParticles + numBoundaries];
    }


    void Update()
    {
        Stopwatch watch3 = new Stopwatch();
        watch3.Start();
        if (previousBorderOffset != borderOffset && tests == 0)
        {
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

            helper.initializeParticles(resetParticles, (int)amountParticles.x, (int)amountParticles.y, particlePosition, particleStartSpacing);

            // Initialize grid
            spatialGrid.emptyGrid();
            spatialGrid.clearCellCounter(0);
            spatialGrid.clearHashTable();

            // Initialize boundary particles
            if (tests == 0)
            {
                helper.initializeBorder0(doubleBorder);
                helper.initializeBorderWall(borderOffset, doubleBorder);
                mainCamera.orthographicSize = 5;
                mainCamera.transform.position = new Vector3(8.91f, 5, -10);
            }
            else if (tests == 1)
            {
                helper.initializeBorder2();
                mainCamera.orthographicSize = 10;
                mainCamera.transform.position = new Vector3(12, 10, -10);
            }

            else if (tests == 2)
            {
                helper.initializeBorder(166, 166, 166, 166, true);
                mainCamera.orthographicSize = 10;
                mainCamera.transform.position = new Vector3(12, 10, -10);
            }

            else if (tests == 3)
            {
                helper.initializeBorder(332, 332, 623, 623, true);
                mainCamera.orthographicSize = 23;
                mainCamera.transform.position = new Vector3(41f, 23, -10);
            }

            else if (tests == 4)
            {
                helper.initializeBorder(800, 800, 800, 800, true);
                mainCamera.orthographicSize = 40;
                mainCamera.transform.position = new Vector3(50, 40, -10);
            }

            else if (tests == 5)
            {
                helper.initializeBorder(1700, 1700, 1700, 1700, true);
                mainCamera.orthographicSize = 100;
                mainCamera.transform.position = new Vector3(100, 100, -10);
            }

            // Inform the shader about the total amount of drawn particles
            drawCirclesScript.total = numParticles + numBoundaries;

            // IMPORTANT!!!!
            sortedParticles = new (long, int)[numParticles + numBoundaries];
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
            colorParticles = !colorParticles;
        }

        if (Input.GetKeyDown(KeyCode.V))
        {
            colorSpatialLocality = !colorSpatialLocality;
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
        for (int particle = 0; particle < numParticles + numBoundaries; particle++)
        {
            int i = particleArray[particle].particleIndex;
            if (isFluid[i])
            {
                if (velocitys[i].magnitude > maxVelocity.magnitude && velocitys[i].y > -15 && velocitys[i].magnitude > 0)
                {
                    maxVelocity = velocitys[i];
                }
            }
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
        if (spatialGrid.chooseNeighborSearch == 0)
        {
            // Measure construction time
            Stopwatch v1 = new Stopwatch();
            v1.Start();
            basicGrid.construction();
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
            basicGrid.query();
            v2.Stop();
            if (moveParticles)
            {
                measurements.queryTime = v2.Elapsed.TotalMilliseconds;
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
            indexSort.construction();
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
            indexSort.query();
            v2.Stop();
            if (moveParticles)
            {
                measurements.queryTime = v2.Elapsed.TotalMilliseconds;
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
            zIndexSort.construction();
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
            zIndexSort.query();
            v2.Stop();
            if (moveParticles)
            {
                measurements.queryTime = v2.Elapsed.TotalMilliseconds;
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
            spatialHashing.construction();
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
            spatialHashing.query();
            v2.Stop();
            if (moveParticles)
            {
                measurements.queryTime = v2.Elapsed.TotalMilliseconds;
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
            compactHashing.construction();
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
            compactHashing.query();
            v2.Stop();
            if (moveParticles)
            {
                measurements.queryTime = v2.Elapsed.TotalMilliseconds;
                sumQueryTime += measurements.queryTime;
                measurements.averageQueryTime = sumQueryTime / tS;

                measurements.averageTotalTime = measurements.averageConstructionTime + measurements.averageQueryTime;
            }
        }
        else if (spatialGrid.chooseNeighborSearch == 5)
        {
            // Measure construction time
            Stopwatch v1 = new Stopwatch();
            v1.Start();
            cellLinkedList.construction();
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
            cellLinkedList.query();
            v2.Stop();
            if (moveParticles)
            {
                measurements.queryTime = v2.Elapsed.TotalMilliseconds;
                sumQueryTime += measurements.queryTime;
                measurements.averageQueryTime = sumQueryTime / tS;

                measurements.averageTotalTime = measurements.averageConstructionTime + measurements.averageQueryTime;
            }
        }

        // Change color according to velocity
        if (colorParticles)
        {
            for (int particle = 0; particle < numParticles + numBoundaries; particle++)
            {
                int i = particleArray[particle].particleIndex;
                if (isFluid[i])
                {
                    float speed = velocitys[i].magnitude / speedColor;
                    colors[i] = Color.Lerp(Color.blue, Color.red, speed);
                }
            }
        }

        // Change color according to spatial locality
        if (colorSpatialLocality)
        {
            for (int particle = 0; particle < numParticles + numBoundaries; particle++)
            {
                int i = particleArray[particle].particleIndex;
                float locality = (float)particle / (numParticles + numBoundaries);
                colors[i] = Color.Lerp(Color.blue, Color.red, locality);
            }
        }


        // Compute densitys
        Stopwatch watchDensity = new Stopwatch();
        watchDensity.Start();
        Parallel.For(0, numParticles + numBoundaries, particle =>
        {
            int i = particleArray[particle].particleIndex;
            if (isFluid[i])
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
        Parallel.For(0, numParticles + numBoundaries, particle =>
        {
            int i = particleArray[particle].particleIndex;
            if (isFluid[i])
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
        Parallel.For(0, numParticles + numBoundaries, particle =>
        {
            int i = particleArray[particle].particleIndex;
            if (isFluid[i])
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
        Parallel.For(0, numParticles + numBoundaries, particle =>
        {
            int i = particleArray[particle].particleIndex;
            if (isFluid[i])
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
        Parallel.For(0, numParticles + numBoundaries, particle =>
        {
            int i = particleArray[particle].particleIndex;
            if (isFluid[i])
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

    private void ShuffleParticlesRandom()
    {
        System.Random rnd = new System.Random();
        // Iterate through all Particles
        for (int i = 0; i < numParticles + numBoundaries; i++)
        {
            // Choose a random Particle
            int rndParticle = rnd.Next(0, numParticles + numBoundaries);
            // Swap Particle attributes
            (long, int) tmpParticleReference = particleArray[i];
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

        for (int particle = 0; particle < numParticles + numBoundaries; particle++)
        {
            int i = particleArray[particle].particleIndex;
            if (isFluid[i])
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
                if (isFluid[num])
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
                if (isFluid[num])
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



    // Color all neighbors in one color
    public void colorNeighbors(int i, Color color)
    {
        if (isFluid[i])
        {
            for (int n = 0; n < numParticleNeighbors; n++)
            {
                int num = neighborsParticles[neighbors[i] + n];
                if (num >= 0)
                {
                    if (isFluid[num])
                    {
                        colors[num] = color;
                    }
                }
            }
        }
    }

    public void colorBoundaryNeighbors(int i, Color color)
    {
        if (isFluid[i])
        {
            for (int n = 0; n < numParticleNeighbors; n++)
            {
                int num = neighborsParticles[neighbors[i] + n];
                if (num >= 0)
                {
                    if (!isFluid[num])
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
        for (int particle = 0; particle < numParticles + numBoundaries; particle++)
        {
            int i = particleArray[particle].particleIndex;
            if (isFluid[i])
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
        for (int particle = 0; particle < numParticles + numBoundaries; particle++)
        {
            int i = particleArray[particle].particleIndex;
            if (isFluid[i])
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
