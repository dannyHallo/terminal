using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Itemdift : MonoBehaviour
{
    public enum XYZ { X,Y,Z};
    public XYZ rotationDirection;
    [HideInInspector]public Vector3 rotationVector;
    private Vector3 orginalPosition;
    // Start is called before the first frame update
    void Start()
    {
        switch (rotationDirection)
        {
            case XYZ.X:
                rotationVector = Vector3.right;
                break;
            case XYZ.Y:
                rotationVector = Vector3.up;
                break;




            case XYZ.Z:
                rotationVector = Vector3.forward;
                break;
        }



        orginalPosition = gameObject.transform.position;
    }

    // Update is called once per frame
    void Update()
    {
       // if

        gameObject.transform.eulerAngles += rotationVector * 50f* Time.deltaTime;
    }
}
