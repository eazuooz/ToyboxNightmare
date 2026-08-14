using GameFramework.Event;
using UnityEngine;
using UnityGameFramework.Runtime;

namespace ToyBoxNightmare
{
    public class SurvivalGame : GameBase
    {
        // ─── 설정 ───

        // 캐릭터 키와 Addressables 주소는 같은 문자열이다("Girl" 주소로 로드한 엔티티의 키가 "Girl").
        // 둘이 갈라지면 SpawnSelectCharacter 에 주소를 따로 받아야 한다.
        private const string GirlCharacterKey = "Girl";
        private const string BoyCharacterKey  = "Boy";

        /// <summary>플레이어·선택 캐릭터가 들어가는 엔티티 그룹. GameFramework.prefab 의 mEntityGroups 와 일치해야 한다.</summary>
        private const string PlayerEntityGroup = "Player";

        /// <summary>
        /// EntityData 의 TypeId. 아직 타입 테이블이 없어 프로젝트 전체가 1 로 고정이다.
        /// <see cref="EnemySpawner"/> 도 같은 값으로 적을 띄우므로 public 이다.
        /// </summary>
        public const int DefaultEntityTypeId = 1;

        /// <summary>선택 화면에서 두 캐릭터가 서는 자리.</summary>
        private static readonly Vector3 GirlSelectPosition = new Vector3(-2f, 0f, 0f);
        private static readonly Vector3 BoySelectPosition  = new Vector3( 2f, 0f, 0f);

        // ─── 싱글턴 ───
        public static SurvivalGame Instance { get; private set; }

        // ─── 런타임 상태 ───
        private Player            mPlayer     = null;
        private PlayerSelectLogic mGirlSelect = null;
        private PlayerSelectLogic mBoySelect  = null;

        // CharacterSelected 는 선택 순간 스스로 해제하므로, Shutdown 시점에 아직
        // 구독 중인지 알아야 한다. 미등록 핸들러를 Unsubscribe 하면 코어가 예외를 던진다.
        private bool  mCharacterSelectSubscribed = false;
        private float mGameTime = 0f;

        // ─── 스포너 ───
        // 적 스폰은 통째로 EnemySpawner 가 맡는다. 여기서는 매 프레임 굴려 주고,
        // 엔티티 표시 성공/실패 콜백만 중계한다.
        private readonly EnemySpawner mSpawner = new EnemySpawner();

        public override GameMode GameMode => GameMode.Survival;

        /// <summary>누적 점수. 적 처치로 오른다.</summary>
        public int Score { get; private set; }

        /// <summary>게임 시작(프로시저 진입) 이후 경과 시간. 스포너 난이도 곡선에 쓴다.</summary>
        public float GameTime => mGameTime;

        /// <summary>
        /// 현재 플레이어. 선택 전이거나 이미 회수된 뒤에는 null.
        /// 엔티티는 풀에서 재사용되므로 참조가 살아 있어도 Available 을 확인해야 한다.
        /// </summary>
        public Player Player => (mPlayer != null && mPlayer.Available) ? mPlayer : null;

        // ─── 초기화 ───

        public override void Initialize()
        {
            base.Initialize();

            Instance = this;

            // 스폰 테이블 정합성은 판이 시작될 때 한 번만 본다.
            // [Conditional] 이라 릴리스 빌드에서는 이 호출 자체가 사라진다.
            EnemyTable.Validate();

            ResetRoundState();
            SubscribeEvents();
            SpawnSelectCharacters();
        }

        public override void Shutdown()
        {
            UnsubscribeEvents();

            Instance = null;
            base.Shutdown();
        }

        /// <summary>
        /// 프로시저 재진입(재시작)으로 새 인스턴스가 만들어지므로 상태를 명시적으로 초기화한다.
        /// </summary>
        private void ResetRoundState()
        {
            Score       = 0;
            mGameTime   = 0f;
            mPlayer     = null;
            mGirlSelect = null;
            mBoySelect  = null;

            mSpawner.Reset();
        }

        private void SubscribeEvents()
        {
            // 초기화 시점에 EventComponent 가 없으면 게임이 성립하지 않는다 — 시끄럽게 알린다.
            EventComponent events = RequireEventComponent();
            if (events == null) return;

            events.Subscribe(ShowEntitySuccessEventArgs.EventId, OnShowEntitySuccess);
            events.Subscribe(ShowEntityFailureEventArgs.EventId, OnShowEntityFailure);
            events.Subscribe(EnemyDiedEventArgs.EventId,         OnEnemyDied);
            events.Subscribe(PlayerDiedEventArgs.EventId,        OnPlayerDied);

            events.Subscribe(CharacterSelectedEventArgs.EventId, OnCharacterSelected);
            mCharacterSelectSubscribed = true;
        }

        private void UnsubscribeEvents()
        {
            // 앱 종료 경로에서는 EventComponent 가 먼저 파괴됐을 수 있다.
            // 그때는 구독도 함께 사라진 뒤라 조용히 빠져나가는 게 맞다 — 여기서 로그를 남기면
            // 정상 종료마다 에러가 찍힌다.
            EventComponent events = FindEventComponent();
            if (events == null) return;

            events.Unsubscribe(ShowEntitySuccessEventArgs.EventId, OnShowEntitySuccess);
            events.Unsubscribe(ShowEntityFailureEventArgs.EventId, OnShowEntityFailure);
            events.Unsubscribe(EnemyDiedEventArgs.EventId,         OnEnemyDied);
            events.Unsubscribe(PlayerDiedEventArgs.EventId,        OnPlayerDied);

            // 캐릭터를 고르지 않고 종료하면 아직 구독 상태다.
            UnsubscribeCharacterSelected();
        }

        private void UnsubscribeCharacterSelected()
        {
            if (!mCharacterSelectSubscribed) return;

            // Shutdown 경로에서도 불리므로 조용한 쪽을 쓴다(UnsubscribeEvents 주석 참조).
            EventComponent events = FindEventComponent();
            if (events == null) return;

            events.Unsubscribe(CharacterSelectedEventArgs.EventId, OnCharacterSelected);
            mCharacterSelectSubscribed = false;
        }

        public override void Update(float elapseSeconds, float realElapseSeconds)
        {
            base.Update(elapseSeconds, realElapseSeconds);

            if (GameOver) return;

            // 캐릭터 선택 중에는 시계를 세워 둔다. 선택 시점부터가 '게임 시작'이다.
            if (Player == null) return;

            mGameTime += elapseSeconds;

            mSpawner.OnUpdate(elapseSeconds);
        }

        // ─── 점수 ───

        /// <summary>점수를 더하고 ScoreChanged 를 발행한다.</summary>
        public void AddScore(int delta)
        {
            if (delta == 0) return;

            Score += delta;

            EventComponent events = RequireEventComponent();
            if (events == null)
            {
                // Acquire 한 인자를 Fire 하지 못하면 풀에 돌아가지 못하므로, 여기서 만들지 않는다.
                return;
            }

            events.Fire(this, ScoreChangedEventArgs.Create(Score, delta));
        }

        // ─── 스폰 ───

        private void SpawnSelectCharacters()
        {
            SpawnSelectCharacter(GirlCharacterKey, GirlSelectPosition);
            SpawnSelectCharacter(BoyCharacterKey,  BoySelectPosition);
        }

        /// <summary>
        /// 캐릭터 선택용 엔티티를 스폰한다.
        /// 캐릭터 키가 곧 Addressables 주소라 인자 하나로 둘 다 쓴다.
        /// </summary>
        private void SpawnSelectCharacter(string characterKey, Vector3 position)
        {
            EntityComponent entityComponent = GameEntry.GetComponent<EntityComponent>();
            if (entityComponent == null)
            {
                Log.Error("EntityComponent 를 찾을 수 없어 선택 캐릭터 '{0}' 를 스폰하지 못했다.", characterKey);
                return;
            }

            int id = EntitySerialId.Next();
            entityComponent.ShowEntity(
                id,
                typeof(PlayerSelectLogic),
                characterKey,
                PlayerEntityGroup,
                CharacterSelectData.Create(id, DefaultEntityTypeId, characterKey, position));
        }

        private void SpawnPlayer(string characterKey)
        {
            EntityComponent entityComponent = GameEntry.GetComponent<EntityComponent>();
            if (entityComponent == null)
            {
                Log.Error("EntityComponent 를 찾을 수 없어 플레이어 '{0}' 를 스폰하지 못했다.", characterKey);
                return;
            }

            int id = EntitySerialId.Next();
            entityComponent.ShowEntity(
                id,
                typeof(Player),
                characterKey,
                PlayerEntityGroup,
                PlayerData.Create(id, DefaultEntityTypeId));
        }

        // ─── 이벤트 핸들러 ───

        private void OnCharacterSelected(object sender, GameEventArgs e)
        {
            CharacterSelectedEventArgs ne = e as CharacterSelectedEventArgs;
            if (ne == null)
            {
                GameAssert.Unreachable("CharacterSelected 핸들러에 다른 타입의 이벤트 인자가 들어왔다.");
                return;
            }

            // 한 번만 처리한다.
            UnsubscribeCharacterSelected();

            Log.Info("Character selected: {0}", ne.CharacterKey);

            StartRound();
            DismissSelectCharacters(ne.CharacterKey);
            SpawnPlayer(ne.CharacterKey);
        }

        /// <summary>선택 시점부터를 게임 시작으로 본다. 스포너도 여기서 열린다.</summary>
        private void StartRound()
        {
            mGameTime = 0f;
            mSpawner.Open();
        }

        /// <summary>
        /// 선택된 쪽은 즉시 회수, 선택받지 못한 쪽은 사망 연출 후 회수.
        /// HideEntity 를 직접 부르지 않고 SafeHide 를 쓴다 — 중복 Hide 는 코어에서 예외다.
        ///
        /// EntityLogic 은 UnityEngine.Object 라 ?. 를 쓰면 안 된다.
        /// ?. 는 ReferenceEquals 라서 이미 파괴된 오브젝트를 살아 있는 것으로 보고 호출한다.
        /// </summary>
        private void DismissSelectCharacters(string selectedCharacterKey)
        {
            bool girlSelected = selectedCharacterKey == GirlCharacterKey;

            PlayerSelectLogic selected   = girlSelected ? mGirlSelect : mBoySelect;
            PlayerSelectLogic unselected = girlSelected ? mBoySelect  : mGirlSelect;

            // 선택된 쪽을 먼저 치우는 순서를 유지한다.
            if (selected != null)
            {
                selected.SafeHide();
            }

            if (unselected != null)
            {
                unselected.DisableAndHide();
            }

            // 회수한 뒤에는 참조를 반드시 비운다.
            // 엔티티는 풀에서 재사용되므로, 남겨 두면 나중에 같은 인스턴스가 다른 용도로
            // 살아났을 때 그걸 선택 캐릭터로 착각해 건드리게 된다.
            mGirlSelect = null;
            mBoySelect  = null;
        }

        private void OnEnemyDied(object sender, GameEventArgs e)
        {
            // 게임오버 확정 후 도착한 처치는 점수에 넣지 않는다.
            // Fire 는 큐잉이라 사망과 같은 프레임의 처치가 한 프레임 늦게 도착할 수 있다.
            if (GameOver) return;

            EnemyDiedEventArgs ne = e as EnemyDiedEventArgs;
            if (ne == null)
            {
                GameAssert.Unreachable("EnemyDied 핸들러에 다른 타입의 이벤트 인자가 들어왔다.");
                return;
            }

            AddScore(ne.ScoreValue);
        }

        private void OnPlayerDied(object sender, GameEventArgs e)
        {
            if (GameOver) return;

            GameOver = true;
            Log.Info("Game over. Final score: {0}", Score);
        }

        protected override void OnShowEntitySuccess(object sender, GameEventArgs e)
        {
            ShowEntitySuccessEventArgs ne = e as ShowEntitySuccessEventArgs;
            if (ne == null)
            {
                GameAssert.Unreachable("ShowEntitySuccess 핸들러에 다른 타입의 이벤트 인자가 들어왔다.");
                return;
            }

            if (ne.EntityLogicType == typeof(PlayerSelectLogic))
            {
                CacheSelectCharacter(ne.Entity);
                return;
            }

            if (ne.EntityLogicType == typeof(Enemy))
            {
                mSpawner.DecrementPendingSpawn(ne.Entity.EntityAssetName);
                return;
            }

            if (ne.EntityLogicType == typeof(Player))
            {
                CachePlayer(ne.Entity);
            }
        }

        /// <summary>선택 캐릭터 참조를 잡아 둔다. 나중에 DismissSelectCharacters 가 쓴다.</summary>
        private void CacheSelectCharacter(Entity entity)
        {
            PlayerSelectLogic logic = entity.Logic as PlayerSelectLogic;
            if (logic == null)
            {
                Log.Error("PlayerSelectLogic 으로 스폰했는데 로직이 그 타입이 아니다: {0}", entity.EntityAssetName);
                return;
            }

            if (logic.CharacterKey == GirlCharacterKey)
            {
                mGirlSelect = logic;
            }
            else
            {
                mBoySelect = logic;
            }
        }

        private void CachePlayer(Entity entity)
        {
            Player player = entity.Logic as Player;
            if (player == null)
            {
                Log.Error("Player 로 스폰했는데 로직이 그 타입이 아니다: {0}", entity.EntityAssetName);
                return;
            }

            mPlayer = player;
            Log.Info("Player spawned: {0}", entity.EntityAssetName);
        }

        /// <summary>
        /// <b>base.OnShowEntityFailure 를 부르지 않는다.</b> 베이스도 스폰 데이터를 반납하므로
        /// 같이 부르면 이중 Release 가 되어 코어가 예외를 던진다. 대신 반납만 여기서 직접 한다.
        /// </summary>
        protected override void OnShowEntityFailure(object sender, GameEventArgs e)
        {
            ShowEntityFailureEventArgs ne = e as ShowEntityFailureEventArgs;
            if (ne == null)
            {
                GameAssert.Unreachable("ShowEntityFailure 핸들러에 다른 타입의 이벤트 인자가 들어왔다.");
                return;
            }

            // 스폰이 실패하면 OnShow 가 불리지 않아 아무도 소유권을 잡지 않는다.
            // 어느 분기로 빠지든 반드시 반납되도록 분기보다 먼저 처리한다.
            ReleaseSpawnData(ne);

            // 플레이어 실패만 Warning 이 아니라 Error 로 따로 다룬다. 아래 주석 참조.
            if (ne.EntityLogicType == typeof(Player))
            {
                FailRoundOnPlayerSpawnFailure(ne);
                return;
            }

            // 실패해도 대기 카운트를 빼야 한다. 안 그러면 그 종은 영원히 상한에 걸린 것으로 보인다.
            if (ne.EntityLogicType == typeof(Enemy))
            {
                mSpawner.DecrementPendingSpawn(ne.EntityAssetName);
            }

            Log.Warning("Show entity failure: {0} ({1})", ne.ErrorMessage, ne.EntityAssetName);
        }

        /// <summary>
        /// 플레이어 스폰 실패 처리. 판을 게임오버로 끝낸다.
        ///
        /// <b>이 처리를 빼면 게임이 조용히 영구 정지한다.</b> mPlayer 는 영원히 null 로 남고
        /// <see cref="Update"/> 가 Player == null 에서 매 프레임 조기 리턴하므로, 시계도
        /// 스포너도 게임오버도 돌지 않는다. 화면은 멀쩡한데 아무 일도 일어나지 않는 상태다.
        /// 게임오버로 빠지면 최소한 ProcedureGameOver 가 판을 정리하고 원인이 로그에 남는다.
        ///
        /// 원인은 대부분 설정 실수다 — Addressables Address 누락, Play Mode Script 미설정,
        /// 프리팹 이름과 캐릭터 키 불일치. 그래서 Warning 이 아니라 Error 다.
        /// </summary>
        private void FailRoundOnPlayerSpawnFailure(ShowEntityFailureEventArgs ne)
        {
            Log.Error("플레이어 '{0}' 스폰에 실패했다. 판을 진행할 수 없어 게임오버로 끝낸다. 원인: {1}",
                ne.EntityAssetName, ne.ErrorMessage);

            // 이미 true 여도 그대로 두면 되므로 따로 가드하지 않는다.
            // ProcedureMain 이 다음 Update 에서 이 값을 보고 ProcedureGameOver 로 전이한다.
            GameOver = true;
        }

        /// <summary>
        /// EventComponent 조회. 없으면 <b>조용히</b> null 을 돌려준다.
        /// 컴포넌트가 없는 것이 정상일 수 있는 종료 경로에서 쓴다.
        /// </summary>
        private static EventComponent FindEventComponent()
        {
            return GameEntry.GetComponent<EventComponent>();
        }

        /// <summary>
        /// EventComponent 조회. 없으면 원인을 로그로 남긴다.
        /// 반드시 있어야 하는 경로(초기화·이벤트 발행)에서 쓴다. 프리팹 구성이 깨졌을 때만 null 이다.
        /// </summary>
        private static EventComponent RequireEventComponent()
        {
            EventComponent events = FindEventComponent();
            if (events == null)
            {
                Log.Error("EventComponent 를 찾을 수 없다. GameFramework.prefab 구성을 확인할 것.");
            }

            return events;
        }
    }
}
