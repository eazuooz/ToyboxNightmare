using GameFramework.Event;
using UnityEngine;
using UnityGameFramework.Runtime;

namespace ToyBoxNightmare
{
    public class SurvivalGame : GameBase
    {
        // ─── 설정 ───
        private const string GirlSelectAssetPath = "Girl";
        private const string BoySelectAssetPath  = "Boy";

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
        private bool          mSpawningEnabled = false;
        private readonly float[] mSpawnTimers   = new float[EnemyTable.SpawnPoints.Length];

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

            // 프로시저 재진입(재시작)으로 새 인스턴스가 만들어지므로 상태를 명시적으로 초기화한다.
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

            var events = GameEntry.GetComponent<EventComponent>();
            events.Subscribe(ShowEntitySuccessEventArgs.EventId, OnShowEntitySuccess);
            events.Subscribe(ShowEntityFailureEventArgs.EventId, OnShowEntityFailure);
            events.Subscribe(EnemyDiedEventArgs.EventId,         OnEnemyDied);
            events.Subscribe(PlayerDiedEventArgs.EventId,        OnPlayerDied);

            events.Subscribe(CharacterSelectedEventArgs.EventId, OnCharacterSelected);
            mCharacterSelectSubscribed = true;

            // 캐릭터 선택용 엔티티 스폰
            SpawnSelectCharacter(GirlSelectAssetPath, new Vector3(-2f, 0f, 0f));
            SpawnSelectCharacter(BoySelectAssetPath,  new Vector3( 2f, 0f, 0f));
        }

        public override void Shutdown()
        {
            var events = GameEntry.GetComponent<EventComponent>();
            events.Unsubscribe(ShowEntitySuccessEventArgs.EventId, OnShowEntitySuccess);
            events.Unsubscribe(ShowEntityFailureEventArgs.EventId, OnShowEntityFailure);
            events.Unsubscribe(EnemyDiedEventArgs.EventId,         OnEnemyDied);
            events.Unsubscribe(PlayerDiedEventArgs.EventId,        OnPlayerDied);

            // 캐릭터를 고르지 않고 종료하면 아직 구독 상태다.
            UnsubscribeCharacterSelected();

            Instance = null;
            base.Shutdown();
        }

        public override void Update(float elapseSeconds, float realElapseSeconds)
        {
            base.Update(elapseSeconds, realElapseSeconds);

            if (GameOver)
            {
                return;
            }

            // 캐릭터 선택 중에는 시계를 세워 둔다. 선택 시점부터가 '게임 시작'이다.
            if (Player == null)
            {
                return;
            }

            mGameTime += elapseSeconds;

            if (mSpawningEnabled)
            {
                UpdateSpawners(elapseSeconds);
            }
        }

        // ─── 스포너 ───

        private void UpdateSpawners(float elapseSeconds)
        {
            var entityComponent = GameEntry.GetComponent<EntityComponent>();

            for (int i = 0; i < EnemyTable.SpawnPoints.Length; i++)
            {
                EnemySpawnPoint point = EnemyTable.SpawnPoints[i];

                mSpawnTimers[i] += elapseSeconds;
                if (mSpawnTimers[i] < point.Interval)
                {
                    continue;
                }

                mSpawnTimers[i] = 0f;

                // 상한에 걸리면 이번 틱은 그냥 건너뛴다(원본 EnemySpawner 동작).
                int alive = entityComponent.GetEntities(point.AssetName).Length + mPendingSpawns[i];
                if (alive >= point.MaxAlive)
                {
                    continue;
                }

                SpawnEnemy(i, point);
            }
        }

        private void SpawnEnemy(int spawnIndex, EnemySpawnPoint point)
        {
            EnemyStats stats;
            if (!EnemyTable.TryGetStats(point.AssetName, out stats))
            {
                Log.Error("Enemy stats not found for '{0}'.", point.AssetName);
                return;
            }

            int id = EntitySerialId.Next();
            mPendingSpawns[spawnIndex]++;

            GameEntry.GetComponent<EntityComponent>().ShowEntity(
                id,
                typeof(Enemy),
                point.AssetName,
                EnemyTable.EntityGroup,
                new EnemyData(id, 1, stats) { Position = point.Position });
        }

        /// <summary>로드가 끝났거나 실패한 스폰 요청을 대기 카운트에서 뺀다.</summary>
        private void DecrementPendingSpawn(string assetName)
        {
            for (int i = 0; i < EnemyTable.SpawnPoints.Length; i++)
            {
                if (EnemyTable.SpawnPoints[i].AssetName != assetName)
                {
                    continue;
                }

                if (mPendingSpawns[i] > 0)
                {
                    mPendingSpawns[i]--;
                }
                return;
            }
        }

        private void UnsubscribeCharacterSelected()
        {
            if (!mCharacterSelectSubscribed)
            {
                return;
            }

            GameEntry.GetComponent<EventComponent>().Unsubscribe(
                CharacterSelectedEventArgs.EventId, OnCharacterSelected);
            mCharacterSelectSubscribed = false;
        }

        /// <summary>점수를 더하고 ScoreChanged 를 발행한다.</summary>
        public void AddScore(int delta)
        {
            if (delta == 0)
            {
                return;
            }

            Score += delta;
            GameEntry.GetComponent<EventComponent>().Fire(
                this, ScoreChangedEventArgs.Create(Score, delta));
        }

        // ─── 스폰 ───

        private void SpawnSelectCharacter(string assetPath, Vector3 position)
        {
            int id = EntitySerialId.Next();
            string characterKey = assetPath == GirlSelectAssetPath ? "Girl" : "Boy";
            GameEntry.GetComponent<EntityComponent>().ShowEntity(
                id,
                typeof(PlayerSelectLogic),
                assetPath,
                "Player",
                new CharacterSelectData(id, 1, characterKey) { Position = position });
        }

        private void SpawnPlayer(string characterKey)
        {
            int id = EntitySerialId.Next();
            GameEntry.GetComponent<EntityComponent>().ShowEntity(
                id,
                typeof(Player),
                characterKey,
                "Player",
                new PlayerData(id, 1));
        }

        // ─── 이벤트 핸들러 ───

        private void OnCharacterSelected(object sender, GameEventArgs e)
        {
            var ne = (CharacterSelectedEventArgs)e;

            // 한 번만 처리한다.
            UnsubscribeCharacterSelected();

            Log.Info("Character selected: {0}", ne.CharacterKey);

            // 선택 시점부터를 게임 시작으로 본다. 스포너도 여기서 열린다.
            mGameTime = 0f;
            mSpawningEnabled = true;

            // 선택된 쪽은 즉시 회수, 선택받지 못한 쪽은 사망 연출 후 회수.
            // HideEntity 를 직접 부르지 않고 SafeHide 를 쓴다 — 중복 Hide 는 코어에서 예외다.
            bool girlSelected = ne.CharacterKey == "Girl";
            if (girlSelected)
            {
                mGirlSelect?.SafeHide();
                mBoySelect?.DisableAndHide();
            }
            else
            {
                mBoySelect?.SafeHide();
                mGirlSelect?.DisableAndHide();
            }

            SpawnPlayer(ne.CharacterKey);
        }

        private void OnEnemyDied(object sender, GameEventArgs e)
        {
            // 게임오버 확정 후 도착한 처치는 점수에 넣지 않는다.
            // Fire 는 큐잉이라 사망과 같은 프레임의 처치가 한 프레임 늦게 도착할 수 있다.
            if (GameOver)
            {
                return;
            }

            var ne = (EnemyDiedEventArgs)e;
            AddScore(ne.ScoreValue);
        }

        private void OnPlayerDied(object sender, GameEventArgs e)
        {
            if (GameOver)
            {
                return;
            }

            GameOver = true;
            Log.Info("Game over. Final score: {0}", Score);
        }

        protected override void OnShowEntitySuccess(object sender, GameEventArgs e)
        {
            var ne = (ShowEntitySuccessEventArgs)e;

            if (ne.EntityLogicType == typeof(PlayerSelectLogic))
            {
                var logic = (PlayerSelectLogic)ne.Entity.Logic;
                if (logic.CharacterKey == "Girl") mGirlSelect = logic;
                else                              mBoySelect  = logic;
                return;
            }

            if (ne.EntityLogicType == typeof(Enemy))
            {
                DecrementPendingSpawn(ne.Entity.EntityAssetName);
                return;
            }

            if (ne.EntityLogicType == typeof(Player))
            {
                mPlayer = (Player)ne.Entity.Logic;
                Log.Info("Player spawned: {0}", ne.Entity.EntityAssetName);
            }
        }

        protected override void OnShowEntityFailure(object sender, GameEventArgs e)
        {
            var ne = (ShowEntityFailureEventArgs)e;

            // 실패해도 대기 카운트를 빼야 한다. 안 그러면 그 종은 영원히 상한에 걸린 것으로 보인다.
            if (ne.EntityLogicType == typeof(Enemy))
            {
                DecrementPendingSpawn(ne.EntityAssetName);
            }

            Log.Warning("Show entity failure: {0} ({1})", ne.ErrorMessage, ne.EntityAssetName);
        }
    }
}
