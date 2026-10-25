using GameFramework.Sound;
using UnityEngine;
using UnityGameFramework.Runtime;

namespace ToyBoxNightmare
{
    /// <summary>
    /// 게임이 소리를 내는 유일한 창구. 전부 <see cref="SoundComponent"/> 로 간다.
    ///
    /// <b>프리팹에 AudioSource 를 박아 두고 직접 <c>Play()</c> 하지 않는다.</b> 그렇게 낸 소리는
    /// 프레임워크의 사운드 그룹을 거치지 않아서 볼륨 설정·일시정지 스냅샷·동시 재생 수 제한이
    /// 하나도 먹지 않는다. 클립은 Addressables 에 등록하고 주소(<see cref="SoundTable"/>)로 재생한다.
    /// </summary>
    public static class GameSound
    {
        /// <summary>재생 중인 소리가 없음을 뜻하는 일련번호. 프레임워크는 1부터 발급한다.</summary>
        public const int NoSound = 0;

        /// <summary>반복음의 우선순위. 에이전트가 모자랄 때 한 번 울리고 끝나는 효과음보다 늦게 밀려난다.</summary>
        private const int LoopPriority = 1;

        /// <summary>
        /// 배경음악을 틀어 준 <see cref="SoundComponent"/>. 음악은 판이 바뀌어도 끊지 않으므로,
        /// "이 프레임워크 인스턴스에서 이미 틀었는가" 만 기억하면 된다.
        /// </summary>
        private static SoundComponent sMusicOwner = null;

        /// <summary>한 번 울리고 끝나는 효과음.</summary>
        public static void PlaySfx(string assetName, Vector3 worldPosition)
        {
            if (string.IsNullOrEmpty(assetName)) return;

            SoundComponent sound = GameEntry.GetComponent<SoundComponent>();
            if (sound == null) return;

            sound.PlaySound(assetName, SoundTable.SfxGroup, worldPosition);
        }

        /// <summary>
        /// 멈출 때까지 반복하는 효과음(빙결 콘, 슬라임 지속음). 돌려준 일련번호를
        /// <see cref="Stop"/> 에 넘겨 끈다. 재생하지 못했으면 <see cref="NoSound"/>.
        /// </summary>
        public static int PlayLoop(string assetName, Vector3 worldPosition, float volume)
        {
            if (string.IsNullOrEmpty(assetName)) return NoSound;

            SoundComponent sound = GameEntry.GetComponent<SoundComponent>();
            if (sound == null) return NoSound;

            // PlaySoundParams 는 참조 풀 객체다. 넘기고 나면 프레임워크가 반납한다.
            PlaySoundParams playSoundParams = PlaySoundParams.Create();
            playSoundParams.Loop               = true;
            playSoundParams.VolumeInSoundGroup = volume;

            return sound.PlaySound(assetName, SoundTable.SfxGroup, LoopPriority, playSoundParams, worldPosition);
        }

        /// <summary>
        /// <see cref="PlayLoop"/> 로 낸 소리를 끈다. 아직 로드 중이어도 된다 —
        /// 프레임워크가 로드가 끝나는 순간 재생하지 않고 버린다.
        /// </summary>
        public static void Stop(int serialId)
        {
            if (serialId == NoSound) return;

            SoundComponent sound = GameEntry.GetComponent<SoundComponent>();
            if (sound == null) return;

            // 이미 끝난 소리면 false 가 돌아올 뿐이다.
            sound.StopSound(serialId);
        }

        /// <summary>
        /// 배경음악을 튼다. 이미 틀었으면 아무것도 하지 않는다 —
        /// 판을 다시 시작할 때마다 불려도 음악이 처음으로 돌아가지 않는다.
        /// </summary>
        public static void PlayMusic()
        {
            SoundComponent sound = GameEntry.GetComponent<SoundComponent>();
            if (sound == null) return;

            // 프레임워크가 다시 만들어지면(에디터에서 재생을 다시 누르는 경우 등) 예전 컴포넌트는
            // 파괴된 뒤라 지금 것과 같지 않다. 그때는 다시 튼다.
            if (sMusicOwner == sound) return;

            sMusicOwner = sound;

            PlaySoundParams playSoundParams = PlaySoundParams.Create();
            playSoundParams.Loop = true;

            sound.PlaySound(SoundTable.BackgroundMusic, SoundTable.MusicGroup, playSoundParams);
        }
    }
}
