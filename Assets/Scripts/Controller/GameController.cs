using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using Manager;
using Photon.Pun;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
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
        public Dictionary<int, int> ReadyDict;
        public bool canNext;
        public int nowTurn;
        public int nowTile;
        [HideInInspector] public GameObject tile;
        public Image bg;
        public TMP_Text text;
        public Button button;

        /// <summary>
        /// 初始化
        /// </summary>
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
            bg.gameObject.SetActive(false);
            playerCount = PhotonNetwork.CurrentRoom.PlayerCount;
            _gameManagerPhotonView = GameManager.instance.GetComponent<PhotonView>();
            canNext = true;
            ReadyDict = new Dictionary<int, int>();
        }

        /// <summary>
        /// 给button注册事件
        /// </summary>
        private void Start()
        {
            pongButton.onClick.AddListener(() =>
            {
                //得到出牌权（但是不发牌）
                _gameManagerPhotonView.RPC(nameof(GameManager.instance.NextTurn), RpcTarget.All,
                    myPlayerController.playerID, false);
                _gameManagerPhotonView.RPC(nameof(GameManager.instance.HideButton), RpcTarget.All);
                DestroyAndGenerate();
            });
            kongButton.onClick.AddListener(() =>
            {
                //得到出牌权（同时发牌）
                _gameManagerPhotonView.RPC(nameof(GameManager.instance.NextTurn), RpcTarget.All,
                    myPlayerController.playerID, true);
                _gameManagerPhotonView.RPC(nameof(GameManager.instance.HideButton), RpcTarget.All);
                DestroyAndGenerate();
            });
            skipButton.onClick.AddListener(() =>
            {
                _gameManagerPhotonView.RPC(nameof(GameManager.instance.NextTurn), RpcTarget.All,
                    nowTurn, true);
                _gameManagerPhotonView.RPC(nameof(GameManager.instance.HideButton),
                    RpcTarget.All);
            });
            winButton.onClick.AddListener(() =>
            {
                _gameManagerPhotonView.RPC(nameof(GameManager.instance.ShowResult),
                    RpcTarget.Others);
                text.text = "You Win!";
                bg.gameObject.SetActive(true);
                _gameManagerPhotonView.RPC(nameof(GameManager.instance.HideButton),
                    RpcTarget.All);
            });
            button.onClick.AddListener(() =>
            {
                Destroy(GameManager.instance.gameObject);
                PhotonNetwork.LeaveRoom();
            });
        }

        public override void OnLeftRoom()
        {
            PhotonNetwork.LoadLevel(0);
        }

        /// <summary>
        /// 开始游戏
        /// </summary>
        public void StartGame()
        {
            if (PhotonNetwork.IsMasterClient)
            {
                GetComponent<PhotonView>().RPC(nameof(NotMyTurn), RpcTarget.Others);
            }

            GeneratePlayers();
        }

        /// <summary>
        /// 生成n个玩家
        /// </summary>
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
                myPlayerController.putPos = GameManager.instance.GetPlayerPutPositions()[
                    myPlayerController.playerID - 1];
                myPlayerController.MyMahjong =
                    GameManager.instance.GenerateMahjongAtStart(player.Key - 1);
            }
        }

        /// <summary>
        /// 让主客户端每回合处理牌
        /// </summary>
        private void Update()
        {
            if (!PhotonNetwork.IsMasterClient) return;
            //所有玩家在某人打出牌之后向主客户端汇报自己的状态（能否碰/杠/胡牌）
            //当字典的count等于玩家count，主客户端开始处理，否则锁死所有客户端
            if (ReadyDict.Count != playerCount) return;
            var flag = false;
            foreach (var item in ReadyDict)
            {
                switch (item.Value)
                {
                    case 0:
                        continue;
                    //To Do:该客户端可以处理牌
                    //给他处理
                    //可以碰牌
                    case 1:
                        _gameManagerPhotonView.RPC(nameof(GameManager.instance.CanP), RpcTarget.All,
                            item.Key);
                        break;
                    //可以杠牌
                    case 2:
                        _gameManagerPhotonView.RPC(nameof(GameManager.instance.CanK), RpcTarget.All,
                            item.Key);
                        break;
                    //可以胡牌
                    case 3:
                        _gameManagerPhotonView.RPC(nameof(GameManager.instance.CanH), RpcTarget.All,
                            item.Key);
                        break;
                    case 4:
                        _gameManagerPhotonView.RPC(nameof(GameManager.instance.CanPAndK),
                            RpcTarget.All,
                            item.Key);
                        break;
                    //碰且赢
                    case 5:
                        _gameManagerPhotonView.RPC(nameof(GameManager.instance.CanPAndH),
                            RpcTarget.All,
                            item.Key);
                        break;
                    case 6:
                        _gameManagerPhotonView.RPC(nameof(GameManager.instance.CanKAndH),
                            RpcTarget.All,
                            item.Key);
                        break;
                    case 7:
                        _gameManagerPhotonView.RPC(nameof(GameManager.instance.CanPAndKAndH),
                            RpcTarget.All,
                            item.Key);
                        break;
                }

                //只要有一个人可以处理牌，就不应该继续发牌
                flag = true;
            }

            //清空字典，准备下一回合
            ReadyDict.Clear();
            //只要有一个人可以处理牌，就不应该继续发牌
            if (!flag)
            {
                //下一回合，给下一位发牌
                _gameManagerPhotonView.RPC(nameof(GameManager.instance.NextTurn), RpcTarget.All,
                    nowTurn, true);
            }
        }

        [PunRPC]
        public void NotMyTurn()
        {
            myPlayerController.isMyTurn = false;
        }

        public void SortMyMahjong()
        {
            var num = 1;
            foreach (var item in myPlayerController.MyMahjong)
            {
                foreach (var go in item.Value)
                {
                    var script = go.GetComponent<MouseEvent>();
                    if (!script.canPlay)
                    {
                        continue;
                    }

                    script.num = num++;
                    if (script.num == 0)
                    {
                        continue;
                    }

                    go.transform.DOMove(
                        GameManager.instance.GetPickPoses()[
                                myPlayerController.playerID - 1]
                            .position +
                        GameManager.instance.GetBias()[myPlayerController.playerID - 1] *
                        (script.num - 1), 1f);
                }
            }
        }

        private void DestroyAndGenerate()
        {
            foreach (var go in myPlayerController.MyMahjong[nowTile])
            {
                var script = go.GetComponent<MouseEvent>();
                script.canPlay = false;
                go.transform.DOMove(myPlayerController.putPos, 1f);
                go.transform.DORotate(
                    GameManager.instance.GetPlayerPutRotations()[
                        myPlayerController.playerID - 1], 1f);
                myPlayerController.putPos +=
                    GameManager.instance.GetBias()[myPlayerController.playerID - 1];
                script.num = 0;
            }

            var newGo = PhotonNetwork.Instantiate("mahjong_tile_" + nowTile,
                myPlayerController.putPos,
                Quaternion.Euler(GameManager.instance.GetPlayerPutRotations()[
                    myPlayerController.playerID - 1]));
            newGo.GetComponent<MouseEvent>().num = 0;
            myPlayerController.MyMahjong[nowTile].Add(newGo);
            SortMyMahjong();
            _gameManagerPhotonView.RPC(nameof(GameManager.instance.DestroyItem),
                RpcTarget.MasterClient);
        }

        // private enum OperationCode
        // {
        //     None = 0,
        //     Pong = 1,
        //     Kong = 2,
        //     Win = 3,
        //     PongAndKong = 4,
        //     PongAndWin = 5,
        //     KongAndWin = 6,
        //     PongAndKongAndWin = 7
        // }
        public int CheckMyState(int id)
        {
            var ans = 0;
            if (myPlayerController.MyMahjong.ContainsKey(id))
            {
                //可以碰
                if (myPlayerController.MyMahjong[id].Count == 2)
                {
                    ans = 1;
                }

                //可以杠
                if (myPlayerController.MyMahjong[id].Count == 3)
                {
                    if (ans == 1)
                    {
                        ans = 4;
                    }
                    else
                    {
                        ans = 2;
                    }
                }

                if (CheckWin(id))
                {
                    if (ans == 1)
                    {
                        ans = 5;
                    }
                    else if (ans == 2)
                    {
                        ans = 6;
                    }
                    else if (ans == 4)
                    {
                        ans = 7;
                    }
                    else
                    {
                        ans = 3;
                    }
                }
            }

            return ans;
        }

        private bool CheckWin(int id)
        {
            var cnt2 = 0;
            var cnt3 = 0;
            var cnt4 = 0;
            foreach (var item in myPlayerController.MyMahjong)
            {
                if (item.Key == id)
                {
                    if (item.Value.Count == 1)
                    {
                        cnt2++;
                    }
                    else if (item.Value.Count == 2)
                    {
                        cnt3++;
                    }
                    else if (item.Value.Count == 3)
                    {
                        cnt4++;
                    }
                }
                else
                {
                    if (item.Value.Count == 3)
                    {
                        cnt3++;
                    }
                    else if (item.Value.Count == 4)
                    {
                        cnt4++;
                    }
                }
            }

            return cnt2 + cnt3 + cnt4 == 5;
        }
    }
}