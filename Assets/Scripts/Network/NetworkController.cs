using UnityEngine;
using Mirror;

public class NetworkController : MonoBehaviour
{
    [SerializeField] Transform playerTrans1;
    [SerializeField] Transform playerTrans2;
    NetworkManager networkManager;

    private void Awake()
    {
        networkManager = GetComponent<NetworkManager>();
        NetworkManager.RegisterStartPosition(playerTrans1);
        NetworkManager.RegisterStartPosition(playerTrans2);
    }


}

