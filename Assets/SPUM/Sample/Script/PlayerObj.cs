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
    }
    public void SetStateAnimationIndex(PlayerState state, int index = 0){
        IndexPair[state] = index;
    }
    public void PlayStateAnimation(PlayerState state){
        // StateAnimationPairs가 초기화되었는지 확인
        if(_prefabs.StateAnimationPairs == null || _prefabs.StateAnimationPairs.Count == 0)
        {
            Debug.LogWarning("StateAnimationPairs가 초기화되지 않았습니다. 기본 애니메이션을 재생합니다.");
            return;
        }
        
        // StateAnimationPairs에 키가 있는지 확인
        if(!_prefabs.StateAnimationPairs.TryGetValue(state.ToString(), out var animationList))
        {
            Debug.LogWarning($"StateAnimationPairs에 '{state}' 키가 없습니다!");
            Debug.LogWarning($"현재 등록된 키들: {string.Join(", ", _prefabs.StateAnimationPairs.Keys)}");
            return;
        }
        
        // 애니메이션 리스트가 비어있는지 확인
        if(animationList == null || animationList.Count == 0)
        {
            Debug.LogWarning($"'{state}' 상태의 애니메이션 리스트가 비어있습니다!");
            return;
        }
        
        _prefabs.PlayAnimation(state, IndexPair[state]);
    }
    void Update()
    {
        if(isAction) return;

        transform.position = new Vector3(transform.position.x,transform.position.y,transform.localPosition.y * 0.01f);
        
        // WASD 입력 처리 추가
        HandleWASDInput();
        
        switch(_currentState)
        {
            case PlayerState.IDLE:
            
            break;

            case PlayerState.MOVE:
            DoMove();
            break;
        }
        PlayStateAnimation(_currentState);

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
    
    /// <summary>
    /// WASD 키보드 입력을 처리하여 SPUM 이동 시스템과 연동
    /// </summary>
    void HandleWASDInput()
    {
        // Unity Input Manager 사용 (기본 설정)
        Vector2 inputVector = new Vector2(
            Input.GetAxisRaw("Horizontal"), // A(-1) / D(+1)
            Input.GetAxisRaw("Vertical")    // S(-1) / W(+1)
        );
        
        // 스페이스바 공격 (기존 기능 유지)
        if(Input.GetKeyDown(KeyCode.Space))
        {
            PlayAttack();
            return;
        }
        
        // 입력이 있을 때만 이동 처리
        if(inputVector.magnitude > 0.1f)
        {
            // 현재 위치에서 입력 방향으로 목표 지점 계산
            float moveDistance = 1.0f; // 한 번에 이동할 거리
            Vector3 targetPosition = transform.position + (Vector3)inputVector.normalized * moveDistance;
            
            // SPUM의 기존 SetMovePos 방식 활용
            SetMovePos(targetPosition);
        }
        else
        {
            // 입력이 없으면 현재 위치를 목표로 설정 (즉시 정지)
            if(_currentState == PlayerState.MOVE)
            {
                _goalPos = transform.position;
            }
        }
    }
    
    /// <summary>
    /// 공격 애니메이션 재생 (기존 기능)
    /// </summary>
    void PlayAttack()
    {
        if(_currentState == PlayerState.ATTACK) return;
        
        isAction = true;
        _currentState = PlayerState.ATTACK;
        PlayStateAnimation(_currentState);
        
        // 0.5초 후 공격 애니메이션 종료
        StartCoroutine(EndAttackAnimation());
    }
    
    /// <summary>
    /// 공격 애니메이션 종료 처리
    /// </summary>
    IEnumerator EndAttackAnimation()
    {
        yield return new WaitForSeconds(0.5f);
        isAction = false;
        _currentState = PlayerState.IDLE;
    }
}
