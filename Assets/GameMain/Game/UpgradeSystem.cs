using System.Collections.Generic;
using UnityGameFramework.Runtime;

namespace ToyBoxNightmare
{
    /// <summary>
    /// 업그레이드 풀을 관리하고 레벨업마다 3개를 랜덤 선택한다.
    /// </summary>
    public class UpgradeSystem
    {
        private readonly List<UpgradeDefinition> mPool = new List<UpgradeDefinition>();

        public void Initialize(Player player)
        {
            ProjectileWeapon weapon = player.GetComponent<ProjectileWeapon>();

            mPool.Add(new UpgradeDefinition(
                "투사체 강화",
                "투사체 데미지 +10",
                () => { if (weapon) weapon.Damage += 10; }));

            mPool.Add(new UpgradeDefinition(
                "속사",
                "공격 간격 20% 감소",
                () => { if (weapon) weapon.AttackInterval *= 0.8f; }));

            mPool.Add(new UpgradeDefinition(
                "투사체 가속",
                "투사체 속도 +3",
                () => { if (weapon) weapon.Speed += 3f; }));

            mPool.Add(new UpgradeDefinition(
                "이동 속도 증가",
                "플레이어 이동 속도 +1",
                () => player.UpgradeMoveSpeed(1f)));

            mPool.Add(new UpgradeDefinition(
                "범위 공격 추가",
                "플레이어 주변에 범위 공격 무기 장착",
                () =>
                {
                    if (!player.GetComponent<AreaWeapon>())
                        player.AttachWeapon<AreaWeapon>();
                }));

            mPool.Add(new UpgradeDefinition(
                "생명력 회복",
                "HP +30 회복",
                () => player.HealHitPoints(30)));
        }

        /// <summary>
        /// 풀에서 count개를 중복 없이 랜덤 선택한다.
        /// </summary>
        public List<UpgradeDefinition> PickRandom(int count)
        {
            var result  = new List<UpgradeDefinition>();
            var indices = new List<int>();

            for (int i = 0; i < mPool.Count; i++) indices.Add(i);

            int take = System.Math.Min(count, mPool.Count);
            for (int i = 0; i < take; i++)
            {
                int rand = UnityEngine.Random.Range(0, indices.Count);
                result.Add(mPool[indices[rand]]);
                indices.RemoveAt(rand);
            }

            return result;
        }
    }
}
