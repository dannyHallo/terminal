using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CloudRotate : MonoBehaviour
{
    public Vector3 cloudPosition;
    public float floatingSpeed;
    public Transform heightControl;
    [Range(1600,3000)] public float setHeight;
    private Vector3 heightVector3;
    public Vector3 minPosition= new Vector3(0,-70,0);
    public Vector3 maxPosition= new Vector3(360, 70, 360);
    // Start is called before the first frame update
    void Start()
    {
        SetRandomInitialPosition();
    }

    // Update is called once per frame
    void Update()
    {

        CloudRotateAnimation();
        SetCloudHeight();
    }

    public void CloudRotateAnimation()
    {
        cloudPosition = Vector3.zero;

        cloudPosition.z = floatingSpeed * Time.deltaTime;

        transform.Rotate(cloudPosition);
    }

    public void SetCloudHeight()
    {
        heightVector3 = Vector3.zero;
        heightVector3.y = setHeight;
        heightControl.localPosition = heightVector3;

    }

    public void SetRandomInitialPosition()
    {
        Vector3 randomPosition = new Vector3(Random.Range(minPosition.x, maxPosition.x), Random.Range(minPosition.y, maxPosition.y), Random.Range(minPosition.z, maxPosition.z));
        transform.localEulerAngles = randomPosition;
    }
}
