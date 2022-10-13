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

        public SortedDictionary<int,List<GameObject>> GenerateMahjongAtStart(int id)
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
        
        [PunRPC]
        public void NextTurn(int userId)
        {
            if (GameController.instance.myPlayerController.playerID == userId)
            {
                GameController.instance.myPlayerController.isMyTurn = true;
                var go = PhotonNetwork.Instantiate(GetMahjongList()[0].Name,
                    GameObject.Find("NewPos" + userId).transform.position,
                    Quaternion.Euler(GetRotateList()[userId - 1]));
                go.GetComponent<MouseEvent>().id = GetMahjongList()[0].ID;
                if (!GameController.instance.myPlayerController.MyMahjong
                        .ContainsKey(go.GetComponent<MouseEvent>().id))
                {
                    GameController.instance.myPlayerController
                        .MyMahjong[go.GetComponent<MouseEvent>().id] = new List<GameObject>();

                }
                GameController.instance.myPlayerController
                    .MyMahjong[go.GetComponent<MouseEvent>().id].Add(go);
                var flag = true;
                var idx = 1;
                foreach (var item in GameController.instance.myPlayerController.MyMahjong)
                {
                    foreach (var iGameObject in item.Value)
                    {
                        iGameObject.GetComponent<MouseEvent>().num = idx++;
                    }
                }
            }
            else
            {
                GameController.instance.myPlayerController.isMyTurn = false;
            }

            GetMahjongList().RemoveAt(0);
        }

        [PunRPC]
        public void PlayTile(int id,int playerID)
        {
            //每个客户端先把把当前轮次的ID设置好（下面代码可能会更改）
            GameController.instance.nowTurn = playerID == PhotonNetwork.CurrentRoom.PlayerCount
                ? 1
                : playerID + 1;
            //每个客户端先把把当前轮次的牌ID设置好（下面代码可能会更改）
            GameController.instance.nowTile = id;
            var thisID = GameController.instance.myPlayerController.playerID;
            //打出牌的一定准备好了
            if (playerID==thisID)
            {
                //是主客户端，直接加入
                if (PhotonNetwork.IsMasterClient)
                {
                    GameController.instance.ReadyList.Add(playerID,0);
                }
                //向主客户端发送自己的状态
                else
                {
                    photonView.RPC(nameof(Send), RpcTarget.MasterClient, playerID,0);
                }
            }
            else
            {
                //check自己的状态
                var flag= GameController.instance.CheckMyState(id);
                //是主客户端，直接加入
                if (PhotonNetwork.IsMasterClient)
                {
                    GameController.instance.ReadyList.Add(GameController.instance.myPlayerController.playerID,flag);
                }
                //向主客户端发送自己的状态
                else
                {
                    photonView.RPC(nameof(Send), RpcTarget.MasterClient, GameController.instance.myPlayerController.playerID,flag);
                }
            }
        }

        [PunRPC]
        public void Send(int id,int flag)
        {
            GameController.instance.ReadyList.Add(id, flag);
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
    }
}