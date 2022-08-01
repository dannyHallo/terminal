using Assets.WasapiAudio.Scripts.Unity;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using System.Linq;

// This script is written by hallidev
// https://github.com/hallidev/UnityWasapiAudio
// With some minor modifications by dannyHallo
public class BarSpectrum : MonoBehaviour
{
    private GameObject[] spectrumBars;
    public AudioProcessor audioProcessor;

    public Orientation orientation;

    public float singleBarVisibleWidth = 4f;
    public float singleBarHoldWidth = 8f;

    public enum Orientation { BottomLeft, TopRight }

    private void Start()
    {
        spectrumBars = new GameObject[audioProcessor.spectrumChannelNum];
        RectTransform rectTransform = GetComponent<RectTransform>();
        GenerateSpectrum();
    }

    void GenerateSpectrum()
    {
        if (orientation == Orientation.BottomLeft)
        {
            for (int i = 0; i < audioProcessor.spectrumChannelNum; i++)
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
            for (int i = 0; i < audioProcessor.spectrumChannelNum; i++)
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

    void UpdateSpectrum()
    {
        for (var i = 0; i < audioProcessor.spectrumChannelNum; i++)
            spectrumBars[i].GetComponent<RectTransform>().sizeDelta =
                new Vector2(singleBarVisibleWidth, audioProcessor.GetStrengthFromSpectrumIndex(i));
    }

    public void Update()
    {
        UpdateSpectrum();
    }
}
