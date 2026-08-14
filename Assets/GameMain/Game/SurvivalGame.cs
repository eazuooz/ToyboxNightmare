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

        /// <summary>EntityData 의 TypeId. 아직 타입 테이블이 없어 프로젝트 전체가 1 로 고정이다.</summary>
        private const int DefaultEntityTypeId = 1;

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
        // 캐릭터를 고르기 전에는 돌지 않는다(원본의 스포너 SetActive 게이트와 같은 의미).
        private bool             mSpawningEnabled = false;
        private readonly float[] mSpawnTimers     = new float[EnemyTable.SpawnPoints.Length];

        // ShowEntity 요청 후 아직 인스턴스화되지 않은 수. GetEntities 가 세지 못하므로
        // 따로 들고 있지 않으면 로드 지연 동안 상한을 넘겨 과다 스폰한다.
        private readonly int[]   mPendingSpawns = new int[EnemyTable.SpawnPoints.Length];

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

            mSpawningEnabled = false;
            for (int i = 0; i < mSpawnTimers.Length; i++)
            {
                mSpawnTimers[i]   = 0f;
                mPendingSpawns[i] = 0;
            }
        }

        private void SubscribeEvents()
        {
            EventComponent events = GetEventComponent();
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
            EventComponent events = GetEventComponent();
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

            EventComponent events = GetEventComponent();
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

            if (mSpawningEnabled)
            {
                UpdateSpawners(elapseSeconds);
            }
        }

        // ─── 스포너 ───

        private void UpdateSpawners(float elapseSeconds)
        {
            // 반복문마다 다시 찾을 이유가 없다. 스폰 지점 수만큼 리스트를 훑게 된다.
            EntityComponent entityComponent = GameEntry.GetComponent<EntityComponent>();
            if (entityComponent == null)
            {
                Log.Error("EntityComponent 를 찾을 수 없어 적 스폰을 건너뛴다. GameFramework.prefab 구성을 확인할 것.");
                return;
            }

            for (int spawnIndex = 0; spawnIndex < EnemyTable.SpawnPoints.Length; spawnIndex++)
            {
                if (!TickSpawnTimer(spawnIndex, elapseSeconds)) continue;

                // 상한에 걸리면 이번 틱은 그냥 건너뛴다(원본 EnemySpawner 동작).
                if (IsSpawnLimitReached(entityComponent, spawnIndex)) continue;

                SpawnEnemy(entityComponent, spawnIndex);
            }
        }

        /// <summary>
        /// 스폰 타이머를 진행시킨다. 이번 틱이 스폰 차례면 타이머를 되감고 true 를 돌려준다.
        /// 상한에 걸려 스폰을 못 해도 타이머는 이미 되감긴 상태다(원본 동작).
        /// </summary>
        private bool TickSpawnTimer(int spawnIndex, float elapseSeconds)
        {
            GameAssert.InRange(spawnIndex, mSpawnTimers.Length, "spawnIndex");

            mSpawnTimers[spawnIndex] += elapseSeconds;
            if (mSpawnTimers[spawnIndex] < EnemyTable.SpawnPoints[spawnIndex].Interval) return false;

            mSpawnTimers[spawnIndex] = 0f;
            return true;
        }

        /// <summary>
        /// 이 종이 동시 생존 상한에 도달했는가.
        /// 아직 로드 중인 요청(mPendingSpawns)까지 세지 않으면 로드 지연 동안 과다 스폰한다.
        /// </summary>
        private bool IsSpawnLimitReached(EntityComponent entityComponent, int spawnIndex)
        {
            EnemySpawnPoint point = EnemyTable.SpawnPoints[spawnIndex];

            int aliveCount   = entityComponent.GetEntities(point.AssetName).Length;
            int pendingCount = mPendingSpawns[spawnIndex];

            return aliveCount + pendingCount >= point.MaxAlive;
        }

        private void SpawnEnemy(EntityComponent entityComponent, int spawnIndex)
        {
            EnemySpawnPoint point = EnemyTable.SpawnPoints[spawnIndex];

            EnemyStats stats;
            if (!EnemyTable.TryGetStats(point.AssetName, out stats))
            {
                // 스폰 지점 주소가 EnemyTable 에 없다 — 데이터 누락이다.
                Log.Error("Enemy stats not found for '{0}'.", point.AssetName);
                return;
            }

            int id = EntitySerialId.Next();

            // 반드시 ShowEntity 보다 먼저 올린다. 성공/실패 콜백이 이 카운트를 되돌린다.
            mPendingSpawns[spawnIndex]++;

            entityComponent.ShowEntity(
                id,
                typeof(Enemy),
                point.AssetName,
                EnemyTable.EntityGroup,
                new EnemyData(id, DefaultEntityTypeId, stats) { Position = point.Position });
        }

        /// <summary>로드가 끝났거나 실패한 스폰 요청을 대기 카운트에서 뺀다.</summary>
        private void DecrementPendingSpawn(string assetName)
        {
            for (int i = 0; i < EnemyTable.SpawnPoints.Length; i++)
            {
                if (EnemyTable.SpawnPoints[i].AssetName != assetName) continue;

                if (mPendingSpawns[i] > 0)
                {
                    mPendingSpawns[i]--;
                }

                // 주소는 스폰 지점당 하나뿐이므로 첫 일치에서 끝낸다.
                return;
            }
        }

        // ─── 점수 ───

        /// <summary>점수를 더하고 ScoreChanged 를 발행한다.</summary>
        public void AddScore(int delta)
        {
            if (delta == 0) return;

            Score += delta;

            EventComponent events = GetEventComponent();
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
                new CharacterSelectData(id, DefaultEntityTypeId, characterKey) { Position = position });
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
                new PlayerData(id, DefaultEntityTypeId));
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
            mGameTime        = 0f;
            mSpawningEnabled = true;
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
                DecrementPendingSpawn(ne.Entity.EntityAssetName);
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

        protected override void OnShowEntityFailure(object sender, GameEventArgs e)
        {
            ShowEntityFailureEventArgs ne = e as ShowEntityFailureEventArgs;
            if (ne == null)
            {
                GameAssert.Unreachable("ShowEntityFailure 핸들러에 다른 타입의 이벤트 인자가 들어왔다.");
                return;
            }

            // 실패해도 대기 카운트를 빼야 한다. 안 그러면 그 종은 영원히 상한에 걸린 것으로 보인다.
            if (ne.EntityLogicType == typeof(Enemy))
            {
                DecrementPendingSpawn(ne.EntityAssetName);
            }

            Log.Warning("Show entity failure: {0} ({1})", ne.ErrorMessage, ne.EntityAssetName);
        }

        /// <summary>EventComponent 조회. 프리팹 구성이 깨졌을 때만 null 이다.</summary>
        private static EventComponent GetEventComponent()
        {
            EventComponent events = GameEntry.GetComponent<EventComponent>();
            if (events == null)
            {
                Log.Error("EventComponent 를 찾을 수 없다. GameFramework.prefab 구성을 확인할 것.");
            }

            return events;
        }
    }
}
