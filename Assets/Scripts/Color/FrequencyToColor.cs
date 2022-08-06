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
    [Range(0, 1)] public float mixratio = .6f;
    [Header("Speed Control")]
    public bool redSpeedLimit;
    public bool greenSpeedLimit;
    public bool blueSpeedLimit;
    public float maxColorChangeSpeeed = .2f;
    [Header("Darkness")]
    public float colorDarknessFactor = 600f;
    public bool useDarknessFactor;
    public Color HighestFrequencyColor;
    public Color FrequencyAverageColor;

    [Header("Camera")]
    public Camera camera;
    public float red;
    public float redboost;
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
        _color.r = orginalColor.r / 256 * lightness;
        // Debug.Log("r"+orginalColor.r);
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

    public void AverageFrequencyColorDecider()
    {
        FrequencyAverageColor = _colorObject.material.color;
        // Debug.Log("before" + targetColor);
        if (redSpeedLimit)
        {
            FrequencyAverageColor.r += Mathf.Clamp(FinalColor.r - FrequencyAverageColor.r, Mathf.Min((FinalColor.r - FrequencyAverageColor.r) * Time.deltaTime, -maxColorChangeSpeeed * Time.deltaTime), Mathf.Max((FinalColor.r - FrequencyAverageColor.r) * Time.deltaTime, maxColorChangeSpeeed * Time.deltaTime));
        }
        else
        {
            FrequencyAverageColor.r = FinalColor.r;
        }
        if (blueSpeedLimit)
        {
            FrequencyAverageColor.b += Mathf.Clamp(FinalColor.b - FrequencyAverageColor.b, Mathf.Min((FinalColor.b - FrequencyAverageColor.b) * Time.deltaTime, -maxColorChangeSpeeed * Time.deltaTime), Mathf.Max((FinalColor.b - FrequencyAverageColor.b) * Time.deltaTime, maxColorChangeSpeeed * Time.deltaTime));
        }
        else
        {
            FrequencyAverageColor.b = FinalColor.b;
        }
        if (greenSpeedLimit)
        {
            FrequencyAverageColor.g += Mathf.Clamp(FinalColor.g - FrequencyAverageColor.g, Mathf.Min((FinalColor.g - FrequencyAverageColor.g) * Time.deltaTime, -maxColorChangeSpeeed * Time.deltaTime), Mathf.Max((FinalColor.g - FrequencyAverageColor.g) * Time.deltaTime, maxColorChangeSpeeed * Time.deltaTime));
        }
        else
        {
            FrequencyAverageColor.g = FinalColor.g;
        }

    }
    public void HighFrequencyColorDecider()
    {
        HighestFrequencyColor = ColorHandler.waveLengthToRGB(audioProcessor.loudestFrequency);
        float darken = audioProcessor.GetStrengthFromSpectrumIndex(audioProcessor.loudestSpectrumBarIndex) / 100f;
        // Debug.Log(darken+"  "+particalColor);

        HighestFrequencyColor = ColorDarknessModifer(HighestFrequencyColor, darken);
        HighestFrequencyColor = BaseColorMix(HighestFrequencyColor, baseColor, mixratio);
    }

    public void BackgroundColor()
    {
        if (camera != null)
        {
            //可能需要优化
            float loudness = audioProcessor.beatsStrength;
            redboost = redboost + ((loudness) - .01f) * Time.deltaTime;//,-Time.deltaTime*.001f,Time.deltaTime*(loudness / 100)*.2f)     ;
            if (red < .35f * loudness / 100)
            {
                red += Mathf.Min(redboost,.6f*Time.deltaTime);
            }
            else
            {
                red -= .2f * Time.deltaTime;
            }
            Color color = new Color(red, .25f,.5f,1f);
            camera.backgroundColor = color;

        }
    }
    // Start is called before the first frame update
    void Start()
    {
        FinalColor.a = 1;
        GotColor = false;
        //m_YourFirstButton.onClick.AddListener(FrequenciesToColors);
        //  FrequencyReturn();
        if (_colorObject != null)
        {
        _colorObject.material.SetColor("_color", Color.blue);
        }
        //camera
        camera = FindObjectOfType<Camera>();
        redboost = 0f;
        red = .2f;
    }

    // Update is called once per frame
    void Update()
    {
        if (Time.frameCount == 60)
        {
            FrequenciesToColors();
        }
        if (GotColor)
        {
            CaculateFinalColor();
            BaseColorMix();
            HighFrequencyColorDecider();
            AverageFrequencyColorDecider();
            BackgroundColor();
            if (partS!=null)
            {
                var partMain = partS.main;
                
                partMain.startColor = HighestFrequencyColor;
            }

            if (_colorObject != null)
            {

                //Debug.Log("Nice"+FinalColor );
                
                //  Debug.Log(targetColor);
                

                    _colorObject.material.color = FrequencyAverageColor;
                
            }
            else
            {
                // Debug.Log("Np");
            }


        }




    }
}