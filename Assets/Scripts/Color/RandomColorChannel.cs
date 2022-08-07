using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RandomColorChannel : ColorChannel
{
    
    public Color baseColor;
    public bool randomlizeRed = true, randomlizeBlue = true, randomlizeGreen = true;
    public Color startRangeColor = Color.black;
    public Color endRangeColor = Color.white;
    // Start is called before the first frame update
    void Start()
    {
        //Random.InitState(GetInstanceID());
        if (mainInputChannel)
            baseColor = mainInputColor;
        else
            baseColor = Color.white;
        float red, blue, green;
        if (randomlizeRed)
        {
            red = Random.Range(Mathf.Min(startRangeColor.r, endRangeColor.r), Mathf.Max(startRangeColor.r, endRangeColor.r));
        }
        else
        {
            red = baseColor.r;
        }
        if (randomlizeBlue)
        {
            blue = Random.Range(Mathf.Min(startRangeColor.b, endRangeColor.b), Mathf.Max(startRangeColor.b, endRangeColor.b));
        }
        else
        {
            blue = baseColor.b;
        }
        if (randomlizeGreen)
        {
            green = Random.Range(Mathf.Min(startRangeColor.g, endRangeColor.g), Mathf.Max(startRangeColor.g, endRangeColor.g));
        }
        else
        {
            green = baseColor.g;
        }

        outputColor = new Color(red, green, blue, baseColor.a);


    }

    // Update is called once per frame
    void Update()
    {

    }
}
