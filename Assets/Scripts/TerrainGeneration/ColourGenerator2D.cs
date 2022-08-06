using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class ColourGenerator2D : MonoBehaviour
{
    [Header("General")]
    public Material mat;
    public float normalOffsetWeight;


    [Header("ColorPalette")]
    public Gradient gradient;
    [SerializeField, Range(2, 6)] int colorNum;
    [SerializeField] Texture2D colorPalette;
    Color[] colorsInPalette;
    GradientColorKey[] colorKey;
    GradientAlphaKey[] alphaKey;


    [Header("Test")]
    public float f1;
    public float f2;
    public float f3;

    public float minMaxBounds;
    public float offsetY;

    public Texture2D orignalPalette;
    public Texture2D userTex;
    public Texture2D originalGrayscaleTex;

    [Range(0, 0.01f)] public float mapBound;

    const int textureResolution = 50;

    public bool usePalette = false;
    public bool updateRequest = false;


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
                // colours[i] = allColor;
            }

            orignalPalette.SetPixels(colours);
            orignalPalette.Apply();
        }

        // Testing vals
        mat.SetFloat("f1", f1);
        mat.SetFloat("f2", f2);
        mat.SetFloat("f3", f3);

        mat.SetFloat("mapBound", mapBound);
        mat.SetFloat("normalOffsetWeight", normalOffsetWeight);
        mat.SetFloat("minMaxBounds", minMaxBounds);
        mat.SetFloat("offsetY", offsetY);
        mat.SetTexture("originalPalette", orignalPalette);
        mat.SetTexture("originalGrayscaleTex", originalGrayscaleTex);
        mat.SetTexture("userTex", userTex);
    }

    private void Update()
    {
        if (updateRequest)
        {
            if (usePalette)
                UpdatePalette();

            UpdateShader();
            updateRequest = false;
        }
    }

    private void OnValidate()
    {
        updateRequest = true;
    }
}