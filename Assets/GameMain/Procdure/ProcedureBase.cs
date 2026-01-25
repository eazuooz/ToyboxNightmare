//------------------------------------------------------------
// Game Framework - MIT License
// Copyright © 2013–2021 Jiang Yin (EllanJiang)
// Modified © 2025 얌얌코딩
// Homepage: https://www.yamyamcoding.com/
// Feedback: mailto:eazuooz@gmail.com
//------------------------------------------------------------

namespace ToyBoxNightmare
{
    public abstract class ProcedureBase : GameFramework.Procedure.ProcedureBase
    {
        /// <summary>
        /// 일부 특수한 프로시저(예: 게임 UI 리소스가 아직 로드되지 않은 상태의 프로시저)에서는,
        /// 네이티브 다이얼로그를 사용해서 메시지를 보여줄 수 있다.
        /// </summary>
        public abstract bool UseNativeDialog
        {
            get;
        }
    }
}
