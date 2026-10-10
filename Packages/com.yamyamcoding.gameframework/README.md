# Game Framework (YamYamCoding Edition)

Unity용 모듈형 게임 프레임워크. Procedure / FSM, Entity, UI, Event, ObjectPool, ReferencePool, Sound, Scene, Setting, DataTable, Localization, Network 등을 제공한다.

[Game Framework](https://github.com/EllanJiang/GameFramework) 와 [Unity Game Framework](https://github.com/EllanJiang/UnityGameFramework) (Jiang Yin, MIT) 를 기반으로 하며, 이 에디션은 다음이 다르다.

- 리소스 파이프라인을 **Addressables** 로 교체했다.
- 코어를 미리 빌드된 DLL 이 아니라 **소스로 포함**해 디버깅과 수정이 가능하다.
- Unity 6 에서 동작한다.

## 요구 사항

| 항목 | 값 |
|---|---|
| Unity | 6000.0 이상 (검증은 6000.6 에서만 했다) |
| Addressables | 2.9.0 이상 — 자동 설치 |
| SharpZipLib | `com.unity.sharp-zip-lib` 1.4.2 이상 — 자동 설치 |

## 설치

**Package Manager → `+` → Add package from git URL** 에 아래를 넣는다.

```
https://github.com/<owner>/<repo>.git?path=Packages/com.yamyamcoding.gameframework
```

또는 이 폴더를 프로젝트의 `Packages/` 아래에 그대로 복사한다.

## 구성

| 경로 | 어셈블리 | 내용 |
|---|---|---|
| `Runtime/Core` | `GameFramework` | 엔진에 의존하지 않는 코어. `UnityEngine` 을 참조하지 않는다 |
| `Runtime/UnityGameFramework` | `UnityGameFramework.Runtime` | Unity 연동 계층. `~Component`, Helper, `EntityLogic`, `UIFormLogic` |

## 게임 코드에서 지킬 것

프레임워크는 게임 코드(`Assembly-CSharp`)와 **다른 어셈블리**다.

- `EntityLogic` / `UIFormLogic` / `ProcedureBase` / `FsmState<T>` 의 훅은 **`protected override`** 로 오버라이드한다.
  `protected internal override` 는 컴파일 에러(CS0507)다.
- 엔티티 프리팹에는 `EntityLogic` 을 붙이지 않는다(런타임에 붙는다). UI 폼 프리팹에는 `UIFormLogic` 을 **반드시** 붙인다.
- 엔티티는 풀에서 재사용된다. Unity 의 `Awake` / `Start` 대신 `OnInit` / `OnShow` / `OnHide` 를 쓴다.
- 프리팹의 Addressables 주소는 `ShowEntity` / `OpenUIForm` 에 넘기는 문자열과 정확히 같아야 한다.

## 라이선스

MIT. `LICENSE.md` 와 `Third Party Notices.md` 를 볼 것.
원저작자 표기(각 소스 파일 헤더 포함)는 재배포 조건이므로 지우면 안 된다.
