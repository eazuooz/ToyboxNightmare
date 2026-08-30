using UnityEngine;
using UnityGameFramework.Runtime;

namespace ToyBoxNightmare
{
    /// <summary>
    /// 효과음 재생 창구. GameFramework 의 <see cref="SoundComponent"/> 를 거친다 —
    /// AudioSource 를 직접 붙이면 믹서 라우팅이 빠져 M8 의 볼륨 슬라이더가 안 먹는다.
    ///
    /// <b>단, 이펙트 프리팹에 이미 구워져 있는 AudioSource 는 그대로 둔다.</b>
    /// (LightningBolt 의 발사음, HitEffect 의 착탄음 등) 그건 파티클과 한 덩어리로
    /// 저작된 연출이라 떼어내면 프리팹 단위 조정이 불가능해진다.
    /// 여기서 다루는 것은 <b>프리팹에 없는 게임플레이 사운드</b>다 — 피격, 사망.
    /// </summary>
    public static class GameSound
    {
        /// <summary>
        /// 월드 좌표에서 효과음을 한 번 재생한다.
        ///
        /// 주소가 비어 있거나 사운드 시스템이 없으면 조용히 넘어간다 —
        /// 소리가 안 나는 것이 게임을 멈출 이유는 되지 않는다.
        /// </summary>
        public static void PlaySfx(string assetName, Vector3 worldPosition)
        {
            if (string.IsNullOrEmpty(assetName)) return;

            SoundComponent sound = GameEntry.GetComponent<SoundComponent>();
            if (sound == null) return;

            sound.PlaySound(assetName, SoundTable.SfxGroup, worldPosition);
        }
    }
}
