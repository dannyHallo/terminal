using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class FramerateCounter : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI display;
    public enum DisplayMode { FPS, MS };
    [SerializeField] DisplayMode displayMode = DisplayMode.FPS;

    [SerializeField, Range(0.1f, 2f)] float sampleDuration = 1f;
    
    int frames;
    float duration;
    float worstDuration = 0;
    float bestDuration = float.MaxValue;
    
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        float frameDuration = Time.unscaledDeltaTime;
        ++frames;
        duration += frameDuration;

        if (frameDuration < bestDuration)
        {
            bestDuration = frameDuration;
        }
        if (frameDuration > worstDuration)
        {
            worstDuration = frameDuration;
        }

        if (duration >= sampleDuration)
        {
            if (displayMode == DisplayMode.FPS)
            {
                display.SetText("FPS\n{0:0}\n{1:0}\n{2:0}",
                    1f / bestDuration,
                    frames / duration,
                    1f / worstDuration);
                frames = 0;
                duration = 0;
                bestDuration = float.MaxValue;
                worstDuration = 0;
            }
            else if (displayMode == DisplayMode.MS)
            {
                display.SetText("FPS\n{0:1}\n{1:1}\n{2:1}",
                    bestDuration * 1000f,
                    duration / frames * 1000f,
                    worstDuration * 1000f);
                frames = 0;
                duration = 0;
                bestDuration = float.MaxValue;
                worstDuration = 0;
            }

        }
    }
}
