using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;
using UnityEngine.Serialization;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private float mouseSensitivity;
    [SerializeField] private GameObject cameraHolder;
    private float _verticalRotation;
    private bool _grounded;
    private Vector3 _smoothMoveVelocity;
    private Vector3 _moveAmount;
    private PhotonView _pv;
    public SortedDictionary<int,List<GameObject>> MyMahjong;
    public bool isMyTurn = true;
    public int playerID;
    public Vector3 putPos;
    public Vector3 putRotate;
    private void Awake()
    {
        _pv = GetComponent<PhotonView>();
        MyMahjong = new SortedDictionary<int,List<GameObject>>();
        
    }

    private void Look()
    {
        //GetAxisRaw返回1/-1，GetAxis返回类似加速度
        transform.Rotate(Vector3.up * (Input.GetAxisRaw("Mouse X") * mouseSensitivity));
        _verticalRotation += Input.GetAxisRaw("Mouse Y") * mouseSensitivity;
        _verticalRotation = Mathf.Clamp(_verticalRotation, -90f, 90f);
        cameraHolder.transform.localEulerAngles = Vector3.left * _verticalRotation;
    }

    private void Start()
    {
        if (_pv.IsMine) return;
        
        Destroy(GetComponentInChildren<Camera>().gameObject);
    }

    
}