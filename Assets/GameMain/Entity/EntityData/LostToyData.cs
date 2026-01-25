using GameFramework.DataTable;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityGameFramework.Runtime;

namespace ToyBoxNightmare
{
    public class LostToyData : TargetableObjectData
    {
        //[SerializeField]
        //private List<WeaponData> mWeaponDatas = new List<WeaponData>();

        //[SerializeField]
        //private List<ArmorData> mArmorDatas = new List<ArmorData>();

        [SerializeField]
        private int mMaxHP = 0;

        [SerializeField]
        private int mDefense = 0;

        [SerializeField]
        private float mMoveSpeed = 0.0f;

        [SerializeField]
        private int mDeadEffectId = 0;

        [SerializeField]
        private int mDeadSoundId = 0;

        public LostToyData(int entityId, int typeId/*, CampType camp*/)
            : base(entityId, typeId/*, camp*/)
        {
            //IDataTable<DRLostToy> dtLostToy = GameEntry.DataTable.GetDataTable<DRLostToy>();
            //if (dtLostToy == null)
            //{
            //    return;
            //}

            //DRLostToy drLostToy = dtLostToy.GetDataRow(TypeId);
            //if (drLostToy == null)
            //{
            //    return;
            //}

            // 기본 스탯
            //mMaxHP = drLostToy.MaxHP;
            //mDefense = drLostToy.Defense;
            //mMoveSpeed = drLostToy.MoveSpeed;

            // 장착 장비(무기/아머)
            //for (int index = 0, weaponId = 0; (weaponId = drLostToy.GetWeaponIdAt(index)) > 0; index++)
            //{
            //    AttachWeaponData(new WeaponData(GameEntry.Entity.GenerateSerialId(), weaponId, Id, Camp));
            //}

            //for (int index = 0, armorId = 0; (armorId = drLostToy.GetArmorIdAt(index)) > 0; index++)
            //{
            //    AttachArmorData(new ArmorData(GameEntry.Entity.GenerateSerialId(), armorId, Id, Camp));
            //}

            // 사망 연출
            //mDeadEffectId = drLostToy.DeadEffectId;
            //mDeadSoundId = drLostToy.DeadSoundId;

            // 현재 HP 초기화
            //HP = mMaxHP;

            // 아머 기반으로 HP/Defense를 합산하는 방식으로 하고 싶으면 아래 RefreshData()로 통일해도 됨.
            // RefreshData();
            // HP = mMaxHP;
        }

        public override int MaxHitPoints
        {
            get
            {
                return mMaxHP;
            }
        }

        public int Defense
        {
            get
            {
                return mDefense;
            }
        }

        public float MoveSpeed
        {
            get
            {
                return mMoveSpeed;
            }
        }

        public int DeadEffectId
        {
            get
            {
                return mDeadEffectId;
            }
        }

        public int DeadSoundId
        {
            get
            {
                return mDeadSoundId;
            }
        }

        //public List<WeaponData> GetAllWeaponDatas()
        //{
        //    return mWeaponDatas;
        //}

        //public void AttachWeaponData(WeaponData weaponData)
        //{
        //    if (weaponData == null)
        //    {
        //        return;
        //    }

        //    if (mWeaponDatas.Contains(weaponData))
        //    {
        //        return;
        //    }

        //    mWeaponDatas.Add(weaponData);
        //}

        //public void DetachWeaponData(WeaponData weaponData)
        //{
        //    if (weaponData == null)
        //    {
        //        return;
        //    }

        //    mWeaponDatas.Remove(weaponData);
        //}

        //public List<ArmorData> GetAllArmorDatas()
        //{
        //    return mArmorDatas;
        //}

        //public void AttachArmorData(ArmorData armorData)
        //{
        //    if (armorData == null)
        //    {
        //        return;
        //    }

        //    if (mArmorDatas.Contains(armorData))
        //    {
        //        return;
        //    }

        //    mArmorDatas.Add(armorData);
        //    RefreshData();
        //}

        //public void DetachArmorData(ArmorData armorData)
        //{
        //    if (armorData == null)
        //    {
        //        return;
        //    }

        //    mArmorDatas.Remove(armorData);
        //    RefreshData();
        //}

        //private void RefreshData()
        //{
        //    // 아머의 합으로 MaxHP/Defense를 결정하는 방식(StarForce와 동일)
        //    mMaxHP = 0;
        //    mDefense = 0;

        //    for (int i = 0; i < mArmorDatas.Count; i++)
        //    {
        //        mMaxHP += mArmorDatas[i].MaxHP;
        //        mDefense += mArmorDatas[i].Defense;
        //    }

        //    if (HP > mMaxHP)
        //    {
        //        HP = mMaxHP;
        //    }
        //}
    }
}
