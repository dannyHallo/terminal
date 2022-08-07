using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Itemdift : MonoBehaviour
{

    private Vector3 orginalPosition;
    // Start is called before the first frame update
    void Start()
    {
        orginalPosition = gameObject.transform.position;
    }

    // Update is called once per frame
    void Update()
    {
       // if

        gameObject.transform.eulerAngles += Vector3.up *50f* Time.deltaTime;
    }
}
