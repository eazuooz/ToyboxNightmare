namespace ToyBoxNightmare
{
    /// <summary>
    /// 엔티티 고유 ID 전역 생성기.
    /// 모든 엔티티(플레이어, 적, 투사체 등)가 이걸 통해 ID를 받는다.
    /// </summary>
    public static class EntitySerialId
    {
        private static int mNext = 0;

        public static int Next() => ++mNext;
    }
}
