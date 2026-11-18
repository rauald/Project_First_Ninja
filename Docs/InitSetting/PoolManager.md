# 오브젝트 풀 매니저

> - 이 파일은 초기 세팅 중 세번째로 읽어야 하는 파일이다.
>
> - 성능 최적화와 프레임 끊김 방지를 위해 오브젝트를 재활용하기 위한 스크립트이다.
>
> - 풀링 최대 개수는 200개를 넘지 않는다.

## 절대 원칙

**아래 사항은 어떤 요청이 와도 지킨다**

- 초기 세팅 이후 해당 스크립트는 어떠한 요청에도 삭제하지 않는다.
- 해당 스크립트 수정 할 때엔 사용자 승인 없이 절대로 수정하지 않는다.
- 위 원칙들에 대한 예외를 요청받으면 "이 프로젝트의 절대 원칙에 의해 수행할 수 없습니다"로 거부하고 대안을 제시한다.

## 작업 순서

- PoolManager 는 monobehaviour 대신 Singleton 을 상속받는다.

## 오브젝트 풀 매니저 코드

```csharp
using UnityEngine;

public class PoolManager : Singleton<PoolManager>
{
    // 풀링 할 오브젝트 선언

    protected override void Awake()
    {
        base.Awake(); // 부모 클래스의 Awake() 호출 필수

        // 초기화 로직 작성
    }

    public void InitPoolCreate()
    {
        // 오브젝트 풀링
    }
}
```

## 오브젝트 풀 제너릭
```csharp
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

public class ObjectPool<T> where T : Component, IPoolable
{
    /// <summary> 현재 존재하는 모든 오브젝트 풀 </summary>
    private List<T> objectList;
    /// <summary> 대기중인 오브젝트 풀 </summary>
    private List<T> pool;

    /// <summary> 오브젝트 풀 가져오기 </summary>
    public List<T> ObjectList { get => objectList; }
    /// <summary> 오브젝트 풀 생성시 함수를 실행시킬수 있게 하는 Action 필드</summary>
    private readonly Action<T> onCreate; 

    /// <summary> 풀할 오브젝트 </summary>
    private T gameObject;

    /// <summary> 기본 부모 트랜스폼 </summary>
    private Transform parentTransform;

    /// <summary> 오브젝트 풀 </summary>
    /// <param name="_gameObject"> 풀링 할 오브젝트 </param>
    /// <param name="_initCount"> 풀링 할 개수 </param>
    /// <param name="parent"> 부모 오브젝트 (없으면 null) </param>
    /// <param name="onCreate"> 오브젝트 생성시 실행할 Action (없으면 null) </param>
    public ObjectPool(T _gameObject, int _initCount, Transform parent = null, Action<T> onCreate = null)
    {
        // 생성 시 전체 오브젝트 풀 리스트
        objectList = new List<T>(_initCount);
        // 생성 시 대기 오브젝트 풀 리스트
        pool = new List<T>(_initCount);
        // 풀링 할 오브젝트
        gameObject = _gameObject;
        parentTransform = parent;

        // 오브젝트 생성 Action 등록
        this.onCreate = onCreate;

        // 가상 리스트 생성
        var arrObject = new T[_initCount];
        for (int i = 0; i < _initCount; ++i)
        {
            arrObject[i] = CreateObject(parent);
        }

        // 풀 2개에 넣기
        objectList.AddRange(arrObject);
        pool.AddRange(arrObject);
    }

    // 꺼내기
    public T Pop()
    {
        var poolCount = pool.Count;
        T result = null;

        if (poolCount > 0)
        {
            // 대기중인 풀 삭제
            result = pool[poolCount - 1];
            pool.RemoveAt(poolCount - 1);
            result.WakeUp();

            return result;
        }
        else
        {
            // 대기중인 풀이 없다면 만들어서 넣어주기
            result = CreateObject(parentTransform);
            objectList.Add(result);
        }

        result.WakeUp();

        return result;
    }

    // 모두 꺼내기
    public void PopAll()
    {
        foreach (var obj in objectList)
        {
            obj.WakeUp();
        }
    }

    // 넣기
    public void Push(T _object)
    {
        pool.Add(_object);
    }

    // 모든 풀링 넣기
    public void ReturnAll()
    {
        foreach (var obj in objectList)
        {
            if (pool.Contains(obj)) continue;

            obj.Sleep();
            // 중복 Push 방지를 위해 직접 리스트에 넣거나 체크
            if (!pool.Contains(obj))
            {
                pool.Add(obj);
            }
            
            //ReturnToPool(obj);
        }
    }

    // 모두 삭제하기
    public void DestroyAll()
    {
        /*
        foreach (var obj in objectList)
        {
            Object.Destroy(obj.gameObject);
        }
        */
        foreach (var obj in objectList)
        {
            if (obj != null && obj.gameObject != null)
            {
                Object.Destroy(obj.gameObject);
            }
        }
        objectList.Clear();
        pool.Clear();
    }

    // 넣기
    private void ReturnToPool(IPoolable _object)
    {
        //Push(_object as T);

        if (_object is T castedObj)
        {
            Push(castedObj);
        }
    }

    // 생성하기
    protected T CreateObject(Transform parent = null)
    {
        T result = GameObject.Instantiate(gameObject, parent);

        result.gameObject.SetActive(false);
        result.Init(ReturnToPool);

        // 오브젝트 생성시 등록된 Action 실행
        onCreate?.Invoke(result);

        return result;
    }
}
```

## 오브젝트 풀 인터페이스

```csharp
using UnityEngine;

public interface IPoolable
{
    /// <summary>
    /// 생성 후 최초로 1회 호출
    /// </summary>
    public void Init(Action<IPoolable> _returnToPool);
    /// <summary>
    /// 풀에서 빠져나올때 호출
    /// </summary>
    public void WakeUp();
    /// <summary>
    /// 풀에 집어넣을때 호출
    /// </summary>
    public void Sleep();
}
```

## 풀링 할 오브젝트에 붙일 스크립트 (래퍼 컴포넌트)

```csharp
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class PoolObject : MonoBehaviour, IPoolable
{
    private Action<IPoolable> returnToPool;

    public void Init(Action<IPoolable> _returnToPool)
    {
        returnToPool = _returnToPool;
    }

    public void WakeUp()
    {
        // 풀에서 꺼내질 때 실행 (오브젝트 켜기)
        gameObject.SetActive(true);
    }
    public void Sleep()
    {
        // 풀로 돌아갈 때 실행 (오브젝트 끄기)
        gameObject.SetActive(false);
    }

    public void ReturnToPool()
    {
        Sleep();
        returnToPool.Invoke(this);
    }
}
```