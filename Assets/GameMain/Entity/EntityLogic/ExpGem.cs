using UnityEngine;
using UnityGameFramework.Runtime;

namespace ToyBoxNightmare
{
    /// <summary>
    /// 경험치 보석. 플레이어 근처에서 자석처럼 끌려간다.
    /// 플레이어와 접촉하면 경험치를 주고 소멸한다.
    /// </summary>
    public class ExpGem : EntityLogicBase
    {
        private ExpGemData mData = null;

        // CollectRadius 가 0 보다 크고 AttractRadius 보다 작다는 것이 자석 이동의 전제다.
        // 수집 반경 안이면 먼저 회수되므로, 자석 분기에서는 거리가 절대 0 이 아니다
        // (= normalized 가 길이 0 벡터를 만나지 않는다).
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

            if (mData == null || IsHiding) return;

            Player player = Player.Instance;
            if (player == null || player.IsDead) return;

            Vector3 toPlayer         = player.CachedTransform.position - CachedTransform.position;
            float   distanceToPlayer = toPlayer.magnitude;

            if (distanceToPlayer <= CollectRadius)
            {
                Collect();
                return;
            }

            if (distanceToPlayer <= AttractRadius)
            {
                AttractToward(toPlayer, elapseSeconds);
            }
        }

        /// <summary>
        /// 수집. 경험치 지급은 레벨 시스템 복원 시 여기에 연결한다
        /// (그때까지 <c>ExpGemData.ExpAmount</c> 는 쓰이지 않는다).
        /// </summary>
        private void Collect()
        {
            SafeHide();
        }

        /// <summary>
        /// 자석 이동. 호출 시점에 거리가 CollectRadius 보다 크다는 것이 보장돼 있어
        /// normalized 가 길이 0 벡터를 만나지 않는다.
        /// </summary>
        private void AttractToward(Vector3 toPlayer, float elapseSeconds)
        {
            CachedTransform.position += toPlayer.normalized * mData.MoveSpeed * elapseSeconds;
        }
    }
}
