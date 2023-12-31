using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
public class PlayerController : MonoBehaviourPunCallbacks, IPunObservable
{
    [HideInInspector]
    public int id;

    [Header("Info")]
    public float moveSpeed;
    public float jumpForce;
    public GameObject hatObject;

    [HideInInspector]
    public float curHatTime;
    public bool hasExploded;

    [Header("Components")]
    public Rigidbody rig;
    public Player photonPlayer;

    [PunRPC]
    public void Initialize (Player player)
    {
        hasExploded = false;
        photonPlayer = player;
        id = player.ActorNumber;

        this.GetComponent<Renderer>().material = GameManager.instance.materials[Random.Range(0, GameManager.instance.materials.Length)];


        GameManager.instance.players[id - 1] = this;

        //  give the first player the hat
        if (id == 1)
            GameManager.instance.GiveHat(id, true);

        // if this isn't our local player, disable physics as that's
        // controlled by the user synced to all other clients
        if (!photonView.IsMine)
            rig.isKinematic = true;

    }

    private void Update()
    {
        if(PhotonNetwork.IsMasterClient)
        {
            if(curHatTime >= GameManager.instance.TimeToDetonation && !hasExploded)
            {
                hasExploded = true;
                GameManager.instance.photonView.RPC("EliminatePlayer", RpcTarget.All, id);
                if (GameManager.instance.playersInGame < 2)
                    GameManager.instance.photonView.RPC("WinGame", RpcTarget.All);
                else
                    GameManager.instance.photonView.RPC("GiveHat", RpcTarget.All, GameManager.instance.GetActivePlayer(), false);

                Debug.Log("Eliminated Player through character controller");
            }
        }

        if(photonView.IsMine)
        {
            Move();

            if(Input.GetKeyDown(KeyCode.Space))
                TryJump();

            // track the ammount of time we're wearing the hat
            if(hatObject.activeInHierarchy)
                curHatTime += Time.deltaTime;
        }

    }

    void Move()
    {
        float x = Input.GetAxis("Horizontal") * moveSpeed;
        float z = Input.GetAxis("Vertical") * moveSpeed;

        rig.velocity = new Vector3(x, rig.velocity.y, z);
    }

    void TryJump ()
    {
        Ray ray = new Ray(transform.position, Vector3.down);

        if (Physics.Raycast(ray, 0.7f))
        {
            rig.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        }
    }

    public void SetHat (bool hasHat)
    {
        Debug.Log("Setting hat");
        hatObject.SetActive(hasHat);
    }

    void OnCollisionEnter(Collision collision)
    {
        if (!photonView.IsMine)
            return;
        
        // did we hit another player?
        if(collision.gameObject.CompareTag("Player"))
        {
            //do they have the hat?
            if(GameManager.instance.GetPlayer(collision.gameObject).id == GameManager.instance.playerWithHat)
            {
                // can we get the hat?
                if(GameManager.instance.CanGetHat())
                {
                    //give us the hat
                    GameManager.instance.photonView.RPC("GiveHat", RpcTarget.All, id, false);
                }
            }
        }
    }

    public void OnPhotonSerializeView (PhotonStream stream, PhotonMessageInfo info)
    {
        if(stream.IsWriting)
        {
            stream.SendNext(curHatTime);
            
        }
        else if(stream.IsReading)
        {
            curHatTime = (float)stream.ReceiveNext();
        }
    }
}