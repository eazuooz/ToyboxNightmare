using UnityEngine;
using UnityGameFramework.Runtime;

namespace ToyBoxNightmare
{
    /// <summary>
    /// 캐릭터 선택 화면의 조명. 커서가 가리키는 바닥을 따라 비춘다(원본 CharacterSpotlight 자리).
    ///
    /// <b>엔티티다.</b> "선택 중에만 있어야 하는 것" 이라 수명을 엔티티로 표현한다 —
    /// <see cref="SurvivalGame"/> 이 판을 시작하면서 띄우고 캐릭터가 골라지면 회수한다.
    /// 그래서 "지금 선택 중인가" 를 매 프레임 따로 묻지 않는다. 떠 있으면 선택 중이다.
    ///
    /// 프리팹(<c>CharacterSelectSpotlightLogic</c>)에는 이 로직을 붙여 두지 않는다. 프레임워크가 띄울 때 붙인다.
    /// </summary>
    public class CharacterSelectSpotlightLogic : EntityLogicBase
    {
        /// <summary>커서를 따라다닐 조명 자식. 프리팹에서는 꺼진 채로 저장돼 있다.</summary>
        private const string SpotlightPath = "Select spot PC";

        private GameObject mSpotlight = null;

        protected internal override void OnInit(object userData)
        {
            base.OnInit(userData);

            Transform spotlight = CachedTransform.Find(SpotlightPath);
            if (spotlight == null)
            {
                Log.Warning("CharacterSelectSpotlightLogic: 자식 '{0}' 을 찾지 못했다. 선택 화면 연출이 나오지 않는다.", SpotlightPath);
                return;
            }

            mSpotlight = spotlight.gameObject;
        }

        protected internal override void OnShow(object userData)
        {
            base.OnShow(userData);

            SetSpotlightActive(true);
        }

        protected internal override void OnHide(bool isShutdown, object userData)
        {
            // 풀에 돌아갈 때는 프리팹 기본 상태(꺼짐)로 되돌린다.
            SetSpotlightActive(false);

            base.OnHide(isShutdown, userData);
        }

        protected internal override void OnUpdate(float elapseSeconds, float realElapseSeconds)
        {
            base.OnUpdate(elapseSeconds, realElapseSeconds);

            if (mSpotlight == null) return;

            // 커서가 바닥 밖(하늘, UI 위)을 가리키면 마지막 방향을 그대로 둔다.
            if (!MouseGround.TryGetGroundPoint(out Vector3 groundPoint)) return;

            mSpotlight.transform.LookAt(groundPoint);
        }

        private void SetSpotlightActive(bool active)
        {
            if (mSpotlight == null) return;
            if (mSpotlight.activeSelf == active) return;

            mSpotlight.SetActive(active);
        }
    }
}
