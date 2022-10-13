using System.Collections.Generic;
using Controller;
using DG.Tweening;
using Manager;
using Photon.Pun;
using UnityEngine;

public class MouseEvent : MonoBehaviourPunCallbacks
{
    private readonly Vector3 _pos = Vector3.zero;
    private PhotonView _photonView;
    public int id;
    public int num;
    private PhotonView _gameManagerPhotonView;
    private void Awake()
    {
        _photonView = gameObject.GetComponent<PhotonView>();
        _gameManagerPhotonView = GameManager.instance.GetComponent<PhotonView>();
    }

    private void OnMouseEnter()
    {
        if (!_photonView.IsMine) return;
        var transform1 = transform;
        transform1.localScale = new Vector3(3f, 3f, 3f);
        transform1.localPosition += new Vector3(0f, 1f, 0f);
    }

    private void OnMouseExit()
    {
        if (!_photonView.IsMine) return;
        var transform1 = transform;
        transform1.localScale = new Vector3(2f, 2f, 2f);
        transform1.localPosition -= new Vector3(0f, 1f, 0f);
    }

    private void OnMouseDown()
    {
        if (!_photonView.IsMine) return;
        if (!GameController.instance.myPlayerController.isMyTurn) return;
        var transform1 = transform;
        transform1.DOMove(Vector3.zero, 1f);
        //_gameManagerPhotonView.RPC(nameof(GameManager.instance.SendID), RpcTarget.Others,id);
        if (GameController.instance.myPlayerController.MyMahjong[id].Count == 1)
        {
            GameController.instance.myPlayerController.MyMahjong.Remove(id);
        }
        else
        {
            GameObject t = null;
            foreach (var iGameObject in GameController.instance.myPlayerController.MyMahjong[id])
            {
                if (iGameObject.GetComponent<MouseEvent>().num == num)
                {
                    t = iGameObject;
                    
                }
            }
            if (t != null)
            {
                GameController.instance.myPlayerController.MyMahjong[id].Remove(t);
            }
        }
        GameController.instance.SortMyMahjong(id,num);
        var next = GameController.instance.myPlayerController.playerID + 1 > GameController.instance.playerCount
            ? 1 : GameController.instance.myPlayerController.playerID + 1;
        //我打出一张牌
        _gameManagerPhotonView.RPC(nameof(GameManager.instance.PlayTile), RpcTarget.All, id,
            GameController.instance.myPlayerController.playerID);
        GameController.instance.NotMyTurn();
    }
}