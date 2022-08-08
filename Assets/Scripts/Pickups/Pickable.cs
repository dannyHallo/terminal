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
    public StageManagement stageManagement;
    public int _instrumentInt;

    public UIManager UIManager
    {
        get
        {
            return GameObject.Find("Canvas").GetComponent<UIManager>();
        }
    }

    private void Start()
    {
        stageManagement = FindObjectOfType<StageManagement>();
    }

    private void Update()
    {

    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.tag != "Player")
            return;

        UIManager.hintText.text = "Press E to Pickup";
    }
    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.tag != "Player")
            return;

        UIManager.hintText.text = "";
    }
    private void OnTriggerStay(Collider other)
    {

        if (other.gameObject.tag != "Player")
        { return; }

        if (Input.GetKeyDown(KeyCode.E))
        {
            if (instrumentType == PlayerController.InstrumentTypes.Guitar)
            {
                stageManagement.StageSwitch(5);
            }
            if (instrumentType == PlayerController.InstrumentTypes.Sax)
            {
                stageManagement.StageSwitch(1);
            }
            if (instrumentType == PlayerController.InstrumentTypes.Dudelsa)
            {
                stageManagement.StageSwitch(3);
            }
            if (instrumentType == PlayerController.InstrumentTypes.Mic)
            {
                stageManagement.StageSwitch(7);
            }
            GameObject player = GameObject.Find("Player");

            player.GetComponent<PlayerController>().EquipInstrument(instrumentType);
            player.GetComponent<PlayerController>().TryUseInstrument(instrumentType);

            Destroy(this.gameObject);
            UIManager.hintText.text = "";
        }

    }



}

