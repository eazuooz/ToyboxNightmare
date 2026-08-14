using System.Collections.Generic;
using UnityEngine;
using UnityGameFramework.Runtime;

namespace ToyBoxNightmare
{
    /// <summary>타겟 마커 색이 뜻하는 것 — "내 무기로 지금 때릴 수 있나".</summary>
    public enum TargetState
    {
        /// <summary>사거리 밖. 빨강.</summary>
        OutOfRange,

        /// <summary>사거리 근처 — 조금만 다가가면 닿는다. 노랑.</summary>
        Near,

        /// <summary>사거리 안. 지금 때리고 있다. 초록.</summary>
        InRange,
    }

    /// <summary>무기와 착탄 이펙트가 함께 쓰는 적 탐색 유틸.</summary>
    public static class WeaponUtil
    {
        // ─── 타겟 마커 색 ───

        /// <summary>사거리 안은 아니지만 "곧 닿는" 것으로 볼 배율.</summary>
        public const float NearRangeScale = 1.4f;

        /// <summary>원본 레티클 3색. 그대로 쓴다.</summary>
        public static readonly Color ColorOutOfRange = new Color(1f, 0f, 0f, 1f);
        public static readonly Color ColorNear       = new Color(1f, 0.922f, 0.016f, 1f);
        public static readonly Color ColorInRange    = new Color(0f, 1f, 0f, 1f);

        public static Color GetTargetColor(TargetState state)
        {
            switch (state)
            {
                case TargetState.InRange:    return ColorInRange;
                case TargetState.Near:       return ColorNear;
                case TargetState.OutOfRange: return ColorOutOfRange;

                default:
                    // TargetState 에 값을 추가하고 여기를 안 고치면 조용히 빨강이 된다.
                    GameAssert.Unreachable("GetTargetColor: 처리되지 않은 TargetState");
                    return ColorOutOfRange;
            }
        }

        // ─── 거리 ───

        /// <summary>
        /// XZ 평면 거리. 무기 사거리는 전부 이걸로 잰다 —
        /// 캐릭터 콜라이더 중심이 Y 로 올라가 있어 3D 거리를 쓰면 사거리가 높이차에 흔들린다.
        /// </summary>
        public static float PlanarDistance(Vector3 a, Vector3 b)
        {
            Vector3 delta = a - b;
            delta.y = 0f;
            return delta.magnitude;
        }

        // ─── 적 탐색 ───

        /// <summary>Shootable(9). 적 탐색용.</summary>
        public const int ShootableMask = 1 << 9;

        /// <summary>Shootable(9) | Blocking(10) | Environment(14) = 17920. 원본 Lightning 마스크.</summary>
        public const int HitscanMask = (1 << 9) | (1 << 10) | (1 << 14);

        /// <summary>
        /// OverlapSphere 결과 스크래치. 프레임 할당을 없앤다.
        /// 전역 공유지만 <see cref="FindEnemiesInSphere"/> 안에서만 살아 있고 결과는 호출자
        /// 리스트로 복사되므로, 호출을 중첩해도 서로의 결과를 덮지 않는다.
        /// </summary>
        private static readonly Collider[] sOverlapBuffer = new Collider[64];

        /// <summary>
        /// 구 안의 살아있는 적을 중복 없이 채운다. 반환값은 개수.
        ///
        /// 적 프리팹은 루트에 SphereCollider(trigger)와 CapsuleCollider 가 둘 다 붙어 있고
        /// 둘 다 레이어 9다. Physics.queriesHitTriggers 기본값이 true 라 OverlapSphere 가
        /// 적 1기당 원소를 2개 돌려준다 — 중복 제거를 빼면 디버프가 2회 적용된다.
        /// </summary>
        public static int FindEnemiesInSphere(Vector3 origin, float radius, List<Enemy> result)
        {
            GameAssert.IsTrue(result != null, "FindEnemiesInSphere: 결과 버퍼가 null 이다.");
            if (result == null) return 0;

            result.Clear();

            // 버퍼가 꽉 차면 초과분은 조용히 잘린다. 적 1기당 콜라이더가 2개라 실효 상한은
            // 약 32기다 — 동시 스폰 수를 올릴 때 sOverlapBuffer 도 함께 키워야 한다.
            // (매 프레임 도는 경로라 포화를 로그로 알리지 않는다. 알리면 그 순간부터 도배된다.)
            int hitCount = Physics.OverlapSphereNonAlloc(origin, radius, sOverlapBuffer, ShootableMask);

            for (int i = 0; i < hitCount; i++)
            {
                Enemy enemy = GetAttackableEnemy(sOverlapBuffer[i]);
                if (enemy == null) continue;

                // OverlapSphere 는 **콜라이더 겹침**으로 잡는다. 적의 Shootable 트리거 구가
                // 반지름 0.8~1.63 이라 중심이 반경 밖에 있어도 걸린다. 반면 타겟 마커 색은
                // 중심의 평면 거리로 판정하므로(WeaponBase.EvaluateTarget) 그대로 두면
                // "노란 링(사거리 밖)이 뜬 적이 실제로는 맞고 죽는" 불일치가 난다.
                // 최종 집합을 마커와 같은 기준으로 맞춘다.
                if (PlanarDistance(enemy.CachedTransform.position, origin) > radius) continue;

                // 적 1기가 콜라이더 2개로 잡히므로 중복 제거가 필수다(위 주석 참조).
                if (result.Contains(enemy)) continue;

                result.Add(enemy);
            }

            return result.Count;
        }

        /// <summary>
        /// 콜라이더 하나에서 "지금 때릴 수 있는 적" 을 꺼낸다. 아니면 null.
        /// 바닥·환경 콜라이더, 시체, 회수된 엔티티가 전부 여기서 걸러진다.
        /// </summary>
        private static Enemy GetAttackableEnemy(Collider collider)
        {
            // 같은 프레임에 파괴된 콜라이더가 버퍼에 남아 있을 수 있다.
            if (collider == null) return null;

            Entity entity = collider.GetComponentInParent<Entity>();
            if (entity == null) return null;

            Enemy enemy = entity.Logic as Enemy;
            if (enemy == null || enemy.IsDead || !enemy.Available) return null;

            return enemy;
        }

        /// <summary>
        /// origin 에서 가장 가까운 살아있는 적. 없으면 null.
        ///
        /// 거리 비교만 3D(sqrMagnitude)다. 후보를 고르는 <see cref="FindEnemiesInSphere"/> 는
        /// 평면 거리로 걸렀으므로 집합은 같고, 그 안에서의 순위만 정하는 계산이라 문제가 없다.
        /// </summary>
        public static Enemy FindNearestEnemy(Vector3 origin, float radius, List<Enemy> scratch)
        {
            int count = FindEnemiesInSphere(origin, radius, scratch);

            Enemy nearest = null;
            float nearestDistanceSqr = float.MaxValue;

            for (int i = 0; i < count; i++)
            {
                float distanceSqr = (scratch[i].CachedTransform.position - origin).sqrMagnitude;
                if (distanceSqr < nearestDistanceSqr)
                {
                    nearestDistanceSqr = distanceSqr;
                    nearest            = scratch[i];
                }
            }

            return nearest;
        }

        // ─── 이펙트 스폰 ───

        /// <summary>
        /// 일회성 이펙트 엔티티를 띄운다.
        ///
        /// 원본은 씬에 하나씩 깔아둔 오브젝트를 좌표만 옮겨 재사용했다(그래서 동시 명중이
        /// 겹치면 이전 이펙트가 끊겼다). 엔티티 풀로 띄우면 그 제약이 사라진다.
        /// </summary>
        public static void SpawnEffect(System.Type logicType, string assetName,
                                       Vector3 position, Quaternion rotation, float lifetime)
        {
            // 주소가 비면 ShowEntity 가 조용히 실패한다(CLAUDE.md 의 Addressables 규약).
            // 여기서 잡아 두지 않으면 "이펙트가 안 나온다" 만 남고 원인이 안 보인다.
            if (logicType == null || string.IsNullOrEmpty(assetName))
            {
                Log.Error("SpawnEffect: 이펙트 종류나 주소가 비어 있다. (logicType={0}, assetName={1})",
                    logicType, assetName);
                return;
            }

            GameAssert.IsTrue(lifetime > 0f, "SpawnEffect: lifetime 이 0 이하면 이펙트가 보이기 전에 회수된다.");

            EntityComponent entityComponent = GameEntry.GetComponent<EntityComponent>();
            if (entityComponent == null)
            {
                Log.Error("SpawnEffect: EntityComponent 가 없다. GameFramework 프리팹 구성을 확인할 것.");
                return;
            }

            int id = EntitySerialId.Next();
            entityComponent.ShowEntity(
                id,
                logicType,
                assetName,
                WeaponTable.EffectGroup,
                new EffectData(id, 1)
                {
                    Position = position,
                    Rotation = rotation,
                    Lifetime = lifetime,
                });
        }
    }
}
