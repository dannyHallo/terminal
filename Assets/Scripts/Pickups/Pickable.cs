using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(AudioClip))]
public class Pickable : MonoBehaviour
{
    private void Start()
    {

    }

    private void Update()
    {

    }

    private void OnCollisionEnter(Collision other)
    {
        if (other.gameObject.tag != "Player")
            return;

        print("Player enter!");
        Destroy(gameObject);

    }
}
