using GameFramework;
using UnityEngine;

namespace ToyBoxNightmare
{
    /// <summary>
    /// 캐릭터 선택 단계에 세워 두는 모델(<see cref="PlayerSelectLogic"/>)의 데이터.
    /// </summary>
    public class PlayerSelectData : EntityData
    {
        // 풀에서 나온 객체는 필드 초기화식이 다시 돌지 않는다. Clear 가 이 상수로 되돌린다.
        /// <summary>원본 낙선 연출 길이.</summary>
        private const float DefaultHideDelay = 1.5f;

        /// <summary>
        /// 선택되면 스폰할 플레이어의 Addressables 주소("Girl" / "Boy").
        /// 이 문자열이 그대로 ShowEntity 의 assetName 이 되므로
        /// Groups 창의 Address 와 정확히 같아야 한다.
        /// </summary>
        public string CharacterKey { get; private set; }

        /// <summary>고르지 않은 캐릭터가 쓰러진 뒤 회수될 때까지의 시간(초). 사망 연출이 보일 시간이다.</summary>
        public float HideDelay { get; private set; } = DefaultHideDelay;

        public static PlayerSelectData Create(int entityId, int typeId,
                                              string characterKey, Vector3 position)
        {
            // 비어 있으면 PlayerSelectLogic 이 프리팹 기본값을 빈 문자열로 덮어써서
            // 선택 후 SpawnPlayer 가 주소를 못 찾고 조용히 실패한다.
            GameAssert.IsTrue(!string.IsNullOrEmpty(characterKey),
                "PlayerSelectData 의 characterKey 가 비어 있다.");

            PlayerSelectData data = ReferencePool.Acquire<PlayerSelectData>();
            data.Fill(entityId, typeId);

            data.CharacterKey = characterKey;
            data.Position     = position;

            return data;
        }

        public override void Clear()
        {
            base.Clear();

            // 남겨 두면 다음 선택 캐릭터가 이전 판의 키를 물고 나와 두 캐릭터가 겹친다.
            CharacterKey = null;
            HideDelay    = DefaultHideDelay;
        }
    }
}
