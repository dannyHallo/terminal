using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FlyingBehaviour : MonoBehaviour
{
    private GameObject terrainGen
    {
        get
        {
            return GameObject.Find("TerrainGen");
        }
    }

    [Header("Creature Normal Settings")]
    public float minDistanceFromGround;
    public float maxDistanceFromGround;
    public float verticalMovementSpeed;
    public float horizontalMovementSpeed;
    public float verticalMovementFrequency;
    public float stopChasingThrehold;
    public float rotateSpeed;
    public int drawRange;
    public float digStrength;
    public float growSpeed;
    public float maxToMinScaleRatio;


    [Header("Preferance")]
    [Range(0, 1)] public float age_weight;
    [Range(0, 1)] public float do_something_weight;
    [Range(0, 1)] public float player_weight;
    [Range(0, 1)] public float env_change_weight;
    [Range(0, 1)] public float attack_weight;


    [Header("Read-onlys")]
    public float age_score;
    public float do_something_score;
    public float player_score;
    public float env_change_score;
    public float attack_score;

    public bool do_something;
    public bool chasePlayer;
    public bool env_change;
    public bool attack;


    [Header("debug")]
    public float f1;

    private Vector3 playerPos;
    private Vector3 creaturePos;
    private Vector3 hitTerrainPos;
    private float currentScale;
    private float initScale;

    private void GrowupIfNotMaximized()
    {
        if (currentScale >= maxToMinScaleRatio * initScale)
            return;

        currentScale += growSpeed * Time.deltaTime;
        age_score += age_weight * Time.deltaTime;

        transform.localScale = new Vector3(currentScale, currentScale, currentScale);
    }

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

            hitTerrainPos = hit.point;
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

    private void Awake()
    {
        initScale = transform.localScale.x;
        currentScale = initScale;
    }

    void Update()
    {
        playerPos = GameObject.Find("Player").transform.position;
        creaturePos = transform.position;

        GrowupIfNotMaximized();
        KeepDistanceFromTerrain();  // Vertically
        GetStatus();
        Move();                     // Horizontally
        DrawGrass(true);
    }

    private void GetStatus()
    {
        if (Vector3.Distance(playerPos, creaturePos) < stopChasingThrehold)
        {
            chasePlayer = false;
        }
    }

    private void DrawGrass(bool positiveToAdd)
    {
        // Draw grass
        terrainGen.GetComponent<ColourGenerator2D>().CreateTextureIfNeeded();
        terrainGen.GetComponent<ColourGenerator2D>().DrawTextureOnWorldPos(
            terrainGen.GetComponent<ColourGenerator2D>().userTex, hitTerrainPos, drawRange, false);

        // Change env
        terrainGen.GetComponent<TerrainMesh>().DrawOnChunk(hitTerrainPos, drawRange, 0, positiveToAdd ? 1 : 0, true);
    }

    private void Move()
    {
        if (chasePlayer)
        {
            Vector3 directionNormalized = playerPos - creaturePos;
            directionNormalized.y = 0;

            creaturePos.x += directionNormalized.x * Time.deltaTime * horizontalMovementSpeed;
            creaturePos.z += directionNormalized.z * Time.deltaTime * horizontalMovementSpeed;


            // Vector3 playerP = new Vector3(playerPos.x, creaturePos.y, playerPos.z);
            Vector3 direction = playerPos - transform.position;
            Quaternion fromRotation = transform.rotation;
            transform.LookAt(playerPos, Vector3.up);
            Quaternion toRotation = transform.rotation;
            transform.rotation = fromRotation;

            transform.rotation = Quaternion.Lerp(fromRotation, toRotation, rotateSpeed * Time.deltaTime);
            transform.position = creaturePos;
        }
    }
}
