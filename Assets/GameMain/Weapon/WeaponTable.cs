namespace ToyBoxNightmare
{
    /// <summary>
    /// 무기 관련 상수. Addressables 주소는 여기 문자열과 Groups 창의 Address 가
    /// 정확히 같아야 한다 — 다르면 ShowEntity 가 조용히 실패한다.
    ///
    /// <b>쿨다운은 전역이다.</b> 무기가 자기 타이머를 돌리지 않는다. 여기 값은
    /// "이 무기로 쏘고 나면 <b>모든</b> 무기가 몇 초 잠기는가" 이고, 그 타이머는
    /// <see cref="WeaponLoadout"/> 이 한 쌍만 들고 있다(원본 <c>PlayerAttack</c> 과 같은 구조).
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

        // ─── Lightning ───
        // 원본: 누르고 있는 동안 매 프레임 발사. 쿨다운 1초. 레티클 없음.

        /// <summary>
        /// 원본 실효값(프리팹 직렬화)은 20 이었다. 자동조준으로 바뀌면서 화면 밖의 적까지
        /// 때려 교전 거리가 사라져 절반으로 줄인 값이 지금의 10이다.
        /// 수동 조준으로 돌아왔으니 원본 20으로 되돌릴지는 플레이해 보고 정한다.
        /// </summary>
        public const float LightningRange = 10f;

        public const int LightningDamage = 50;

        /// <summary>원본 <c>LightningAttack.Cooldown</c>. 발사에 성공하면 전역으로 걸린다.</summary>
        public const float LightningCooldown = 1f;

        /// <summary>착탄 이펙트 수명. 원본 파티클이 0.5~0.75초라 여유를 둔 값.</summary>
        public const float LightningHitLifetime = 1f;

        // ─── Frost (데미지 0. Lightning 과 병행 장착이 전제다) ───
        // 원본: 누르면 콘 ON, 떼면 콘 OFF. 레티클 없음.

        /// <summary>
        /// <b>Frost 에는 쿨다운이 없다.</b> 원본 <c>PlayerAttack</c> 도 Frost 를 쏜 뒤
        /// <c>attackCooldown</c> 에 0을 넣는다 — 즉 다음 발사가 곧바로 가능하다.
        /// 0 이라는 사실 자체가 계약(<c>WeaponBase.Cooldown</c>)의 일부라 상수로 이름을 붙여 둔다.
        /// </summary>
        public const float FrostCooldown = 0f;

        /// <summary>
        /// 콘 안의 적을 다시 훑는 주기. 발사 간격이 아니다 —
        /// 원본은 트리거 Enter/Exit 로 처리했고 우리는 매 틱 집합을 다시 계산한다.
        /// </summary>
        public const float FrostRetickInterval = 0.2f;

        public const float FrostConeRadius    = 6f;
        public const float FrostConeHalfAngle = 30f;   // 개구각 60°

        // FrostConeTurnSpeed 는 삭제했다. 원본은 콘을 적 쪽으로 돌리지 않는다 —
        // 캐릭터가 마우스를 바라보므로 콘은 캐릭터 전방에 고정이다.

        // ─── Stink ───
        // 원본: **떼는 프레임에** 마우스 지면 지점으로 투척. 쿨다운 5초. 레티클 있음.

        /// <summary>원본 <c>StinkAttack.Cooldown</c>.</summary>
        public const float StinkCooldown = 5f;

        /// <summary>
        /// 투척 사거리. 플레이어와 마우스 지면 지점 사이 거리가 이 값 이내여야 던진다
        /// (원본 <c>StinkAttack.inRange</c> 판정). 밖이면 레티클이 빨강이고 발사도 안 된다.
        ///
        /// 원본 프리팹 직렬화값은 9다. 지금 10인 것은 자동조준 시절 "적 탐색 반경" 으로
        /// 쓰면서 발밑 기준으로 환산했기 때문이다. 값은 그대로 두되 근거는 사라졌다.
        /// </summary>
        public const float StinkThrowRange = 10f;

        public const float StinkSpeed       = 10f;
        public const float StinkBlastRadius = 4f;
        public const float StinkFleeDuration = 4f;
        public const float StinkHitLifetime  = 4f;

        // ─── Slime ───
        // 원본: **떼는 프레임에** 발사. 쿨다운 3.5초. 레티클 있음.
        // 대상은 마우스 지면 지점에서 위로 쏜 짧은 레이에 걸린 적이다. 대상이 없으면
        // 발사 자체가 실패하고 쿨다운도 걸리지 않는다.

        /// <summary>원본 <c>SlimeAttack.Cooldown</c>.</summary>
        public const float SlimeCooldown = 3.5f;

        /// <summary>
        /// 대상 판정 레이 길이. 마우스 지면 지점에서 <c>Vector3.up</c> 으로 이만큼 쏜다.
        /// 원본 <c>Physics.Raycast(targetPosition, Vector3.up, out hit, 2f, whatIsShootable)</c> 그대로다.
        /// </summary>
        public const float SlimeTargetRayLength = 2f;

        // SlimeDetectRadius(20) 는 삭제했다. 자동조준이 최근접 적을 찾던 반경이라
        // 수동 조준에는 대응물이 없다 — 원본도 Slime 에 사거리 개념이 없다.

        public const float SlimeSpeed      = 20f;
        public const float SlimeHitRadius  = 1f;
        public const float SlimeHitLifetime = 1.2f;

        // DoT — 프리팹 실효값. 코드 기본값(4틱 × 10)과 섞지 말 것. 총합은 같지만 케이던스가 다르다.
        public const int   SlimeTicks        = 6;
        public const float SlimeTickInterval = 0.5f;
        public const int   SlimeTickDamage   = 20;
    }
}
