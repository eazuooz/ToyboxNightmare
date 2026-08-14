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
        /// <summary>EffectData 가 오지 않았을 때 쓸 수명. 원본 파티클(0.5~0.75초)이 다 끝나고도 남는 길이다.</summary>
        private const float FallbackLifetime = 2f;

        private ParticleSystem[] mParticles = null;
        private AudioSource[]    mAudios    = null;

        private float mLifetime = FallbackLifetime;
        private float mElapsed  = 0f;

        protected internal override void OnInit(object userData)
        {
            base.OnInit(userData);

            mParticles = GetComponentsInChildren<ParticleSystem>(true);
            mAudios    = GetComponentsInChildren<AudioSource>(true);

            // 착탄 이펙트인데 재생할 것이 하나도 없으면 프리팹 구성이 잘못된 것이다.
            // 게임플레이는 그대로 돌아가므로 경고만 남기고 진행한다.
            if (mParticles.Length == 0 && mAudios.Length == 0)
            {
                Log.Warning("HitEffect '{0}' 에 ParticleSystem 도 AudioSource 도 없다. 프리팹 구성을 확인할 것.", Name);
            }
        }

        protected internal override void OnShow(object userData)
        {
            base.OnShow(userData);

            mElapsed = 0f;
            ApplyEffectData(userData as EffectData);

            // 배치가 끝난 뒤에 재생한다. 프리팹에 따라 PlayOnAwake 가 0 인 것도 있어
            // 명시 호출이 반드시 필요하다(SlimeHit 이 그렇다).
            RestartEffects();

            OnEffectStarted();
        }

        /// <summary>
        /// 수명과 배치를 데이터에서 가져온다.
        /// 데이터가 없으면 지금 자리에서 기본 수명으로 튼다 — 이펙트를 통째로 씹는 것보다 낫다.
        /// </summary>
        private void ApplyEffectData(EffectData data)
        {
            if (data == null)
            {
                // WeaponUtil.SpawnEffect 는 항상 EffectData 를 넘긴다. 여기 오면 호출부 실수다.
                Log.Warning("HitEffect '{0}' 에 EffectData 가 오지 않았다. 배치를 건너뛰고 기본 수명 {1}초로 재생한다.",
                    Name, FallbackLifetime);
                mLifetime = FallbackLifetime;
                return;
            }

            // 0 이하면 첫 OnUpdate 에서 곧바로 회수되어 화면에 아무것도 안 남는다.
            GameAssert.IsTrue(data.Lifetime > 0f, "EffectData.Lifetime 이 0 이하다. 이펙트가 즉시 회수된다.");

            mLifetime = data.Lifetime;

            CachedTransform.position = data.Position;
            CachedTransform.rotation = data.Rotation;
        }

        /// <summary>파생 클래스가 착탄 판정 같은 1회성 처리를 여기에 넣는다.</summary>
        protected virtual void OnEffectStarted()
        {
        }

        protected internal override void OnUpdate(float elapseSeconds, float realElapseSeconds)
        {
            base.OnUpdate(elapseSeconds, realElapseSeconds);

            if (IsHiding) return;

            mElapsed += elapseSeconds;
            if (mElapsed >= mLifetime)
            {
                SafeHide();
            }
        }

        private void RestartEffects()
        {
            RestartParticles();
            RestartAudios();
        }

        /// <summary>풀에서 재사용되므로 이전 재생분을 지우고 처음부터 다시 튼다.</summary>
        private void RestartParticles()
        {
            if (mParticles == null) return;

            foreach (var ps in mParticles)
            {
                if (ps == null) continue;

                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                ps.Play(true); // 인자 true 가 자식까지 재생한다. 빼면 스파크가 안 나온다
            }
        }

        private void RestartAudios()
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
    }
}
