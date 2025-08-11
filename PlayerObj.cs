using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;


public class PlayerObj : MonoBehaviour
{
    public SPUM_Prefabs _prefabs;
    public float _charMS;
    private PlayerState _currentState;

    public Vector3 _goalPos;
    public bool isAction = false;
    public Dictionary<PlayerState, int> IndexPair = new ();
    
    // 직접 이동을 위한 새로운 변수들
    [Header("Direct Movement Settings")]
    public bool useDirectMovement = true;
    private Rigidbody2D rb;
    private Vector2 inputDirection;
    void Start()
    {
        if(_prefabs == null )
        {
            _prefabs = transform.GetChild(0).GetComponent<SPUM_Prefabs>();
            if(!_prefabs.allListsHaveItemsExist()){
                _prefabs.PopulateAnimationLists();
            }
        }
        _prefabs.OverrideControllerInit();
        foreach (PlayerState state in Enum.GetValues(typeof(PlayerState)))
        {
            IndexPair[state] = 0;
        }
        
        // Rigidbody2D 컴포넌트 가져오기 (직접 이동용)
        rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
        }
        
        // Rigidbody2D 설정
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
    }
    public void SetStateAnimationIndex(PlayerState state, int index = 0){
        IndexPair[state] = index;
    }
    public void PlayStateAnimation(PlayerState state){
        _prefabs.PlayAnimation(state, IndexPair[state]);
    }
    void Update()
    {
        if(isAction) return;

        if (useDirectMovement)
        {
            HandleDirectMovement();
        }
        else
        {
            // 기존 방식 (목표점 이동)
            transform.position = new Vector3(transform.position.x,transform.position.y,transform.localPosition.y * 0.01f);
            switch(_currentState)
            {
                case PlayerState.IDLE:
                
                break;

                case PlayerState.MOVE:
                DoMove();
                break;
            }
        }
        
        PlayStateAnimation(_currentState);
    }
    
    void HandleDirectMovement()
    {
        // 키보드 입력 받기
        inputDirection.x = Input.GetAxisRaw("Horizontal");
        inputDirection.y = Input.GetAxisRaw("Vertical");
        
        // 대각선 이동시 속도 정규화
        if (inputDirection.magnitude > 1f)
        {
            inputDirection = inputDirection.normalized;
        }
        
        // Rigidbody2D로 직접 이동
        rb.linearVelocity = inputDirection * _charMS;
        
        // 상태 업데이트
        if (inputDirection.magnitude > 0.1f)
        {
            _currentState = PlayerState.MOVE;
            
            // 스프라이트 방향 설정
            if (inputDirection.x > 0)
                _prefabs.transform.localScale = new Vector3(-1, 1, 1);
            else if (inputDirection.x < 0)
                _prefabs.transform.localScale = new Vector3(1, 1, 1);
        }
        else
        {
            _currentState = PlayerState.IDLE;
        }
        
        // Z position 설정 (SPUM 에셋 특성)
        transform.position = new Vector3(transform.position.x, transform.position.y, transform.position.y * 0.01f);
    }

    void DoMove()
    {
        Vector3 _dirVec  = _goalPos - transform.position ;
        Vector3 _disVec = (Vector2)_goalPos - (Vector2)transform.position ;
        if( _disVec.sqrMagnitude < 0.1f )
        {
            _currentState = PlayerState.IDLE;
            return;
        }
        Vector3 _dirMVec = _dirVec.normalized;
        transform.position += _dirMVec * _charMS * Time.deltaTime;
        

        if(_dirMVec.x > 0 ) _prefabs.transform.localScale = new Vector3(-1,1,1);
        else if (_dirMVec.x < 0) _prefabs.transform.localScale = new Vector3(1,1,1);
    }

    public void SetMovePos(Vector2 pos)
    {
        isAction = false;
        _goalPos = pos;
        _currentState = PlayerState.MOVE;
    }
}
