using System.Collections.Generic;
using GameFramework.Event;
using UnityEngine;
using UnityGameFramework.Runtime;

namespace ToyBoxNightmare
{
    public class SurvivalGame : GameBase
    {
        // ─── 설정 ───
        private const string PlayerAssetPath     = "Player";
        private const string EnemyAssetPath      = "Enemy";
        private const string ExpGemAssetPath     = "ExpGem";
        private const string UpgradeFormAssetPath = "UpgradeForm";
        private const string UpgradeFormGroupName = "Default";

        private const float SpawnRadius       = 15f;
        private const int   MaxEnemyCount     = 50;
        private const float InitSpawnInterval = 3f;

        // ─── 싱글턴 ───
        public static SurvivalGame Instance { get; private set; }

        // ─── 런타임 상태 ───
        private Player   mPlayer       = null;
        private float    mSpawnTimer   = 0f;
        private float    mSpawnInterval = InitSpawnInterval;
        private float    mSurvivalTime = 0f;
        private readonly HashSet<int> mEnemyIds = new HashSet<int>(); // 적 ID 추적

        public override GameMode GameMode => GameMode.Survival;

        public float         SurvivalTime  => mSurvivalTime;
        public int           EnemyCount    => mEnemyIds.Count;
        public LevelSystem   LevelSystem   { get; private set; }
        public UpgradeSystem UpgradeSystem { get; private set; }

        // ─── 초기화 ───

        public override void Initialize()
        {
            base.Initialize();

            Instance      = this;
            LevelSystem   = new LevelSystem();
            UpgradeSystem = new UpgradeSystem();

            var events = GameEntry.GetComponent<EventComponent>();
            events.Subscribe(ShowEntitySuccessEventArgs.EventId,  OnShowEntitySuccess);
            events.Subscribe(ShowEntityFailureEventArgs.EventId,  OnShowEntityFailure);
            events.Subscribe(HideEntityCompleteEventArgs.EventId, OnHideEntityComplete);
            events.Subscribe(GameOverEventArgs.EventId,           OnGameOver);
            events.Subscribe(LevelUpEventArgs.EventId,           OnLevelUp);

            SpawnPlayer();
        }

        public override void Shutdown()
        {
            var events = GameEntry.GetComponent<EventComponent>();
            events.Unsubscribe(ShowEntitySuccessEventArgs.EventId,  OnShowEntitySuccess);
            events.Unsubscribe(ShowEntityFailureEventArgs.EventId,  OnShowEntityFailure);
            events.Unsubscribe(HideEntityCompleteEventArgs.EventId, OnHideEntityComplete);
            events.Unsubscribe(GameOverEventArgs.EventId,           OnGameOver);
            events.Unsubscribe(LevelUpEventArgs.EventId,           OnLevelUp);

            mEnemyIds.Clear();
            Instance = null;
            base.Shutdown();
        }

        // ─── 업데이트 ───

        public override void Update(float elapseSeconds, float realElapseSeconds)
        {
            base.Update(elapseSeconds, realElapseSeconds);

            if (GameOver || mPlayer == null) return;

            mSurvivalTime += elapseSeconds;

            mSpawnInterval = Mathf.Max(0.5f, InitSpawnInterval - mSurvivalTime * 0.05f);

            mSpawnTimer += elapseSeconds;
            if (mSpawnTimer >= mSpawnInterval && EnemyCount < MaxEnemyCount)
            {
                mSpawnTimer = 0f;
                SpawnEnemy();
            }
        }

        // ─── 스폰 ───

        private void SpawnPlayer()
        {
            int id = EntitySerialId.Next();
            GameEntry.GetComponent<EntityComponent>().ShowEntity(
                id,
                typeof(Player),
                PlayerAssetPath,
                "Player",
                new PlayerData(id, 1));
        }

        private void SpawnEnemy()
        {
            if (mPlayer == null) return;

            int id = EntitySerialId.Next();

            float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            Vector3 spawnPos = mPlayer.CachedTransform.position
                + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * SpawnRadius;

            GameEntry.GetComponent<EntityComponent>().ShowEntity(
                id,
                typeof(Enemy),
                EnemyAssetPath,
                "Enemy",
                new EnemyData(id, 1) { Position = spawnPos });

            mEnemyIds.Add(id);
        }

        public void SpawnExpGem(Vector3 position, int expAmount)
        {
            int id = EntitySerialId.Next();
            GameEntry.GetComponent<EntityComponent>().ShowEntity(
                id,
                typeof(ExpGem),
                ExpGemAssetPath,
                "ExpGem",
                new ExpGemData(id, 1) { Position = position, ExpAmount = expAmount });
        }

        // ─── 이벤트 ───

        protected override void OnShowEntitySuccess(object sender, GameEventArgs e)
        {
            var ne = (ShowEntitySuccessEventArgs)e;

            if (ne.EntityLogicType == typeof(Player))
            {
                mPlayer = (Player)ne.Entity.Logic;
                mPlayer.AttachWeapon<ProjectileWeapon>();
                UpgradeSystem.Initialize(mPlayer);
                Log.Info("Player spawned.");
            }
        }

        protected override void OnShowEntityFailure(object sender, GameEventArgs e)
        {
            var ne = (ShowEntityFailureEventArgs)e;
            Log.Warning("Show entity failure: {0}", ne.ErrorMessage);
        }

        private void OnHideEntityComplete(object sender, GameEventArgs e)
        {
            var ne = (HideEntityCompleteEventArgs)e;
            mEnemyIds.Remove(ne.EntityId); // Enemy든 아니든 제거 시도 (없으면 무시)
        }

        private void OnGameOver(object sender, GameEventArgs e)
        {
            GameOver = true;
            Log.Info("=== GAME OVER === Survived: {0:F1} seconds", mSurvivalTime);
            // TODO: GameOver UI 표시
        }

        private void OnLevelUp(object sender, GameEventArgs e)
        {
            var ne = (LevelUpEventArgs)e;
            Log.Info("Level up! Lv.{0}", ne.Level);

            var options  = UpgradeSystem.PickRandom(3);
            var formData = new UpgradeFormData(options);
            GameEntry.GetComponent<UIComponent>().OpenUIForm(
                UpgradeFormAssetPath, UpgradeFormGroupName, formData);
        }
    }
}
