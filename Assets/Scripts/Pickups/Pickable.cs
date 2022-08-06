using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(AudioClip))]
public class Pickable : MonoBehaviour
{
    public GameObject PressE;
    private void Start()
    {
        PressE= GameObject.Find("Press E To Pickup"); 
    }

    private void Update()
    {

    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.tag != "Player")
            PressE.SetActive(true);
            return;


    }
    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.tag != "Player")
        {
            PressE.SetActive(false);
            return;
        }
            
    }
    private void OnTriggerStay(Collider other)
    {
        if (Input.GetKeyDown(KeyCode.E))
            {
            GameObject player = GameObject.Find("Player");
            PlayerController.InstrumentTypes instrumentType = PlayerController.InstrumentTypes.Guitar;

            player.GetComponent<PlayerController>().UseInstrument(instrumentType);
            Destroy(gameObject);
            PressE.SetActive(false);
        }

    }



}

