using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityGameFramework.Runtime;

namespace ToyBoxNightmare
{
    /// <summary>
    /// 일시정지 메뉴. Esc 로 열고 닫으며 볼륨·음소거·종료를 제공한다.
    ///
    /// <b>폼 자체는 판이 시작될 때 열려서 계속 떠 있고, 패널만 켜고 끈다.</b>
    /// 닫혀 있으면 Esc 를 읽을 주체가 없어지기 때문이다(원본도 PauseMenuCanvas 가
    /// 씬에 항상 있고 패널만 토글한다).
    ///
    /// <b>이 스크립트는 프리팹에 직접 붙어 있어야 한다</b>(UI 폼 규약 — 엔티티와 정반대).
    /// </summary>
    public class PauseMenuForm : UIFormLogic
    {
        // 프리팹 계층 계약.
        private const string PanelPath         = "PausePanel";
        private const string ResumeButtonPath  = "PausePanel/ResumeButton";
        private const string QuitButtonPath    = "PausePanel/QuitButton";
        private const string AudioTogglePath   = "PausePanel/AudioToggle";
        private const string EffectsSliderPath = "PausePanel/EffectsSlider";
        private const string MusicSliderPath   = "PausePanel/MusicSlider";

        // 믹서에 노출된 파라미터 이름. 원본 PauseMenu 와 같다.
        private const string SfxVolumeParam   = "sfxVol";
        private const string MusicVolumeParam = "musicVol";

        [Tooltip("MasterMixer. 볼륨 슬라이더가 이 믹서의 노출 파라미터를 직접 건드린다.")]
        [SerializeField] private AudioMixer masterMixer = null;

        [Tooltip("음소거 스냅샷.")]
        [SerializeField] private AudioMixerSnapshot mutedSnapshot = null;

        [Tooltip("음소거 해제 스냅샷.")]
        [SerializeField] private AudioMixerSnapshot unMutedSnapshot = null;

        private GameObject mPanel         = null;
        private Button     mResumeButton  = null;
        private Button     mQuitButton    = null;
        private Toggle     mAudioToggle   = null;
        private Slider     mEffectsSlider = null;
        private Slider     mMusicSlider   = null;

        // ─── 생명주기 ───

        protected internal override void OnInit(object userData)
        {
            base.OnInit(userData);

            Transform panel = CachedTransform.Find(PanelPath);
            mPanel = panel != null ? panel.gameObject : null;
            if (mPanel == null)
            {
                Log.Error("PauseMenuForm: {0} 을 찾지 못했다. 일시정지 화면이 뜨지 않는다.", PanelPath);
            }

            mResumeButton  = Find<Button>(ResumeButtonPath);
            mQuitButton    = Find<Button>(QuitButtonPath);
            mAudioToggle   = Find<Toggle>(AudioTogglePath);
            mEffectsSlider = Find<Slider>(EffectsSliderPath);
            mMusicSlider   = Find<Slider>(MusicSliderPath);

            // 버튼 연결은 프리팹의 onClick 직렬화가 아니라 코드로 건다.
            // 원본 프리팹의 onClick 은 제거한 Sample PauseMenu 를 가리키고 있어 그대로 두면 죽은 참조다.
            // OnInit 은 인스턴스당 1회라 리스너가 중복되지 않는다.
            if (mResumeButton != null) mResumeButton.onClick.AddListener(Resume);
            if (mQuitButton != null)   mQuitButton.onClick.AddListener(Quit);
            if (mAudioToggle != null)  mAudioToggle.onValueChanged.AddListener(SetSoundOn);

            if (mEffectsSlider != null) mEffectsSlider.onValueChanged.AddListener(SetEffectsLevel);
            if (mMusicSlider != null)   mMusicSlider.onValueChanged.AddListener(SetMusicLevel);
        }

        private T Find<T>(string path) where T : Component
        {
            Transform child = CachedTransform.Find(path);
            if (child == null)
            {
                Log.Warning("PauseMenuForm: 자식 {0} 이 없다.", path);
                return null;
            }

            T component = child.GetComponent<T>();
            if (component == null)
            {
                Log.Warning("PauseMenuForm: {0} 에 {1} 컴포넌트가 없다.", path, typeof(T).Name);
            }

            return component;
        }

        protected internal override void OnOpen(object userData)
        {
            base.OnOpen(userData);

            // 폼은 판 내내 떠 있지만 패널은 닫힌 상태로 시작한다.
            SetPanelActive(false);
            GamePause.SetPaused(false);

            PullVolumeFromMixer();
        }

        protected internal override void OnClose(bool isShutdown, object userData)
        {
            // 정지 상태로 판을 벗어나면 timeScale 이 0 인 채로 남아 다음 판이 통째로 멈춘다.
            GamePause.Clear();

            base.OnClose(isShutdown, userData);
        }

        /// <summary>
        /// 정지 중에도 돌아야 하므로 <c>realElapseSeconds</c> 기준으로 생각한다.
        /// (UIFormLogic.OnUpdate 는 timeScale 과 무관하게 매 프레임 불린다.)
        /// </summary>
        protected internal override void OnUpdate(float elapseSeconds, float realElapseSeconds)
        {
            base.OnUpdate(elapseSeconds, realElapseSeconds);

            if (!IsCancelPressed()) return;

            Toggle();
        }

        /// <summary>Esc(원본 Cancel 축). 키보드가 없으면 무시한다.</summary>
        private static bool IsCancelPressed()
        {
            Keyboard keyboard = Keyboard.current;

            return keyboard != null && keyboard.escapeKey.wasPressedThisFrame;
        }

        // ─── 정지 토글 ───

        public void Toggle()
        {
            bool paused = !GamePause.IsPaused;

            GamePause.SetPaused(paused);
            SetPanelActive(paused);
        }

        public void Resume()
        {
            GamePause.SetPaused(false);
            SetPanelActive(false);
        }

        private void SetPanelActive(bool active)
        {
            if (mPanel == null) return;
            if (mPanel.activeSelf == active) return;

            mPanel.SetActive(active);
        }

        // ─── 오디오 ───

        /// <summary>믹서의 현재 값을 슬라이더에 반영한다. 원본 PauseMenu.Awake 와 같다.</summary>
        private void PullVolumeFromMixer()
        {
            if (masterMixer == null)
            {
                Log.Warning("PauseMenuForm: MasterMixer 가 지정되지 않았다. 볼륨 조절이 동작하지 않는다.");
                return;
            }

            if (mEffectsSlider != null && masterMixer.GetFloat(SfxVolumeParam, out float sfx))
            {
                mEffectsSlider.SetValueWithoutNotify(sfx);
            }

            if (mMusicSlider != null && masterMixer.GetFloat(MusicVolumeParam, out float music))
            {
                mMusicSlider.SetValueWithoutNotify(music);
            }
        }

        public void SetEffectsLevel(float level)
        {
            if (masterMixer == null) return;

            masterMixer.SetFloat(SfxVolumeParam, level);
        }

        public void SetMusicLevel(float level)
        {
            if (masterMixer == null) return;

            masterMixer.SetFloat(MusicVolumeParam, level);
        }

        /// <summary>음소거는 볼륨 값이 아니라 스냅샷 전환으로 한다(원본과 같다).</summary>
        public void SetSoundOn(bool soundOn)
        {
            AudioMixerSnapshot snapshot = soundOn ? unMutedSnapshot : mutedSnapshot;
            if (snapshot == null)
            {
                Log.Warning("PauseMenuForm: 오디오 스냅샷이 지정되지 않았다.");
                return;
            }

            snapshot.TransitionTo(0f);
        }

        // ─── 종료 ───

        public void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
