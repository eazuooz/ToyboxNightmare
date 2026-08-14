using System.Diagnostics;
using UnityEngine;

namespace ToyBoxNightmare
{
    /// <summary>
    /// 적 한 종의 전투 스탯. 값은 전부 원본 ZombieToys 프리팹의 직렬화 실효값이다.
    /// </summary>
    public readonly struct EnemyStats
    {
        /// <summary>원본 트리거 구가 플레이어 콜라이더에 닿는 거리를 모델링하기 위한 보정값.</summary>
        private const float PlayerColliderRadius = 0.5f;

        /// <summary>Addressables 주소이자 종 식별자.</summary>
        public readonly string AssetName;

        public readonly int   MaxHitPoints;
        public readonly int   AttackDamage;
        public readonly float TimeBetweenAttacks;
        public readonly int   ScoreValue;
        public readonly float MoveSpeed;
        public readonly float StoppingDistance;

        /// <summary>원본 프리팹의 공격 판정용 SphereCollider 반지름.</summary>
        public readonly float AttackSphereRadius;

        /// <summary>
        /// 실제 공격 판정 거리(XZ 평면 기준).
        /// 원본은 "적 중심의 트리거 구 ↔ 플레이어 콜라이더" 접촉이었으므로,
        /// 구 반지름에 플레이어 반지름을 더한 값으로 환산한다.
        /// 이 값이 StoppingDistance 보다 커야 적이 멈춘 자리에서 공격이 닿는다.
        ///
        /// 트리거 접촉의 근사이므로 오차가 있다. 콜라이더 모양을 정확히 반영하지는 않지만,
        /// 거리를 XZ 평면에서 재기 때문에 높이 오프셋에 의한 흔들림은 없다.
        /// </summary>
        public float AttackRange => AttackSphereRadius + PlayerColliderRadius;

        public EnemyStats(string assetName, int maxHitPoints, int attackDamage, float timeBetweenAttacks,
                          int scoreValue, float moveSpeed, float stoppingDistance, float attackSphereRadius)
        {
            AssetName          = assetName;
            MaxHitPoints       = maxHitPoints;
            AttackDamage       = attackDamage;
            TimeBetweenAttacks = timeBetweenAttacks;
            ScoreValue         = scoreValue;
            MoveSpeed          = moveSpeed;
            StoppingDistance   = stoppingDistance;
            AttackSphereRadius = attackSphereRadius;
        }
    }

    /// <summary>스포너 한 지점의 설정. 원본 Spawn Points.prefab 값.</summary>
    public readonly struct EnemySpawnPoint
    {
        public readonly string  AssetName;
        public readonly Vector3 Position;

        /// <summary>스폰 간격(초).</summary>
        public readonly float Interval;

        /// <summary>이 종의 동시 생존 상한.</summary>
        public readonly int MaxAlive;

        public EnemySpawnPoint(string assetName, Vector3 position, float interval, int maxAlive)
        {
            AssetName = assetName;
            Position  = position;
            Interval  = interval;
            MaxAlive  = maxAlive;
        }
    }

    /// <summary>
    /// 적 관련 상수와 테이블. 현재는 C# 상수지만, 튜닝이 잦아지면
    /// ScriptableObject 나 DataTable 로 옮기기 쉬운 형태로 모아 두었다.
    /// </summary>
    public static class EnemyTable
    {
        /// <summary>추적 목적지 갱신 주기. 원본 EnemyMovement 재현.</summary>
        public const float RetargetInterval = 0.5f;

        /// <summary>사망 연출 시작부터 회수까지의 시간.</summary>
        public const float DeathEffectTime = 2f;

        /// <summary>사망 후 바닥으로 가라앉는 속도.</summary>
        public const float SinkSpeed = 2.5f;

        /// <summary>엔티티 그룹 이름. GameFramework.prefab 의 mEntityGroups 와 일치해야 한다.</summary>
        public const string EntityGroup = "Enemy";

        //                                     주소            HP   공격  간격   점수  속도  정지   공격구
        private static readonly EnemyStats[] Table =
        {
            new EnemyStats("Zombunny",        100,  10,  0.5f,  10,  3.5f, 1.1f, 0.8f),
            new EnemyStats("ZomBear",         100,  10,  0.5f,  10,  3.5f, 1.1f, 0.8f),
            new EnemyStats("ZombieDuck",      120,  20,  1.0f,  20,  3.5f, 1.1f, 1.0f),
            new EnemyStats("Clown",           150,  30,  1.5f,  25,  3.5f, 1.1f, 0.8f),
            new EnemyStats("Hellephant",      200,  35,  2.0f,  50,  3.5f, 1.9f, 1.63f),
        };

        /// <summary>
        /// 스폰 지점. 좌표는 원본 Spawn Points.prefab 값이며 베이크된 NavMesh 위에 있음을 확인했다.
        /// 동시 생존 상한 합계 13.
        /// </summary>
        public static readonly EnemySpawnPoint[] SpawnPoints =
        {
            new EnemySpawnPoint("Zombunny",   new Vector3(-20.5f,  0f,  12.5f),  5f, 4),
            new EnemySpawnPoint("ZomBear",    new Vector3( 22.5f,  0f,  15f),    6f, 3),
            new EnemySpawnPoint("Hellephant", new Vector3(  0f,    0f,  32f),   10f, 2),
            new EnemySpawnPoint("ZombieDuck", new Vector3( -0.86f, 0f, -33.1f), 10f, 2),
            new EnemySpawnPoint("Clown",      new Vector3(-26.55f, 0f,   5.29f),15f, 2),
        };

        public static bool TryGetStats(string assetName, out EnemyStats stats)
        {
            for (int i = 0; i < Table.Length; i++)
            {
                if (Table[i].AssetName == assetName)
                {
                    stats = Table[i];
                    return true;
                }
            }

            stats = default;
            return false;
        }

        /// <summary>
        /// 테이블 정합성 검사. 두 가지를 본다.
        ///
        /// 1. 모든 <see cref="SpawnPoints"/> 의 주소가 <see cref="Table"/> 에 있는가.
        ///    없으면 그 지점은 스폰 틱마다 "Enemy stats not found" 만 찍고 영원히 아무것도 못 낸다.
        ///    스폰 간격이 5~15초라 로그가 드문드문 나와 알아채기까지 오래 걸린다.
        ///
        /// 2. 같은 주소를 쓰는 스폰 지점이 둘 이상인가.
        ///    SurvivalGame 의 대기 카운트 처리(DecrementPendingSpawn / IsSpawnLimitReached)가
        ///    "한 주소 = 한 지점" 을 전제로 짜여 있다. 중복이 생기면 뒤쪽 지점의 대기 카운트가
        ///    영원히 안 줄어 그 지점이 상한에 고착된다. 자세한 근거는 두 함수의 주석 참조.
        ///
        /// 에디터와 개발 빌드에서만 돈다. 릴리스에서는 <see cref="ConditionalAttribute"/> 가
        /// 호출부를 통째로 제거하므로 부담이 없다. 값이 코드 상수라 런타임에 바뀔 일이 없어
        /// 판마다 한 번만 부르면 충분하다.
        /// </summary>
        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        public static void Validate()
        {
            for (int i = 0; i < SpawnPoints.Length; i++)
            {
                string assetName = SpawnPoints[i].AssetName;

                // 스탯 값 자체는 필요 없다. 존재 여부만 본다.
                GameAssert.IsTrue(TryGetStats(assetName, out _), string.Format(
                    "EnemyTable.SpawnPoints[{0}] 의 주소 '{1}' 가 Table 에 없다. 이 지점은 영원히 스폰하지 못한다.",
                    i, assetName));

                for (int j = i + 1; j < SpawnPoints.Length; j++)
                {
                    GameAssert.IsTrue(SpawnPoints[j].AssetName != assetName, string.Format(
                        "EnemyTable.SpawnPoints 의 주소 '{0}' 가 [{1}] 과 [{2}] 에 중복이다. " +
                        "SurvivalGame 의 대기 카운트가 '한 주소 = 한 지점' 전제라 뒤쪽 지점이 상한에 고착된다.",
                        assetName, i, j));
                }
            }
        }
    }
}
