using UnityEngine;

namespace ToyBoxNightmare
{
    /// <summary>
    /// 아군(양) 튜닝값. 전부 원본 좀비토이 씬의 AllyManager / Ally 인스펙터 실측값이다.
    /// </summary>
    public static class AllyTable
    {
        /// <summary>Addressables 주소. 이미 등록돼 있다.</summary>
        public const string Asset = "Sheep";

        /// <summary>엔티티 그룹. GameFramework.prefab 의 mEntityGroups 와 이름이 같아야 한다.</summary>
        public const string Group = "Ally";

        /// <summary>소환 비용(점수). 원본 AllyManager.allyCost.</summary>
        public const int Cost = 30;

        /// <summary>소환 후 유지 시간(초). 원본 Ally.Duration.</summary>
        public const float Duration = 10f;

        /// <summary>소환 위치. 원본 씬의 AllySpawnPoint.</summary>
        public static readonly Vector3 SpawnPosition = new Vector3(29.93f, 0f, 4.61f);

        /// <summary>EntityData 의 TypeId. 아직 타입 테이블이 없어 프로젝트 전체가 1 이다.</summary>
        public const int EntityTypeId = 1;
    }
}
