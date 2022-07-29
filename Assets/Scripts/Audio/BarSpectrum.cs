using Assets.WasapiAudio.Scripts.Unity;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;

// This script is written by hallidev
// https://github.com/hallidev/UnityWasapiAudio
// With some minor modifications by dannyHallo
namespace Assets.WasapiAudio.Scripts.Unity
{
    public class BarSpectrum : AudioVisualizationEffect
    {
        private GameObject[] spectrumBars;
        public List<float> processedAudioScales;
        public Orientation orientation;

        public float singleBarVisibleWidth = 4f;
        public float singleBarHoldWidth = 8f;
        public float scale;
        public float power;

        public enum Orientation { BottomLeft, TopRight }

        private void Start()
        {
            RectTransform rectTransform = GetComponent<RectTransform>();

            spectrumBars = new GameObject[SpectrumSize];
            processedAudioScales = new List<float>(new float[SpectrumSize]);
            GenerateSpectrum();
        }

        void GenerateSpectrum()
        {
            if (orientation == Orientation.BottomLeft)
            {
                for (int i = 0; i < SpectrumSize; i++)
                {
                    spectrumBars[i] = new GameObject("Spectrum no." + i.ToString());
                    spectrumBars[i].AddComponent<Image>();
                    // spectrumBars[i].GetComponent<RectTransform>().position.

                    spectrumBars[i].transform.SetParent(transform);

                    spectrumBars[i].GetComponent<RectTransform>().pivot = new Vector2(0, 0);
                    spectrumBars[i].GetComponent<RectTransform>().sizeDelta = new Vector2(singleBarVisibleWidth, singleBarVisibleWidth);
                    spectrumBars[i].GetComponent<RectTransform>().localScale = new Vector2(1, 1);
                    spectrumBars[i].GetComponent<RectTransform>().localPosition = new Vector3(i * singleBarHoldWidth, 0, 0);
                }
            }
            else if (orientation == Orientation.TopRight)
            {
                for (int i = 0; i < SpectrumSize; i++)
                {
                    spectrumBars[i] = new GameObject("Spectrum no." + i.ToString());
                    spectrumBars[i].AddComponent<Image>();
                    // spectrumBars[i].GetComponent<RectTransform>().position.

                    spectrumBars[i].transform.SetParent(transform);

                    spectrumBars[i].GetComponent<RectTransform>().pivot = new Vector2(1, 1);
                    spectrumBars[i].GetComponent<RectTransform>().sizeDelta = new Vector2(singleBarVisibleWidth, singleBarVisibleWidth);
                    spectrumBars[i].GetComponent<RectTransform>().localScale = new Vector2(1, 1);
                    spectrumBars[i].GetComponent<RectTransform>().localPosition = new Vector3(-i * singleBarHoldWidth, 0, 0);
                }
            }
        }

        public void Update()
        {
            float[] spectrumData = GetSpectrumData();

            for (var i = 0; i < SpectrumSize; i++)
            {
                float audioScale = Mathf.Pow(spectrumData[i] * scale, power);
                processedAudioScales[i] = audioScale;
                spectrumBars[i].GetComponent<RectTransform>().sizeDelta = new Vector2(singleBarVisibleWidth, audioScale);
            }
        }
    }

}
