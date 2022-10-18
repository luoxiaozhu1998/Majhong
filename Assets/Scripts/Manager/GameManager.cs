using System;
using System.Collections.Generic;
using Controller;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Manager
{
    /// <summary>
    /// 游戏总管理类,外观模式+中介者模式
    /// </summary>
    public class GameManager : MonoBehaviourPunCallbacks
    {
        public static GameManager instance { get; private set; }

        private ResourceManager _resourceManager;

        private MenuManager _menuManager;

        private void Awake()
        {
            if (instance)
            {
                Destroy(gameObject);
            }

            DontDestroyOnLoad(gameObject);
            instance = this;
            _resourceManager = new ResourceManager();
            _menuManager = new MenuManager();
        }

        private void Start()
        {
            Screen.SetResolution(1024, 768, false);
        }

        public override void OnEnable()
        {
            base.OnEnable();
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        public override void OnDisable()
        {
            base.OnDisable();
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode loadSceneMode)
        {
            if (scene.buildIndex != 1) return; //we are in the game scene
            InitWhenStart();
            GameController.instance.StartGame();
            
        }

        #region PlayerManager

        #endregion

        #region ResourceManager

        public List<Mahjong> GetMahjongList()
        {
            return _resourceManager.GetMahjongList();
        }

        public void MahjongSplit(int count)
        {
            _resourceManager.MahjongSplit(count);
        }

        private void InitWhenStart()
        {
            _resourceManager.InitWhenStart();
        }

        public List<Transform> GetPickPoses()
        {
            return _resourceManager.GetPickPoses();
        }

        public SortedDictionary<int, List<GameObject>> GenerateMahjongAtStart(int id)
        {
            return _resourceManager.GenerateMahjongAtStart(id);
        }

        public GameObject GeneratePlayer(int id)
        {
            return _resourceManager.GeneratePlayer(id);
        }

        public Dictionary<string, GameObject> GetMenus()
        {
            return _resourceManager.Menus;
        }

        public List<Vector3> GetRotateList()
        {
            return _resourceManager.GetRotateList();
        }

        public List<Vector3> GetBias()
        {
            return _resourceManager.GetBias();
        }

        public List<Vector3> GetPlayerPutPositions()
        {
            return _resourceManager.GetPlayerPutPositions();
        }

        public List<Vector3> GetPlayerPutRotations()
        {
            return _resourceManager.GetPlayerPutRotations();
        }

        #endregion

        #region MenuManager

        public void OpenMenu(string menuName)
        {
            _menuManager.OpenMenu(menuName);
        }

        public void CloseMenu(string menuName)
        {
            _menuManager.CloseMenu(menuName);
        }

        #endregion


        /// <summary>
        /// 给出牌权
        /// </summary>
        /// <param name="nextUserId">下一个出牌用户编号</param>
        /// <param name="drawTile">给不给他发牌</param>
        [PunRPC]
        public void NextTurn(int nextUserId, bool drawTile)
        {
            if (GameController.instance.myPlayerController.playerID == nextUserId)
            {
                GameController.instance.myPlayerController.isMyTurn = true;
                if (drawTile)
                {
                    var go = PhotonNetwork.Instantiate(GetMahjongList()[0].Name,
                        GameObject.Find("NewPos" + nextUserId).transform.position,
                        Quaternion.Euler(GetRotateList()[nextUserId - 1]));
                    var newScript = go.GetComponent<MouseEvent>();
                    newScript.canPlay = true;
                    var myMahjong = GameController.instance.myPlayerController.MyMahjong;
                    newScript.id = GetMahjongList()[0].ID;
                    if (!myMahjong.ContainsKey(newScript.id))
                    {
                        myMahjong[newScript.id] = new List<GameObject>();
                    }

                    myMahjong[newScript.id].Add(go);
                    var idx = 1;
                    foreach (var item in myMahjong)
                    {
                        foreach (var iGameObject in item.Value)
                        {
                            var script = iGameObject.GetComponent<MouseEvent>();
                            if (script.num == 0 || !script.canPlay)
                            {
                                continue;
                            }

                            script.num = idx++;
                        }
                    }

                    if (myMahjong[newScript.id].Count == 4)
                    {
                        photonView.RPC(nameof(CanK), RpcTarget.All,
                            GameController.instance.myPlayerController.playerID);
                    }
                }
            }
            else
            {
                GameController.instance.myPlayerController.isMyTurn = false;
            }

            if (drawTile)
            {
                GetMahjongList().RemoveAt(0);
            }
        }

        [PunRPC]
        public void PlayTile(int id, int playerID)
        {
            //每个客户端先把把当前轮次的ID设置好（下面代码可能会更改）
            GameController.instance.nowTurn = playerID == PhotonNetwork.CurrentRoom.PlayerCount
                ? 1
                : playerID + 1;
            //每个客户端先把把当前轮次的牌ID设置好（下面代码可能会更改）
            GameController.instance.nowTile = id;
            var thisID = GameController.instance.myPlayerController.playerID;
            //打出牌的一定准备好了
            if (playerID == thisID)
            {
                //是主客户端，直接加入
                if (PhotonNetwork.IsMasterClient)
                {
                    GameController.instance.ReadyDict.Add(playerID, 0);
                }
                //向主客户端发送自己的状态
                else
                {
                    photonView.RPC(nameof(Send), RpcTarget.MasterClient, playerID, 0);
                }
            }
            else
            {
                //check自己的状态
                var flag = GameController.instance.CheckMyState(id);
                //是主客户端，直接加入
                if (PhotonNetwork.IsMasterClient)
                {
                    GameController.instance.ReadyDict.Add(
                        GameController.instance.myPlayerController.playerID, flag);
                }
                //向主客户端发送自己的状态
                else
                {
                    photonView.RPC(nameof(Send), RpcTarget.MasterClient,
                        GameController.instance.myPlayerController.playerID, flag);
                }
            }
        }

        [PunRPC]
        public void Send(int id, int flag)
        {
            GameController.instance.ReadyDict.Add(id, flag);
        }


        [PunRPC]
        public void CanNext(bool flag)
        {
            GameController.instance.canNext = flag;
        }

        /// <summary>
        /// 可以碰牌的客户端
        /// </summary>
        /// <param name="id">客户端id</param>
        [PunRPC]
        public void CanP(int id)
        {
            if (id != GameController.instance.myPlayerController.playerID) return;
            GameController.instance.pongButton.gameObject.SetActive(true);
            GameController.instance.skipButton.gameObject.SetActive(true);
        }

        /// <summary>
        /// 可以杠牌的客户端
        /// </summary>
        /// <param name="id">客户端id</param>
        [PunRPC]
        public void CanK(int id)
        {
            if (id != GameController.instance.myPlayerController.playerID) return;
            GameController.instance.pongButton.gameObject.SetActive(true);
            GameController.instance.kongButton.gameObject.SetActive(true);
            GameController.instance.skipButton.gameObject.SetActive(true);
        }

        /// <summary>
        /// 可以胡牌的客户端
        /// </summary>
        /// <param name="id">客户端id</param>
        [PunRPC]
        public void CanH(int id)
        {
            if (id != GameController.instance.myPlayerController.playerID) return;
            GameController.instance.winButton.gameObject.SetActive(true);
            GameController.instance.skipButton.gameObject.SetActive(true);
        }

        [PunRPC]
        public void CanPAndK(int id)
        {
            if (id != GameController.instance.myPlayerController.playerID) return;
            GameController.instance.pongButton.gameObject.SetActive(true);
            GameController.instance.kongButton.gameObject.SetActive(true);
            GameController.instance.skipButton.gameObject.SetActive(true);
        }

        [PunRPC]
        public void CanPAndH(int id)
        {
            if (id != GameController.instance.myPlayerController.playerID) return;
            GameController.instance.pongButton.gameObject.SetActive(true);
            GameController.instance.winButton.gameObject.SetActive(true);
            GameController.instance.skipButton.gameObject.SetActive(true);
        }

        [PunRPC]
        public void CanKAndH(int id)
        {
            if (id != GameController.instance.myPlayerController.playerID) return;
            GameController.instance.kongButton.gameObject.SetActive(true);
            GameController.instance.winButton.gameObject.SetActive(true);
            GameController.instance.skipButton.gameObject.SetActive(true);
        }

        [PunRPC]
        public void CanPAndKAndH(int id)
        {
            if (id != GameController.instance.myPlayerController.playerID) return;
            GameController.instance.pongButton.gameObject.SetActive(true);
            GameController.instance.kongButton.gameObject.SetActive(true);
            GameController.instance.winButton.gameObject.SetActive(true);
            GameController.instance.skipButton.gameObject.SetActive(true);
        }

        [PunRPC]
        public void HideButton()
        {
            GameController.instance.pongButton.gameObject.SetActive(false);
            GameController.instance.kongButton.gameObject.SetActive(false);
            GameController.instance.skipButton.gameObject.SetActive(false);
        }

        [PunRPC]
        public void StoreTile(GameObject go)
        {
            GameController.instance.tile = go;
        }

        [PunRPC]
        public void DestroyItem()
        {
            PhotonNetwork.Destroy(GameController.instance.tile);
        }

        [PunRPC]
        public void ShowResult(int id)
        {
            GameController.instance.text.text = "You Lose!";
            GameController.instance.bg.gameObject.SetActive(true);
        }

        public void JoinLobby()
        {
            Launcher.Instance.JoinLobby();
        }
    }
}