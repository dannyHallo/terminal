using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
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


    [Header("Source Textures - READONLY")]
    public Texture2D blankTex;
    public Texture2D originalGrayscaleTex;


    [Header("Drawing")]
    public Color metalColor = new Color();
    public Color grassColor = new Color();
    public float strokeMul;


    [Header("Debug")]
    public bool updateRequest = false;
    public bool dispatch = false;
    public float f1;
    public float f2;
    public float f3;


    const int textureResolution = 50;

    private Color[] colorsInPalette;
    private GradientColorKey[] colorKey;
    private GradientAlphaKey[] alphaKey;

    [HideInInspector] public Texture2D userTex;
    [HideInInspector] public Texture2D metallicTex;
    private RenderTexture universalRenderTex;
    private Texture2D orignalPalette;

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
        if (orignalPalette == null || orignalPalette.width != textureResolution)
            orignalPalette = new Texture2D(textureResolution, 1, TextureFormat.RGBA32, false);

        if (gradient != null)
        {
            Color[] colours = new Color[orignalPalette.width];
            for (int i = 0; i < textureResolution; i++)
            {
                Color gradientCol = gradient.Evaluate(i / (textureResolution - 1f));
                colours[i] = gradientCol;
            }

            orignalPalette.SetPixels(colours);
            orignalPalette.Apply();
        }

        // Testing vals
        mat.SetFloat("f1", f1);
        mat.SetFloat("f2", f2);
        mat.SetFloat("f3", f3);

        mat.SetColor("metalColor", metalColor);
        mat.SetColor("grassColor", grassColor);
        mat.SetFloat("mapBound", 1 / (2.0f * worldPosOffset));
        mat.SetFloat("normalOffsetWeight", normalOffsetWeight);
        mat.SetFloat("minMaxBounds", minMaxBounds);
        mat.SetFloat("offsetY", offsetY);
        mat.SetFloat("worldPosOffset", worldPosOffset);
        mat.SetTexture("originalPalette", orignalPalette);
        mat.SetTexture("originalGrayscaleTex", originalGrayscaleTex);
        mat.SetTexture("userTex", userTex);
        mat.SetTexture("metallicTex", metallicTex);
    }

    private void CreateTexture2DFromBlank(Texture2D src, out Texture2D dst)
    {
        dst = new Texture2D(src.width, src.height, TextureFormat.RGBA32, false);
        dst.SetPixels(src.GetPixels());
        dst.Apply();
    }

    private void Update()
    {
        if (!universalRenderTex)
        {
            universalRenderTex = new RenderTexture(userTex.width, userTex.height, 0);
            universalRenderTex.enableRandomWrite = true;
            universalRenderTex.Create();
        }

        if (updateRequest)
        {
            if (usePalette)
                UpdatePalette();

            CreateTexture2DFromBlank(blankTex, out userTex);
            CreateTexture2DFromBlank(blankTex, out metallicTex);

            UpdateShader();
            updateRequest = false;
        }

        // Test function
        if (dispatch)
        {
            // DrawOnTexture(userTex, (int)0, (int)0, 10, true);
            // DrawOnTexture(userTex, (int)1024, (int)1024, 10, true);
            // DrawOnTexture(userTex, (int)0, (int)1024, 10, true);
            // DrawOnTexture(userTex, (int)1024, (int)0, 10, true);

            dispatch = false;
        }


    }

    public void DrawTextureOnWorldPos(Texture2D texture, Vector3 position, int radius, bool isMetal)
    {
        float ratio = 1 / (2.0f * worldPosOffset);

        float x = position.x;
        float z = position.z;

        x += worldPosOffset;
        z += worldPosOffset;
        x *= ratio * texture.width;
        z *= ratio * texture.height;

        // print("Drawing origin: " + (int)x + ", " + (int)z);

        DrawOnTexture(texture, (int)x, (int)z, Mathf.CeilToInt(radius * strokeMul), isMetal);
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

    private void DrawOnTexture(Texture2D texture, int originX, int originY, int radius, bool isMetal)
    {
        Graphics.Blit(texture, universalRenderTex);

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
        drawImageComputeShader.SetTexture(0, "image", universalRenderTex);
        drawImageComputeShader.Dispatch(0, 128, 128, 1);

        RenderTexture.active = universalRenderTex;
        texture.ReadPixels(new Rect(0, 0, universalRenderTex.width, universalRenderTex.height), 0, 0);
        texture.Apply();
        RenderTexture.active = null;
    }

    private void OnValidate()
    {
        updateRequest = true;
    }
}