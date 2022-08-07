using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ColorSwitchingAnimationChannel : ColorChannel
{
    bool ToSecondColor;
    public ColorChannel InputChannelTwo;
    public float changeSpeed;
    Color inputColorTarget
    {
        get { return InputChannelTwo.outputColor; }
    }
    // Start is called before the first frame update


    Color ChangeBetween(Color colorNow, Color colorA, Color colorB, float maxspeed)
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
    bool CheckColorReach(Color colorNow, Color targetColor)
    {
        if (colorNow == targetColor)
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    void Start()
    {
        outputColor = mainInputColor;
        ToSecondColor = true;
    }

    // Update is called once per frame
    void Update()
    {
        outputColor = ChangeBetween(outputColor,mainInputColor, inputColorTarget, changeSpeed);
    }
}
