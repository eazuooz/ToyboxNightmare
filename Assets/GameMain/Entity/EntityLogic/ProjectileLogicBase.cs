using UnityEngine;
using UnityGameFramework.Runtime;

namespace ToyBoxNightmare
{
    /// <summary>
    /// 투사체 공통 베이스. 수명 상한과 착탄 이펙트 스폰만 공유한다 —
    /// 궤적(포물선/유도)은 파생 클래스가 전적으로 결정한다.
    /// </summary>
    public abstract class ProjectileLogicBase : EntityLogicBase
    {
        /// <summary>파생 클래스가 따로 잡지 않았을 때 쓰는 수명 상한.</summary>
        private const float DefaultMaxLifetime = 3f;

        /// <summary>이 시간이 지나면 무조건 회수된다. 유실 투사체 방지.</summary>
        protected float MaxLifetime = DefaultMaxLifetime;

        private float mElapsed = 0f;

        /// <summary>이 투사체가 들고 있는 파티클. 풀 재사용 시 Stop → 배치 → Play 순서가 중요하다.</summary>
        private ParticleSystem[] mParticles = null;
        private AudioSource[]    mAudios    = null;

        protected internal override void OnInit(object userData)
        {
            base.OnInit(userData);

            mParticles = GetComponentsInChildren<ParticleSystem>(true);
            mAudios    = GetComponentsInChildren<AudioSource>(true);
        }

        protected internal override void OnShow(object userData)
        {
            base.OnShow(userData);

            mElapsed = 0f;

            // 배치 전에 반드시 멈춘다. 순서를 지키지 않으면 재사용 시
            // 이전 위치에서 현재 위치까지 연기 줄기가 그어진다.
            StopParticles();
        }

        /// <summary>위치를 다 잡은 뒤 파생 클래스가 호출한다.</summary>
        protected void PlayEffects()
        {
            PlayParticles();
            PlayAudios();
        }

        private void PlayParticles()
        {
            if (mParticles == null) return;

            foreach (var ps in mParticles)
            {
                if (ps != null)
                {
                    ps.Play(true); // 인자 true 가 자식까지 재생한다
                }
            }
        }

        private void PlayAudios()
        {
            if (mAudios == null) return;

            foreach (var audio in mAudios)
            {
                if (audio != null && audio.clip != null)
                {
                    audio.Play();
                }
            }
        }

        protected void StopParticles()
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

        protected internal override void OnUpdate(float elapseSeconds, float realElapseSeconds)
        {
            base.OnUpdate(elapseSeconds, realElapseSeconds);

            if (IsHiding) return;

            mElapsed += elapseSeconds;
            if (mElapsed >= MaxLifetime)
            {
                SafeHide();
                return;
            }

            OnFly(elapseSeconds);
        }

        /// <summary>매 프레임 궤적을 진행시킨다.</summary>
        protected abstract void OnFly(float elapseSeconds);

        /// <summary>착탄 이펙트 엔티티를 띄운다.</summary>
        protected static void SpawnEffect(System.Type logicType, string assetName,
                                          Vector3 position, Quaternion rotation, float lifetime)
        {
            // assetName 이 비면 ShowEntity 가 조용히 실패해 "착탄했는데 아무것도 안 보이는"
            // 증상만 남는다. 여기서 걸러 원인을 로그에 남긴다.
            if (logicType == null || string.IsNullOrEmpty(assetName))
            {
                Log.Error("착탄 이펙트 스폰 인자가 잘못됐다. logicType={0}, assetName='{1}' — WeaponTable 을 확인할 것.",
                    logicType, assetName);
                return;
            }

            WeaponUtil.SpawnEffect(logicType, assetName, position, rotation, lifetime);
        }
    }
}
