using GameFramework;
using UnityEngine;

namespace ToyBoxNightmare
{
    /// <summary>
    /// 아군 스폰 데이터.
    ///
    /// 아군은 소환 시점에 목적지를 <b>한 번만</b> 받고 그리로 걸어간다(원본 Ally.Move 가
    /// SetDestination 을 1회만 부른다). 그래서 목적지가 런타임 상태가 아니라 스폰 인자다.
    /// </summary>
    public class AllyData : EntityData
    {
        /// <summary>소환 직후 걸어갈 목적지. 원본은 소환 시점의 플레이어 위치를 넣는다.</summary>
        public Vector3 MoveDestination { get; private set; }

        public static AllyData Create(int entityId, int typeId, Vector3 position, Vector3 moveDestination)
        {
            var data = ReferencePool.Acquire<AllyData>();
            data.Fill(entityId, typeId);
            data.Position        = position;
            data.MoveDestination = moveDestination;
            return data;
        }

        public override void Clear()
        {
            base.Clear();

            MoveDestination = Vector3.zero;
        }
    }
}
