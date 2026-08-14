namespace ToyBoxNightmare
{
    /// <summary>
    /// 캐릭터 선택 단계에 세워 두는 모델(<see cref="PlayerSelectLogic"/>)의 데이터.
    /// </summary>
    public class CharacterSelectData : EntityData
    {
        /// <summary>
        /// 선택되면 스폰할 플레이어의 Addressables 주소("Girl" / "Boy").
        /// 이 문자열이 그대로 ShowEntity 의 assetName 이 되므로
        /// Groups 창의 Address 와 정확히 같아야 한다.
        /// </summary>
        public string CharacterKey { get; }

        public CharacterSelectData(int entityId, int typeId, string characterKey)
            : base(entityId, typeId)
        {
            // 비어 있으면 PlayerSelectLogic 이 프리팹 기본값을 빈 문자열로 덮어써서
            // 선택 후 SpawnPlayer 가 주소를 못 찾고 조용히 실패한다.
            GameAssert.IsTrue(!string.IsNullOrEmpty(characterKey),
                "CharacterSelectData 의 characterKey 가 비어 있다.");

            CharacterKey = characterKey;
        }
    }
}
