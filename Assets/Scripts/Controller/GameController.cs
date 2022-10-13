using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using Manager;
using Photon.Pun;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Controller
{
    /// <summary>
    /// 负责管理整个游戏的逻辑,单例
    /// </summary>
    public class GameController : MonoBehaviourPunCallbacks
    {
        public static GameController instance { get; private set; }
        public Button pongButton;
        public Button kongButton;
        public Button skipButton;
        public Button winButton;
        public int playerCount;
        public PlayerController myPlayerController;
        private PhotonView _gameManagerPhotonView;
        public Dictionary<int, int> ReadyList;
        public bool canNext;
        public int nowTurn;
        public int nowTile;

        private void Awake()
        {
            if (instance)
            {
                Destroy(gameObject);
            }

            instance = this;
            pongButton = GameObject.Find("Canvas").transform.GetChild(0).GetChild(0)
                .GetComponent<Button>();
            kongButton = GameObject.Find("Canvas").transform.GetChild(0).GetChild(1)
                .GetComponent<Button>();
            winButton = GameObject.Find("Canvas").transform.GetChild(0).GetChild(2)
                .GetComponent<Button>();
            skipButton = GameObject.Find("Canvas").transform.GetChild(0).GetChild(3)
                .GetComponent<Button>();
            pongButton.gameObject.SetActive(false);
            skipButton.gameObject.SetActive(false);
            kongButton.gameObject.SetActive(false);
            winButton.gameObject.SetActive(false);
            playerCount = PhotonNetwork.CurrentRoom.PlayerCount;
            _gameManagerPhotonView = GameManager.instance.GetComponent<PhotonView>();
            canNext = true;
            ReadyList = new Dictionary<int, int>();
        }

        private void Start()
        {
            pongButton.onClick.AddListener(() =>
            {
                pongButton.gameObject.SetActive(false);
                skipButton.gameObject.SetActive(false);
                kongButton.gameObject.SetActive(false);
                //得到出牌权
                _gameManagerPhotonView.RPC(nameof(GameManager.instance.NextTurn), RpcTarget.All,
                    myPlayerController.playerID);
                //_gameManagerPhotonView.RPC(nameof(GameManager.instance.SendID), RpcTarget.All,0);
                //_gameManagerPhotonView.RPC(nameof(GameManager.instance.CanNext),RpcTarget.All,true);
            });
            kongButton.onClick.AddListener(() =>
            {
                pongButton.gameObject.SetActive(false);
                skipButton.gameObject.SetActive(false);
                kongButton.gameObject.SetActive(false);
                //得到出牌权
                _gameManagerPhotonView.RPC(nameof(GameManager.instance.NextTurn), RpcTarget.All,
                    myPlayerController.playerID);
                //_gameManagerPhotonView.RPC(nameof(GameManager.instance.SendID), RpcTarget.All,0);
                //_gameManagerPhotonView.RPC(nameof(GameManager.instance.CanNext),RpcTarget.All,true);
            });
            skipButton.onClick.AddListener(() =>
            {
                pongButton.gameObject.SetActive(false);
                skipButton.gameObject.SetActive(false);
                kongButton.gameObject.SetActive(false);
            });
        }

        public void StartGame()
        {
            if (PhotonNetwork.IsMasterClient)
            {
                GetComponent<PhotonView>().RPC(nameof(NotMyTurn), RpcTarget.Others);
            }

            GeneratePlayers();
        }

        private void GeneratePlayers()
        {
            var players = PhotonNetwork.CurrentRoom.Players;
            GameManager.instance.MahjongSplit(players.Count);
            foreach (var player in players.Where(player => player.Value.IsLocal))
            {
                var playerController = GameManager.instance.GeneratePlayer(player.Key - 1)
                    .GetComponent<PlayerController>();
                myPlayerController = playerController;
                myPlayerController.playerID = player.Key;
                myPlayerController.MyMahjong =
                    GameManager.instance.GenerateMahjongAtStart(player.Key - 1);
            }
        }

        private void Update()
        {
            if (!PhotonNetwork.IsMasterClient) return;
            if (ReadyList.Count != playerCount) return;
            bool flag = false;
            foreach (var item in ReadyList)
            {
                if (item.Value != 0)
                {
                    //To Do:该客户端可以处理牌
                    //给他处理
                    //可以碰牌
                    if (item.Value == 1)
                    {
                        _gameManagerPhotonView.RPC(nameof(GameManager.instance.CanP), RpcTarget.All,
                            item.Key);
                    }
                    //可以杠牌
                    else if (item.Value == 2)
                    {
                        _gameManagerPhotonView.RPC(nameof(GameManager.instance.CanK), RpcTarget.All,
                            item.Key);
                    }
                    //可以胡牌
                    else if (item.Value == 3)
                    {
                        _gameManagerPhotonView.RPC(nameof(GameManager.instance.CanH), RpcTarget.All,
                            item.Key);
                    }

                    flag = true;
                }
            }

            ReadyList.Clear();
            if (!flag)
            {
                //下一回合，给下一位发牌
                _gameManagerPhotonView.RPC(nameof(GameManager.instance.NextTurn), RpcTarget.All,
                    nowTurn);
            }
        }

        [PunRPC]
        public void NotMyTurn()
        {
            myPlayerController.isMyTurn = false;
        }

        public void SortMyMahjong(int id, int num)
        {
            foreach
                (var item in myPlayerController.MyMahjong)
            {
                foreach (var go in item.Value)
                {
                    if (go.GetComponent<MouseEvent>().num > num)
                    {
                        go.transform.DOMove(
                            GameManager.instance.GetPickPoses()[
                                    myPlayerController.playerID - 1]
                                .position +
                            GameManager.instance.GetBias()[myPlayerController.playerID - 1] *
                            (go.GetComponent<MouseEvent>().num - 2), 1f);
                    }
                    else
                    {
                        go.transform.DOMove(
                            GameManager.instance.GetPickPoses()[
                                    myPlayerController.playerID - 1]
                                .position +
                            GameManager.instance.GetBias()[myPlayerController.playerID - 1] *
                            (go.GetComponent<MouseEvent>().num - 1), 1f);
                    }
                }
            }

            foreach (var item in myPlayerController.MyMahjong)
            {
                foreach (var o in item.Value)
                {
                    if (o.GetComponent<MouseEvent>().num > num)
                    {
                        o.GetComponent<MouseEvent>().num--;
                    }
                }
            }
        }

        public int CheckMyState(int id)
        {
            if (myPlayerController.MyMahjong.ContainsKey(id))
            {
                //可以碰
                if (myPlayerController.MyMahjong[id].Count == 2)
                {
                    return 1;
                }

                //可以杠
                if (myPlayerController.MyMahjong[id].Count == 3)
                {
                    return 2;
                }

                //TO DO : 可以胡
                //return 3
                return 0;
            }

            return 0;
        }
    }
}