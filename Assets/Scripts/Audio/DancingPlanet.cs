using Assets.WasapiAudio.Scripts.Unity;
using UnityEngine;
using UnityEngine.UI;

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
        public float loudestFrequency;


        [Space]
        private TerrainMesh terrainMesh;
        private NoiseDensity noiseDensity;
        private ColourGenerator2D colourGenerator;

        private void Start()
        {
            RectTransform rectTransform = GetComponent<RectTransform>();

            terrainMesh = planetGenerator.GetComponent<TerrainMesh>();
            noiseDensity = planetGenerator.GetComponent<NoiseDensity>();
            colourGenerator = planetGenerator.GetComponent<ColourGenerator2D>();

            if (!terrainMesh || !noiseDensity || !colourGenerator)
            {
                print("Script dependency is not fully satisfied.");
            }
        }

        public void Update()
        {
            loudestId = audioProcessor.loudestSpectrumBarIndex;
            loudestFrequency = audioProcessor.loudestFrequency;

            // noiseDensity.planetRadius = Mathf.Lerp(
            //     minPlanetScale,
            //     maxPlanetScale,
            //     dynamicScaleSensitivity * audioProcessor.loudness);

            // noiseDensity.f1 += textureRollingSpeed * Time.deltaTime * 0.01f;

            // // noiseDensity.noiseWeight = noiseWeightMul * audioProcessor.processedAudioScales[5];
            // terrainMesh.RequestMeshUpdate();

            // colourGenerator.allColor = ColorHandler.GetColorFromFrequency(loudestFrequency);

        }
    }

}
