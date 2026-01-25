using GameFramework;
using GameFramework.DataTable;
using GameFramework.Event;
using UnityEngine;
using UnityGameFramework.Runtime;

namespace ToyBoxNightmare
{
    public class SurvivalGame : GameBase
    {
        private LostToy mPlayer = null;

        public override GameMode GameMode
        {
            get
            {
                return GameMode.Survival;
            }
        }

        public override void Initialize()
        {
            base.Initialize();

            mPlayer = null;

            //GameEntry.Entity.ShowLostToy(new LostToyData(GameEntry.Entity.GenerateSerialId(), 10000)
            //{
            //    Name = "LostToy",
            //    Position = UnityEngine.Vector3.zero,
            //});
        }

        public override void Update(float elapseSeconds, float realElapseSeconds)
        {
            base.Update(elapseSeconds, realElapseSeconds);

            if (GameOver)
            {
                return;
            }

            if (mPlayer == null)
            {
                return;
            }

            // 여기부터 뱀서류 룰(스폰/웨이브/난이도)을 진행
        }

        protected override void OnShowEntitySuccess(object sender, GameEventArgs e)
        {
            base.OnShowEntitySuccess(sender, e);

            ShowEntitySuccessEventArgs ne = (ShowEntitySuccessEventArgs)e;
            if (ne.EntityLogicType == typeof(LostToy))
            {
                mPlayer = (LostToy)ne.Entity.Logic;
            }
        }
    }
}
