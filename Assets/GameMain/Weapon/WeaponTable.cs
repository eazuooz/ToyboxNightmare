namespace ToyBoxNightmare
{
    /// <summary>
    /// 무기 관련 상수. Addressables 주소는 여기 문자열과 Groups 창의 Address 가
    /// 정확히 같아야 한다 — 다르면 ShowEntity 가 조용히 실패한다.
    /// </summary>
    public static class WeaponTable
    {
        // ─── 엔티티 그룹 ───
        public const string ProjectileGroup = "Projectile";
        public const string EffectGroup     = "Effect";

        // ─── Addressables 주소 ───
        public const string LightningHitAsset    = "LightningHit";
        public const string StinkProjectileAsset = "StinkProjectile";
        public const string StinkHitAsset        = "StinkHit";
        public const string SlimeProjectileAsset = "SlimeProjectile";
        public const string SlimeHitAsset        = "SlimeHit";

        /// <summary>착탄 이펙트 수명. 원본 파티클이 0.5~0.75초라 여유를 둔 값.</summary>
        public const float LightningHitLifetime = 1f;

        // ─── Frost (데미지 0. Lightning 과 병행 장착이 전제다) ───
        public const float FrostRetickInterval = 0.2f;  // 발사가 아니라 콘 재판정 주기
        public const float FrostConeRadius     = 6f;
        public const float FrostConeHalfAngle  = 30f;   // 개구각 60°

        // ─── Stink ───
        public const float StinkCooldown     = 5f;
        public const float StinkDetectRadius = 10f;     // 원본 range 9 를 발밑 기준으로 환산한 값
        public const float StinkSpeed        = 10f;
        public const float StinkBlastRadius  = 4f;
        public const float StinkFleeDuration = 4f;
        public const float StinkHitLifetime  = 4f;

        // ─── Slime ───
        public const float SlimeCooldown     = 3.5f;
        public const float SlimeDetectRadius = 20f;
        public const float SlimeSpeed        = 20f;
        public const float SlimeHitRadius    = 1f;
        public const float SlimeHitLifetime  = 1.2f;

        // DoT — 프리팹 실효값. 코드 기본값(4틱 × 10)과 섞지 말 것. 총합은 같지만 케이던스가 다르다.
        public const int   SlimeTicks        = 6;
        public const float SlimeTickInterval = 0.5f;
        public const int   SlimeTickDamage   = 20;
    }
}
