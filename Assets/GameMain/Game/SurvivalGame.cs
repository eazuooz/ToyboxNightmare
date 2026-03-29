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

        public override GameMode GameMode => GameMode.Survival;

        // ─── 초기화 ───

        public override void Initialize()
        {
            base.Initialize();

            Instance = this;

            var events = GameEntry.GetComponent<EventComponent>();
            events.Subscribe(ShowEntitySuccessEventArgs.EventId, OnShowEntitySuccess);
            events.Subscribe(ShowEntityFailureEventArgs.EventId, OnShowEntityFailure);
            events.Subscribe(CharacterSelectedEventArgs.EventId, OnCharacterSelected);

            // 캐릭터 선택용 엔티티 스폰
            SpawnSelectCharacter(GirlSelectAssetPath, new Vector3(-2f, 0f, 0f));
            SpawnSelectCharacter(BoySelectAssetPath,  new Vector3( 2f, 0f, 0f));
        }

        public override void Shutdown()
        {
            var events = GameEntry.GetComponent<EventComponent>();
            events.Unsubscribe(ShowEntitySuccessEventArgs.EventId, OnShowEntitySuccess);
            events.Unsubscribe(ShowEntityFailureEventArgs.EventId, OnShowEntityFailure);
            // CharacterSelectedEventArgs 는 OnCharacterSelected 에서 이미 해제됨

            Instance = null;
            base.Shutdown();
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

            // 한 번만 처리
            GameEntry.GetComponent<EventComponent>().Unsubscribe(
                CharacterSelectedEventArgs.EventId, OnCharacterSelected);

            Log.Info("Character selected: {0}", ne.CharacterKey);

            // 선택된 쪽은 즉시 Hide, 선택받지 못한 쪽은 사망 연출 후 Hide
            bool girlSelected = ne.CharacterKey == "Girl";
            if (girlSelected)
            {
                if (mGirlSelect != null)
                    GameEntry.GetComponent<EntityComponent>().HideEntity(mGirlSelect.Entity);
                mBoySelect?.DisableAndHide();
            }
            else
            {
                if (mBoySelect != null)
                    GameEntry.GetComponent<EntityComponent>().HideEntity(mBoySelect.Entity);
                mGirlSelect?.DisableAndHide();
            }

            SpawnPlayer(ne.CharacterKey);
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

            if (ne.EntityLogicType == typeof(Player))
            {
                mPlayer = (Player)ne.Entity.Logic;
                Log.Info("Player spawned: {0}", ne.Entity.EntityAssetName);
            }
        }

        protected override void OnShowEntityFailure(object sender, GameEventArgs e)
        {
            var ne = (ShowEntityFailureEventArgs)e;
            Log.Warning("Show entity failure: {0}", ne.ErrorMessage);
        }
    }
}
