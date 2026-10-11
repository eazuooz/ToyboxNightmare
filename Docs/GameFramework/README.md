# Game Framework (YamYamCoding Edition)

Unity용 모듈형 게임 프레임워크. Procedure / FSM, Entity, UI, Event, ObjectPool, ReferencePool, Sound, Scene, Setting, DataTable, Localization, Network 등을 제공한다.

[Game Framework](https://github.com/EllanJiang/GameFramework) 와 [Unity Game Framework](https://github.com/EllanJiang/UnityGameFramework) (Jiang Yin, MIT) 를 기반으로 하며, 이 에디션은 다음이 다르다.

- 리소스 파이프라인을 **Addressables** 로 교체했다.
- Unity 6 에서 동작한다.

## 구성

세 층이다. 코어는 Unity 와 무관한 별도 .NET 어셈블리이고, Unity Game Framework 가 그것을 Unity 에 연결하며, 게임은 그 둘을 쓴다.

| 경로 | 내용 |
|---|---|
| `Assets/Plugins/GameFramework.dll` | **Game Framework (코어).** 엔진에 의존하지 않는다. 별도 C# 프로젝트(netstandard2.1)를 빌드한 결과물 |
| `Assets/Scripts/` | **Unity Game Framework.** `~Component`, Helper, `EntityLogic`, `UIFormLogic` |
| `Assets/Scripts/Editor/` | 커스텀 인스펙터와 `Game Framework` 메뉴 |
| `Assets/Prefabs/GameFramework.prefab` | 프레임워크 컴포넌트가 모두 붙은 프리팹. 씬에 하나만 둔다 |

## 요구 사항

| 항목 | 값 |
|---|---|
| Unity | 6000.6 에서 검증했다 |
| Addressables | `com.unity.addressables` — `.unitypackage` 임포트 시 자동 설치 |
| SharpZipLib | `com.unity.sharp-zip-lib` — `.unitypackage` 임포트 시 자동 설치 |

## 시작하기

1. `.unitypackage` 를 임포트한다. Package Manager 의존성 설치를 묻으면 수락한다.
2. 씬에 `Assets/Prefabs/GameFramework.prefab` 을 놓는다.
3. `GameFramework.Procedure.ProcedureBase` 를 상속한 절차(Procedure)를 하나 만들고, 프리팹의 **Procedure** 오브젝트 인스펙터에서 체크한 뒤 진입 절차(Entrance Procedure)로 고른다.
4. 메뉴 **Game Framework → Log Scripting Define Symbols → Enable All Logs** 로 프레임워크 로그를 켠다.
   켜지 않으면 `Log.Info` / `Log.Error` 가 컴파일에서 빠져 아무것도 찍히지 않는다.

## 게임 코드에서 지킬 것

- `EntityLogic` / `UIFormLogic` 의 훅은 **`protected internal override`** 로 오버라이드한다(프레임워크와 게임 코드가 같은 어셈블리다).
- 엔티티 프리팹에는 `EntityLogic` 을 붙이지 않는다(런타임에 붙는다). UI 폼 프리팹에는 `UIFormLogic` 을 **반드시** 붙인다.
- 엔티티는 풀에서 재사용된다. Unity 의 `Awake` / `Start` 대신 `OnInit` / `OnShow` / `OnHide` 를 쓴다.
- 프리팹의 Addressables 주소는 `ShowEntity` / `OpenUIForm` 에 넘기는 문자열과 정확히 같아야 한다.

## 라이선스

MIT. `LICENSE.md` 와 `Third Party Notices.md` 를 볼 것.
원저작자 표기(각 소스 파일 헤더 포함)는 재배포 조건이므로 지우면 안 된다.
