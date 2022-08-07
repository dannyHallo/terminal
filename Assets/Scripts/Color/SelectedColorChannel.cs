using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SelectedColorChannel : ColorChannel
{
    public Color SelectColor;

    // Start is called before the first frame update
    void Start()
    {
        outputColor = SelectColor;
    }

    // Update is called once per frame
}
