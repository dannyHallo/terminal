using Assets.WasapiAudio.Scripts.Unity;
using UnityEngine;
using UnityEngine.UI;

// This script is written by hallidev
// https://github.com/hallidev/UnityWasapiAudio
// With some minor modifications by dannyHallo
namespace Assets.WasapiAudio.Scripts.Unity
{
    public class DancingPlanet : MonoBehaviour
    {
        public GameObject planetGenerator;
        public BarSpectrum barSpectrum;
        public float noiseWeightMul = 1.0f;

        private TerrainMesh terrainMesh;
        private NoiseDensity noiseDensity;

        private void Start()
        {
            RectTransform rectTransform = GetComponent<RectTransform>();

            terrainMesh = planetGenerator.GetComponent<TerrainMesh>();
            noiseDensity = planetGenerator.GetComponent<NoiseDensity>();
        }

        public void Update()
        {
            noiseDensity.noiseWeight = noiseWeightMul * barSpectrum.processedAudioScales[5];
            terrainMesh.RequestMeshUpdate();
        }
    }

}
