using Assets.WasapiAudio.Scripts.Unity;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using System.Linq;
using System;

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

        spectrumFrequencies = new float[spectrumChannelNum];
        processedAudioScales = new List<float>(new float[spectrumChannelNum]);

        // Run this for once, aquire the according freq list
        for (int spectrumIndex = 0; spectrumIndex < spectrumChannelNum; spectrumIndex++)
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

    private float CALC_GetFrequencyFromSpectrumIndex(int spectrumIndex)
    {
        const int minFrequency = 20;
        const int maxFrequency = 20000;
        const int _minimumFrequencyIndex = 1;
        const int _maximumFrequencyIndex = 1707;

        int logIndex;
        float freq;

        int indexCount = _maximumFrequencyIndex - _minimumFrequencyIndex;

        var maxLog = Math.Log(spectrumChannelNum, spectrumChannelNum);

        if (spectrumIndex == spectrumChannelNum - 1)
            logIndex = _maximumFrequencyIndex;
        else
            logIndex = (int)((maxLog - Math.Log((spectrumChannelNum + 1) - spectrumIndex,
                                (spectrumChannelNum + 1))) * indexCount) + _minimumFrequencyIndex;

        freq = (float)minFrequency +
                (float)(maxFrequency - minFrequency) *
                (float)((float)(logIndex - _minimumFrequencyIndex) / (float)(_maximumFrequencyIndex - _minimumFrequencyIndex));

        // Debug.Log("LogId = " + logIndex + ", Freq = " + freq);
        return freq;
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