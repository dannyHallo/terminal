using Assets.WasapiAudio.Scripts.Unity;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using System.Linq;

// This script is written by hallidev
// https://github.com/hallidev/UnityWasapiAudio
// With some minor modifications by dannyHallo
public class AudioProcessor : AudioVisualizationEffect
{
    private GameObject[] spectrumBars;
    public List<float> processedAudioScales;

    // FIXME: this is inaccurate
    public float beatsStrength;
    public float loudness;
    public int loudestSpectrumBarIndex;
    public float loudestFrequency;

    public Orientation orientation;

    public float singleBarVisibleWidth = 4f;
    public float singleBarHoldWidth = 8f;
    public float scale;
    public float power;

    public enum Orientation { BottomLeft, TopRight }
    public static float[] spectrumFrequencies;
    public int spectrumChannelNum;

    private void Start()
    {
        spectrumChannelNum = SpectrumSize;

        spectrumFrequencies = new float[64];
        processedAudioScales = new List<float>(new float[spectrumChannelNum]);

        // Run this for once, aquire the according freq list
        for (int spectrumIndex = 0; spectrumIndex < 64; spectrumIndex++)
        {
            float thisSpecFreq = CALC_GetFrequencyFromSpectrumIndex(spectrumIndex);
            spectrumFrequencies[spectrumIndex] = thisSpecFreq;
            // print(spectrumIndex + " -> " + thisSpecFreq + "Hz");
        }
    }

    public float GetStrengthFromSpectrumIndex(int spectrumIndex)
    {
        if (!SpectrumIdIsValid(spectrumIndex))
            return 0;

        return processedAudioScales[spectrumIndex];
    }

    // 20 - 20000Hz, 64 spectrums, only for init (dont't call this one, it is expensive)
    // private static float CALC_GetFrequencyFromSpectrumIndex(int spectrumIndex)
    // {
    //     float lFreq = 2.0f * Mathf.Pow(10.0f, (spectrumIndex / 64.0f) * 3.0f + 1.0f);
    //     float rFreq = 2.0f * Mathf.Pow(10.0f, ((spectrumIndex + 1) / 64.0f) * 3.0f + 1.0f);
    //     return (lFreq + rFreq) / 2.0f;
    // }

    private static float CALC_GetFrequencyFromSpectrumIndex(int spectrumIndex)
    {
        float a = 31.53408f;
        float b = 67.35244f;
        float c = 0.7440902f;
        float d = 0.02971706f;
        float e = -0.00119847f;
        float f = 0.00001785697f;

        return
        a +
        b * Mathf.Pow(spectrumIndex, 1) +
        c * Mathf.Pow(spectrumIndex, 2) +
        d * Mathf.Pow(spectrumIndex, 3) +
        e * Mathf.Pow(spectrumIndex, 4) +
        f * Mathf.Pow(spectrumIndex, 5);
    }

    // Lookup method (use this one)
    public float GetFrequencyFromSpectrumIndex(int spectrumIndex)
    {
        if (!SpectrumIdIsValid(spectrumIndex))
            return 0;

        return spectrumFrequencies[spectrumIndex];
    }

    public bool SpectrumIdIsValid(int spectrumIndex)
    {
        if (spectrumIndex < 0 || spectrumIndex >= spectrumChannelNum)
        {
            print("Spectrum index out of bound!");
            return false;
        }
        return true;
    }

    private void UpdateRuntimeAudioData(float[] spectrumData)
    {
        // Spectrum update
        for (var i = 0; i < spectrumChannelNum; i++)
        {
            float audioScale = Mathf.Pow(spectrumData[i] * scale, power);
            processedAudioScales[i] = audioScale;
        }

        // Lownote strength
        beatsStrength = (
            processedAudioScales[0] +
            processedAudioScales[1] +
            processedAudioScales[2]) / 3;

        // Average strength = loudness
        loudness = 0;
        foreach (float audioScale in processedAudioScales)
            loudness += audioScale;
        loudness /= processedAudioScales.Count;

        // Find max
        int maxId = 0;
        float maxVal = 0;
        for (int it = 0; it < processedAudioScales.Count; it++)
        {
            if (processedAudioScales[it] > maxVal)
            {
                maxVal = processedAudioScales[it];
                maxId = it;
            }
        }
        loudestSpectrumBarIndex = maxId;
        loudestFrequency = GetFrequencyFromSpectrumIndex(maxId);
    }

    public void Update()
    {
        float[] spectrumData = GetSpectrumData();

        // Update public data
        UpdateRuntimeAudioData(spectrumData);
    }
}