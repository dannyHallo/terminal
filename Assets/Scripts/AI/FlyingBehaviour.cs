using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FlyingBehaviour : MonoBehaviour
{
    public float minDistanceFromGround;
    public float maxDistanceFromGround;
    public float verticalMovementSpeed;
    public float horizontalMovementSpeed;
    public float verticalMovementFrequency;

    public bool chasePlayer;
    public bool leavePlayer;

    [Header("debug")]
    public float f1;

    private Vector3 playerPos;
    private Vector3 creaturePos;

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
            // It is buried! Get up!
            if (hit.collider.tag != "Chunk")
            {
                creaturePos.y += verticalMovementSpeed * Time.deltaTime;
                return;
            }

            float rayLengthToTerrain = Vector3.Distance(hit.point, creaturePos);

            f1 = rayLengthToTerrain;

            if (rayLengthToTerrain < minDistanceFromGround)
            {
                creaturePos.y += verticalMovementSpeed * Time.deltaTime;
            }
            else if (rayLengthToTerrain > maxDistanceFromGround)
            {
                creaturePos.y -= verticalMovementSpeed * Time.deltaTime;
            }
            else
            {
                creaturePos.y += Mathf.Sin(Time.time * verticalMovementFrequency) * verticalMovementSpeed * Time.deltaTime;
            }
        }
    }


    void Start()
    {
    }

    void Update()
    {
        playerPos = GameObject.Find("Player").transform.position;
        creaturePos = transform.position;

        KeepDistanceFromTerrain();
        Move();

        transform.position = creaturePos;
    }

    private void Move()
    {
        if (chasePlayer)
        {
            Vector3 directionNormalized = playerPos - creaturePos;
            directionNormalized.y = 0;

            creaturePos.x += directionNormalized.x * Time.deltaTime * horizontalMovementSpeed;
            creaturePos.z += directionNormalized.z * Time.deltaTime * horizontalMovementSpeed;
        }
    }

    private void Dig()
    {

    }
}
