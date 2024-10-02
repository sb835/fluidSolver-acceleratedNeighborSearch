using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

public class ParticleScript : MonoBehaviour
{
    private SimulationScript simulation;
    public bool colorDensitys;
    public float alpha;
    public float addAlpha;
    public int particle;
    public Vector2 gridCell;
    public Vector2 particlePosition;
    public Vector2 particleVelocity;
    public int neighbor;
    public float kernel;
    public Vector2 kernelGradient;
    public float kernelSum;
    public Vector2 kernelDerivativeSum;
    public float density;
    public float pressure;
    public int numPerfectDensitys;
    public int numPerfectPressure;
    private Vector2 force;
    private int previousParticle = 0;
    // Start is called before the first frame update
    void Start()
    {
        simulation = GameObject.FindGameObjectWithTag("Simulation").GetComponent<SimulationScript>();
        // alpha = 5 / (14 * Mathf.PI * (simulation.particleSpacing * simulation.particleSpacing));
        alpha = 2.839606f;
    }

    // Update is called once per frame
    void Update()
    {
        // Update alpha value
        simulation.alpha = alpha + addAlpha;

        particle = simulation.currentParticle;
        gridCell = simulation.currentGridCell;

        if (simulation.moveParticles && simulation.positions.Count > 0)
        {
            simulation.colorNeighbors(particle, Color.blue);
            simulation.colorBoundaryNeighbors(particle, Color.black);
        }

        if (!simulation.moveParticles && simulation.positions.Count > 0)
        {

            particlePosition = simulation.positions[particle];
            particleVelocity = simulation.velocitys[particle];

            // Reset particle color
            if (previousParticle != particle)
            {
                simulation.colorNeighbors(previousParticle, Color.blue);
                simulation.colorBoundaryNeighbors(previousParticle, Color.black);
            }

            // Compute kernels
            kernel = simulation.smoothingKernel(simulation.positions[particle], simulation.positions[neighbor], simulation.particleSpacing);
            kernelGradient = simulation.smoothingKernelDerivative(simulation.positions[particle], simulation.positions[neighbor], simulation.particleSpacing);

            float kS = 0.0f;
            Vector2 gS = new Vector2(0, 0);
            foreach (int p in simulation.neighbors[particle])
            {
                kS += simulation.smoothingKernel(simulation.positions[particle], simulation.positions[p], simulation.particleSpacing);
                gS += simulation.smoothingKernelDerivative(simulation.positions[particle], simulation.positions[p], simulation.particleSpacing);
            }
            kernelSum = kS;
            kernelDerivativeSum = gS;

            // density + pressure
            density = simulation.densitys[particle];
            pressure = simulation.pressures[particle];

            // Color particles
            simulation.colorNeighbors(particle, Color.yellow);
            simulation.colorBoundaryNeighbors(particle, Color.cyan);
            simulation.colors[particle] = Color.red;

            int sumDensity = 0;
            int sumPressure = 0;
            // Check for perfect density
            for (int i = 0; i < simulation.numParticles; i++)
            {
                if (Mathf.Abs(simulation.densitys[i] - 1.5f) <= 0.00001)
                {
                    if (colorDensitys)
                    {
                        simulation.colors[i] = Color.red;
                    }
                    sumDensity++;
                }
                if (Mathf.Abs(simulation.pressures[i]) <= 0.001)
                {
                    sumPressure++;
                }
            }

            numPerfectDensitys = sumDensity;
            numPerfectPressure = sumPressure;



            previousParticle = particle;
        }

    }
}
