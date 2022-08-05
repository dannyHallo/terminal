using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class UIManagement : MonoBehaviour
{
    public TextMeshProUGUI outText1;
    // public TextMeshProUGUI outText2;

    private void Start()
    {
        PushText("Welcome!");
    }

    private void Update()
    {

    }

    public void PushText(string message)
    {
        outText1.text = message;
    }
}
