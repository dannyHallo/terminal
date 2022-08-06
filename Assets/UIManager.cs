using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    public GameObject pickUpUI;
    public GameObject InstrumentOneUI;
    public GameObject InstrumentTwoUI;
    public GameObject InstrumentThreeUI;
    public GameObject InstrumentFourUI;

    // Start is called before the first frame update
    void Start()
    {
        pickUpUI.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
