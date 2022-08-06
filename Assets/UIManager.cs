using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    public GameObject pickUpUI;
    public List<GameObject> InstrumentsUI;

    // Start is called before the first frame update
    void Start()
    {
        pickUpUI.SetActive(false);
        foreach (GameObject instumentUI in InstrumentsUI){
            instumentUI.SetActive(false);
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
