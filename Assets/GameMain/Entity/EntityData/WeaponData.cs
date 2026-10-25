namespace ToyBoxNightmare
{
    /// <summary>
    /// 무기 엔티티 데이터의 공통 부분. 무기마다 자기 능력치를 더한 파생 데이터가 있다
    /// (<see cref="LightningWeaponData"/>, <see cref="FrostWeaponData"/>, <see cref="StinkWeaponData"/>, <see cref="SlimeWeaponData"/>).
    ///
    /// 무기는 혼자 서지 않는다. <see cref="OwnerId"/> 의 엔티티(플레이어)에 붙어서 쓰이고,
    /// 소유자가 회수되면 프레임워크가 함께 회수한다.
    /// </summary>
    public abstract class WeaponData : EntityData
    {
        /// <summary>이 무기를 드는 엔티티의 Id. 무기는 뜨자마자 여기에 붙는다.</summary>
        public int OwnerId { get; private set; }

        /// <summary>
        /// 로드아웃에서의 자리(0부터). 무기는 비동기로 로드되어 도착 순서가 요청 순서와
        /// 다를 수 있으므로, Tab 순환 순서는 도착 순서가 아니라 이 값으로 정한다.
        /// </summary>
        public int Slot { get; private set; }

        /// <summary>
        /// 이 무기로 발사에 성공했을 때 걸리는 <b>전역</b> 쿨다운(초). 쿨다운이 없으면 0.
        /// 무기는 값만 알려 주고 타이머는 <see cref="WeaponLoadout"/> 이 하나만 돌린다.
        /// </summary>
        public float Cooldown { get; private set; }

        /// <summary>파생 데이터의 Create 가 공통 부분을 채운다.</summary>
        protected void FillWeapon(int entityId, int typeId, int ownerId, int slot, float cooldown)
        {
            Fill(entityId, typeId);

            OwnerId  = ownerId;
            Slot     = slot;
            Cooldown = cooldown;
        }

        public override void Clear()
        {
            base.Clear();

            // 0 으로 되돌리지 않으면 다음에 이 데이터를 받은 무기가 이전 소유자를 찾아간다.
            OwnerId  = 0;
            Slot     = 0;
            Cooldown = 0f;
        }
    }
}
