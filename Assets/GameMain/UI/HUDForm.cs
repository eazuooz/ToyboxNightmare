using GameFramework.Event;
using UnityEngine;
using UnityEngine.UI;
using UnityGameFramework.Runtime;

namespace ToyBoxNightmare
{
    /// <summary>
    /// 게임 중 HUD. 점수 / 체력 / 공격 쿨다운 / 게임오버 문구 / 피격 플래시를 담당한다.
    ///
    /// <b>이 스크립트는 프리팹에 직접 붙어 있어야 한다.</b> <c>UIForm.cs</c> 가
    /// <c>GetComponent&lt;UIFormLogic&gt;()</c> 로 찾기 때문이며, 엔티티와는 정반대 규약이다
    /// (엔티티 프리팹에 EntityLogic 을 붙이면 중복 부착으로 깨진다).
    ///
    /// 화면 요소는 원본 좀비토이 HUDCanvas 를 그대로 쓴다. 갱신은 전부 이벤트 구독으로 하며
    /// 게임 로직을 직접 폴링하지 않는다 — HUD 가 없어도 게임은 돌아야 하기 때문이다.
    /// </summary>
    public class HUDForm : UIFormLogic
    {
        // 프리팹 계층 계약. 자식 이름이 바뀌면 조용히 null 이 되므로 한곳에 모아 둔다.
        private const string HealthSliderPath = "HealthUI/HealthSlider";
        private const string DamageImagePath  = "DamageImage";
        private const string GameOverTextPath = "GameOverText";
        private const string InfoTextPath     = "InfoText";
        private const string AllyButtonPath   = "Ally Button";
        private const string CountdownPath    = "CountdownSlider";

        /// <summary>캐릭터를 고르기 전에 띄우는 안내문. 원본 InfoText 의 초기값과 같다.</summary>
        private const string SelectPrompt = "Select a character";

        private const string ScoreFormat = "Score: {0}";

        private Slider     mHealthSlider = null;
        private FlashFade  mDamageFlash  = null;
        private Text       mGameOverText = null;
        private Text       mInfoText     = null;
        private GameObject mAllyButton   = null;
        private Countdown  mCountdown    = null;

        private bool mSubscribed = false;

        // ─── 생명주기 ───

        protected internal override void OnInit(object userData)
        {
            base.OnInit(userData);

            mHealthSlider = Find<Slider>(HealthSliderPath);
            mDamageFlash  = Find<FlashFade>(DamageImagePath);
            mGameOverText = Find<Text>(GameOverTextPath);
            mInfoText     = Find<Text>(InfoTextPath);
            mCountdown    = Find<Countdown>(CountdownPath);

            Transform allyButton = CachedTransform.Find(AllyButtonPath);
            mAllyButton = allyButton != null ? allyButton.gameObject : null;
            if (mAllyButton == null)
            {
                Log.Warning("HUDForm: 자식 {0} 을 찾지 못했다.", AllyButtonPath);
            }
        }

        /// <summary>자식을 경로로 찾고 없으면 한 번 알린다. 조용한 null 이 가장 찾기 어렵다.</summary>
        private T Find<T>(string path) where T : Component
        {
            Transform child = CachedTransform.Find(path);
            if (child == null)
            {
                Log.Warning("HUDForm: 자식 {0} 이 없다. 프리팹 구조를 확인할 것.", path);
                return null;
            }

            T component = child.GetComponent<T>();
            if (component == null)
            {
                Log.Warning("HUDForm: {0} 에 {1} 컴포넌트가 없다.", path, typeof(T).Name);
            }

            return component;
        }

        protected internal override void OnOpen(object userData)
        {
            base.OnOpen(userData);

            // UI 폼도 풀에서 재사용된다. 이전 판의 점수와 게임오버 문구를 물고 나오지 않도록
            // 구독보다 먼저 화면을 초기 상태로 되돌린다.
            ResetVisuals();
            Subscribe();
        }

        protected internal override void OnClose(bool isShutdown, object userData)
        {
            Unsubscribe();

            base.OnClose(isShutdown, userData);
        }

        /// <summary>캐릭터 선택 직전 상태로 되돌린다.</summary>
        private void ResetVisuals()
        {
            if (mInfoText != null)
            {
                mInfoText.text = SelectPrompt;
            }

            if (mGameOverText != null)
            {
                mGameOverText.enabled = false;
            }

            if (mHealthSlider != null)
            {
                mHealthSlider.value = mHealthSlider.maxValue;
            }

            // 아군 시스템은 M5 다. 그때까지 아이콘을 숨겨 둔다.
            if (mAllyButton != null)
            {
                mAllyButton.SetActive(false);
            }
        }

        // ─── 이벤트 구독 ───
        // 같은 (id, handler) 를 두 번 Subscribe 하거나 미등록 핸들러를 Unsubscribe 하면
        // 코어가 즉시 예외를 던진다(CLAUDE.md 규약). mSubscribed 로 짝을 보장한다.

        private void Subscribe()
        {
            if (mSubscribed) return;

            EventComponent events = GameEntry.GetComponent<EventComponent>();
            if (events == null)
            {
                Log.Error("HUDForm: EventComponent 가 없다. HUD 가 아무것도 갱신하지 못한다.");
                return;
            }

            events.Subscribe(CharacterSelectedEventArgs.EventId,     OnCharacterSelected);
            events.Subscribe(ScoreChangedEventArgs.EventId,          OnScoreChanged);
            events.Subscribe(PlayerHealthChangedEventArgs.EventId,   OnPlayerHealthChanged);
            events.Subscribe(WeaponCooldownStartedEventArgs.EventId, OnWeaponCooldownStarted);
            events.Subscribe(PlayerDiedEventArgs.EventId,            OnPlayerDied);

            mSubscribed = true;
        }

        private void Unsubscribe()
        {
            if (!mSubscribed) return;

            mSubscribed = false;

            // 종료 순서에 따라 EventComponent 가 먼저 파괴돼 있을 수 있다.
            EventComponent events = GameEntry.GetComponent<EventComponent>();
            if (events == null) return;

            events.Unsubscribe(CharacterSelectedEventArgs.EventId,     OnCharacterSelected);
            events.Unsubscribe(ScoreChangedEventArgs.EventId,          OnScoreChanged);
            events.Unsubscribe(PlayerHealthChangedEventArgs.EventId,   OnPlayerHealthChanged);
            events.Unsubscribe(WeaponCooldownStartedEventArgs.EventId, OnWeaponCooldownStarted);
            events.Unsubscribe(PlayerDiedEventArgs.EventId,            OnPlayerDied);
        }

        // ─── 핸들러 ───

        /// <summary>캐릭터를 고른 순간 안내문이 점수 표시로 바뀐다(원본 GameManager.PlayerChosen).</summary>
        private void OnCharacterSelected(object sender, GameEventArgs e)
        {
            SetScore(0);
        }

        private void OnScoreChanged(object sender, GameEventArgs e)
        {
            ScoreChangedEventArgs ne = e as ScoreChangedEventArgs;
            if (ne == null)
            {
                GameAssert.Unreachable("HUDForm: ScoreChanged 핸들러에 다른 타입이 들어왔다.");
                return;
            }

            SetScore(ne.Score);
        }

        private void SetScore(int score)
        {
            if (mInfoText == null) return;

            mInfoText.text = string.Format(ScoreFormat, score);
        }

        private void OnPlayerHealthChanged(object sender, GameEventArgs e)
        {
            PlayerHealthChangedEventArgs ne = e as PlayerHealthChangedEventArgs;
            if (ne == null)
            {
                GameAssert.Unreachable("HUDForm: PlayerHealthChanged 핸들러에 다른 타입이 들어왔다.");
                return;
            }

            if (mHealthSlider != null)
            {
                mHealthSlider.value = Mathf.Clamp01(ne.ToRatio) * mHealthSlider.maxValue;
            }

            // 스폰 통보(비율이 그대로인 경우)에는 화면을 번쩍이지 않는다.
            if (ne.IsDamage && mDamageFlash != null)
            {
                mDamageFlash.Flash();
            }
        }

        private void OnWeaponCooldownStarted(object sender, GameEventArgs e)
        {
            WeaponCooldownStartedEventArgs ne = e as WeaponCooldownStartedEventArgs;
            if (ne == null)
            {
                GameAssert.Unreachable("HUDForm: WeaponCooldownStarted 핸들러에 다른 타입이 들어왔다.");
                return;
            }

            if (mCountdown == null) return;

            mCountdown.BeginCountdown(ne.Cooldown);
        }

        private void OnPlayerDied(object sender, GameEventArgs e)
        {
            if (mGameOverText == null) return;

            mGameOverText.enabled = true;
        }
    }
}
