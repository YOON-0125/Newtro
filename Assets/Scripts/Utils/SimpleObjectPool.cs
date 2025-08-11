using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 간단한 오브젝트 풀 (싱글톤)
/// </summary>
public class SimpleObjectPool : MonoBehaviour
{
    public static SimpleObjectPool Instance { get; private set; }

    private class PoolMember : MonoBehaviour
    {
        public int key;
    }

    private readonly Dictionary<int, Stack<GameObject>> pool = new Dictionary<int, Stack<GameObject>>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// 풀에서 오브젝트 가져오기 (없으면 Instantiate)
    /// </summary>
    public GameObject Get(GameObject prefab, Vector3 pos, Quaternion rot)
    {
        if (prefab == null) return null;

        int key = prefab.GetInstanceID();
        if (!pool.TryGetValue(key, out Stack<GameObject> stack))
        {
            stack = new Stack<GameObject>();
            pool[key] = stack;
        }

        GameObject obj;
        if (stack.Count > 0)
        {
            obj = stack.Pop();
            obj.transform.SetPositionAndRotation(pos, rot);
            obj.SetActive(true);
        }
        else
        {
            obj = Instantiate(prefab, pos, rot);
            var member = obj.GetComponent<PoolMember>();
            if (member == null) member = obj.AddComponent<PoolMember>();
            member.key = key;
        }
        return obj;
    }

    /// <summary>
    /// 오브젝트 반환 (지연 지원)
    /// </summary>
    public void Release(GameObject go, float delay = 0f)
    {
        if (go == null) return;
        StartCoroutine(ReleaseRoutine(go, delay));
    }

    private IEnumerator ReleaseRoutine(GameObject go, float delay)
    {
        if (delay > 0f) yield return new WaitForSeconds(delay);
        if (go == null) yield break;
        var member = go.GetComponent<PoolMember>();
        if (member == null)
        {
            Destroy(go);
            yield break;
        }
        go.SetActive(false);
        if (!pool.TryGetValue(member.key, out Stack<GameObject> stack))
        {
            stack = new Stack<GameObject>();
            pool[member.key] = stack;
        }
        stack.Push(go);
    }
}
