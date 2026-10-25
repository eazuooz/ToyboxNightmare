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
    public class HitEffectLogic : EntityLogicBase
    {
        private ParticleSystem[] mParticles = null;

        private float mElapsed = 0f;

        /// <summary>스폰 데이터(수명·배치). 파생 이펙트는 자기 데이터 타입으로 읽는다. 회수된 뒤에는 null.</summary>
        protected HitEffectData Data { get; private set; }

        /// <summary>데이터가 정한 수명. 데이터 없이 떴으면 기본 수명이다.</summary>
        private float Lifetime
        {
            get { return Data != null ? Data.Lifetime : HitEffectData.DefaultLifetime; }
        }

        protected internal override void OnInit(object userData)
        {
            base.OnInit(userData);

            mParticles = GetComponentsInChildren<ParticleSystem>(true);

            // 착탄 이펙트인데 재생할 것이 하나도 없으면 프리팹 구성이 잘못된 것이다.
            // 게임플레이는 그대로 돌아가므로 경고만 남기고 진행한다.
            // (소리는 여기서 내지 않는다. 이펙트를 띄우는 쪽이 GameSound 로 낸다.)
            if (mParticles.Length == 0)
            {
                Log.Warning("HitEffectLogic '{0}' 에 ParticleSystem 이 없다. 프리팹 구성을 확인할 것.", Name);
            }
        }

        protected internal override void OnShow(object userData)
        {
            base.OnShow(userData);

            mElapsed = 0f;
            Data     = userData as HitEffectData;
            ApplyEffectData(Data);

            // 배치가 끝난 뒤에 재생한다. 프리팹에 따라 PlayOnAwake 가 0 인 것도 있어
            // 명시 호출이 반드시 필요하다(SlimeHit 이 그렇다).
            RestartEffects();

            OnEffectStarted();
        }

        /// <summary>
        /// 수명과 배치를 데이터에서 가져온다.
        /// 데이터가 없으면 지금 자리에서 기본 수명으로 튼다 — 이펙트를 통째로 씹는 것보다 낫다.
        /// </summary>
        private void ApplyEffectData(HitEffectData data)
        {
            if (data == null)
            {
                // WeaponUtil.SpawnEffect 는 항상 HitEffectData 를 넘긴다. 여기 오면 호출부 실수다.
                Log.Warning("HitEffectLogic '{0}' 에 HitEffectData 가 오지 않았다. 배치를 건너뛰고 기본 수명 {1}초로 재생한다.",
                    Name, HitEffectData.DefaultLifetime);
                return;
            }

            // 0 이하면 첫 OnUpdate 에서 곧바로 회수되어 화면에 아무것도 안 남는다.
            GameAssert.IsTrue(data.Lifetime > 0f, "HitEffectData.Lifetime 이 0 이하다. 이펙트가 즉시 회수된다.");

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
            if (mElapsed >= Lifetime)
            {
                SafeHide();
            }
        }

        protected internal override void OnHide(bool isShutdown, object userData)
        {
            // 데이터는 base 체인 끝에서 참조 풀로 반납된다. 반납된 것을 들고 있지 않는다.
            Data = null;

            base.OnHide(isShutdown, userData);
        }

        private void RestartEffects()
        {
            RestartParticles();
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
    }
}
