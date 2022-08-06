using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(AudioClip))]
public class Pickable : MonoBehaviour
{
    public UIManager PressE;
    private void Start()
    {
        PressE = FindObjectOfType<UIManager>();
    }

    private void Update()
    {

    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.tag != "Player")
        {
            return;

        }
        PressE.pickUpUI.SetActive(true);
    }
    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.tag != "Player")
        {
            return;
        }
        PressE.pickUpUI.SetActive(false);

    }
    private void OnTriggerStay(Collider other)
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            GameObject player = GameObject.Find("Player");
            PlayerController.InstrumentTypes instrumentType = PlayerController.InstrumentTypes.Guitar;

            player.GetComponent<PlayerController>().UseInstrument(instrumentType);
            Destroy(gameObject);
            PressE.pickUpUI.SetActive(false);
        }

    }



}

