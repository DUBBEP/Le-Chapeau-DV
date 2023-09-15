using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using System.Linq;
using UnityEditor;

public class GameManager : MonoBehaviourPunCallbacks
{
    [Header("Stats")]
    public bool gameEnded = false;
    public float TimeToDetonation;
    public float invincibleDuration;
    private float hatPickupTime;

    [Header("Player")]
    public string playerPrefabLocation;
    public Transform[] spawnPoints;
    public PlayerController[] players;
    public Material[] materials;
    public int playerWithHat;
    public int playersInGame;

    public static GameManager instance;
    public GameObject explosion;
    private void Awake()
    {
        instance = this;
    }

    private void Start()
    {
        players = new PlayerController[PhotonNetwork.PlayerList.Length];
        photonView.RPC("ImInGame", RpcTarget.AllBuffered);
    }

    [PunRPC]
    void ImInGame ()
    {
        playersInGame++;

        // when all players are in the scene - spawn the players
        if (playersInGame == PhotonNetwork.PlayerList.Length)
            SpawnPlayer();
    }

    // spawns a player and initializes it
    void SpawnPlayer ()
    {
        // instantiate the player across the network
        GameObject playerObj = PhotonNetwork.Instantiate(playerPrefabLocation, spawnPoints[Random.Range(0, spawnPoints.Length)].position, Quaternion.identity);
        
        // get the player script
        PlayerController playerScript = playerObj.GetComponent<PlayerController>();

        // initialize the player
        playerScript.photonView.RPC("Initialize", RpcTarget.All, PhotonNetwork.LocalPlayer);
    }

    public PlayerController GetPlayer (int playerId)
    {
        return players.First(x => x.id == playerId);
    }

    public PlayerController GetPlayer (GameObject playerObj)
    {
        return players.First(x => x.gameObject == playerObj);
    }

    public int GetActivePlayer()
    {
        PlayerController player;
        while (true)
        {
            player = GetPlayer(players[Random.Range(0, players.Length)].id);
            if (player.hasExploded == false)
            {
                return player.id;
            }
        }
    }

    // called when a player hits the hatted player - giving them the hat
    [PunRPC]
    public void GiveHat (int playerId, bool initialGive)
    {
        // remove the hat from the currently hatted player
        if (!initialGive)
            GetPlayer(playerWithHat).SetHat(false);

        // give the hat to the new player
        playerWithHat = playerId;
        GetPlayer(playerId).SetHat(true);
        hatPickupTime = Time.time;
    }

    public bool CanGetHat ()
    {
        if (Time.time > hatPickupTime + invincibleDuration)
            return true;
        else
            return false;
    }

    // When a player has reached max time eliminate them from the game
    [PunRPC]
    void EliminatePlayer (int playerId)
    {
        Debug.Log("Eliminate player ran in Game Manager");
        PlayerController player = GetPlayer(playerId);
        player.hasExploded = true;
        Object.Instantiate(explosion, player.gameObject.transform.position, Quaternion.identity);
        player.transform.position = new Vector3(1000,0,1000);
        playersInGame--;
    }

    [PunRPC]
    void WinGame ()
    {
        int playerId = GetActivePlayer();
        gameEnded = true;
        PlayerController player = GetPlayer(playerId);
        GameUI.instance.SetWinText(player.photonPlayer.NickName);

        Invoke("GoBackToMenu", 3.0f);
    }

    // called after the game has won - navigates back to Menu scene
    void GoBackToMenu ()
    {
        PhotonNetwork.LeaveRoom();
        NetworkManager.instance.ChangeScene("Menu");
        // Destroy network manager so there aren't duplicates in the menu scene
        Destroy(NetworkManager.instance.gameObject);
    }
}
