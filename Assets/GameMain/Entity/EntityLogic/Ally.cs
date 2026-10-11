using UnityEngine;
using UnityEngine.AI;
using UnityGameFramework.Runtime;

namespace ToyBoxNightmare
{
    /// <summary>
    /// 아군(양). 소환되면 목적지로 한 번 걸어갈 뿐 아무것도 하지 않는다 —
    /// <b>미끼다.</b> 적이 아군을 쫓게 만드는 것은 아군 자신이 아니라
    /// <see cref="SurvivalGame.GetChaseTargetPosition"/> 이 돌려주는 좌표다.
    ///
    /// 원본 Ally 도 NavMeshAgent 에 SetDestination 을 1회 걸고 끝이며,
    /// 도착 후 다시 움직이거나 플레이어를 따라다니지 않는다.
    /// </summary>
    public class Ally : EntityLogicBase
    {
        private NavMeshAgent mAgent    = null;
        private AllyData     mAllyData = null;

        /// <summary>적이 쫓을 좌표. 회수된 뒤에는 의미가 없으므로 호출부가 Available 을 먼저 볼 것.</summary>
        public Vector3 LurePosition => CachedTransform.position;

        protected internal override void OnInit(object userData)
        {
            base.OnInit(userData);

            mAgent = GetComponent<NavMeshAgent>();
            if (mAgent == null)
            {
                Log.Warning("Ally: NavMeshAgent 가 없다. 소환 위치에 그대로 서 있게 된다.");
            }
        }

        protected internal override void OnShow(object userData)
        {
            base.OnShow(userData);

            mAllyData = userData as AllyData;
            if (mAllyData == null)
            {
                Log.Error("Ally: AllyData 가 아니다. 회수한다.");
                SafeHide();
                return;
            }

            PlaceOnNavMesh(mAllyData.Position);
            MoveTo(mAllyData.MoveDestination);
        }

        protected internal override void OnHide(bool isShutdown, object userData)
        {
            StopAgent();

            mAllyData = null;

            base.OnHide(isShutdown, userData);
        }

        /// <summary>
        /// 스폰 좌표로 옮기고 NavMesh 폴리곤 위로 스냅한다.
        /// (Enemy 와 같은 절차다 — 에이전트가 켜진 채 transform 을 옮기면 Unity 가 경고를 낸다.)
        /// </summary>
        private void PlaceOnNavMesh(Vector3 position)
        {
            if (mAgent == null)
            {
                CachedTransform.position = position;
                return;
            }

            mAgent.enabled = false;
            CachedTransform.position = position;
            mAgent.enabled = true;

            mAgent.Warp(position);

            if (mAgent.isOnNavMesh)
            {
                mAgent.isStopped = false;
            }
        }

        /// <summary>목적지를 한 번만 준다. 원본과 같이 이후 재조준하지 않는다.</summary>
        private void MoveTo(Vector3 destination)
        {
            if (mAgent == null || !mAgent.enabled || !mAgent.isOnNavMesh) return;

            mAgent.SetDestination(destination);
        }

        /// <summary>회수 전에 반드시 멈춘다. 켜진 채로 풀에 들어가면 다음 소환에서 옛 경로를 이어 간다.</summary>
        private void StopAgent()
        {
            if (mAgent == null || !mAgent.enabled) return;

            if (mAgent.isOnNavMesh)
            {
                mAgent.isStopped = true;
                mAgent.ResetPath();
            }

            mAgent.enabled = false;
        }
    }
}
