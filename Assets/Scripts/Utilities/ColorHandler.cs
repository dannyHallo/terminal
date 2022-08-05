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
        //orginal range numbers
        float range0 = 100;
        float range1 = 330;
        float range2 = 450;
        float range3 = 510;
        float range4 = 580;
        float range5 = 645;
        float range6 = 781;
        #region
        //orginal range numbers
        //float range0 = 380;
        //float range1 = 440;
        //float range2 = 490;
        //float range3 = 510;
        //float range4 = 580;
        //float range5 = 645;
        //float range6 = 781;
        #endregion 
        if ((Wavelength >= range0) && (Wavelength < range1))
        {
            Red = -(Wavelength - range1) / (range1 - range0);
            Green = 0.0f;
            Blue = 1.0f;
        }
        else if ((Wavelength >= range1) && (Wavelength < range2))
        {
            Red = 0.0f;
            Green = (Wavelength - range1) / (range2 - range1);
            Blue = 1.0f;
        }
        else if ((Wavelength >= range2) && (Wavelength < range3))
        {
            Red = 0.0f;
            Green = 1.0f;
            Blue = -(Wavelength - range3) / (range3 - range2);
        }
        else if ((Wavelength >= range3) && (Wavelength < range4))
        {
            Red = (Wavelength - range3) / (range4 - range3);
            Green = 1.0f;
            Blue = 0.0f;
        }
        else if ((Wavelength >= range4) && (Wavelength < range5))
        {
            Red = 1.0f;
            Green = -(Wavelength - range5) / (range5 - range4);
            Blue = 0.0f;
        }
        else if ((Wavelength >= range5) && (Wavelength < range6))
        {
            Red = 1.0f;
            Green = 0.0f;
            Blue = 0.0f;
        }
        else if(Wavelength >= range6)
        {
            Red = 1.0f;
            Green = 1.0f;
            Blue = 1.0f;
        }
        else
        {
            Red = 0.0f;
            Green = 0.0f;
            Blue = 0.0f;
        }

        float range21=420;
        float range22= 701;

        // Let the intensity fall off near the vision limits

        if ((Wavelength >= range0) && (Wavelength < range21))
        {
            factor = 0.3f + 0.7f * (Wavelength - range0) / (range21 - range0);
        }
        else if ((Wavelength >= range21) && (Wavelength < range22))
        {
            factor = 1.0f;
        }
        else if ((Wavelength >= range22) && (Wavelength < range6))
        {
            factor = 0.3f + 0.7f * (range6 - Wavelength) / (range6 - range22);
        }
        else
        {
            factor = 1f;
        }


        Color rgb = new Color();

        // Don't want 0^x = 1 for x <> 0
        rgb.r = Red == 0.0 ? 0 : (int)Mathf.Round(IntensityMax * Mathf.Pow(Red * factor, Gamma));
        rgb.g = Green == 0.0 ? 0 : (int)Mathf.Round(IntensityMax * Mathf.Pow(Green * factor, Gamma));
        rgb.b = Blue == 0.0 ? 0 : (int)Mathf.Round(IntensityMax * Mathf.Pow(Blue * factor, Gamma));
        rgb.a = IntensityMax;

        return rgb;
    }
}
