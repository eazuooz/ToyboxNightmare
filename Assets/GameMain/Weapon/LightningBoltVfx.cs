using UnityEngine;

namespace ToyBoxNightmare
{
    /// <summary>
    /// Girl/Boy 프리팹의 <c>Antenna/LightningAttack/LightningBolt</c> 를 구동한다.
    /// 그 GameObject 에는 LineRenderer(ZapBeamAdditive) · ParticleSystem · Light ·
    /// AudioSource 가 이미 붙어 있고 기본 비활성 상태다.
    ///
    /// 원본 LightningBolt.cs 의 정리본이다. 핵심은 <b>지그재그 폴리라인</b>이다 —
    /// 원본은 AnimationCurve 6개를 미리 만들어 두고 매 프레임 하나씩 돌려가며
    /// 정점을 재배치한다. 직선 2점으로 그리면 완전히 다른 물건이 된다.
    ///
    /// 아래 커브 상수는 원본 Girl.prefab 의 rayPhases 직렬화 실측값이다.
    /// </summary>
    public class LightningBoltVfx : MonoBehaviour
    {
        /// <summary>빔이 보이는 시간. 원본 effectDuration.</summary>
        private const float EffectDuration = 0.75f;

        /// <summary>
        /// 형상 재생성 주기. 원본 프리팹 실효값(코드 기본값 0.1 이 아니다).
        /// 프레임 시간보다 짧아 사실상 매 프레임 새 형상이 된다 — 그게 지지직거리는 느낌의 정체다.
        /// <b>가시성 토글이 아니다.</b> 원본은 0.75초 동안 빔을 한 번도 끄지 않는다.
        /// </summary>
        private const float PhaseDuration = 0.01f;

        /// <summary>지그재그 세로 진폭 배율. 원본 rayHeight.</summary>
        private const float RayHeight = 2f;

        // 원본 rayPhases — 커브별 key.time.
        // 값 하나가 정점 하나이며, 0=발사점 / 1=착탄점 사이의 보간 비율이다.
        // PhaseKeyOffsets 와 인덱스가 1:1 로 대응한다(같은 위상의 같은 정점).
        private static readonly float[][] PhaseKeyTimes =
        {
            new[]{0f,.0355f,.0757f,.0974f,.1174f,.1388f,.1504f,.1934f,.2206f,.3176f,
                  .3429f,.3995f,.4697f,.4894f,.5059f,.5123f,.5563f,.5808f,.5912f,.6593f,
                  .6619f,.7049f,.7203f,.7769f,.8166f,.8737f,.8741f,.9209f,.9662f,1f},
            new[]{0f,.0173f,.0261f,.0528f,.0693f,.0826f,.1010f,.2245f,.2640f,.3278f,
                  .4391f,.5022f,.5642f,.5961f,.6211f,.7924f,.9337f,1f},
            new[]{0f,.0173f,.0261f,.0528f,.9684f,1f},
            new[]{0f,.0173f,.0261f,.0528f,.0670f,.0818f,.1010f,.1395f,.1489f,.1600f,
                  .1702f,.2127f,.2198f,.2245f,.2396f,.2530f,.2569f,.2740f,.2837f,.2963f,
                  .3318f,.3459f,.3514f,.3616f,.3861f,.3968f,.4115f,.4318f,.4619f,.4752f,
                  .4943f,.5216f,.5532f,.5784f,.5922f,.6329f,.7675f,.7841f,.8037f,.8252f,
                  .8597f,.9109f,.9188f,.9345f,.9503f,.9684f,1f},
            new[]{0f,.0173f,.0261f,.0528f,.3459f,.7342f,.9109f,.9684f,1f},
            new[]{0f,.0173f,.0261f,.0528f,.3861f,.3968f,.4115f,.4318f,.4619f,.4943f,
                  .5216f,.5532f,.6329f,.7675f,.7841f,.8037f,.8252f,.8597f,.9109f,.9188f,
                  .9345f,.9503f,.9684f,1f},
        };

        // 원본 rayPhases — 커브별 key.value.
        // 해당 정점을 직선에서 밀어내는 양이다. 월드 +Y 로만 흔들린다(수평 흔들림은 없다).
        private static readonly float[][] PhaseKeyOffsets =
        {
            new[]{0f,-.0017f,.0296f,-.0178f,.0006f,-.0099f,.0144f,-.0087f,.0239f,-.0161f,
                  .0184f,-.0066f,.0256f,-.0026f,.0059f,-.0016f,-.0014f,-.0184f,.0194f,-.0064f,
                  .0078f,.0057f,.0260f,-.0170f,.0101f,-.0102f,.0092f,-.0060f,-.0004f,0f},
            new[]{-.0008f,.0282f,-.0127f,-.0124f,-.2070f,.1440f,-.0242f,-.0468f,-.0627f,.2171f,
                  -.0259f,-.0454f,.0032f,.0550f,-.0362f,.1051f,-.0055f,-.0008f},
            new[]{.0011f,-.0266f,-.0109f,-.0105f,.0012f,.0011f},
            new[]{.0011f,-.0266f,-.0109f,-.0105f,-.0275f,.0120f,-.0223f,-.0213f,.0048f,-.0666f,
                  -.0307f,-.0290f,-.0821f,-.0449f,-.0868f,.0202f,-.0403f,-.0404f,-.1521f,-.0104f,
                  -.0130f,.0599f,-.0683f,.0020f,-.1093f,.0191f,-.0089f,-.0063f,-.0054f,-.1620f,
                  -.0045f,.0384f,-.0365f,.1551f,-.1281f,.0006f,.0031f,-.0746f,.1055f,-.0041f,
                  -.0043f,-.0057f,.0333f,-.0442f,.0421f,.0012f,.0011f},
            new[]{.0011f,-.0266f,-.0109f,-.0105f,.0599f,-.0261f,-.0057f,.0012f,.0011f},
            new[]{.0011f,-.0266f,-.0109f,-.0105f,-.1093f,.0191f,-.0089f,-.0063f,-.0054f,-.0045f,
                  .0384f,-.0365f,.0006f,.0031f,-.0746f,.1055f,-.0041f,-.0043f,-.0057f,.0333f,
                  -.0442f,.0421f,.0012f,.0011f},
        };

        private LineRenderer     mLine      = null;
        private Light            mLight     = null;
        private AudioSource      mAudio     = null;
        private ParticleSystem[] mParticles = null;

        private Vector3 mEndPoint  = Vector3.zero;
        private float   mRemaining = 0f;
        private float   mPhaseTimer = 0f;

        // 원본은 OnEnable 에서 이 인덱스를 리셋하지 않는다. 발사마다 다른 커브로 시작해야
        // 반복 발사가 똑같아 보이지 않는다.
        private int mPhaseIndex = 0;

        private void Awake()
        {
            mLine      = GetComponent<LineRenderer>();
            mLight     = GetComponent<Light>();
            mAudio     = GetComponent<AudioSource>();
            mParticles = GetComponentsInChildren<ParticleSystem>(true);

            if (mLine != null)
            {
                mLine.useWorldSpace = true;
            }

            DisableAutoPlay();
            StopParticles();
            SetVisible(false);
        }

        /// <summary>
        /// 이 GameObject 는 프리팹에서 비활성이었다가 무기 장착 시 켜진다.
        /// Play On Awake 파티클이 그대로 돌면 안테나에서 계속 스파크가 튄다.
        ///
        /// 한 번 멈추는 것만으로는 부족하다. 이 GameObject 의 부모(Antenna/LightningAttack)가
        /// 무기 전환 때마다 껐다 켜지는데, playOnAwake 파티클은 <b>재활성될 때마다</b> 다시
        /// 재생을 시작한다(프리팹 실측: playOnAwake=True, loop=True). 그러면 발사와 무관하게
        /// 안테나 끝에서 착탄 스파크가 영구히 루프한다. 자동 재생 자체를 끈다.
        /// </summary>
        private void DisableAutoPlay()
        {
            if (mParticles != null)
            {
                foreach (var ps in mParticles)
                {
                    if (ps == null) continue;

                    ParticleSystem.MainModule main = ps.main;
                    main.playOnAwake = false;
                }
            }

            if (mAudio != null)
            {
                mAudio.playOnAwake = false;
            }
        }

        /// <summary>
        /// 빔을 표시한다. 시작점은 받지 않는다 — 이 GameObject 자신의 위치(=안테나)를
        /// 매 프레임 다시 읽으므로, 발사 후 플레이어가 움직여도 빔이 손에 붙어 있는다.
        /// </summary>
        public void Play(Vector3 endPoint)
        {
            mEndPoint   = endPoint;
            mRemaining  = EffectDuration;
            mPhaseTimer = 0f;

            SetVisible(true);
            ChangePhase();
            PlayParticles();
            PlayAudio();
        }

        /// <summary>
        /// 빔을 즉시 끈다.
        ///
        /// <b>반드시 무기를 내려놓는 쪽에서 불러야 한다.</b> 이 GameObject 는 무기 VFX 루트
        /// (<c>Antenna/LightningAttack</c>)의 자식이라, 무기를 끄면 계층상 비활성이 되어
        /// <see cref="Update"/> 가 멈춘다. 그러면 <c>mRemaining &gt; 0</c> / LineRenderer 켜짐 /
        /// 마지막 월드 좌표 폴리라인이 그대로 얼어붙고, 다음에 무기를 다시 켜는 순간
        /// <b>이전 발사의 표적 지점으로 향하는 유령 빔</b>이 잔여 시간만큼 그어진다.
        /// </summary>
        public void StopImmediate()
        {
            mRemaining  = 0f;
            mPhaseTimer = 0f;

            SetVisible(false);
            StopParticles();
            StopAudio();
        }

        private void Update()
        {
            if (mRemaining <= 0f) return;

            mRemaining -= Time.deltaTime;
            if (mRemaining <= 0f)
            {
                // 수명이 다했다. 형상만 지운다 — 오디오를 끊는 곳은 StopImmediate 뿐이다.
                SetVisible(false);
                StopParticles();
                return;
            }

            mPhaseTimer += Time.deltaTime;
            if (mPhaseTimer < PhaseDuration) return;

            mPhaseTimer = 0f;
            ChangePhase();
        }

        /// <summary>다음 커브로 넘어가 정점을 다시 배치한다.</summary>
        private void ChangePhase()
        {
            if (mLine == null) return;

            // 원본과 같은 순서 — 쓰기 '전에' 증가시킨다.
            mPhaseIndex++;
            if (mPhaseIndex >= PhaseKeyTimes.Length)
            {
                mPhaseIndex = 0;
            }

            float[] keyTimes   = PhaseKeyTimes[mPhaseIndex];
            float[] keyOffsets = PhaseKeyOffsets[mPhaseIndex];

            // 두 배열은 같은 정점을 가리키는 쌍이다. 길이가 어긋나면 한쪽이 범위를 넘는다.
            GameAssert.IsTrue(keyTimes.Length == keyOffsets.Length,
                "LightningBoltVfx: PhaseKeyTimes 와 PhaseKeyOffsets 의 길이가 다르다.");

            int pointCount = Mathf.Min(keyTimes.Length, keyOffsets.Length);

            Vector3 start      = transform.position;
            Vector3 startToEnd = mEndPoint - start;

            mLine.positionCount = pointCount;
            for (int i = 0; i < pointCount; i++)
            {
                // 직선 위의 한 점을 잡고, 그 점만 월드 +Y 로 밀어 지그재그를 만든다.
                Vector3 point = start + startToEnd * keyTimes[i];
                point += Vector3.up * (keyOffsets[i] * RayHeight);
                mLine.SetPosition(i, point);
            }
        }

        private void PlayParticles()
        {
            if (mParticles == null) return;

            foreach (var ps in mParticles)
            {
                if (ps != null)
                {
                    ps.Play(true);
                }
            }
        }

        private void StopParticles()
        {
            if (mParticles == null) return;

            foreach (var ps in mParticles)
            {
                if (ps != null)
                {
                    ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                }
            }
        }

        /// <summary>클립이 비어 있으면 재생하지 않는다 — 빈 AudioSource 는 경고만 남긴다.</summary>
        private void PlayAudio()
        {
            if (mAudio != null && mAudio.clip != null)
            {
                mAudio.Play();
            }
        }

        private void StopAudio()
        {
            if (mAudio != null)
            {
                mAudio.Stop();
            }
        }

        private void SetVisible(bool visible)
        {
            if (mLine != null)
            {
                mLine.enabled = visible;
            }

            if (mLight != null)
            {
                mLight.enabled = visible;
            }
        }
    }
}
