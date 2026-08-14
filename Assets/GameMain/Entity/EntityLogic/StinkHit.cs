using System.Collections.Generic;
using UnityEngine;
using UnityGameFramework.Runtime;

namespace ToyBoxNightmare
{
    /// <summary>
    /// 악취 가스 구름. 착탄 순간 한 번만 범위 판정을 하고, 맞은 적을 도주시킨다.
    ///
    /// 원본은 씬 싱글턴 하나를 재사용하면서 맞은 적 배열을 캐시해 뒀다가 4초 뒤
    /// 되돌리는 구조였는데, 두 가지 문제가 있었다.
    ///   - 4초 사이에 적이 회수되고 다른 종으로 재사용되면 남의 적을 복귀시킨다
    ///   - 이미 활성인 GameObject 에 SetActive(true) 는 no-op 이라 두 번째 발사가 통째로 씹힌다
    /// 도주 타이머를 Enemy 쪽에 두면 둘 다 사라진다.
    /// </summary>
    public class StinkHit : HitEffect
    {
        /// <summary>
        /// 판정 결과를 담을 공용 버퍼. 매 착탄마다 List 를 새로 만들면 GC 가 돈다.
        /// static 이어도 안전한 이유는 이 버퍼를 쓰는 구간(아래 순회)이
        /// 다른 StinkHit 을 스폰하지도, 재진입하지도 않기 때문이다 —
        /// ApplyFlee 는 Enemy 의 도주 타이머만 건드린다.
        /// </summary>
        private static readonly List<Enemy> sBuffer = new List<Enemy>(32);

        protected override void OnEffectStarted()
        {
            base.OnEffectStarted();

            int fleeingCount = WeaponUtil.FindEnemiesInSphere(
                CachedTransform.position, WeaponTable.StinkBlastRadius, sBuffer);

            GameAssert.IsTrue(fleeingCount <= sBuffer.Count,
                "FindEnemiesInSphere 가 채운 개수보다 큰 값을 돌려줬다.");

            MakeEnemiesFlee(fleeingCount);

            Log.Info("StinkHit: {0}마리 도주", fleeingCount);
        }

        private static void MakeEnemiesFlee(int count)
        {
            for (int i = 0; i < count; i++)
            {
                Enemy enemy = sBuffer[i];
                if (enemy == null) continue;

                enemy.ApplyFlee(WeaponTable.StinkFleeDuration);
            }
        }
    }
}
