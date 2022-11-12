using System;
using System.Collections.Generic;
using Photon.Pun;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation;

public class PlayerController : MonoBehaviour
{
    // [SerializeField] private float mouseSensitivity;
    // [SerializeField] private GameObject cameraHolder;
    private float _verticalRotation;
    private bool _grounded;
    private Vector3 _smoothMoveVelocity;
    private Vector3 _moveAmount;
    private PhotonView _pv;
    public SortedDictionary<int,List<GameObject>> MyMahjong;
    public bool isMyTurn = true;
    // [SerializeField]
    // public List<List<Mahjong>> UserMahjongLists;
    // [SerializeField]
    // public List<Mahjong> MahjongList ;
    public int playerID;
    public Vector3 putPos;
    public Vector3 putRotate;
    private Transform _mainCamera;
    private Transform XROriginTransform;
    private void Awake()
    {
        // MahjongList = new List<Mahjong>();
        // UserMahjongLists = new List<List<Mahjong>>();
        _pv = GetComponent<PhotonView>();
        MyMahjong = new SortedDictionary<int,List<GameObject>>();
        XROriginTransform = GameObject.Find("XR Origin").transform;
    }

    private void Update()
    {
        if (_pv.IsMine)
        {
            XROriginTransform.position = transform.position;
            XROriginTransform.rotation = transform.rotation;
        }
    }
    // private void Look()
    // {
    //     //GetAxisRaw返回1/-1，GetAxis返回类似加速度
    //     transform.Rotate(Vector3.up * (Input.GetAxisRaw("Mouse X") * mouseSensitivity));
    //     _verticalRotation += Input.GetAxisRaw("Mouse Y") * mouseSensitivity;
    //     _verticalRotation = Mathf.Clamp(_verticalRotation, -90f, 90f);
    //     cameraHolder.transform.localEulerAngles = Vector3.left * _verticalRotation;
    // }

    // public void SetUserMahjongLists(List<List<Mahjong>> userMahjongLists)
    // {
    //     UserMahjongLists = userMahjongLists;
    // }
    // public void SetMahjongList(List<Mahjong> mahjongList)
    // {
    //     MahjongList = mahjongList;
    // }
    // public List<List<Mahjong>> GetUserMahjongLists()
    // {
    //     return UserMahjongLists;
    // }
    // public List<Mahjong> GetMahjongList()
    // {
    //     return MahjongList;
    // }
}