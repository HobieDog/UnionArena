using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine.SceneManagement;

public class MatchmakingManager : MonoBehaviourPunCallbacks
{
    private void Start()
    {
        PhotonNetwork.AutomaticallySyncScene = true; // 씬 자동 동기화 활성화
    }
    public void StartGame()
    {
        if (!PhotonNetwork.IsConnected)
        {
            Debug.LogError("Photon 서버에 연결되지 않았습니다!");
            return;
        }

        PhotonNetwork.JoinRandomRoom(); // 랜덤 매칭 시도
    }

    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        PhotonNetwork.CreateRoom(null, new RoomOptions { MaxPlayers = 2 });
    }

    public override void OnJoinedRoom()
    {
        if (PhotonNetwork.CurrentRoom.PlayerCount == 2 && PhotonNetwork.IsMasterClient)
            PhotonNetwork.LoadLevel("GameScene");
        else
            return;
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        Debug.Log($"🚀 새로운 플레이어 입장: {newPlayer.NickName}, 현재 인원: {PhotonNetwork.CurrentRoom.PlayerCount}");

        if (PhotonNetwork.CurrentRoom.PlayerCount == 2 && PhotonNetwork.IsMasterClient)
        {
            PhotonNetwork.LoadLevel("GameScene");
        }
    }
}
