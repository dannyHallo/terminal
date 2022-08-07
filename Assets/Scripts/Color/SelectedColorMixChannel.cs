using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SelectedColorMixChannel : ColorChannel
{
    public Color SelectColor;
    [Range(0,1)]public float selectMixRatio;
    // Start is called before the first frame update



    // Update is called once per frame
    void Update()
    {
        if (mainInputChannel)
            outputColor = BaseColorMix(mainInputColor,SelectColor,selectMixRatio);


    }
}
