using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class DrawCirclesScript : MonoBehaviour
{

    public ComputeShader shader;
    public int texResolution;

    Renderer rend;
    RenderTexture outputTexture;

    int circlesHandle;
    int clearHandle;

    public Color clearColor = new Color();

    struct Circle
    {
        public Vector2 origin;
        public Color color;
        public float radius;
    }

    public int total;

    Circle[] circleData;
    ComputeBuffer buffer;


    // Use this for initialization
    public void Start()
    {
        outputTexture = new RenderTexture(texResolution, texResolution, 0);
        outputTexture.enableRandomWrite = true;
        outputTexture.Create();

        rend = GetComponent<Renderer>();
        rend.enabled = true;

        InitData();

        InitShader();
    }

    private void InitData()
    {
        circlesHandle = shader.FindKernel("Circles");

        uint threadGroupSizeX;

        shader.GetKernelThreadGroupSizes(circlesHandle, out threadGroupSizeX, out _, out _);

        circleData = new Circle[total];
    }

    private void InitShader()
    {
        clearHandle = shader.FindKernel("Clear");

        shader.SetVector("clearColor", clearColor);
        shader.SetInt("texResolution", texResolution);

        int stride = (2 + 4 + 1) * 4; //2 floats origin, 2 floats velocity, 1 float radius - 4 bytes per float
        buffer = new ComputeBuffer(circleData.Length, stride);
        buffer.SetData(circleData);
        shader.SetBuffer(circlesHandle, "circlesBuffer", buffer);

        shader.SetTexture(circlesHandle, "Result", outputTexture);
        shader.SetTexture(clearHandle, "Result", outputTexture);

        rend.material.SetTexture("_MainTex", outputTexture);
    }

    public void DispatchKernel(int count)
    {
        shader.Dispatch(clearHandle, texResolution / 8, texResolution / 8, 1);
        shader.Dispatch(circlesHandle, count, 1, 1);
    }

    void Update()
    {
    }

    public void OnDestroy()
    {
        buffer.Dispose();
    }

    public void DrawCirclesAtPositions(Vector2[] positions, Color[] colors, float radius)
    {
        InitData();
        InitShader();
        for (int i = 0; i < positions.Length; i++)
        {
            Circle circle = circleData[i];
            circle.origin = positions[i];
            circle.color = colors[i];
            circle.radius = radius;
            circleData[i] = circle;
        }

        buffer.SetData(circleData);
        shader.SetBuffer(circlesHandle, "circlesBuffer", buffer);
    }
}

