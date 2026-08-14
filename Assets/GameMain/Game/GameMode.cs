//------------------------------------------------------------
// Game Framework - MIT License
// Copyright © 2013–2021 Jiang Yin (EllanJiang)
// Modified © 2025 얌얌코딩
// Homepage: https://www.yamyamcoding.com/
// Feedback: mailto:eazuooz@gmail.com
//------------------------------------------------------------

namespace ToyBoxNightmare
{
    /// <summary>
    /// 게임 모드 식별자. <see cref="GameBase.GameMode"/> 가 돌려주는 값이다.
    /// </summary>
    public enum GameMode : byte
    {
        /// <summary>끝없이 몰려오는 적을 버티는 뱀서라이크 모드. 현재 유일한 모드다.</summary>
        Survival,
    }
}
