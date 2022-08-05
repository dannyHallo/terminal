using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.IO;
public class FrequencyToColor : MonoBehaviour
{
    public AudioProcessor audioProcessor;
    // public Button m_YourFirstButton, m_YourSecondButton, m_YourThirdButton;
    [HideInInspector] public List<int> _int;
    [HideInInspector] public List<float> frequencies;
    [HideInInspector] public List<Color> colors;
    [HideInInspector] public List<float> intensities;

    public Color FinalColor;
    [HideInInspector] public bool GotColor;
    //public FrequencyColorList FCL;
    public ParticleSystem partS;
    public Renderer _colorObject;
    [Header("Range")]
    public int start;
    public int end;
    [Header("Color Mix")]
    public Color baseColor;
    [Range(0,1)]public float mixratio = .6f;
    [Header("Speed Control")]
    public bool redSpeedLimit;
    public bool greenSpeedLimit;
    public bool blueSpeedLimit;
    public float maxColorChangeSpeeed = .2f;
    [Header("Darkness")]
    public float colorDarknessFactor = 600f;
    public bool useDarknessFactor;

    public void FrequenciesToColors()
    {
        _int.Clear();
        frequencies.Clear();
        colors.Clear();

        for (int i = start; i <= end; i++)
        {

            _int.Add(i);
            frequencies.Add(audioProcessor.GetFrequencyFromSpectrumIndex(i));
            colors.Add(ColorHandler.waveLengthToRGB(frequencies[i - start]));

            //  Debug.Log(i.ToString() + "   " + audioProcessor.GetFrequencyFromSpectrumIndex(i) + "   " + audioProcessor.GetStrengthFromSpectrumIndex(i));
            GotColor = true;
        }

    }



    public void GetKeyH()
    {
        if (Input.GetKeyDown(KeyCode.H))
        {
            FrequenciesToColors();
        }
    }

    public void CaculateFinalColor()

    {
        float totalIntensity = 0;
        float totalR = 0;
        float totalB = 0;
        float totalG = 0;
        intensities.Clear();
        for (int i = 0; i <= end - start; i++)
        {
            float intensity = audioProcessor.GetStrengthFromSpectrumIndex(_int[i]);

            intensities.Add(intensity);
            totalIntensity += intensity;

            totalR += colors[i].r / 256 * intensity;

            totalB += colors[i].b / 256 * intensity;
            totalG += colors[i].g / 256 * intensity;

        }
        //Debug.Log(colors[4].r + "  "+ colors[4].b + "  " + colors[4].g   + "  " +totalIntensity);
        if (totalIntensity != 0)
        {


            if (useDarknessFactor)
            {
                FinalColor.r = Mathf.Min(totalR / colorDarknessFactor, 1);
                FinalColor.b = Mathf.Min(totalB / colorDarknessFactor, 1);
                FinalColor.g = Mathf.Min(totalG / colorDarknessFactor, 1);
            }
            else
            {
                FinalColor.r = totalR / totalIntensity;
                FinalColor.b = totalB / totalIntensity;
                FinalColor.g = totalG / totalIntensity;
            }
        }
        else
        {
            FinalColor.r = 0f;
            FinalColor.b = 0f;
            FinalColor.g = 0f;

        }

    }

    void BaseColorMix()
    {
        FinalColor.r = FinalColor.r * (1 - mixratio) + baseColor.r * mixratio;
        FinalColor.b = FinalColor.b * (1 - mixratio) + baseColor.b * mixratio;
        FinalColor.g = FinalColor.g * (1 - mixratio) + baseColor.g * mixratio;
    }


    Color ColorDarknessModifer(Color orginalColor, float lightness)
    {
        Color _color;
        _color.r = orginalColor.r/256* lightness;
        Debug.Log("r"+orginalColor.r);
        _color.b = orginalColor.b / 256 * lightness;
        _color.g = orginalColor.g / 256 * lightness;
        _color.a = orginalColor.a;
        return _color;
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


    // Start is called before the first frame update
    void Start()
    {
        FinalColor.a = 1;
        GotColor = false;
        //m_YourFirstButton.onClick.AddListener(FrequenciesToColors);
        //  FrequencyReturn();
        _colorObject.material.SetColor("_color", Color.blue);
    }

    // Update is called once per frame
    void Update()
    {

        GetKeyH();
        if (GotColor)
        {
            CaculateFinalColor();
            BaseColorMix();
            if (_colorObject != null)
            {

                //Debug.Log("Nice"+FinalColor );
                Color targetColor = _colorObject.material.color;
               // Debug.Log("before" + targetColor);
                if (redSpeedLimit)
                {
                    targetColor.r += Mathf.Clamp(FinalColor.r - targetColor.r, Mathf.Min((FinalColor.r - targetColor.r) * Time.deltaTime, -maxColorChangeSpeeed * Time.deltaTime), Mathf.Max((FinalColor.r - targetColor.r) * Time.deltaTime, maxColorChangeSpeeed * Time.deltaTime));
                }
                else
                {
                    targetColor.r = FinalColor.r;
                }
                if (blueSpeedLimit)
                {
                    targetColor.b += Mathf.Clamp(FinalColor.b - targetColor.b, Mathf.Min((FinalColor.b - targetColor.b) * Time.deltaTime, -maxColorChangeSpeeed * Time.deltaTime), Mathf.Max((FinalColor.b - targetColor.b) * Time.deltaTime, maxColorChangeSpeeed * Time.deltaTime));
                }
                else
                {
                    targetColor.b = FinalColor.b;
                }
                if (greenSpeedLimit)
                {
                    targetColor.g += Mathf.Clamp(FinalColor.g - targetColor.g, Mathf.Min((FinalColor.g - targetColor.g) * Time.deltaTime, -maxColorChangeSpeeed * Time.deltaTime), Mathf.Max((FinalColor.g - targetColor.g) * Time.deltaTime, maxColorChangeSpeeed * Time.deltaTime));
                }
                else
                {
                    targetColor.g = FinalColor.g;
                }

              //  Debug.Log(targetColor);
                var partMain = partS.main;
                var particalColor = ColorHandler.waveLengthToRGB(audioProcessor.loudestFrequency);
                float darken = audioProcessor.GetStrengthFromSpectrumIndex(audioProcessor.loudestSpectrumBarIndex) / 100f;
                Debug.Log(darken+"  "+particalColor);
             
                particalColor = ColorDarknessModifer(particalColor, darken);
                particalColor = BaseColorMix(particalColor, baseColor, mixratio);
                partMain.startColor = particalColor;
                _colorObject.material.color = targetColor;

            }
            else
            {
                Debug.Log("Np");
            }


        }
    }
}