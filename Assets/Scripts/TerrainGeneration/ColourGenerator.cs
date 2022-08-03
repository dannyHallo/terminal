using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class ColourGenerator : MonoBehaviour
{
    [Header("General")]
    public GameObject target;
    public Material mat;
    public float normalOffsetWeight;
    public float musicNoise;
    public float musicNoiseWeight;
    public Color allColor;


    [Header("ColorPalette")]
    public Gradient gradient;
    [SerializeField, Range(2, 6)] int colorNum;
    [SerializeField] Texture2D colorPalette;
    Color[] colorsInPalette;
    GradientColorKey[] colorKey;
    GradientAlphaKey[] alphaKey;


    [Header("Offset")]
    public float minMaxBounds;
    public float offsetY;

    Texture2D texture;
    const int textureResolution = 50;

    public bool usePalette = false;
    public bool updateRequest = false;

    private void Awake()
    {
        allColor = Color.black;
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

    void UpdateTexture()
    {
        if (texture == null || texture.width != textureResolution)
            texture = new Texture2D(textureResolution, 1, TextureFormat.RGBA32, false);

        if (gradient != null)
        {
            Color[] colours = new Color[texture.width];
            for (int i = 0; i < textureResolution; i++)
            {
                Color gradientCol = gradient.Evaluate(i / (textureResolution - 1f));
                //colours[i] = gradientCol;
                 colours[i] = allColor;
            }
            //Debug.Log(allColor+" "+1/Time.deltaTime);
            mat.SetColor("_Color", allColor);
            var cubeRenderer = target.GetComponent<Renderer>();
            cubeRenderer.material.SetColor("_Color", allColor);
            //texture.SetPixels(colours);
            texture.Apply();
        }

        mat.SetFloat("minMaxBounds", minMaxBounds);
        mat.SetFloat("offsetY", offsetY);
        mat.SetFloat("planetRadius", gameObject.GetComponent<NoiseDensity>().planetRadius);
        mat.SetFloat("musicNoise", musicNoise);
        mat.SetFloat("musicNoiseWeight", musicNoiseWeight);
        mat.SetFloat("normalOffsetWeight", normalOffsetWeight);
        mat.SetTexture("ramp", texture);
    }

    private void Update()
    {
        // if (updateRequest)
        {
            if (usePalette)
                UpdatePalette();

            UpdateTexture();
            updateRequest = false;
        }
    }

    private void OnValidate()
    {
        updateRequest = true;
    }
}