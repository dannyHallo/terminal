using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(AudioClip))]
public class Pickable : MonoBehaviour
{
    public PlayerController.InstrumentTypes instrumentType;
    public UIManager UIManager;
    private void Start()
    {
        UIManager = FindObjectOfType<UIManager>();
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
        UIManager.pickUpUI.SetActive(true);
    }
    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.tag != "Player")
        {
            return;
        }
        UIManager.pickUpUI.SetActive(false);

    }
    private void OnTriggerStay(Collider other)
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            GameObject player = GameObject.Find("Player");
            // PlayerController.InstrumentTypes instrumentType = PlayerController.InstrumentTypes.Guitar;
            List<PlayerController.enumToInstrument> playerInstuments = player.GetComponent<PlayerController>().instruments;
            for (int i=0; i < playerInstuments.Count; i ++)
            {
                if (playerInstuments[i].e == instrumentType)
                {
                    PlayerController.enumToInstrument instrumentSetting= playerInstuments[i];
                    instrumentSetting.have = true;
                    playerInstuments[i] = instrumentSetting;
                }
            }
            
            player.GetComponent<PlayerController>().UseInstrument(instrumentType);

            Destroy(gameObject);
            UIManager.pickUpUI.SetActive(false);
        }

    }



}

