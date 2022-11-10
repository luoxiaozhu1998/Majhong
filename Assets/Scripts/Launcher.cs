using System.Collections.Generic;
using System.Linq;
using Manager;
using Photon.Pun;
using TMPro;
using UnityEngine;
using Photon.Realtime;


public class Launcher : MonoBehaviourPunCallbacks
{
    [SerializeField] private TMP_InputField roomNameInputField;
    [SerializeField] private TMP_Text errorText;
    [SerializeField] private TMP_Text roomNameText;
    [SerializeField] private Transform roomListContent;
    [SerializeField] private Transform playerListContent;
    [SerializeField] private GameObject roomLIstItemPrefab;
    [SerializeField] private GameObject playerLIstItemPrefab;
    [SerializeField] private GameObject startRoomButton;

    public static Launcher Instance;

    private void Awake()
    {
        Instance = this;
    }

    // Start is called before the first frame update
    private void Start()
    {
        
    }

    public override void OnConnectedToMaster()
    {
        Debug.Log("OnConnectedToMaster()");
        PhotonNetwork.JoinLobby();
        PhotonNetwork.AutomaticallySyncScene = true;
    }

    public void JoinLobby()
    {
        GameManager.Instance.OpenMenu("LoadingMenu");
        PhotonNetwork.ConnectUsingSettings();
    }
    public override void OnJoinedLobby()
    {
        GameManager.Instance.OpenMenu("TitleMenu");
        Debug.Log("OnJoinedLobby()");
        PhotonNetwork.NickName = "Player" + Random.Range(0, 1000).ToString("0000");
    }

    public void CreateRoom()
    {
        if (string.IsNullOrEmpty(roomNameInputField.text))
        {
            return;
        }
        PhotonNetwork.CreateRoom(roomNameInputField.text, new RoomOptions {MaxPlayers = 4});
        GameManager.Instance.OpenMenu("LoadingMenu");
    }

    public override void OnJoinedRoom()
    {
        roomNameText.text = PhotonNetwork.CurrentRoom.Name;
        GameManager.Instance.OpenMenu("RoomMenu");
        foreach (Transform item in playerListContent)
        {
            Destroy(item.gameObject);
        }

        var players = PhotonNetwork.PlayerList;
        foreach (var t in players)
        {
            Instantiate(playerLIstItemPrefab, playerListContent).GetComponent<PlayerListItem>()
                .Setup(t);
        }
        startRoomButton.SetActive(PhotonNetwork.IsMasterClient);
    }

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        errorText.text = "Room Creation Failed" + message;
        GameManager.Instance.OpenMenu("ErrorMenu");
    }

    public void LeaveRoom()
    {
        PhotonNetwork.LeaveRoom();
        GameManager.Instance.OpenMenu("LoadingMenu");
    }

    public override void OnLeftRoom()
    {
        GameManager.Instance.OpenMenu("TitleMenu");
    }

    public override void OnRoomListUpdate(List<RoomInfo> roomList)
    {
        if (roomList.Count <= 0) return;
        foreach (Transform t in roomListContent)
        {
            Destroy(t.gameObject);
        }

        foreach (var info in roomList.Where(info => !info.RemovedFromList))
        {
            Instantiate(roomLIstItemPrefab, roomListContent).GetComponent<RoomListItem>()
                .SetUp(info);
        }
    }

    public void JoinRoom(RoomInfo info)
    {
        GameManager.Instance.OpenMenu("LoadingMenu");
        PhotonNetwork.JoinRoom(info.Name);
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        Instantiate(playerLIstItemPrefab, playerListContent)
            .GetComponent<PlayerListItem>()
            .Setup(newPlayer);
    }

    public void StartGame()
    {
        GameManager.Instance.OpenMenu("LoadingMenu");
        PhotonNetwork.LoadLevel(1);
    }

    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        startRoomButton.SetActive(PhotonNetwork.IsMasterClient);
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
Application.Quit();
#endif
    }
}