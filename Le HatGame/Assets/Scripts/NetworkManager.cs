using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class NetworkManager : MonoBehaviourPunCallbacks
{
    // instance
    public static NetworkManager instance;

    private void Awake()
    {
        if (instance != null && instance != this)
            gameObject.SetActive(false);
        else
        {
            // set the instance
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        
    }
    void Start()
    {
        if(!PhotonNetwork.IsConnected)
            PhotonNetwork.ConnectUsingSettings();
    }

    // attempts to CREATE a room
    public void CreateRoom (string roomName)
    {
        PhotonNetwork.CreateRoom(roomName);
    }

    public override void OnConnectedToMaster()
    {
        Debug.Log("Connected to master server");
    }

    // attempts to JOIN a room
    public void JoinRoom (string roomName)
    {
        PhotonNetwork.JoinRoom(roomName);
    }

    // changes the scene using Photon's system
    [PunRPC]
    public void ChangeScene (string sceneName)
    {
        PhotonNetwork.LoadLevel(sceneName);
    }
}
