using System.Collections.Generic;
using UnityEngine;
using UnityGameFramework.Runtime;

namespace ToyBoxNightmare
{
    /// <summary>무기와 착탄 이펙트가 함께 쓰는 적 탐색 유틸.</summary>
    public static class WeaponUtil
    {
        /// <summary>Shootable(9). 적 탐색용.</summary>
        public const int ShootableMask = 1 << 9;

        /// <summary>Shootable(9) | Blocking(10) | Environment(14) = 17920. 원본 Lightning 마스크.</summary>
        public const int HitscanMask = (1 << 9) | (1 << 10) | (1 << 14);

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
            result.Clear();

            int count = Physics.OverlapSphereNonAlloc(origin, radius, sOverlapBuffer, ShootableMask);
            for (int i = 0; i < count; i++)
            {
                Entity entity = sOverlapBuffer[i].GetComponentInParent<Entity>();
                if (entity == null)
                {
                    continue;
                }

                Enemy enemy = entity.Logic as Enemy;
                if (enemy == null || enemy.IsDead || !enemy.Available)
                {
                    continue;
                }

                if (result.Contains(enemy))
                {
                    continue;
                }

                result.Add(enemy);
            }

            return result.Count;
        }

        /// <summary>
        /// 일회성 이펙트 엔티티를 띄운다.
        ///
        /// 원본은 씬에 하나씩 깔아둔 오브젝트를 좌표만 옮겨 재사용했다(그래서 동시 명중이
        /// 겹치면 이전 이펙트가 끊겼다). 엔티티 풀로 띄우면 그 제약이 사라진다.
        /// </summary>
        public static void SpawnEffect(System.Type logicType, string assetName,
                                       Vector3 position, Quaternion rotation, float lifetime)
        {
            int id = EntitySerialId.Next();
            GameEntry.GetComponent<EntityComponent>().ShowEntity(
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

        /// <summary>origin 에서 가장 가까운 살아있는 적. 없으면 null.</summary>
        public static Enemy FindNearestEnemy(Vector3 origin, float radius, List<Enemy> scratch)
        {
            int count = FindEnemiesInSphere(origin, radius, scratch);

            Enemy nearest = null;
            float minSqr = float.MaxValue;

            for (int i = 0; i < count; i++)
            {
                float sqr = (scratch[i].CachedTransform.position - origin).sqrMagnitude;
                if (sqr < minSqr)
                {
                    minSqr = sqr;
                    nearest = scratch[i];
                }
            }

            return nearest;
        }
    }
}
