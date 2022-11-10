﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Photon.Pun;
using Tools;
using UnityEngine;

namespace Manager
{
    public class ResourceManager
    {
        /// <summary>
        /// 加载的所有麻将
        /// </summary>
        private readonly List<Mahjong> _mahjongList = new();

        /// <summary>
        /// 每个玩家的麻将
        /// </summary>
        private readonly List<List<Mahjong>> _userMahjongLists = new();

        /// <summary>
        /// 每个玩家发牌位置
        /// </summary>
        private readonly List<Transform> _pickPoses = new();

        private readonly string _playerControllerPath =
            Path.Combine("PhotonPrefabs", "PlayerController");

        private readonly List<Vector3> _bias;
        private readonly List<Vector3> _rotate;
        private readonly List<Vector3> _playerInitRotations;
        private readonly List<Vector3> _playerInitPositions;
        private readonly List<Vector3> _playerPutPositions;
        private readonly List<Vector3> _playerPutRotations;
        public readonly Dictionary<string, GameObject> Menus;

        public ResourceManager()
        {
            for (var j = 0; j < Constants.MaxPlayer; j++)
            {
                for (var i = 1; i <= Constants.MaxId; i++)
                {
                    _mahjongList.Add(
                        new Mahjong(i, "mahjong_tile_" + i));
                }
            }

            _mahjongList = _mahjongList.OrderBy(_ => Guid.NewGuid()).ToList();

            Menus = new Dictionary<string, GameObject>
            {
                {"LoadingMenu", GameObject.Find("LoadingMenu")},
                {"TitleMenu", GameObject.Find("TitleMenu")},
                {"CreateRoomMenu", GameObject.Find("CreateRoomMenu")},
                {"RoomMenu", GameObject.Find("RoomMenu")},
                {"ErrorMenu", GameObject.Find("ErrorMenu")},
                {"FindRoomMenu", GameObject.Find("FindRoomMenu")},
                {"StartMenu",GameObject.Find("StartMenu")}
            };

            _bias = new List<Vector3>
            {
                new(0f, 0f, 3f),
                new(-3f, 0f, 0f),
                new(0f, 0f, -3f),
                new(3f, 0f, 0f)
            };

            _rotate = new List<Vector3>
            {
                new(90f, 90f, 0f),
                new(90f, 0f, 0f),
                new(90f, -90f, 0f),
                new(90f, 180f, 0f)
            };

            _playerInitRotations = new List<Vector3>
            {
                new(10f, -90f, 0f),
                new(10f, 180f, 0f),
                new(10f, 90f, 0f),
                new(10f, 0f, 0f)
            };

            _playerInitPositions = new List<Vector3>
            {
                new(66f, 15f, 0),
                new(0f, 15f, 66f),
                new(-66f, 15f, 0f),
                new(0f, 15f, -66f)
            };
            _playerPutPositions = new List<Vector3>
            {
                new (45f,1f,-15f),
                new (15f,1f,45f),
                new (-45f,1f,15f),
                new (-15f,1f,-45f)
            };
            _playerPutRotations = new List<Vector3>
            {
                new(0f, 90f, 0f),
                new(0f, 0f, 0f),
                new(0f, -90f, 0f),
                new(0f, 180f, 0f)
            };
        }

        public void InitWhenStart()
        {
            for (var i = 1; i <= Constants.MaxPlayer; i++)
            {
                _pickPoses.Add(GameObject.Find("PickPos" + i).transform);
            }
        }

        public List<Vector3> GetRotateList()
        {
            return _rotate;
        }
        public List<Vector3> GetBias()
        {
            return _bias;
        }
        public List<Transform> GetPickPoses()
        {
            return _pickPoses;
        }

        public List<Vector3> GetPlayerPutPositions()
        {
            return _playerPutPositions;
        }
        public List<Vector3> GetPlayerPutRotations()
        {
            return _playerPutRotations;
        }
        /// <summary>
        /// 分割麻将为count组,第一组14个,后count-1组13个
        /// </summary>
        /// <param name="count"></param>
        public void MahjongSplit(int count)
        {
            for (var i = 1; i <= count; i++)
            {
                _userMahjongLists.Add(_mahjongList.Take(i == 1 ? 14 : 13)
                    .OrderBy(a => a.ID).ToList());
                _mahjongList.RemoveRange(0, i == 1 ? 14 : 13);
            }
        }

        /// <summary>
        /// 获取当前还未发出的全部麻将的列表
        /// </summary>
        /// <returns></returns>
        public List<Mahjong> GetMahjongList()
        {
            return _mahjongList;
        }

        /// <summary>
        /// 生成编号为id的玩家
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public GameObject GeneratePlayer(int id)
        {
            return PhotonNetwork.Instantiate(_playerControllerPath,
                _playerInitPositions[id],
                Quaternion.Euler(_playerInitRotations[id]));
        }

        /// <summary>
        /// 生成编号为id的玩家的麻将并且返回麻将id数组
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public SortedDictionary<int,List<GameObject>> GenerateMahjongAtStart(int id)
        {
            var pos = _pickPoses[id].position;
            var count = id == 0 ? 14 : 13;
            var ret = new SortedDictionary<int,List<GameObject>>();
            for (var i = 0; i < count; i++)
            {
                var go = PhotonNetwork.Instantiate(
                    _userMahjongLists[id][i].Name, pos,
                    Quaternion.Euler(_rotate[id]));
                var script = go.GetComponent<MouseEvent>();
                script.id = _userMahjongLists[id][i].ID;
                pos += _bias[id];
                go.transform.localScale = new Vector3(2f, 2f, 2f);
                if (!ret.ContainsKey(_userMahjongLists[id][i].ID))
                {
                    ret[_userMahjongLists[id][i].ID] = new List<GameObject>();
                }
                ret[_userMahjongLists[id][i].ID].Add(go);
                script.num = i + 1;
                script.canPlay = true;
            }
            return ret;
        }
        
    }
}