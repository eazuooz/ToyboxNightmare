using System.Collections.Generic;
using UnityGameFramework.Runtime;

namespace ToyBoxNightmare
{
    /// <summary>
    /// 적 스포너. <see cref="EnemyTable.SpawnPoints"/> 의 지점마다 자기 타이머를 돌리다가
    /// 주기가 되면 적 엔티티를 띄운다.
    ///
    /// <b>MonoBehaviour 가 아니다.</b> <see cref="SurvivalGame"/> 이 인스턴스를 들고
    /// 자기 Update 에서 <see cref="OnUpdate"/> 를 직접 굴린다. 이유는 <see cref="WeaponBase"/>
    /// 와 같다 — Unity 메시지는 virtual 이 아니고, 업데이트 순서가 미보장이며, 엔티티 풀
    /// 재사용과 맞지 않는다.
    ///
    /// 엔티티 표시 성공/실패 이벤트는 소유자가 받아 <see cref="DecrementPendingSpawn"/> 로
    /// 중계한다. 스포너는 이벤트를 직접 구독하지 않는다.
    /// </summary>
    public class EnemySpawner
    {
        // 캐릭터를 고르기 전에는 돌지 않는다(원본의 스포너 SetActive 게이트와 같은 의미).
        private bool             mSpawningEnabled = false;
        private readonly float[] mSpawnTimers     = new float[EnemyTable.SpawnPoints.Length];

        // ShowEntity 요청 후 아직 인스턴스화되지 않은 수. GetEntities 가 세지 못하므로
        // 따로 들고 있지 않으면 로드 지연 동안 상한을 넘겨 과다 스폰한다.
        private readonly int[]   mPendingSpawns = new int[EnemyTable.SpawnPoints.Length];

        // 생존 수를 셀 때 쓰는 재사용 버퍼.
        // EntityComponent.GetEntities(string) 는 호출마다 배열을 두 개(IEntity[], Entity[])
        // 새로 만든다. 이 경로는 매 스폰 틱 × 스폰 지점 수만큼 도는 곳이라
        // 무할당 오버로드 GetEntities(string, List<Entity>) 와 짝지어 쓴다.
        private readonly List<Entity> mAliveBuffer = new List<Entity>();

        /// <summary>
        /// 판 상태 초기화. 소유자의 <c>SurvivalGame.ResetRoundState</c> 가 부른다.
        /// 판마다 새 인스턴스가 만들어지지만 상태는 명시적으로 되돌린다.
        /// </summary>
        public void Reset()
        {
            mSpawningEnabled = false;
            for (int i = 0; i < mSpawnTimers.Length; i++)
            {
                mSpawnTimers[i]   = 0f;
                mPendingSpawns[i] = 0;
            }
        }

        /// <summary>
        /// 스포너를 연다. 캐릭터 선택이 끝난 시점에 소유자가 부른다.
        /// 그 전까지는 <see cref="OnUpdate"/> 가 아무 일도 하지 않는다.
        /// </summary>
        public void Open()
        {
            mSpawningEnabled = true;
        }

        /// <summary>
        /// 소유자의 Update 가 매 프레임 부른다.
        /// 게임오버 뒤에는 소유자가 조기 리턴하므로 여기까지 오지 않는다(= 스폰 중단).
        /// </summary>
        public void OnUpdate(float elapseSeconds)
        {
            if (!mSpawningEnabled) return;

            // 반복문마다 다시 찾을 이유가 없다. 스폰 지점 수만큼 리스트를 훑게 된다.
            EntityComponent entityComponent = GameEntry.GetComponent<EntityComponent>();
            if (entityComponent == null)
            {
                Log.Error("EntityComponent 를 찾을 수 없어 적 스폰을 건너뛴다. GameFramework.prefab 구성을 확인할 것.");
                return;
            }

            for (int spawnIndex = 0; spawnIndex < EnemyTable.SpawnPoints.Length; spawnIndex++)
            {
                if (!TickSpawnTimer(spawnIndex, elapseSeconds)) continue;

                // 상한에 걸리면 이번 틱은 그냥 건너뛴다(원본 EnemySpawner 동작).
                if (IsSpawnLimitReached(entityComponent, spawnIndex)) continue;

                SpawnEnemy(entityComponent, spawnIndex);
            }
        }

        /// <summary>
        /// 스폰 타이머를 진행시킨다. 이번 틱이 스폰 차례면 타이머를 되감고 true 를 돌려준다.
        /// 상한에 걸려 스폰을 못 해도 타이머는 이미 되감긴 상태다(원본 동작).
        /// </summary>
        private bool TickSpawnTimer(int spawnIndex, float elapseSeconds)
        {
            GameAssert.InRange(spawnIndex, mSpawnTimers.Length, "spawnIndex");

            mSpawnTimers[spawnIndex] += elapseSeconds;
            if (mSpawnTimers[spawnIndex] < EnemyTable.SpawnPoints[spawnIndex].Interval) return false;

            mSpawnTimers[spawnIndex] = 0f;
            return true;
        }

        /// <summary>
        /// 이 종이 동시 생존 상한에 도달했는가.
        /// 아직 로드 중인 요청(mPendingSpawns)까지 세지 않으면 로드 지연 동안 과다 스폰한다.
        ///
        /// <b>전제: 한 AssetName 은 스폰 지점 하나에만 쓰인다.</b>
        /// 생존 수는 AssetName 으로 세는데 대기 수는 지점별로 세므로, 같은 주소를 쓰는 지점이
        /// 둘 이상이면 두 수의 분모가 어긋나 상한이 지점마다 다르게 계산된다.
        /// 이 전제는 <see cref="EnemyTable.Validate"/> 가 중복 주소를 잡아 지킨다.
        /// </summary>
        private bool IsSpawnLimitReached(EntityComponent entityComponent, int spawnIndex)
        {
            EnemySpawnPoint point = EnemyTable.SpawnPoints[spawnIndex];

            // 배열을 새로 만드는 GetEntities(string) 대신 무할당 오버로드를 쓴다.
            entityComponent.GetEntities(point.AssetName, mAliveBuffer);
            int aliveCount = mAliveBuffer.Count;

            // 다음 틱까지 참조를 들고 있지 않는다. 엔티티는 풀에서 재사용되므로
            // 남아 있는 참조는 이미 다른 용도로 살아난 인스턴스일 수 있다.
            mAliveBuffer.Clear();

            int pendingCount = mPendingSpawns[spawnIndex];

            return aliveCount + pendingCount >= point.MaxAlive;
        }

        private void SpawnEnemy(EntityComponent entityComponent, int spawnIndex)
        {
            EnemySpawnPoint point = EnemyTable.SpawnPoints[spawnIndex];

            EnemyStats stats;
            if (!EnemyTable.TryGetStats(point.AssetName, out stats))
            {
                // 스폰 지점 주소가 EnemyTable 에 없다 — 데이터 누락이다.
                Log.Error("Enemy stats not found for '{0}'.", point.AssetName);
                return;
            }

            int id = EntitySerialId.Next();

            // 반드시 ShowEntity 보다 먼저 올린다. 성공/실패 콜백이 이 카운트를 되돌린다.
            mPendingSpawns[spawnIndex]++;

            entityComponent.ShowEntity(
                id,
                typeof(Enemy),
                point.AssetName,
                EnemyTable.EntityGroup,
                EnemyData.Create(id, SurvivalGame.DefaultEntityTypeId, stats, point.Position));
        }

        /// <summary>
        /// 로드가 끝났거나 실패한 스폰 요청을 대기 카운트에서 뺀다.
        /// 소유자가 ShowEntitySuccess / ShowEntityFailure 양쪽에서 중계한다 —
        /// 한쪽이 빠지면 그 지점은 영원히 상한에 걸린 것으로 보인다.
        ///
        /// <b>전제: 한 AssetName 은 스폰 지점 하나에만 쓰인다.</b>
        /// 성공/실패 이벤트는 AssetName 만 알려주므로 어느 지점의 요청이었는지 되짚을 수 없다.
        /// 같은 주소를 쓰는 지점이 둘이면 항상 앞쪽 지점의 카운트만 줄어들어, 뒤쪽 지점의
        /// mPendingSpawns 가 영원히 남고 그 지점은 상한에 고착돼 스폰을 멈춘다.
        /// 이 전제는 <see cref="EnemyTable.Validate"/> 가 중복 주소를 잡아 지킨다.
        /// </summary>
        public void DecrementPendingSpawn(string assetName)
        {
            for (int i = 0; i < EnemyTable.SpawnPoints.Length; i++)
            {
                if (EnemyTable.SpawnPoints[i].AssetName != assetName) continue;

                if (mPendingSpawns[i] > 0)
                {
                    mPendingSpawns[i]--;
                }

                // 주소는 스폰 지점당 하나뿐이므로 첫 일치에서 끝낸다.
                return;
            }
        }
    }
}
