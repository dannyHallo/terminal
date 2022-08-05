using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class StrangeBehaviour1 : MonoBehaviour
{
    public float searchRange;
    public enum SearchShape { Suqare };
    public SearchShape searchShape;
    public float idleDuration = 1.0f;
    public float jumpForce = 10.0f;

    private Rigidbody r;
    private float timeCount;

    private void Start()
    {
        r = gameObject.GetComponent<Rigidbody>();
        r.freezeRotation = true;
    }

    private void Update()
    {
        if (timeCount < idleDuration)
        {
            timeCount += Time.deltaTime;
            return;
        }

        r.AddForce(jumpForce * new Vector3(1, 1.2f, 0));

        timeCount = 0;
    }

    private void Move()
    {

    }
}
