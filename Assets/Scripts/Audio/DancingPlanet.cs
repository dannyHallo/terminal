using Assets.WasapiAudio.Scripts.Unity;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

// This script is originally written by hallidev
// https://github.com/hallidev/UnityWasapiAudio
// Some minor modifications are made by dannyHallo

namespace Assets.WasapiAudio.Scripts.Unity
{
    public class DancingPlanet : MonoBehaviour
    {
        public GameObject planetGenerator;  // Planet to control
        public AudioProcessor audioProcessor;     // Audio date source
        public float noiseWeightMul = 1.0f;


        [Header("Scaling the globe")]
        public float minPlanetScale = 55f;
        public float maxPlanetScale = 80f;
        [Range(0, 0.02f)] public float dynamicScaleSensitivity;


        [Header("Rolling texture")]
        public float textureRollingSpeed;


        [Header("Debug")]
        public int loudestId;
        public List<float> loudestFrequencyList;
        public float loudestFrequency;
        //public float loudestFrequency1;
        //public float loudestFrequency2;
        //public float loudestFrequency3;
        //public float loudestFrequency4;
        //public float loudestFrequency5;
        //public float loudestFrequency6;
        //public float loudestFrequency7;
        //public float loudestFrequency8;
        public float currrentFrequency=500f;
        public float colorChangeSpeed=1f;
        [Space]
        private TerrainMesh terrainMesh;
        private NoiseDensity noiseDensity;
        private ColourGenerator colourGenerator;

        private void Start()
        {
            RectTransform rectTransform = GetComponent<RectTransform>();

            terrainMesh = planetGenerator.GetComponent<TerrainMesh>();
            noiseDensity = planetGenerator.GetComponent<NoiseDensity>();
            colourGenerator = planetGenerator.GetComponent<ColourGenerator>();

            if (!terrainMesh || !noiseDensity || !colourGenerator)
            {
                print("Script dependency is not fully satisfied.");
            }
            for (int a = 0; a <21; a = a + 1)
            {
                loudestFrequencyList.Add(0f);

            }
        }

        public void Update()
        {

            loudestId = audioProcessor.loudestSpectrumBarIndex;
            for (int a = 20; a >0; a = a - 1)
            {
                loudestFrequencyList[a] = loudestFrequencyList[a - 1];
            }
                loudestFrequencyList[0] = audioProcessor.loudestFrequency;
            float Total=0;
            for (int a = 20; a >= 0; a = a - 1)
            {
                Total+=loudestFrequencyList[a];
            }
            Debug.Log(loudestFrequencyList.Count);
            loudestFrequency = Total / 21;
            if (currrentFrequency > loudestFrequency)
            {
                currrentFrequency -= Mathf.Min(currrentFrequency - loudestFrequency, colorChangeSpeed * Time.deltaTime);

            }
            if (currrentFrequency <= loudestFrequency)
            {
                currrentFrequency += Mathf.Min(loudestFrequency - currrentFrequency, colorChangeSpeed * Time.deltaTime);

            }
            // currrentFrequency = loudestFrequency;
            //noiseDensity.planetRadius = Mathf.Lerp(
            //    minPlanetScale,
            //    maxPlanetScale,
            //    dynamicScaleSensitivity * audioProcessor.loudness);

            // noiseDensity.f1 += textureRollingSpeed * Time.deltaTime * 0.01f;

            // noiseDensity.noiseWeight = noiseWeightMul * audioProcessor.processedAudioScales[5];
            // terrainMesh.RequestMeshUpdate();
            //// Debug.Log("currrentFrequency "+ currrentFrequency);
            colourGenerator.allColor = ColorHandler.GetColorFromFrequency(currrentFrequency);

        }

        public void ColorTransform()
        {

        }
    }

}
