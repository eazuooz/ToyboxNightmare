using UnityEngine;
using UnityGameFramework.Runtime;

namespace ToyBoxNightmare
{
    public class Player : TargetableObject
    {
        // 싱글턴 - 적이 플레이어 위치를 참조할 때 사용
        public static Player Instance { get; private set; }

        private PlayerData mPlayerData = null;

        protected internal override void OnShow(object userData)
        {
            base.OnShow(userData);

            mPlayerData = userData as PlayerData;
            if (mPlayerData == null)
            {
                Log.Error("Player data is invalid.");
                return;
            }

            Instance = this;
            CachedTransform.position = mPlayerData.Position;
            CachedTransform.rotation = mPlayerData.Rotation;
        }

        protected internal override void OnHide(bool isShutdown, object userData)
        {
            Instance = null;
            base.OnHide(isShutdown, userData);
        }

        protected internal override void OnUpdate(float elapseSeconds, float realElapseSeconds)
        {
            base.OnUpdate(elapseSeconds, realElapseSeconds);

            if (IsDead) return;

            HandleMovement(elapseSeconds);
        }

        private void HandleMovement(float elapseSeconds)
        {
            float h = Input.GetAxisRaw("Horizontal");
            float v = Input.GetAxisRaw("Vertical");

            Vector3 direction = new Vector3(h, 0f, v).normalized;
            if (direction == Vector3.zero) return;

            CachedTransform.position += direction * mPlayerData.MoveSpeed * elapseSeconds;
            CachedTransform.forward = direction;
        }

        public T AttachWeapon<T>() where T : WeaponBase
        {
            T weapon = gameObject.AddComponent<T>();
            weapon.Initialize(this);
            return weapon;
        }

        public void UpgradeMoveSpeed(float amount)
        {
            mPlayerData.MoveSpeed += amount;
        }

        public void HealHitPoints(int amount)
        {
            if (IsDead) return;
            mPlayerData.HitPoints = Mathf.Min(mPlayerData.HitPoints + amount, mPlayerData.MaxHitPoints);
        }

        // Enemy가 공격할 때 호출
        public void TakeDamage(Entity attacker, int damage)
        {
            if (IsDead) return;
            ApplyDamage(attacker, damage);
        }

        protected override void OnDead(Entity attacker)
        {
            Log.Info("Player is dead. Game Over!");
            GameEntry.GetComponent<EventComponent>().Fire(this, GameOverEventArgs.Create());
            // 베이스 HideEntity는 호출하지 않음 - 플레이어 사망 연출을 위해
        }
    }
}
