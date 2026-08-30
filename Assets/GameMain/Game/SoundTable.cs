namespace ToyBoxNightmare
{
    /// <summary>
    /// 사운드 그룹 이름과 Addressables 주소를 모아 둔다.
    ///
    /// <b>그룹 이름은 AudioMixer 의 그룹 이름과 정확히 같아야 한다.</b>
    /// <c>SoundComponent.AddSoundGroup</c> 이 <c>Master/{그룹이름}</c> 으로 믹서 그룹을 찾고,
    /// 못 찾으면 조용히 Master 로 떨어진다 — 그러면 M8 의 볼륨 슬라이더가 안 먹는다.
    /// MasterMixer 의 그룹은 Master / Music / Soundeffects 다.
    /// </summary>
    public static class SoundTable
    {
        // ─── 사운드 그룹 ───

        /// <summary>배경음악. 동시에 한 곡만 나오면 되므로 에이전트 1개.</summary>
        public const string MusicGroup = "Music";

        /// <summary>효과음. 피격·사망이 겹치므로 에이전트를 여러 개 둔다.</summary>
        public const string SfxGroup = "Soundeffects";

        // ─── Addressables 주소 ───

        public const string BackgroundMusic = "BackgroundMusic";

        public const string PlayerHurt  = "PlayerHurt";
        public const string PlayerDeath = "PlayerDeath";

        // 적 종별 클립. 원본에 클립이 있는 종은 셋뿐이라(ZomBunny / ZomBear / Hellephant)
        // 나머지 둘은 체급이 비슷한 쪽을 빌려 쓴다. 전용 클립이 생기면 여기만 고치면 된다.
        private const string ZomBunnyHurt   = "ZomBunnyHurt";
        private const string ZomBunnyDeath  = "ZomBunnyDeath";
        private const string ZomBearHurt    = "ZomBearHurt";
        private const string ZomBearDeath   = "ZomBearDeath";
        private const string HellephantHurt  = "HellephantHurt";
        private const string HellephantDeath = "HellephantDeath";

        /// <summary>적 종의 Addressables 주소로 피격음 주소를 얻는다. 모르는 종이면 null.</summary>
        public static string GetEnemyHurtSound(string enemyAssetName)
        {
            switch (enemyAssetName)
            {
                case "Zombunny":   return ZomBunnyHurt;
                case "ZombieDuck": return ZomBunnyHurt;   // 대역 — 전용 클립 없음
                case "ZomBear":    return ZomBearHurt;
                case "Clown":      return ZomBearHurt;    // 대역 — 전용 클립 없음
                case "Hellephant": return HellephantHurt;
                default:           return null;
            }
        }

        /// <summary>적 종의 Addressables 주소로 사망음 주소를 얻는다. 모르는 종이면 null.</summary>
        public static string GetEnemyDeathSound(string enemyAssetName)
        {
            switch (enemyAssetName)
            {
                case "Zombunny":   return ZomBunnyDeath;
                case "ZombieDuck": return ZomBunnyDeath;  // 대역
                case "ZomBear":    return ZomBearDeath;
                case "Clown":      return ZomBearDeath;   // 대역
                case "Hellephant": return HellephantDeath;
                default:           return null;
            }
        }
    }
}
