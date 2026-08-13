using UnityEngine;
using UnityGameFramework.Runtime;

namespace ToyBoxNightmare
{
    /// <summary>
    /// 일회성 착탄 이펙트. 수명이 지나면 스스로 회수된다.
    ///
    /// 원본은 씬에 고정된 오브젝트 하나를 텔레포트시켜 재생하는 방식이라
    /// 동시 명중이 겹치면 이전 이펙트가 끊겼다. 엔티티로 스폰해 그 문제를 없앤다.
    /// </summary>
    public class HitEffect : EntityLogicBase
    {
        private ParticleSystem[] mParticles = null;
        private AudioSource[]    mAudios    = null;

        private float mLifetime = 2f;
        private float mElapsed  = 0f;

        protected internal override void OnInit(object userData)
        {
            base.OnInit(userData);

            mParticles = GetComponentsInChildren<ParticleSystem>(true);
            mAudios    = GetComponentsInChildren<AudioSource>(true);
        }

        protected internal override void OnShow(object userData)
        {
            base.OnShow(userData);

            var data = userData as EffectData;
            mLifetime = data != null ? data.Lifetime : 2f;
            mElapsed  = 0f;

            if (data != null)
            {
                CachedTransform.position = data.Position;
                CachedTransform.rotation = data.Rotation;
            }

            // 배치가 끝난 뒤에 재생한다. 프리팹에 따라 PlayOnAwake 가 0 인 것도 있어
            // 명시 호출이 반드시 필요하다(SlimeHit 이 그렇다).
            RestartEffects();

            OnEffectStarted();
        }

        /// <summary>파생 클래스가 착탄 판정 같은 1회성 처리를 여기에 넣는다.</summary>
        protected virtual void OnEffectStarted()
        {
        }

        protected internal override void OnUpdate(float elapseSeconds, float realElapseSeconds)
        {
            base.OnUpdate(elapseSeconds, realElapseSeconds);

            if (IsHiding)
            {
                return;
            }

            mElapsed += elapseSeconds;
            if (mElapsed >= mLifetime)
            {
                SafeHide();
            }
        }

        private void RestartEffects()
        {
            if (mParticles != null)
            {
                foreach (var ps in mParticles)
                {
                    if (ps == null)
                    {
                        continue;
                    }

                    ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                    ps.Play(true); // 인자 true 가 자식까지 재생한다. 빼면 스파크가 안 나온다
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
    }
}
