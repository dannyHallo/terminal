using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RandomForestGenerator : MonoBehaviour
{
    // Start is called before the first frame update

    public int forestSize = 25; // Overall size of the forest (a square of forestSize X forestSize).
    public int elementSpacing = 3; // The spacing between element placements. Basically grid size.

    public Element[] elements;
    public Vector3 RayCastHeight = new Vector3(0f, 100f, 0f);
    public int startX = -160;
    public int startZ = -160;

    [SerializeField] private int layer = 0;
    private int layerAsLayerMask;

    private void Start()
    {
        layerAsLayerMask = (1 << layer);

    }

    private void Update()
    {
        if (Time.frameCount == 300)
        {
            SpawnForest();

        }
    }
    private void SpawnForest()
    {
        float blankSpaceStartX;
        float blankSpaceEndX;
        float blankSpaceStartZ;
        float blankSpaceEndZ;
        Vector2 blankSpaceCenter;
        float blankSpaceRadius;

        blankSpaceStartX = Random.Range(startX, startX + .8f * forestSize);
        blankSpaceEndX = blankSpaceStartX + Random.Range(.2f * forestSize, .7f * forestSize);
        blankSpaceStartZ = Random.Range(startZ, startZ + .8f * forestSize);
        blankSpaceEndZ = blankSpaceStartZ + Random.Range(.2f * forestSize, .7f * forestSize);
        blankSpaceCenter = new Vector2(Random.Range(startX, startX + forestSize), Random.Range(startZ, startZ + forestSize));
        blankSpaceRadius = Random.Range(.2f * forestSize, .4f * forestSize);






        // Loop through all the positions within our forest boundary.
        for (int x = startX; x < startX + forestSize; x += elementSpacing)
        {
            for (int z = startZ; z < startZ + forestSize; z += elementSpacing)
            {
                bool blankCheck = false;
                if (x <= blankSpaceEndX && x >= blankSpaceStartX && z <= blankSpaceEndZ && z >= blankSpaceStartZ)
                {
                    blankCheck = true;
                }
                Vector2 positionV2 = new Vector2(x, z);
                Vector2 distance = positionV2 - blankSpaceCenter;
                if (distance.magnitude < blankSpaceRadius)
                {
                    blankCheck = true;
                }

                // For each position, loop through each element...
                if (blankCheck != true)
                {


                    for (int i = 0; i < elements.Length; i++)
                    {

                        // Get the current element.
                        Element element = elements[i];


                        // Check if the element can be placed.
                        if (element.CanPlace())
                        {
                            //Debug.Log("what");
                            // Add random elements to element placement.
                            Vector3 position = new Vector3(x, 0f, z);
                            Vector3 offset = new Vector3(Random.Range(-0.75f, 0.75f), 0f, Random.Range(-0.75f, 0.75f));
                            Vector3 rotation = new Vector3(Random.Range(0, 5f), Random.Range(0, 360f), Random.Range(0, 5f));
                            Vector3 scale = Vector3.one * Random.Range(0.75f, 1.25f);

                            // Instantiate and place element in world.
                            Ray ray = new Ray(position + offset + RayCastHeight, Vector3.down);

                            Debug.DrawRay(ray.origin, ray.direction * 120f, Color.green);

                            RaycastHit hit;
                            Vector3 spawnPoint = position + offset;
                            if (Physics.Raycast(ray, out hit, 150f))
                            {
                                //Debug.Log("hit " + hit.point.ToString());
                                // Debug.Log(hit.collider);
                                // Debug.Log("hit " + hit.point.ToString());
                                spawnPoint = hit.point;



                            }





                            GameObject newElement = Instantiate(element.GetRandom());
                            newElement.transform.SetParent(transform);
                            newElement.transform.position = spawnPoint;
                            newElement.transform.eulerAngles = rotation;
                            newElement.transform.localScale = scale;

                            // Break out of this for loop to ensure we don't place another element at this position.
                            break;

                        }
                    }

                }
            }
        }

    }

}

[System.Serializable]
public class Element
{

    public string name;
    [Range(1, 10)]
    public int density;

    public GameObject[] prefabs;

    public bool CanPlace()
    {

        // Validation check to see if element can be placed. More detailed calculations can go here, such as checking perlin noise.

        if (Random.Range(0, 10) < density)
            return true;
        else
            return false;

    }

    public GameObject GetRandom()
    {

        // Return a random GameObject prefab from the prefabs array.

        return prefabs[Random.Range(0, prefabs.Length)];

    }

}


//Original
//https://github.com/b3agz/quick-bits/blob/master/simple-random-forest-generator/RandomForestGenerator.cs
//https://www.youtube.com/watch?v=604lmtHhcQs