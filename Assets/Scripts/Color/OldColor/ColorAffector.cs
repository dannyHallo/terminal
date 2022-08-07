using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ColorAffector : MonoBehaviour
{
    public colorControl CC;
    public Color mixer;
    public float mixRatio; 
    private void OnCollisionStay(Collision collision)
    {
        var otherCollisionRenderer=collision.gameObject.GetComponent<Renderer>();
        CC = collision.gameObject.GetComponent<colorControl>();
        CC.Mix = true;
        CC.mixingColor = mixer;
        Color baseColor = otherCollisionRenderer.material.color;
        otherCollisionRenderer .material.color= CC.BaseColorMix(baseColor, mixer, mixRatio) ;
        Debug.Log("!!");
    }
    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
