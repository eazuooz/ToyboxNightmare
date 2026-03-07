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

            //// ����/���� ��ƼƼ�� �����ؼ� "����"��Ű�� ���
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

            //// ��ġ/ȸ�� �ʱ�ȭ(�����Ͱ� TargetableObjectData�� �̹� ���� ���� ����)
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

        //    // ��� ȿ��/����
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
        //    // �ʱ�ȭ �ڵ� �ۼ�
        //}

        //protected internal override void OnShow(object userData)
        //{
        //    base.OnShow(userData);
        //    Debug.Log("�÷��̾� ����");
        //}

        //protected internal override void OnUpdate(float elapseSeconds, float realElapseSeconds)
        //{
        //    base.OnUpdate(elapseSeconds, realElapseSeconds);
        //    // �� ������ �̵� �Ǵ� �Է� ó��
        //}

    }
}
