using System.Diagnostics;
using UnityGameFramework.Runtime;

namespace ToyBoxNightmare
{
    /// <summary>
    /// 사전조건(precondition) 명시용. "여기까지 왔다면 반드시 참이어야 한다" 를 코드로 적는다.
    ///
    /// <b>런타임에 정상적으로 일어날 수 있는 상황</b>(사거리 안에 적이 없다, 아직 스폰 전이다)은
    /// assert 가 아니라 조기 리턴으로 처리한다. 이건 <b>프로그래머 실수</b>를 잡는 용도다 —
    /// 배열 범위, 도달하면 안 되는 switch 분기, 뒤바뀐 하한/상한, 계약을 어긴 호출 순서 같은 것.
    ///
    /// 에디터와 개발 빌드에서만 살아 있다. 릴리스 빌드에서는 <see cref="ConditionalAttribute"/>
    /// 가 <b>호출부의 인자 계산까지 통째로</b> 제거하므로 성능 부담이 없다.
    /// </summary>
    public static class GameAssert
    {
        /// <summary>condition 이 거짓이면 오류를 남긴다. 조건에는 <b>충족해야 할 것</b>을 쓴다.</summary>
        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        public static void IsTrue(bool condition, string message)
        {
            if (condition) return;

            Log.Error("[사전조건 위반] {0}", message);
        }

        /// <summary>도달하면 안 되는 곳. switch 의 default 절 등에 쓴다.</summary>
        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        public static void Unreachable(string message)
        {
            Log.Error("[도달 불가 분기] {0}", message);
        }

        /// <summary>index 가 [0, count) 안인지 확인한다.</summary>
        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        public static void InRange(int index, int count, string name)
        {
            if (0 <= index && index < count) return;

            Log.Error("[범위 초과] {0} = {1}, 유효 범위는 [0, {2})", name, index, count);
        }
    }
}
