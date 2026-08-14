namespace ToyBoxNightmare
{
    /// <summary>
    /// 엔티티 고유 ID 전역 생성기.
    /// 모든 엔티티(플레이어, 적, 투사체 등)가 이걸 통해 ID를 받는다.
    ///
    /// 리셋 API 가 없다. 프로시저를 재진입해 새 판을 시작해도 번호는 이어서 나간다.
    /// </summary>
    public static class EntitySerialId
    {
        /// <summary>마지막으로 발급한 ID. 아직 아무것도 발급하지 않았으면 0 이고, 첫 발급은 1 이다.</summary>
        private static int mLastIssuedId = 0;

        /// <summary>다음 ID 를 발급한다.</summary>
        public static int Next()
        {
            // int 범위를 넘기면 음수로 돌아 이미 살아 있는 엔티티와 ID 가 충돌한다.
            // 도달하려면 21억 번 스폰해야 하므로 에디터/개발 빌드에서만 확인한다.
            GameAssert.IsTrue(
                mLastIssuedId < int.MaxValue,
                "EntitySerialId 가 int 범위를 넘었다. 발급 ID 가 음수로 돌아 엔티티 ID 가 충돌한다.");

            return ++mLastIssuedId;
        }
    }
}
