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
        public BarSpectrum barSpectrum;     // Audio date source
        public float noiseWeightMul = 1.0f;


        [Header("Scaling the globe")]
        public float minPlanetScale = 55f;
        public float maxPlanetScale = 80f;
        [Range(0, 0.02f)] public float dynamicScaleSensitivity;

        [Header("Rolling texture")]
        public float textureRollingSpeed;

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
            float lownoteStrength = (
                barSpectrum.processedAudioScales[0] +
                barSpectrum.processedAudioScales[1] +
                barSpectrum.processedAudioScales[2]) / 3;

            float avgStrength = 0;
            foreach (float audioScale in barSpectrum.processedAudioScales)
            {
                avgStrength += audioScale;
            }
            avgStrength /= barSpectrum.processedAudioScales.Count;

            noiseDensity.planetRadius = Mathf.Lerp(minPlanetScale, maxPlanetScale, dynamicScaleSensitivity * avgStrength);
            noiseDensity.f1 += textureRollingSpeed * Time.deltaTime * 0.01f;

            // noiseDensity.noiseWeight = noiseWeightMul * barSpectrum.processedAudioScales[5];
            terrainMesh.RequestMeshUpdate();
        }
    }

}
