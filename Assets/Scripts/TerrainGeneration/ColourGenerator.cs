using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class ColourGenerator : MonoBehaviour
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


    [Header("Offset")]
    public float boundsY;
    public float offsetY;

    Texture2D texture;
    const int textureResolution = 50;

    // Set up a 1d texture to store color
    void Init()
    {
        if (texture == null || texture.width != textureResolution)
        {
            texture = new Texture2D(textureResolution, 1, TextureFormat.RGBA32, false);
        }
    }

    void Update()
    {
        Init();
        UpdateTexture();

        mat.SetFloat("boundsY", boundsY);
        mat.SetFloat("offsetY", offsetY);
        mat.SetFloat("normalOffsetWeight", normalOffsetWeight);
        mat.SetTexture("ramp", texture);
    }

    // Update 1d texture with color gradients
    void UpdateTexture()
    {
        if (gradient != null)
        {
            Color[] colours = new Color[texture.width];
            for (int i = 0; i < textureResolution; i++)
            {
                Color gradientCol = gradient.Evaluate(i / (textureResolution - 1f));
                colours[i] = gradientCol;
            }

            texture.SetPixels(colours);
            texture.Apply();
        }
    }

    private void OnValidate()
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
}