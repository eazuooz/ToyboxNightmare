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
        private static readonly List<Enemy> sBuffer = new List<Enemy>(32);

        protected override void OnEffectStarted()
        {
            base.OnEffectStarted();

            int count = WeaponUtil.FindEnemiesInSphere(
                CachedTransform.position, WeaponTable.StinkBlastRadius, sBuffer);

            for (int i = 0; i < count; i++)
            {
                sBuffer[i].ApplyFlee(WeaponTable.StinkFleeDuration);
            }

            Log.Info("StinkHit: {0}마리 도주", count);
        }
    }
}
