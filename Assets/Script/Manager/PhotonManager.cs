using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class PhotonManager : MonoBehaviourPunCallbacks
{
    // Start is called before the first frame update
    private void Start()
    {
        PhotonNetwork.ConnectUsingSettings(); // 서버 연결
    }

    public override void OnConnectedToMaster()
    {
        PhotonNetwork.JoinLobby(); // 로비 접속
    }
}
