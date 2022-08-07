using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DarkenChannel : ColorChannel
{
    public ColorChannel darkenfactorInput;
    public bool darkenFactorInputSwitch;
    // Start is called before the first frame update
    public float darkenFactor { 
    get { if (darkenFactorInputSwitch)
            {
                return darkenfactorInput.outputfloat;
            }
            else
            {
                return darkenFactor;
            }
        }
    }
        

    Color ColorDarknessModifer(Color orginalColor, float lightness)
    {
        Color _color;
        _color.r = orginalColor.r * lightness;
        // Debug.Log("r"+orginalColor.r);
        _color.b = orginalColor.b * lightness;
        _color.g = orginalColor.g * lightness;
        _color.a = orginalColor.a;
        return _color;
    }
    // Update is called once per frame
    void Update()
    {
        if (mainInputChannel)
            outputColor = ColorDarknessModifer(mainInputColor, darkenFactor);


    }
}
