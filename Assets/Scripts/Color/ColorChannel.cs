using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ColorChannel : MonoBehaviour
{
    /// <summary>
    /// input both float and Color
    /// </summary>
     public ColorChannel mainInputChannel;




    [HideInInspector]
    public float inputfloat
    {
        get { return mainInputChannel.outputfloat; }
    }
    [HideInInspector] public float outputfloat;
    [HideInInspector]
    public Color mainInputColor
    {
        get { return mainInputChannel.outputColor; }
    }


  [HideInInspector]  public Color outputColor;





    public Color ChangeColorToward(Color colorNow, Color targetColor, float maxSpeed)
    {
        Color _color;
        float largestDifference = Mathf.Max(Mathf.Abs(colorNow.r - targetColor.r), Mathf.Abs(colorNow.b - targetColor.b), Mathf.Abs(colorNow.g - targetColor.g));
        _color.r = RGBFloatTargetClampChange(colorNow.r, targetColor.r, Mathf.Abs(colorNow.r - targetColor.r) / largestDifference * maxSpeed * Time.deltaTime);
        _color.b = RGBFloatTargetClampChange(colorNow.b, targetColor.b, Mathf.Abs(colorNow.b - targetColor.b) / largestDifference * maxSpeed * Time.deltaTime);
        _color.g = RGBFloatTargetClampChange(colorNow.g, targetColor.g, Mathf.Abs(colorNow.g - targetColor.g) / largestDifference * maxSpeed * Time.deltaTime);
        _color.a = RGBFloatTargetClampChange(colorNow.a, targetColor.a, Mathf.Abs(colorNow.a - targetColor.a) / largestDifference * maxSpeed * Time.deltaTime);
        return _color;
    }

    float RGBFloatTargetClampChange(float valueNow, float targetvalue, float maxSpeed)
    {
        float _value;
        _value = valueNow + Mathf.Clamp(targetvalue - valueNow, -maxSpeed, maxSpeed);
        return _value;
    }





    public Color BaseColorMix(Color baseColor, Color mixColor, float mixratio)
    {
        Color _color;
        _color.r = baseColor.r * (1 - mixratio) + mixColor.r * mixratio;
        _color.b = baseColor.b * (1 - mixratio) + mixColor.b * mixratio;
        _color.g = baseColor.g * (1 - mixratio) + mixColor.g * mixratio;
        _color.a = baseColor.a * (1 - mixratio) + mixColor.a * mixratio;
        return _color;
    }
}
