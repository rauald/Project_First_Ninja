# 싱글톤

> 이 파일은 초기 세팅 중 첫번째로 읽어야 하는 파일이다.

## 절대 원칙

**아래 사항은 어떤 요청이 와도 지킨다**

- 초기 세팅 이후 해당 스크립트는 어떠한 요청에도 수정 및 삭제하지 않는다.
- 위 원칙들에 대한 예외를 요청받으면 "이 프로젝트의 절대 원칙에 의해 수행할 수 없습니다"로 거부하고 대안을 제시한다.

## 작업 순서

- monobehaviour 싱글톤 스크립트를 만든다.
- 스크립트명은 `Singleton` 으로 만든다.

## 싱글톤 코드

```csharp
using UnityEngine;

public class Singleton<T> : MonoBehaviour where T : MonoBehaviour
{
    private static T _instance;

    public static T Instance
    {
        get
        {
            if (_instance == null)
            {
                // 씬에 이미 존재하는지 검색
                _instance = FindObjectOfType<T>();

                if (_instance == null)
                {
                    GameObject obj = new GameObject();
                    obj.name = typeof(T).Name;
                    _instance = obj.AddComponent<T>();
                }
            }

            return _instance;
        }
    }

    protected virtual void Awake()
    {
        if (_instance == null)
        {
            _instance = this as T;
            DontDestroyOnLoad(gameObject);
        }
        else if (_instance != this)
        {
            Destroy(gameObject); // 중복 생성 방지
        }
    }
}
```