using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ColourGenerator2D : MonoBehaviour
{
    [Header("Dependencies")]

    private Material terrainColourMateral;
    private ComputeShader drawImageComputeShader;

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
    private Color clearColor = new Color(0, 0, 0, 0);
    public float strokeMul;

    public const int textureResolution = 496;
    const int paletteResolution = 50;

    private Color[] colorsInPalette;
    private GradientColorKey[] colorKey;
    private GradientAlphaKey[] alphaKey;

    private Texture2D userTex;
    private RenderTexture universalRenderTex;
    private Texture2D orignalPalette;

    public enum DrawType
    {
        Grass,
        Metal,
        Clear
    }

    // Getters
    public Material GetTerrainColourMaterial()
    {
        if (!terrainColourMateral) CreateTerrainColourMaterial();
        return terrainColourMateral;
    }

    public ComputeShader GetDrawImageComputeShader()
    {
        if (!drawImageComputeShader) CreateDrawImageComputeShader();
        return drawImageComputeShader;
    }

    public RenderTexture GetUniversalRenderTexture()
    {
        if (!universalRenderTex) CreateUniversalRenderTexture();
        return universalRenderTex;
    }
    // End: Getters

    // Initializers
    private void CreateTerrainColourMaterial()
    {
        terrainColourMateral = new Material(Shader.Find("Custom/TerrainColour"));
    }

    private void CreateDrawImageComputeShader()
    {
        drawImageComputeShader = (ComputeShader)Resources.Load("DrawOnTexture");
    }

    private void CreateUniversalRenderTexture()
    {
        universalRenderTex = new RenderTexture(textureResolution, textureResolution, 0);
        universalRenderTex.volumeDepth = textureResolution;
        universalRenderTex.dimension = UnityEngine.Rendering.TextureDimension.Tex3D;
        universalRenderTex.filterMode = FilterMode.Point;
        universalRenderTex.enableRandomWrite = true;
        universalRenderTex.useMipMap = false;
        universalRenderTex.Create();
    }
    // End: Initializers


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

        terrainColourMateral.SetColor("metalColor", metalColor);
        terrainColourMateral.SetColor("grassColor", grassColor);
        terrainColourMateral.SetFloat("mapBound", 1 / (2.0f * worldPosOffset));
        terrainColourMateral.SetFloat("normalOffsetWeight", normalOffsetWeight);
        terrainColourMateral.SetFloat("minMaxBounds", minMaxBounds);
        terrainColourMateral.SetFloat("offsetY", offsetY);
        terrainColourMateral.SetFloat("worldPosOffset", worldPosOffset);
        terrainColourMateral.SetTexture("originalPalette", orignalPalette);
        terrainColourMateral.SetTexture("universalRenderTex", GetUniversalRenderTexture());
    }

    private void CreateTexture2DFromBlank(Texture2D src, out Texture2D dst)
    {
        dst = new Texture2D(src.width, src.height, TextureFormat.RGBA32, false);
        dst.SetPixels(src.GetPixels());
        dst.filterMode = FilterMode.Point;
        dst.Apply();
    }

    public void DrawTextureOnWorldPos(Vector3 position, float radius, DrawType drawType)
    {
        float ratio = 1 / (2.0f * worldPosOffset);
        float textureScaleMul = textureResolution / 1024.0f;

        float x = position.x;
        float y = position.y;
        float z = position.z;

        x += worldPosOffset;
        y += worldPosOffset;
        z += worldPosOffset;

        x *= ratio * textureResolution;
        y *= ratio * textureResolution;
        z *= ratio * textureResolution;

        DrawOnTexture((int)x, (int)y, (int)z, Mathf.CeilToInt(textureScaleMul * radius * strokeMul), drawType);
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

    private void DrawOnTexture(int originX, int originY, int originZ, int radius, DrawType drawType)
    {
        int[] origin = new int[3];
        origin[0] = originX;
        origin[1] = originY;
        origin[2] = originZ;

        float[] drawColor = new float[4];

        switch (drawType)
        {
            case DrawType.Grass:
                drawColor = fillColor(grassColor);
                break;
            case DrawType.Metal:
                drawColor = fillColor(metalColor);
                break;
            case DrawType.Clear:
                drawColor = fillColor(clearColor);
                break;
        }

        GetDrawImageComputeShader().SetFloats("drawColor", drawColor);
        GetDrawImageComputeShader().SetInt("radius", radius);
        GetDrawImageComputeShader().SetInts("origin", origin);
        GetDrawImageComputeShader().SetTexture(0, "universalRenderTex", GetUniversalRenderTexture());
        GetDrawImageComputeShader().Dispatch(
            0,
            Mathf.CeilToInt(textureResolution / 8.0f),
            Mathf.CeilToInt(textureResolution / 8.0f),
            Mathf.CeilToInt(textureResolution / 8.0f));
    }
}