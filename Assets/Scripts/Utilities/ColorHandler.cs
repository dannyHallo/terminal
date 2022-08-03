using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ColorHandler
{
    struct HerzWavelen
    {
        float hertz;
        float wavelength;
    }

    public static readonly float[] herzs = {
        440.0f, 457.75f, 472.27f, 491.32f, 506.91f, 511.13f,
        527.35f, 548.62f, 566.03f, 588.86f, 612.61f, 632.05f,
        657.54f, 678.41f, 684.06f, 705.77f, 734.23f, 757.53f,
        788.08f, 819.87f, 880.0f };

    public static readonly float[] wavelens = {
        619.69f, 595.66f, 577.34f, 554.95f, 537.89f, 533.44f,
        517.03f, 496.99f, 481.70f, 463.03f, 445.08f, 431.39f,
        414.67f, 401.91f, 398.59f, 772.66f, 742.71f, 719.86f,
        691.96f, 665.13f, 644.67f };

    const float Gamma = 0.80f;
    const float IntensityMax = 255;




    public static Color GetColorFromFrequency(float frequency)
    {
        if (frequency == 0)
        {
            return Color.black;
        }

        Color color;

        while (frequency < herzs[0])
        {
            frequency *= 2;
        }
        while (frequency > herzs[herzs.Length - 1])
        {
            frequency /= 2;
        }

        color = waveLengthToRGB(frequency);

        return color;
    }

    // Taken from Earl F. Glynn's web page:
    // http://www.efg2.com/Lab/ScienceAndEngineering/Spectra.htm
    // Original java code from squ1dd13 and Tarc:
    // https://stackoverflow.com/questions/1472514/convert-light-frequency-to-rgb
    public static Color waveLengthToRGB(float Wavelength)
    {
        float factor;
        float Red, Green, Blue;

        if ((Wavelength >= 380) && (Wavelength < 1100))
        {
            Red = 1f - .8f * (1100 - Wavelength) / (1100 - 380);
            Green = 0.6f;
            Blue = .9f;
            //Debug.Log("Good");
        }
        //.6f - .2f*(1100-Wavelength) / (1100 - 380)
        //else if ((Wavelength >= 440) && (Wavelength < 490))
        //{
        //    Red = 0.0f;
        //    Green = (Wavelength - 440) / (490 - 440);
        //    Blue = 1.0f;
        //   // Debug.Log("Good");
        //}
        //else if ((Wavelength >= 490) && (Wavelength < 510))
        //{
        //    Red = 0.0f;
        //    Green = 1.0f;
        //    Blue = -(Wavelength - 510) / (510 - 490);
        //   // Debug.Log("Good");
        //}
        //else if ((Wavelength >= 510) && (Wavelength < 580))
        //{
        //    Red = (Wavelength - 510) / (580 - 510);
        //    Green = 1.0f;
        //    Blue = 0.0f;
        // //   Debug.Log("Good");
        //}
        //else if ((Wavelength >= 580) && (Wavelength < 645))
        //{
        //    Red = 1.0f;
        //    Green = -(Wavelength - 645) / (645 - 580);
        //    Blue = 0.0f;
        //  //  Debug.Log("Good");
        //}
        //else if ((Wavelength >= 645) && (Wavelength < 781))
        //{
        //    Red = 1.0f;
        //    Green = 0.0f;
        //    Blue = 0.0f;
        //  //  
        //}
        else
        {
            Red = 0.0f;
            Green = 0.0f;
            Blue = 0.0f;
           // Debug.Log("Nah");
        }
        Debug.Log("Nah" + Wavelength);
        // Let the intensity fall off near the vision limits

        if ((Wavelength >= 380) && (Wavelength < 420))
        {
            factor = 0.3f + 0.7f * (Wavelength - 380) / (420 - 380);
        }
        else if ((Wavelength >= 420) && (Wavelength < 701))
        {
            factor = 1.0f;
        }
        else if ((Wavelength >= 701) && (Wavelength < 781))
        {
            factor = 0.3f + 0.7f * (780 - Wavelength) / (780 - 700);
        }
        else
        {
            factor = 0.0f;
        }


        Color rgb = new Color();

        // Don't want 0^x = 1 for x <> 0
        rgb.r = Red;//== 0.0 ? 0 : (int)Mathf.Round(IntensityMax * Mathf.Pow(Red * factor, Gamma));
        rgb.g = Green;//== 0.0 ? 0 : (int)Mathf.Round(IntensityMax * Mathf.Pow(Green * factor, Gamma));
        rgb.b = Blue;//== 0.0 ? 0 : (int)Mathf.Round(IntensityMax * Mathf.Pow(Blue * factor, Gamma));
        rgb.a = IntensityMax;
        Debug.Log(rgb);
        return rgb;
    }
}
