using System.Collections.Generic;
using Controller;
using DG.Tweening;
using Manager;
using Photon.Pun;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;

public class MouseEvent : MonoBehaviourPunCallbacks
{
    private readonly Vector3 _pos = Vector3.zero;
    private PhotonView _photonView;
    public int id;
    public int num;
    private PhotonView _gameManagerPhotonView;
    public bool canPlay = true;

    private void Awake()
    {
        _photonView = gameObject.GetComponent<PhotonView>();
        _gameManagerPhotonView = GameManager.Instance.GetComponent<PhotonView>();
        id = int.Parse(name[..^7][13..]);
        GetComponent<XRGrabInteractable>().hoverEntered.AddListener(_ => { OnHover(); });
        GetComponent<XRGrabInteractable>().hoverExited.AddListener(_ => { OnHoverExit(); });
        GetComponent<XRGrabInteractable>().activated.AddListener(_ => { OnTrigger(); });
    }

    private void OnHover()
    {
        if (!_photonView.IsMine) return;
        if (!canPlay) return;
        var transform1 = transform;
        //transform1.localScale = new Vector3(3f, 3f, 3f);
        transform1.localPosition += new Vector3(0f, 1f, 0f);
    }

    private void OnHoverExit()
    {
        if (!_photonView.IsMine) return;
        if (!canPlay) return;
        var transform1 = transform;
        //transform1.localScale = new Vector3(2f, 2f, 2f);
        transform1.localPosition -= new Vector3(0f, 1f, 0f);
    }

    private void OnTrigger()
    {
        if (!_photonView.IsMine) return;
        if (!canPlay) return;
        if (!GameController.Instance.myPlayerController.isMyTurn) return;
        GetComponent<BoxCollider>().isTrigger = true;
        var transform1 = transform;
        transform1.DOMove(Vector3.zero, 1f);
        //_gameManagerPhotonView.RPC(nameof(GameManager.instance.SendID), RpcTarget.Others,id);
        if (GameController.Instance.myPlayerController.MyMahjong[id].Count == 1)
        {
            GameController.Instance.myPlayerController.MyMahjong.Remove(id);
        }
        else
        {
            GameObject t = null;
            foreach (var iGameObject in GameController.Instance.myPlayerController.MyMahjong[id])
            {
                if (iGameObject.GetComponent<MouseEvent>().num == num)
                {
                    t = iGameObject;
                }
            }

            if (t != null)
            {
                GameController.Instance.myPlayerController.MyMahjong[id].Remove(t);
            }
        }

        GameController.Instance.SortMyMahjong();
        //我打出一张牌
        _gameManagerPhotonView.RPC(nameof(GameManager.Instance.PlayTile), RpcTarget.All, id,
            GameController.Instance.myPlayerController.playerID);
        GameController.Instance.NotMyTurn();
        //_gameManagerPhotonView.RPC(nameof(GameManager.instance.StoreTile), RpcTarget.MasterClient, gameObject);
        GameController.Instance.tile = gameObject;
    }
}