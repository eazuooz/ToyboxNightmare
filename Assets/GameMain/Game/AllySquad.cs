using UnityEngine;
using UnityGameFramework.Runtime;

namespace ToyBoxNightmare
{
    /// <summary>
    /// 아군 소환 경제. 점수를 소환 포인트로 쌓고, 한 마리를 정해진 시간만 유지한다.
    ///
    /// 원본 AllyManager 를 옮긴 것이지만 두 가지가 다르다.
    /// <list type="bullet">
    /// <item>MonoBehaviour 싱글턴이 아니라 <see cref="SurvivalGame"/> 이 소유하는 순수 객체다.
    ///       새 전역 시스템을 만들려면 GameFrameworkComponent 를 상속해야 하는데,
    ///       판 단위 상태라 게임 객체가 들고 있는 편이 수명이 맞는다.</item>
    /// <item>아군을 미리 Instantiate 해 두지 않고 <c>EntityComponent</c> 로 스폰한다.
    ///       엔티티 풀이 재사용을 대신 해 준다.</item>
    /// </list>
    ///
    /// <b>미끼 역할은 여기서 하지 않는다.</b> 적은 <see cref="SurvivalGame.GetChaseTargetPosition"/> 을
    /// 폴링하고, 그 함수가 <see cref="GetLurePosition"/> 을 거쳐 아군 좌표를 돌려준다.
    /// </summary>
    public class AllySquad
    {
        /// <summary>"소환된 아군 없음" 을 뜻하는 엔티티 Id. EntitySerialId 는 1부터 발급한다.</summary>
        private const int NoAllyId = 0;

        private int   mPoints           = 0;
        private int   mAllyEntityId     = NoAllyId;
        private Ally  mAlly             = null;
        private float mRemainingSeconds = 0f;

        /// <summary>
        /// 로드가 끝나기 전에 회수 요청이 들어왔는지. ShowEntity 는 비동기라
        /// 소환 직후 판이 끝나면 아군이 <b>다음 판에</b> 나타날 수 있다.
        /// </summary>
        private bool mUnsummonRequested = false;

        /// <summary>마지막으로 통보한 가부. 값이 뒤집힐 때만 이벤트를 쏜다.</summary>
        private bool mNotifiedCanSummon = false;

        /// <summary>지금 소환할 수 있는가. 원본 AllyManager.CanSummonAlly 와 같은 조건이다.</summary>
        public bool CanSummon => mPoints >= AllyTable.Cost && !HasAlly;

        /// <summary>소환된(또는 소환 중인) 아군이 있는가.</summary>
        public bool HasAlly => mAllyEntityId != NoAllyId;

        // ─── 판 생명주기 ───

        /// <summary>새 판 시작. 포인트와 아군 상태를 전부 지운다.</summary>
        public void Reset()
        {
            mPoints            = 0;
            mAllyEntityId      = NoAllyId;
            mAlly              = null;
            mRemainingSeconds  = 0f;
            mUnsummonRequested = false;

            // 판이 바뀌면 HUD 아이콘도 반드시 꺼져야 하므로 강제로 통보한다.
            mNotifiedCanSummon = true;
            NotifyAvailability();
        }

        /// <summary>점수를 얻을 때마다 같은 양이 소환 포인트로 쌓인다(원본 GameManager.AddScore).</summary>
        public void AddPoints(int amount)
        {
            if (amount <= 0) return;

            mPoints += amount;

            NotifyAvailability();
        }

        // ─── 소환 / 회수 ───

        /// <summary>
        /// 소환을 시도한다. 성공하면 true.
        /// destination 은 아군이 걸어갈 목적지이며, 원본은 소환 시점의 플레이어 위치를 넣는다.
        /// </summary>
        public bool TrySummon(Vector3 destination)
        {
            if (!CanSummon) return false;

            EntityComponent entityComponent = GameEntry.GetComponent<EntityComponent>();
            if (entityComponent == null)
            {
                Log.Error("AllySquad: EntityComponent 가 없다. 아군을 소환할 수 없다.");
                return false;
            }

            int id = EntitySerialId.Next();

            // 아군이 실제로 뜨기 전에 상태를 먼저 세운다. ShowEntity 가 비동기라
            // 여기서 세우지 않으면 로드가 끝나기 전에 한 번 더 소환될 수 있다.
            mAllyEntityId      = id;
            mAlly              = null;
            mRemainingSeconds  = AllyTable.Duration;
            mUnsummonRequested = false;

            entityComponent.ShowEntity(
                id,
                typeof(Ally),
                AllyTable.Asset,
                AllyTable.Group,
                AllyData.Create(id, AllyTable.EntityTypeId, AllyTable.SpawnPosition, destination));

            NotifyAvailability();
            return true;
        }

        /// <summary>
        /// <see cref="SurvivalGame.OnShowEntitySuccess"/> 가 아군 엔티티를 넘겨준다.
        /// 로드 중에 회수 요청이 들어왔다면 여기서 즉시 되돌린다.
        /// </summary>
        public void CacheAlly(Entity entity)
        {
            if (entity == null) return;

            Ally ally = entity.Logic as Ally;
            if (ally == null)
            {
                GameAssert.Unreachable("AllySquad: Ally 로직이 아닌 엔티티가 넘어왔다.");
                return;
            }

            // 이 판의 소환이 아니다(이전 판의 잔여 로드). 바로 돌려보낸다.
            if (entity.Id != mAllyEntityId || mUnsummonRequested)
            {
                ally.SafeHide();
                return;
            }

            mAlly = ally;
        }

        /// <summary>
        /// 아군을 거둔다. <b>포인트도 함께 몰수된다</b> — 원본 AllyManager.UnSummonAlly 가
        /// allyPoints 를 0 으로 되돌린다. 한 번 부르면 다시 30점을 모아야 한다.
        /// </summary>
        public void Unsummon()
        {
            if (!HasAlly) return;

            if (mAlly != null && mAlly.Available)
            {
                mAlly.SafeHide();
            }
            else
            {
                // 아직 로드 중이다. 도착하는 순간 CacheAlly 가 되돌린다.
                mUnsummonRequested = true;
            }

            mPoints           = 0;
            mAllyEntityId     = NoAllyId;
            mAlly             = null;
            mRemainingSeconds = 0f;

            NotifyAvailability();
        }

        // ─── 갱신 ───

        public void OnUpdate(float elapseSeconds)
        {
            if (!HasAlly) return;

            // 판 정리 등으로 아군이 밖에서 회수됐을 수 있다. 그 경우 타이머를 기다리지 않고 정리한다.
            if (mAlly != null && !mAlly.Available)
            {
                Unsummon();
                return;
            }

            mRemainingSeconds -= elapseSeconds;
            if (mRemainingSeconds > 0f) return;

            Unsummon();
        }

        /// <summary>
        /// 적이 쫓을 좌표. 아군이 실제로 화면에 서 있을 때만 아군을 돌려주고,
        /// 그 외에는 fallback(플레이어)을 그대로 돌려준다.
        ///
        /// 소환 직후 로드가 끝나기 전에도 fallback 이 나간다 — 원본도 아군이 존재해야
        /// EnemyTarget 이 바뀌므로 같은 동작이다.
        /// </summary>
        public Vector3 GetLurePosition(Vector3 fallback)
        {
            if (mAlly == null || !mAlly.Available) return fallback;

            return mAlly.LurePosition;
        }

        // ─── HUD 통보 ───

        private void NotifyAvailability()
        {
            bool canSummon = CanSummon;
            if (canSummon == mNotifiedCanSummon) return;

            mNotifiedCanSummon = canSummon;

            EventComponent events = GameEntry.GetComponent<EventComponent>();
            if (events == null) return;

            events.Fire(this, AllyAvailabilityChangedEventArgs.Create(canSummon));
        }
    }
}
