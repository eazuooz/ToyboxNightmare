using System.Collections.Generic;
using UnityEngine;
using UnityGameFramework.Runtime;

namespace ToyBoxNightmare
{
    /// <summary>
    /// 레티클 3색이 뜻하는 것. 원본 <c>StinkAttack</c>/<c>SlimeAttack</c> 의
    /// <c>invalidTargetTint</c>/<c>notReadyTint</c>/<c>readyTint</c> 와 1:1이다.
    ///
    /// 사거리 3단계(멀다/가깝다/닿는다)가 아니다 — 자동조준 시절의 타겟 마커가 그랬고,
    /// 수동 조준으로 돌아오면서 "지금 이 지점에 쏠 수 있나" 하나만 남았다.
    /// </summary>
    public enum TargetState
    {
        /// <summary>조준 지점이 무효다. 빨강. (Stink: 사거리 밖 / Slime: 대상 없음)</summary>
        Invalid,

        /// <summary>조준은 유효한데 전역 쿨다운이 안 끝났다. 노랑.</summary>
        NotReady,

        /// <summary>지금 쏠 수 있다. 초록.</summary>
        Ready,
    }

    /// <summary>무기와 착탄 이펙트가 함께 쓰는 적 탐색 유틸.</summary>
    public static class WeaponUtil
    {
        // ─── 레티클 색 ───

        /// <summary>원본 레티클 3색. 프리팹 직렬화값 그대로다.</summary>
        public static readonly Color ColorInvalid  = new Color(1f, 0f, 0f, 1f);
        public static readonly Color ColorNotReady = new Color(1f, 0.922f, 0.016f, 1f);
        public static readonly Color ColorReady    = new Color(0f, 1f, 0f, 1f);

        public static Color GetTargetColor(TargetState state)
        {
            switch (state)
            {
                case TargetState.Ready:    return ColorReady;
                case TargetState.NotReady: return ColorNotReady;
                case TargetState.Invalid:  return ColorInvalid;

                default:
                    // TargetState 에 값을 추가하고 여기를 안 고치면 조용히 빨강이 된다.
                    GameAssert.Unreachable("GetTargetColor: 처리되지 않은 TargetState");
                    return ColorInvalid;
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
        /// OverlapSphere 결과 스크래치의 길이.
        ///
        /// 적 1기당 콜라이더가 2개(CapsuleCollider + Shootable 트리거 구)라 <b>실효 상한은 절반</b>인
        /// 128기다. 64였을 때는 32기가 상한이라 몰리는 구간에서 반경 안의 적이 탐색에서 빠졌다 —
        /// Frost 콘 안의 적이 안 얼고 Stink 폭발이 일부를 그냥 지나쳤다.
        /// </summary>
        private const int OverlapBufferSize = 256;

        /// <summary>
        /// OverlapSphere 결과 스크래치. 프레임 할당을 없앤다.
        /// 전역 공유지만 <see cref="FindEnemiesInSphere"/> 안에서만 살아 있고 결과는 호출자
        /// 리스트로 복사되므로, 호출을 중첩해도 서로의 결과를 덮지 않는다.
        /// </summary>
        private static readonly Collider[] sOverlapBuffer = new Collider[OverlapBufferSize];

        /// <summary>
        /// 버퍼 포화를 <b>한 번만</b> 알리기 위한 플래그. 매 프레임 도는 경로라 반복 로그는
        /// 콘솔을 도배해 다른 로그를 전부 밀어낸다. 한 번 뜨면 그것으로 충분한 진단이므로
        /// 일부러 되돌리지 않는다(플레이 모드 재진입 시 도메인 리로드가 알아서 리셋한다).
        /// </summary>
        private static bool sOverlapOverflowReported = false;

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

            int hitCount = Physics.OverlapSphereNonAlloc(origin, radius, sOverlapBuffer, ShootableMask);
            WarnOnBufferSaturation(hitCount);

            for (int i = 0; i < hitCount; i++)
            {
                Enemy enemy = GetAttackableEnemy(sOverlapBuffer[i]);
                if (enemy == null) continue;

                // OverlapSphere 는 **콜라이더 겹침**으로 잡는다. 적의 Shootable 트리거 구가
                // 반지름 0.8~1.63 이라 중심이 반경 밖에 있어도 걸린다. 그대로 두면 Frost 콘과
                // Stink 폭발의 실효 반경이 적 콜라이더 크기만큼 제멋대로 부풀어, 표기 사거리와
                // 실제로 맞는 범위가 어긋난다. 최종 집합을 중심 거리 기준으로 맞춘다.
                if (PlanarDistance(enemy.CachedTransform.position, origin) > radius) continue;

                // 적 1기가 콜라이더 2개로 잡히므로 중복 제거가 필수다(위 주석 참조).
                if (result.Contains(enemy)) continue;

                result.Add(enemy);
            }

            return result.Count;
        }

        /// <summary>
        /// 버퍼가 가득 찼으면 <b>딱 한 번</b> 알린다.
        ///
        /// <c>OverlapSphereNonAlloc</c> 은 초과분을 조용히 버린다 — 반경 안의 적이 탐색에서
        /// 통째로 빠진다. 증상만 보면 "가끔 적을 안 때린다" 라서 원인을 못 찾는다.
        /// 여기서 이름을 붙여 준다.
        /// </summary>
        private static void WarnOnBufferSaturation(int hitCount)
        {
            if (hitCount < sOverlapBuffer.Length || sOverlapOverflowReported) return;

            sOverlapOverflowReported = true;
            Log.Warning("WeaponUtil: 적 탐색 버퍼({0})가 가득 찼다. 초과분이 잘려 반경 안의 적이 빠질 수 있다. "
                        + "OverlapBufferSize 를 키울 것. (이 경고는 한 번만 나온다)", OverlapBufferSize);
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
            // Available 을 IsDead 보다 먼저 본다. IsDead 는 스폰 데이터를 읽는데,
            // 회수된 엔티티의 데이터는 이미 풀로 반납된 뒤라 답을 신뢰할 수 없다.
            if (enemy == null || !enemy.Available || enemy.IsDead) return null;

            return enemy;
        }

        // FindNearestEnemy 는 자동조준 전용이라 삭제했다. 조준은 이제 플레이어가 마우스로 한다.
        // 최근접 탐색이 다시 필요해지면 FindEnemiesInSphere 위에 얹으면 된다.

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
                EffectData.Create(id, 1, position, rotation, lifetime));
        }
    }
}
