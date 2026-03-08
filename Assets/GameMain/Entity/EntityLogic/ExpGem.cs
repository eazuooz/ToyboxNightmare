using UnityEngine;
using UnityGameFramework.Runtime;

namespace ToyBoxNightmare
{
    /// <summary>
    /// 경험치 보석. 플레이어 근처에서 자석처럼 끌려간다.
    /// 플레이어와 접촉하면 경험치를 주고 소멸한다.
    /// </summary>
    public class ExpGem : EntityLogic
    {
        private ExpGemData mData = null;

        private const float AttractRadius = 5f;   // 자석 발동 반경
        private const float CollectRadius = 0.5f; // 수집 판정 반경

        protected internal override void OnShow(object userData)
        {
            base.OnShow(userData);

            mData = userData as ExpGemData;
            if (mData == null)
            {
                Log.Error("ExpGem data is invalid.");
                return;
            }

            CachedTransform.position = mData.Position;
        }

        protected internal override void OnUpdate(float elapseSeconds, float realElapseSeconds)
        {
            base.OnUpdate(elapseSeconds, realElapseSeconds);

            Player player = Player.Instance;
            if (player == null || player.IsDead) return;

            Vector3 toPlayer = player.CachedTransform.position - CachedTransform.position;
            float dist = toPlayer.magnitude;

            if (dist <= CollectRadius)
            {
                // 수집
                SurvivalGame.Instance?.LevelSystem.AddExp(mData.ExpAmount);
                GameEntry.GetComponent<EntityComponent>().HideEntity(Entity);
                return;
            }

            if (dist <= AttractRadius)
            {
                // 자석 이동
                CachedTransform.position += toPlayer.normalized * mData.MoveSpeed * elapseSeconds;
            }
        }
    }
}
