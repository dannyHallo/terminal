using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SelectedColorChannel : ColorChannel
{
    public Color SelectColor;

    //[HideInInspector] public ColorChannel mainInputChannel;
    // Start is called before the first frame update
    void Start()
    {

        outputColor = SelectColor;
    }

    // Update is called once per frame
}
