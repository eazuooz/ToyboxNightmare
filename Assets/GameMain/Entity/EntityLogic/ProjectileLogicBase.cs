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
        /// <summary>이 시간이 지나면 무조건 회수된다. 유실 투사체 방지.</summary>
        protected float MaxLifetime = 3f;

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
            if (mParticles != null)
            {
                foreach (var ps in mParticles)
                {
                    if (ps != null)
                    {
                        ps.Play(true);
                    }
                }
            }

            if (mAudios != null)
            {
                foreach (var audio in mAudios)
                {
                    if (audio != null && audio.clip != null)
                    {
                        audio.Play();
                    }
                }
            }
        }

        protected void StopParticles()
        {
            if (mParticles == null)
            {
                return;
            }

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

            if (IsHiding)
            {
                return;
            }

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
            WeaponUtil.SpawnEffect(logicType, assetName, position, rotation, lifetime);
        }
    }
}
