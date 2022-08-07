using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class colorControl : MonoBehaviour
{
    // Start is called before the first frame update
    public GameObject cube;


    private Color lastFrameColor;
    // Create a new RGBA color using the Color constructor and store it in a variable
    public Color frameColor;

    [Range(0, 1)] public float red;
    [Range(0, 1)] public float blue;
    [Range(0, 1)] public float green;
    public bool _red;
    public bool _blue;
    public bool _green;
    [Range(0,2)]public float standardMaxSpeed=.3f;
    [Range(0, 2)] public float redChangeSpeed;
    [Range(0, 2)] public float blueChangeSpeed;
    [Range(0, 2)] public float greenChangeSpeed;
   [HideInInspector] public Renderer cubeRenderer;


    public bool Mix;
    public Color mixingColor;

    [Header("Change Between")]
    private bool ToSecondColor;

    public Color colorOne;
    public Color colorTwo;
    private void Start()
    {
        // frameColor.a = 1;
        // red=Random.Range(0,1);
        // green = Random.Range(0, 1);
        // blue = Random.Range(0, 1);

        // redChangeSpeed = Random.Range(.1f, standardMaxSpeed);
        // greenChangeSpeed = Random.Range(.1f, standardMaxSpeed);
        // blueChangeSpeed = Random.Range(.1f, standardMaxSpeed);
        // frameColor.r = red;
        // frameColor.b = blue;
        // frameColor.g = green;
        // Debug.Log(frameColor);
        cubeRenderer = this.GetComponent<Renderer>();
       //var cubeRenderer = cube.GetComponent<Renderer>();
        //cubeRenderer.material.SetColor("_Color", frameColor);

    }
    void Update()
    {
        colorTwo = colorOne;
        if (Mix)
        {
            colorTwo = BaseColorMix(colorTwo, mixingColor, .4f);
        }
        //  frameColor = ChangeBetween(frameColor, colorOne, colorTwo, standardMaxSpeed);
        frameColor = ChangeColorToward(frameColor,colorTwo, standardMaxSpeed);

        cubeRenderer.material.SetColor("_Color", frameColor);
      //  Debug.Log("cgag"+customColor);
        // Call SetColor using the shader property name "_Color" and setting the color to red
      //  Debug.Log(1/Time.deltaTime);
    }




   // public float changeX(float x,bool decrease, float changeSpeed)
   // {

      
   //         if (decrease)
   //         {
   //             x -= Time.deltaTime * changeSpeed;
   //         return x;
   //         }
   //         else
   //         {
   //             x += Time.deltaTime * changeSpeed;
   //         return x;
   //     }
        
   ////     Debug.Log(x+" "+decrease);
   // }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="baseColor"></param>
    /// <param name="mixColor"></param>
    /// <param name="mixratio">should be between [Range(0,1)]</param>
    /// <returns></returns>
    public Color BaseColorMix(Color baseColor, Color mixColor,float mixratio)
    {
        Color _color;
        _color.r = baseColor.r * (1 - mixratio) + mixColor.r * mixratio;
        _color.b = baseColor.b * (1 - mixratio) + mixColor.b * mixratio;
        _color.g = baseColor.g * (1 - mixratio) + mixColor.g * mixratio;
        _color.a = baseColor.a * (1 - mixratio) + mixColor.a * mixratio;
        return _color;
    }
    









    Color ChangeBetween(Color colorNow,Color colorA,Color colorB,float maxspeed)
    {
        Color _color;
        if (ToSecondColor)
        {
            _color = ChangeColorToward(colorNow, colorB, maxspeed);
            if (CheckColorReach(_color, colorB))
            {
                ToSecondColor = false;
            }

            return _color;
        }
        else
        {
            _color = ChangeColorToward(colorNow, colorA, maxspeed);
            if (CheckColorReach(_color, colorA))
            {
                ToSecondColor = true;
            }
            return _color;
        }

        //_red = CheckDecrease(red, _red,Mathf.Max(colorA.r,colorB.r), Mathf.Min(colorA.r, colorB.r));
        //_blue = CheckDecrease(blue, _blue,Mathf.Max(colorA.b, colorB.b), Mathf.Min(colorA.b, colorB.b));
        //_green = CheckDecrease(green, _green,Mathf.Max(colorA.g, colorB.g), Mathf.Min(colorA.g, colorB.g));
    }


    bool CheckColorReach(Color colorNow,Color targetColor)
    {
        if (colorNow==targetColor)
        {
            return true;
        }
        else
        {
            return false;
        }
    }


    /// <summary>
    /// 
    /// </summary>
    /// <param name="colorNow"></param>
    /// <param name="targetColor"></param>
    /// <param name="maxSpeed"> I believe this is per sec</param>
    /// <returns></returns>
    Color ChangeColorToward(Color colorNow,Color targetColor,float maxSpeed)
    {
        Color _color;
        float largestDifference = Mathf.Max(Mathf.Abs(colorNow.r - targetColor.r),Mathf.Abs(colorNow.b-targetColor.b),Mathf.Abs(colorNow.g - targetColor.g));
        _color.r = RGBFloatTargetClampChange(colorNow.r, targetColor.r, Mathf.Abs(colorNow.r - targetColor.r) / largestDifference * maxSpeed * Time.deltaTime);
        _color.b = RGBFloatTargetClampChange(colorNow.b, targetColor.b, Mathf.Abs(colorNow.b - targetColor.b) / largestDifference * maxSpeed * Time.deltaTime);
        _color.g = RGBFloatTargetClampChange(colorNow.g, targetColor.g, Mathf.Abs(colorNow.g - targetColor.g) / largestDifference * maxSpeed * Time.deltaTime);
        _color.a = RGBFloatTargetClampChange(colorNow.a, targetColor.a, Mathf.Abs(colorNow.a - targetColor.a) / largestDifference * maxSpeed * Time.deltaTime);
        return _color;
    }







    /// <summary>
    /// 
    /// </summary>
    /// <param name="valueNow"></param>
    /// <param name="targetvalue"></param>
    /// <param name="maxSpeed">The input value need to be a positive number, and have Timedelta contained</param>
    /// <returns></returns>
    float RGBFloatTargetClampChange(float valueNow,float targetvalue,float maxSpeed)
    {
        float _value;
        _value = valueNow + Mathf.Clamp(targetvalue - valueNow, -maxSpeed, maxSpeed);
        return _value;
    }






    public bool CheckDecrease(float x, bool _b)
    {
        if (x >= 1)
        {
            return true;
            //Debug.Log("A");
        }

        if (x <= .8)
        {
            return false;
        }
        return _b;
    }

    public bool CheckDecrease(float x, bool _b,float max, float min)
    {
        if (x >= max)
        {
            return true;
            //Debug.Log("A");
        }

        if (x <= min)
        {
            return false;
        }
        return _b;
    }





}


