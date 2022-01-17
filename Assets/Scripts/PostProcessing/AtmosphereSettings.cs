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
    public int inScatteringPoints = 10;
    public int opticalDepthPoints = 10;
    public float densityFalloff = 4f;

    public Vector3 wavelengths = new Vector3(700, 530, 440);

    // public Vector4 testParams = new Vector4(7, 1.26f, 0.1f, 3);
    public float scatteringStrength = 20;

    // public float ditherStrength = 0.8f;
    // public float ditherScale = 4;
    // public Texture2D blueNoise;

    // [Range(0, 0.2f)] public float atmosphereScale = 0.01f;
    [Range(0, 300000)] public float planetRadius = 100000f;
    [Range(-10000, 10000)] public float bottomOffset = 0f;
    [Range(0, 20000)] public float atmosHeight = 1000f;


    [Range(0, 5)] public float scatteringIntensity = 1;
    [Range(0, 5)] public float totalIntensity = 1;
    public float intensity;

    [Header("Sun Settings")]
    [Range(0, 1)] public float timeOfDay;
    public float sunDst = 100000;
    float atmosphereRadius = 0;
    // RenderTexture opticalDepthTexture;
    public bool settingsUpToDate;
    // public bool timeFlow = false;

    public void SetProperties(Material material)
    {
        if (!settingsUpToDate || Application.isPlaying)
        {
            var sun = GameObject.Find("Test Sun");
            if (sun)
            {
                sun.transform.position = new Vector3(Mathf.Cos(timeOfDay * 2 * Mathf.PI), Mathf.Sin(timeOfDay * 2 * Mathf.PI), 0) * sunDst;
                sun.transform.LookAt(Vector3.zero);
                // if(Application.isPlaying && timeFlow)
                // 	timeOfDay += 0.0001f;
            }

            // MonoBehaviour.print("setting");
            atmosphereRadius = atmosHeight + planetRadius + bottomOffset;
            material.SetFloat("atmosphereRadius", atmosphereRadius);
            material.SetFloat("planetRadius", planetRadius + bottomOffset);
            material.SetVector("planetCentre", new Vector3(0, -planetRadius, 0));

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
            material.SetFloat("intensity", intensity);
            // material.SetFloat ("ditherStrength", ditherStrength);
            // material.SetFloat ("ditherScale", ditherScale);
            // material.SetTexture ("_BlueNoise", blueNoise);

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