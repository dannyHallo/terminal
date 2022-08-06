using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FlyingBehaviour : MonoBehaviour
{
    public float minDistanceFromGround;
    public float maxDistanceFromGround;
    public float verticalMovementSpeed;
    public float verticalMovementFrequency;

    [Header("debug")]
    public float f1;

    private void KeepDistanceFromTerrain()
    {
        float rayLength = 200f;

        LayerMask creatureMask = LayerMask.GetMask("Creature");

        // actual Ray
        Ray ray = new Ray(transform.position, Vector3.down);

        // debug Ray
        Debug.DrawRay(ray.origin, ray.direction * rayLength, Color.green);

        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, rayLength, ~creatureMask))
        {
            if (hit.collider.tag != "Chunk")
                return;

            Vector3 p = transform.position;
            float rayLengthToTerrain = Vector3.Distance(hit.point, p);

            f1 = rayLengthToTerrain;

            if (rayLengthToTerrain < minDistanceFromGround)
            {
                p.y += verticalMovementSpeed * Time.deltaTime;
            }
            else if (rayLengthToTerrain > maxDistanceFromGround)
            {
                p.y -= verticalMovementSpeed * Time.deltaTime;
            }
            else
            {
                p.y += Mathf.Sin(Time.time * verticalMovementFrequency) * verticalMovementSpeed * Time.deltaTime;
            }


            transform.position = p;
        }
    }


    void Start()
    {

    }

    void Update()
    {
        KeepDistanceFromTerrain();
    }

    private void Dig()
    {

    }
}
