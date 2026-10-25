using System.Collections.Generic;
using GameFramework.Entity;
using UnityGameFramework.Runtime;

namespace ToyBoxNightmare
{
    /// <summary>
    /// 지금 떠 있는 엔티티 중에서 특정 대상을 찾는다.
    ///
    /// 정적 싱글턴(<c>Player.Instance</c> 같은 것)을 두지 않기 위한 창구다. 싱글턴은 엔티티가
    /// 풀에서 재사용될 때마다 "누가 지금 진짜인가" 를 손으로 맞춰 줘야 하고, 한 번 어긋나면
    /// 회수된 인스턴스를 가리킨다. 여기서는 그 답을 매번 <see cref="EntityComponent"/> 에 묻는다 —
    /// 프레임워크가 이미 알고 있는 사실을 따로 복사해 두지 않는다.
    ///
    /// 그룹 하나에 엔티티가 한두 개뿐인 대상(플레이어, 아군)만 찾는다. 적처럼 수십 개가 있는
    /// 그룹을 매 프레임 훑는 용도가 아니다.
    /// </summary>
    public static class EntityFinder
    {
        /// <summary>조회용 재사용 버퍼. 호출마다 배열을 새로 만들지 않기 위해 둔다.</summary>
        private static readonly List<IEntity> sBuffer = new List<IEntity>();

        /// <summary>
        /// 지금 떠 있는 플레이어. 캐릭터를 고르기 전이거나 회수된 뒤면 null 이다.
        /// 죽어서 쓰러지는 중인 플레이어는 아직 떠 있으므로 그대로 돌려준다(<c>IsDead</c> 로 구분한다).
        /// </summary>
        public static PlayerLogic FindPlayer()
        {
            return Find<PlayerLogic>(SurvivalGame.PlayerEntityGroup);
        }

        /// <summary>지금 떠 있는 아군. 소환하지 않았거나 아직 로드 중이면 null 이다.</summary>
        public static AllyLogic FindAlly()
        {
            return Find<AllyLogic>(AllyTable.Group);
        }

        private static T Find<T>(string entityGroupName) where T : EntityLogic
        {
            EntityComponent entityComponent = GameEntry.GetComponent<EntityComponent>();
            if (entityComponent == null) return null;

            IEntityGroup entityGroup = entityComponent.GetEntityGroup(entityGroupName);
            if (entityGroup == null) return null;

            sBuffer.Clear();
            entityGroup.GetAllEntities(sBuffer);

            T found = null;
            for (int i = 0; i < sBuffer.Count; i++)
            {
                Entity entity = sBuffer[i] as Entity;
                if (entity == null) continue;

                // 같은 그룹에 다른 로직이 섞여 있을 수 있다(플레이어 그룹에는 선택 화면 캐릭터도 있다).
                T logic = entity.Logic as T;
                if (logic == null || !logic.Available) continue;

                found = logic;
                break;
            }

            // 엔티티 참조를 버퍼에 남겨 두지 않는다.
            sBuffer.Clear();

            return found;
        }
    }
}
