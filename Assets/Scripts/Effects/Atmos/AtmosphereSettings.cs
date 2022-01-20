using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.Mathf;

// We can create setting assest in project folder
[CreateAssetMenu(menuName = "Celestial Body/Atmosphere")]
public class AtmosphereSettings : ScriptableObject
{
    // public ComputeShader opticalDepthCompute;
    // public int textureSize = 256;
    [Header("Performance")]
    public int inScatteringPoints = 10;
    public int opticalDepthPoints = 10;


    [Header("Dither Settings")]
    public float ditherStrength = 0.8f;
    public float ditherScale = 4;
    public Texture2D blueNoise;


    [Header("Color")]
    public Vector3 wavelengths = new Vector3(700, 530, 440);
    public float densityFalloff = 4f;
    public float scatteringStrength = 20;


    [Header("Intensity")]
    [Range(0, 5)] public float scatteringIntensity = 1;
    [Range(0, 5)] public float totalIntensity = 0.66f;


    [Header("Planet Settings")]
    [Range(0, 300000)] public float planetRadius = 100000f;
    [Range(-10000, 10000)] public float bottomOffset = 0f;
    [Range(0, 20000)] public float atmosHeight = 1000f;


    [Header("Sun Settings")]
    [Range(0, 1)] public float startTimeOfDay;
    [Range(1, 10)] public float sunSpeed;
    public float sunDistance = 100000;
    public bool allowTimeFlow = true;

    float atmosphereRadius = 0;
    // RenderTexture opticalDepthTexture;
    
    // [Header("Test Paras")]
    // public Vector4 testParams = new Vector4(7, 1.26f, 0.1f, 3);

    [Header("Update")]
    public bool settingsUpToDate;

    ObjectControl objectControl;
    
    public void SetProperties(Material material)
    {
        if(!objectControl){
            objectControl = GameObject.Find("Bot1").GetComponent<ObjectControl>();
        }

        if (!settingsUpToDate || Application.isPlaying)
        {
            atmosphereRadius = atmosHeight + planetRadius + bottomOffset;
            material.SetFloat("atmosphereRadius", atmosphereRadius);
            material.SetFloat("planetRadius", planetRadius + bottomOffset);
            material.SetVector("planetCentre", objectControl.planetCentre);

            // material.SetVector("params", testParams);
            material.SetInt("numInScatteringPoints", inScatteringPoints);
            material.SetInt("numOpticalDepthPoints", opticalDepthPoints);
            material.SetFloat("atmosHeight", atmosHeight);
            material.SetFloat("scatteringIntensity", scatteringIntensity);
            material.SetFloat("totalIntensity", totalIntensity);
            material.SetFloat("densityFalloff", densityFalloff);

            // Strength of (rayleigh) scattering is inversely proportional to wavelength^4
            float scatterX = Pow(20 / wavelengths.x, 4);
            float scatterY = Pow(20 / wavelengths.y, 4);
            float scatterZ = Pow(20 / wavelengths.z, 4);
            material.SetVector("scatteringCoefficients", new Vector3(scatterX, scatterY, scatterZ) * scatteringStrength);

            // Dithering            
            material.SetFloat("ditherScale", ditherScale);
            material.SetFloat("ditherStrength", ditherStrength);
            material.SetTexture("_BlueNoise", blueNoise);

            // PrecomputeOutScattering ();
            // material.SetTexture ("_BakedOpticalDepth", opticalDepthTexture);

            settingsUpToDate = true;
        }
    }

    // void PrecomputeOutScattering () {
    // 	if (!settingsUpToDate || opticalDepthTexture == null || !opticalDepthTexture.IsCreated ()) {
    // 		ComputeHelper.CreateRenderTexture (ref opticalDepthTexture, textureSize, FilterMode.Bilinear);
    // 		opticalDepthCompute.SetTexture (0, "Result", opticalDepthTexture);
    // 		opticalDepthCompute.SetInt ("textureSize", textureSize);
    // 		opticalDepthCompute.SetInt ("numOutScatteringSteps", opticalDepthPoints);
    // 		// opticalDepthCompute.SetFloat ("atmosphereRadius", (1 + atmosphereScale));
    // 		opticalDepthCompute.SetFloat ("densityFalloff", densityFalloff);
    // 		opticalDepthCompute.SetVector ("params", testParams);
    // 		ComputeHelper.Run (opticalDepthCompute, textureSize, textureSize);
    // 	}

    // }

    void OnValidate()
    {
        settingsUpToDate = false;
    }
}