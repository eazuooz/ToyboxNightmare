using System.Collections.Generic;
using UnityEngine.InputSystem;
using UnityGameFramework.Runtime;

namespace ToyBoxNightmare
{
    /// <summary>
    /// 플레이어가 장착한 무기 묶음. 장착·전환·갱신·정리를 한곳에서 맡는다.
    ///
    /// <b>MonoBehaviour 가 아니다.</b> 소유 <see cref="Player"/> 가 인스턴스를 만들어 들고
    /// <see cref="Player.OnUpdate"/> 가 <see cref="OnUpdate"/> 를 직접 굴린다.
    /// 이유는 <see cref="WeaponBase"/> 주석에 적어 둔 셋과 같다.
    ///
    /// 수명은 소유 <see cref="Player"/> 로직과 같다. Player 로직은 판마다 통째로 재생성되는 것이
    /// 정상 경로이므로(<see cref="Equip"/> 주석) 이 로드아웃도 함께 새로 만들어진다.
    /// </summary>
    public class WeaponLoadout
    {
        /// <summary>
        /// true 면 한 번에 하나만 쓰고 Tab 으로 전환한다(무기별 확인용).
        /// false 면 장착한 무기가 전부 동시에 나간다(뱀서라이크 본래 형태).
        /// </summary>
        private const bool SingleWeaponMode = true;

        /// <summary>이 로드아웃의 소유자. 무기 초기화에 그대로 넘긴다.</summary>
        private readonly Player mOwner = null;

        // 장착된 무기. 각자 자기 쿨다운을 돌린다.
        private readonly List<WeaponBase> mWeapons = new List<WeaponBase>();

        private int mActiveWeaponIndex = 0;

        public WeaponLoadout(Player owner)
        {
            mOwner = owner;
        }

        /// <summary>
        /// 이 캐릭터가 들고 시작하는 무기. 레벨업 시스템이 붙으면 여기에 추가하는 형태가 된다.
        ///
        /// 무기는 MonoBehaviour 가 아니라 이 로드아웃이 소유하는 순수 객체다. 로드아웃
        /// 인스턴스가 살아남는 재사용 경로에서는 무기도 그대로 재활용한다.
        ///
        /// 단, 그 경로에 기대면 안 된다 — <c>Entity.cs:86-96</c> 은 풀에서 꺼낸 인스턴스의
        /// 로직 타입이 다르면 컴포넌트를 파괴하고 새로 붙인다. 선택화면은
        /// <see cref="PlayerSelectLogic"/>, 플레이는 <see cref="Player"/> 라 <b>판마다
        /// 이 로직이 통째로 재생성되는 것이 정상 경로</b>다(로드아웃도 함께 새로 만들어진다).
        /// 무기가 캐릭터에 남긴 것은 <see cref="Dispose"/> 가 치운다.
        /// </summary>
        public void Equip()
        {
            if (mWeapons.Count == 0)
            {
                mWeapons.Add(new LightningWeapon());
                mWeapons.Add(new FrostWeapon());
                mWeapons.Add(new StinkWeapon());
                mWeapons.Add(new SlimeWeapon());
            }

            for (int i = 0; i < mWeapons.Count; i++)
            {
                mWeapons[i].Initialize(mOwner);
            }

            // 재스폰 때마다 첫 무기부터 시작한다.
            mActiveWeaponIndex = 0;
            ApplySelection();

            if (SingleWeaponMode && mWeapons.Count > 0)
            {
                Log.Info("무기 {0}종 장착. Tab 으로 전환. 현재 → {1}",
                    mWeapons.Count, mWeapons[mActiveWeaponIndex].GetType().Name);
            }
        }

        /// <summary>
        /// 활성 무기만 켠다. 꺼진 무기는 OnUpdate 를 받지 못하고 총구 VFX·타겟 마커도 함께 꺼진다.
        /// </summary>
        private void ApplySelection()
        {
            if (mWeapons.Count > 0)
            {
                GameAssert.InRange(mActiveWeaponIndex, mWeapons.Count, "mActiveWeaponIndex");
            }

            for (int i = 0; i < mWeapons.Count; i++)
            {
                bool isSelectedWeapon = !SingleWeaponMode || i == mActiveWeaponIndex;
                mWeapons[i].SetActive(isSelectedWeapon);

                if (isSelectedWeapon)
                {
                    // 전환 즉시 쏠 수 있게 쿨다운을 채워 준다.
                    mWeapons[i].ResetCooldown();
                }
            }
        }

        /// <summary>
        /// 무기를 굴린다. <see cref="Player.OnUpdate"/> 가 입력 처리 뒤에 부르므로 조준이
        /// 한 프레임 밀리지 않는다 — MonoBehaviour 였을 때는 이 순서가 보장되지 않았다.
        /// </summary>
        public void OnUpdate(float elapseSeconds)
        {
            for (int i = 0; i < mWeapons.Count; i++)
            {
                mWeapons[i].OnUpdate(elapseSeconds);
            }
        }

        /// <summary>
        /// 무기를 전부 끈다. 총구 VFX·타겟 마커·Frost 콘이 여기서 정리된다.
        /// 사망과 회수 양쪽에서 부른다 — 죽으면 OnUpdate 가 멈춰 무기 스스로는 못 끈다.
        /// </summary>
        public void Shutdown()
        {
            for (int i = 0; i < mWeapons.Count; i++)
            {
                mWeapons[i].SetActive(false);
            }
        }

        /// <summary>
        /// 무기가 캐릭터에 남긴 것을 전부 지운다(복제한 마커 링, 꺼 놓은 VFX 루트).
        ///
        /// 이 캐릭터 GameObject 는 풀에 돌아가 <see cref="PlayerSelectLogic"/> 으로도 재사용된다.
        /// 정리를 안 하면 재시작마다 링이 쌓이고, 선택화면 캐릭터의 안테나 이펙트가 꺼진다.
        /// </summary>
        public void Dispose()
        {
            for (int i = 0; i < mWeapons.Count; i++)
            {
                mWeapons[i].Dispose();
            }
        }

        public void ReadSwitchInput()
        {
            if (!SingleWeaponMode || mWeapons.Count <= 1) return;

            Keyboard keyboard = Keyboard.current;
            if (keyboard == null || !keyboard.tabKey.wasPressedThisFrame) return;

            mActiveWeaponIndex = (mActiveWeaponIndex + 1) % mWeapons.Count;
            ApplySelection();

            Log.Info("무기 전환 → {0} ({1}/{2})",
                mWeapons[mActiveWeaponIndex].GetType().Name,
                mActiveWeaponIndex + 1, mWeapons.Count);
        }
    }
}
