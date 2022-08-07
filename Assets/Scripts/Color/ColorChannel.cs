using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ColorChannel : MonoBehaviour
{
    /// <summary>
    /// input both float and Color
    /// </summary>
    public ColorChannel mainInputChannel;



    public Color BaseColorMix(Color baseColor, Color mixColor, float mixratio)
    {
        Color _color;
        _color.r = baseColor.r * (1 - mixratio) + mixColor.r * mixratio;
        _color.b = baseColor.b * (1 - mixratio) + mixColor.b * mixratio;
        _color.g = baseColor.g * (1 - mixratio) + mixColor.g * mixratio;
        _color.a = baseColor.a * (1 - mixratio) + mixColor.a * mixratio;
        return _color;
    }

    public float inputfloat
    {
        get { return mainInputChannel.outputfloat; }
    }
    public float outputfloat;
    public Color mainInputColor
    {
        get { return mainInputChannel.outputColor; }
    }


    public Color outputColor;

}
