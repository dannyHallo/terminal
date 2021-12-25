using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class ColourGenerator : MonoBehaviour {
    public Material mat;
    public Gradient gradient;
    public float normalOffsetWeight;
    public AtmosphereSettings atmosphereSettings;
    Texture2D texture;
    public float boundsY;
    public float offsetY;
    const int textureResolution = 50;

    // Set up a 1d texture to store color
    void Init () {
        if (texture == null || texture.width != textureResolution) {
            texture = new Texture2D (textureResolution, 1, TextureFormat.RGBA32, false);
        }
    }

    void Update () {
        Init ();
        UpdateTexture ();

        mat.SetFloat ("planetRadius", atmosphereSettings.planetRadius);
        mat.SetFloat ("boundsY", boundsY);
        mat.SetFloat ("offsetY", offsetY);
        mat.SetFloat ("normalOffsetWeight", normalOffsetWeight);
        mat.SetTexture ("ramp", texture);
    }
    
    // Update 1d texture with color gradients
    void UpdateTexture () {
        if (gradient != null) {
            Color[] colours = new Color[texture.width];
            for (int i = 0; i < textureResolution; i++) {
                Color gradientCol = gradient.Evaluate (i / (textureResolution - 1f));
                colours[i] = gradientCol;
            }

            texture.SetPixels (colours);
            texture.Apply ();
        }
    }
}