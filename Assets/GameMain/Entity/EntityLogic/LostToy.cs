using System.Collections.Generic;
using ToyBoxNightmare;
using UnityEngine;
using UnityGameFramework.Runtime;

namespace ToyBoxNightmare
{
    public sealed class LostToy : TargetableObject
    {
        //[SerializeField]
        //private List<Weapon> mWeapons = new List<Weapon>();

        //[SerializeField]
        //private List<Armor> mArmors = new List<Armor>();

        protected internal override void OnShow(object userData)
        {
            base.OnShow(userData);

            //mLostToyData = userData as LostToyData;
            //if (mLostToyData == null)
            //{
            //    Log.Error("LostToy data is invalid.");
            //    return;
            //}

            //Name = Utility.Text.Format("LostToy ({0})", Id);

            //// 무기/방어구를 엔티티로 생성해서 "부착"시키는 방식
            //List<WeaponData> weaponDatas = mLostToyData.GetAllWeaponDatas();
            //for (int i = 0; i < weaponDatas.Count; i++)
            //{
            //    GameEntry.Entity.ShowWeapon(weaponDatas[i]);
            //}

            //List<ArmorData> armorDatas = mLostToyData.GetAllArmorDatas();
            //for (int i = 0; i < armorDatas.Count; i++)
            //{
            //    GameEntry.Entity.ShowArmor(armorDatas[i]);
            //}

            //// 위치/회전 초기화(데이터가 TargetableObjectData에 이미 있을 수도 있음)
            //CachedTransform.position = mLostToyData.Position;
            //CachedTransform.rotation = mLostToyData.Rotation;
        }

        //protected override void OnAttached(EntityLogic childEntity, Transform parentTransform, object userData)
        //{
        //    base.OnAttached(childEntity, parentTransform, userData);

        //    if (childEntity is Weapon)
        //    {
        //        mWeapons.Add((Weapon)childEntity);
        //        return;
        //    }

        //    if (childEntity is Armor)
        //    {
        //        mArmors.Add((Armor)childEntity);
        //        return;
        //    }
        //}

        //protected override void OnDetached(EntityLogic childEntity, object userData)
        //{
        //    base.OnDetached(childEntity, userData);

        //    if (childEntity is Weapon)
        //    {
        //        mWeapons.Remove((Weapon)childEntity);
        //        return;
        //    }

        //    if (childEntity is Armor)
        //    {
        //        mArmors.Remove((Armor)childEntity);
        //        return;
        //    }
        //}

        //protected override void OnDead(Entity attacker)
        //{
        //    base.OnDead(attacker);

        //    // 사망 효과/사운드
        //    GameEntry.Entity.ShowEffect(new EffectData(GameEntry.Entity.GenerateSerialId(), mLostToyData.DeadEffectId)
        //    {
        //        Position = CachedTransform.localPosition,
        //    });

        //    GameEntry.Sound.PlaySound(mLostToyData.DeadSoundId);
        //}

        //public override ImpactData GetImpactData()
        //{
        //    return new ImpactData(mLostToyData.Camp, mLostToyData.HP, 0, mLostToyData.Defense);
        //}

        //protected internal override void OnInit(object userData)
        //{
        //    base.OnInit(userData);
        //    // 초기화 코드 작성
        //}

        //protected internal override void OnShow(object userData)
        //{
        //    base.OnShow(userData);
        //    Debug.Log("플레이어 등장");
        //}

        //protected internal override void OnUpdate(float elapseSeconds, float realElapseSeconds)
        //{
        //    base.OnUpdate(elapseSeconds, realElapseSeconds);
        //    // 매 프레임 이동 또는 입력 처리
        //}

        [SerializeField]
        private LostToyData mLostToyData = null;
    }
}
