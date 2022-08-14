using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ColourGenerator2D : MonoBehaviour
{
    [Header("Dependencies")]
    public Material mat;
    public ComputeShader drawImageComputeShader;


    [Header("Pallete Rules")]
    public Gradient gradient;
    [SerializeField, Range(2, 6)] private int colorNum;
    [SerializeField] private Texture2D colorPalette;
    public float minMaxBounds;
    public float offsetY;
    public float worldPosOffset;
    public float normalOffsetWeight;
    public bool usePalette = false;

    [Header("Drawing")]
    public Color metalColor = new Color();
    public Color grassColor = new Color();
    public float strokeMul;


    [Header("Debug")]
    public bool updateRequest = false;

    public const int textureResolution = 496;
    const int paletteResolution = 50;

    private Color[] colorsInPalette;
    private GradientColorKey[] colorKey;
    private GradientAlphaKey[] alphaKey;

    private Texture2D userTex;
    private RenderTexture universalRenderTex;
    private Texture2D orignalPalette;

    private void Awake()
    {
        UpdateShader();
    }

    void UpdatePalette()
    {
        if (colorPalette)
        {
            int w = colorPalette.width;
            int n = colorNum;
            int i = 0;

            colorsInPalette = new Color[n];
            colorKey = new GradientColorKey[n];
            alphaKey = new GradientAlphaKey[n];

            float timeStepSize = 1f / n;

            for (int x = (int)w / (n * 2); x < w; x += (int)w / n)
            {
                Color currentCol = colorPalette.GetPixel(x, 0);
                colorsInPalette[i] = currentCol;
                colorKey[i] = new GradientColorKey(currentCol, Mathf.Clamp((i + 1) * timeStepSize, 0, 1));
                alphaKey[i] = new GradientAlphaKey(1f, Mathf.Clamp((i + 1) * timeStepSize, 0, 1));
                i++;
            }
            gradient.SetKeys(colorKey, alphaKey);
        }
    }

    void UpdateShader()
    {
        if (orignalPalette == null || orignalPalette.width != paletteResolution)
            orignalPalette = new Texture2D(paletteResolution, 1, TextureFormat.RGBA32, false);

        if (gradient != null)
        {
            Color[] colours = new Color[orignalPalette.width];
            for (int i = 0; i < paletteResolution; i++)
            {
                Color gradientCol = gradient.Evaluate(i / (paletteResolution - 1f));
                colours[i] = gradientCol;
            }

            orignalPalette.SetPixels(colours);
            orignalPalette.Apply();
        }

        mat.SetColor("metalColor", metalColor);
        mat.SetColor("grassColor", grassColor);
        mat.SetFloat("mapBound", 1 / (2.0f * worldPosOffset));
        mat.SetFloat("normalOffsetWeight", normalOffsetWeight);
        mat.SetFloat("minMaxBounds", minMaxBounds);
        mat.SetFloat("offsetY", offsetY);
        mat.SetFloat("worldPosOffset", worldPosOffset);
        mat.SetTexture("originalPalette", orignalPalette);
        mat.SetTexture("userTex", universalRenderTex);
    }

    private void CreateTexture2DFromBlank(Texture2D src, out Texture2D dst)
    {
        dst = new Texture2D(src.width, src.height, TextureFormat.RGBA32, false);
        dst.SetPixels(src.GetPixels());
        dst.filterMode = FilterMode.Point;
        dst.Apply();
    }

    public void CreateUniversalRenderTexture()
    {
        universalRenderTex = new RenderTexture(textureResolution, textureResolution, 0);
        universalRenderTex.enableRandomWrite = true;
        universalRenderTex.filterMode = FilterMode.Point;
        universalRenderTex.Create();
    }

    public RenderTexture GetUniversalRenderTexture()
    {
        if (!universalRenderTex) CreateUniversalRenderTexture();
        return universalRenderTex;
    }

    public void DrawTextureOnWorldPos(Vector3 position, float radius, bool isMetal)
    {
        float ratio = 1 / (2.0f * worldPosOffset);
        float textureScaleMul = textureResolution / 1024.0f;

        float x = position.x;
        float z = position.z;

        x += worldPosOffset;
        z += worldPosOffset;
        x *= ratio * textureResolution;
        z *= ratio * textureResolution;

        DrawOnTexture((int)x, (int)z, Mathf.CeilToInt(textureScaleMul * radius * strokeMul), isMetal);
    }

    public float[] fillColor(Color color)
    {
        float[] colorArray = new float[4];
        colorArray[0] = color.r;
        colorArray[1] = color.g;
        colorArray[2] = color.b;
        colorArray[3] = 1.0f;
        return colorArray;
    }

    private void DrawOnTexture(int originX, int originY, int radius, bool isMetal)
    {
        int[] origin = new int[2];
        origin[0] = originX;
        origin[1] = originY;

        float[] drawColor = new float[4];

        if (isMetal)
            drawColor = fillColor(metalColor);
        else
            drawColor = fillColor(grassColor);

        drawImageComputeShader.SetFloats("drawColor", drawColor);
        drawImageComputeShader.SetInt("radius", radius);
        drawImageComputeShader.SetInts("origin", origin);
        drawImageComputeShader.SetTexture(0, "universalRenderTex", universalRenderTex);
        drawImageComputeShader.Dispatch(0, Mathf.CeilToInt(textureResolution / 8.0f), Mathf.CeilToInt(textureResolution / 8.0f), 1);
    }
}