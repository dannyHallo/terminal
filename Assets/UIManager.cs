using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class UIManager : MonoBehaviour
{
    public TextMeshProUGUI hintText;
    public List<GameObject> InstrumentsUI;

    void Start()
    {
        foreach (GameObject instumentUI in InstrumentsUI)
        {
            instumentUI.SetActive(false);
        }
    }

    void Update()
    {

    }
}
