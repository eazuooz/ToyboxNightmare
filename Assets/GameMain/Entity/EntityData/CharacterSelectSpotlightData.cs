using GameFramework;

namespace ToyBoxNightmare
{
    /// <summary>
    /// 캐릭터 선택 조명(<see cref="CharacterSelectSpotlightLogic"/>)의 스폰 데이터.
    /// 조명은 프리팹에 놓인 자리 그대로 쓰므로 Id 말고는 담을 것이 없다.
    /// </summary>
    public class CharacterSelectSpotlightData : EntityData
    {
        public static CharacterSelectSpotlightData Create(int entityId, int typeId)
        {
            CharacterSelectSpotlightData data = ReferencePool.Acquire<CharacterSelectSpotlightData>();
            data.Fill(entityId, typeId);

            return data;
        }
    }
}
