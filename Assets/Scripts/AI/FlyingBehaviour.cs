using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LibNoise.Generator;

public class FlyingBehaviour : MonoBehaviour
{
    private GameObject terrainGen
    {
        get
        {
            return GameObject.Find("TerrainGen");
        }
    }

    private Perlin customPerlin;

    [Header("Creature Normal Settings")]
    public float minDistanceFromGround;
    public float maxDistanceFromGround;
    public float verticalMovementSpeed;
    public float horizontalMovementSpeed;
    [Range(1.0f, 50.0f)] public float randomDecisionSpeedUp = 1.0f;

    public float verticalMovementFrequency;
    public float stopChasingThrehold;
    public float rotateSpeed;
    public float digStrength;
    public float growRequiredTime;
    public float maxToMinScaleRatio;

    private int frameCount = 0;
    public bool fixSkipFrameNum;
    [ConditionalHide("fixSkipFrameNum", true), Range(1, 1000)] public int fixedSkipFrameNum;
    [ConditionalHide("fixSkipFrameNum", false), Range(1, 100)] public int minSkipFrameNum;
    [ConditionalHide("fixSkipFrameNum", false), Range(1, 400)] public int maxSkipFrameNum;
    public float skipFrameNumIncreasement = 10;

    public float noiseChangeFac = 0.1f;
    public float seed;


    [Header("Drawing")]
    [Range(1, 10)] public float initDrawRange = 1.0f;
    [Range(1, 10)] public float maxDrawRange = 10.0f;
    public bool digIn = false;
    public bool drawMetal = false;


    [Header("Preferance")]
    [Range(0, 1)] public float ability_weight;
    [Range(0, 1)] public float do_something_weight;
    [Range(0, 1)] public float player_weight;
    [Range(0, 1)] public float env_change_weight;
    [Range(0, 1)] public float attack_weight;


    [Header("Read-onlys")]
    public float ability_score;
    public float do_something_score;
    public float player_score;
    public float env_change_score;
    public float attack_score;
    public int currentDrawRange;

    public bool wander;
    public bool chasePlayer;
    public bool env_change;
    public bool attack;
    public bool isAboveTerrain;
    public int skipFrameNum;


    [Header("debug")]
    public float f1;
    public Vector3 v1;

    private Vector3 playerPos;
    private Vector3 creaturePos;
    private Vector3 hitTerrainPos;
    private float currentScale;
    private float initScale;

    const float worldBound = 160;
    private int xSign = 1;
    private int zSign = 1;

    private void GrowupIfNotMaximized()
    {
        if (currentScale >= maxToMinScaleRatio * initScale)
            return;

        currentScale += (maxToMinScaleRatio / growRequiredTime) * Time.deltaTime;
        ability_score += ability_weight * Time.deltaTime;

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
                isAboveTerrain = false;
                creaturePos.y += verticalMovementSpeed * Time.deltaTime;
                return;
            }

            isAboveTerrain = true;
            hitTerrainPos = hit.point;
            float rayLengthToTerrain = Vector3.Distance(hit.point, creaturePos);

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
        else
        {
            isAboveTerrain = false;
        }
    }

    private void Awake()
    {
        initScale = transform.localScale.x;
        currentScale = initScale;
        customPerlin = new Perlin();
    }

    void Update()
    {
        playerPos = GameObject.Find("Player").transform.position;
        creaturePos = transform.position;
        frameCount++;

        GetSkipFrameNumThisFrame();
        GrowupIfNotMaximized();
        KeepDistanceFromTerrain();  // Vertically
        GetStatus();

        ReverseSignIfOutOfBound();
        Move();                     // Horizontally

        if (frameCount % skipFrameNum == 0 && isAboveTerrain)
        {
            DrawSpecialTexture(digIn, drawMetal);
            frameCount = 0;
        }
    }

    void ChangeSeedBasedOnTime()
    {
        seed = Time.time * Time.deltaTime * 1000.0f;
    }

    void ReverseSignIfOutOfBound()
    {
        if (Mathf.Abs(creaturePos.x) > worldBound)
        {
            xSign = flipSign(xSign);
        }

        if (Mathf.Abs(creaturePos.z) > worldBound)
        {
            zSign = flipSign(zSign);
        }
    }

    int flipSign(int i)
    {
        return i == 1 ? -1 : 1;
    }

    int GetSkipFrameNumThisFrame()
    {
        if (fixSkipFrameNum)
            return fixedSkipFrameNum;

        skipFrameNum = (int)(minSkipFrameNum + Mathf.Pow(Vector3.Distance(creaturePos, playerPos), 2) * skipFrameNumIncreasement);
        skipFrameNum = Mathf.Clamp(skipFrameNum, minSkipFrameNum, maxSkipFrameNum);

        return skipFrameNum;
    }

    private void GetStatus()
    {
        if (Vector3.Distance(playerPos, creaturePos) < stopChasingThrehold)
        {
            chasePlayer = false;
        }
    }

    private void DrawSpecialTexture(bool digIn, bool drawMetal)
    {
        currentDrawRange = (int)Mathf.Clamp(initDrawRange * (1 + ability_score), 0, maxDrawRange);

        // Draw texture
        terrainGen.GetComponent<ColourGenerator2D>().CreateTextureIfNeeded();
        terrainGen.GetComponent<ColourGenerator2D>().DrawTextureOnWorldPos(
            terrainGen.GetComponent<ColourGenerator2D>().userTex, hitTerrainPos, currentDrawRange, drawMetal);

        // Change env
        terrainGen.GetComponent<TerrainMesh>().DrawOnChunk(
            hitTerrainPos, currentDrawRange, digStrength * skipFrameNum, digIn ? 0 : 1);
    }

    private void Move()
    {
        if (chasePlayer)
        {
            Vector3 directionNormalized = (playerPos - creaturePos).normalized;
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
            return;
        }

        if (wander)
        {
            float seed1 = (Time.time + seed * 1000.0f) * noiseChangeFac * randomDecisionSpeedUp;
            float angle01 = (float)customPerlin.GetValue(seed1, 0, 0);

            // Change direction
            Vector3 directionNormalized =
                new Vector3(Mathf.Cos(angle01 * 2 * Mathf.PI), 0, Mathf.Sin(angle01 * 2 * Mathf.PI));

            creaturePos.x += xSign * directionNormalized.x * Time.deltaTime * horizontalMovementSpeed * randomDecisionSpeedUp;
            creaturePos.z += zSign * directionNormalized.z * Time.deltaTime * horizontalMovementSpeed * randomDecisionSpeedUp;

            transform.LookAt(new Vector3(creaturePos.x, transform.position.y, creaturePos.z), Vector3.up);

            transform.position = creaturePos;
            return;
        }
    }
}
