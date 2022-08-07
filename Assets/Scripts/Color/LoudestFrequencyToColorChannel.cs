using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LoudestFrequencyToColorChannel : ColorChannel
{
    public AudioProcessor audioProcessor;
    // Start is called before the first frame update
    void Start()
    {
        audioProcessor = FindObjectOfType<AudioProcessor>();
    }

    // Update is called once per frame
    void Update()
    {
        outputColor =  ColorHandler.waveLengthToRGB(audioProcessor.loudestFrequency);
    }
}
