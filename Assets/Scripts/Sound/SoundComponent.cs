//------------------------------------------------------------
// Game Framework - MIT License
// Copyright © 2013–2021 Jiang Yin (EllanJiang)
// Modified © 2025 얌얌코딩
// Homepage: https://www.yamyamcoding.com/
// Feedback: mailto:eazuooz@gmail.com
//------------------------------------------------------------


using GameFramework;
using GameFramework.Resource;
#if UNITY_5_3
using GameFramework.Scene;
#endif
using GameFramework.Sound;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

namespace UnityGameFramework.Runtime
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Game Framework/Sound")]
    public sealed partial class SoundComponent : GameFrameworkComponent
    {
        private const int DefaultPriority = 0;

        private ISoundManager m_SoundManager = null;
        private EventComponent m_EventComponent = null;
        private AudioListener m_AudioListener = null;

        [SerializeField]
        private bool m_EnablePlaySoundUpdateEvent = false;

        [SerializeField]
        private bool m_EnablePlaySoundDependencyAssetEvent = false;

        [SerializeField]
        private Transform m_InstanceRoot = null;

        [SerializeField]
        private AudioMixer m_AudioMixer = null;

        [SerializeField]
        private string m_SoundHelperTypeName = "UnityGameFramework.Runtime.DefaultSoundHelper";

        [SerializeField]
        private SoundHelperBase m_CustomSoundHelper = null;

        [SerializeField]
        private string m_SoundGroupHelperTypeName = "UnityGameFramework.Runtime.DefaultSoundGroupHelper";

        [SerializeField]
        private SoundGroupHelperBase m_CustomSoundGroupHelper = null;

        [SerializeField]
        private string m_SoundAgentHelperTypeName = "UnityGameFramework.Runtime.DefaultSoundAgentHelper";

        [SerializeField]
        private SoundAgentHelperBase m_CustomSoundAgentHelper = null;

        [SerializeField]
        private SoundGroup[] m_SoundGroups = null;

        public int SoundGroupCount
        {
            get
            {
                return m_SoundManager.SoundGroupCount;
            }
        }

        public AudioMixer AudioMixer
        {
            get
            {
                return m_AudioMixer;
            }
        }

        protected override void Awake()
        {
            base.Awake();

            m_SoundManager = GameFrameworkEntry.GetModule<ISoundManager>();
            if (m_SoundManager == null)
            {
                Log.Fatal("Sound manager is invalid.");
                return;
            }

            m_SoundManager.PlaySoundSuccess += OnPlaySoundSuccess;
            m_SoundManager.PlaySoundFailure += OnPlaySoundFailure;

            if (m_EnablePlaySoundUpdateEvent)
            {
                m_SoundManager.PlaySoundUpdate += OnPlaySoundUpdate;
            }

            if (m_EnablePlaySoundDependencyAssetEvent)
            {
                m_SoundManager.PlaySoundDependencyAsset += OnPlaySoundDependencyAsset;
            }

            m_AudioListener = gameObject.GetOrAddComponent<AudioListener>();

#if UNITY_5_4_OR_NEWER
            SceneManager.sceneLoaded += OnSceneLoaded;
            SceneManager.sceneUnloaded += OnSceneUnloaded;
#else
            ISceneManager sceneManager = GameFrameworkEntry.GetModule<ISceneManager>();
            if (sceneManager == null)
            {
                Log.Fatal("Scene manager is invalid.");
                return;
            }

            sceneManager.LoadSceneSuccess += OnLoadSceneSuccess;
            sceneManager.LoadSceneFailure += OnLoadSceneFailure;
            sceneManager.UnloadSceneSuccess += OnUnloadSceneSuccess;
            sceneManager.UnloadSceneFailure += OnUnloadSceneFailure;
#endif
        }

        private void Start()
        {
            BaseComponent baseComponent = GameEntry.GetComponent<BaseComponent>();
            if (baseComponent == null)
            {
                Log.Fatal("Base component is invalid.");
                return;
            }

            m_EventComponent = GameEntry.GetComponent<EventComponent>();
            if (m_EventComponent == null)
            {
                Log.Fatal("Event component is invalid.");
                return;
            }

            if (baseComponent.EditorResourceMode)
            {
                m_SoundManager.SetResourceManager(baseComponent.EditorResourceHelper);
            }
            else
            {
                m_SoundManager.SetResourceManager(GameFrameworkEntry.GetModule<IResourceManager>());
            }

            SoundHelperBase soundHelper = Helper.CreateHelper(m_SoundHelperTypeName, m_CustomSoundHelper);
            if (soundHelper == null)
            {
                Log.Error("Can not create sound helper.");
                return;
            }

            soundHelper.name = "Sound Helper";
            Transform transform = soundHelper.transform;
            transform.SetParent(this.transform);
            transform.localScale = Vector3.one;

            m_SoundManager.SetSoundHelper(soundHelper);

            if (m_InstanceRoot == null)
            {
                m_InstanceRoot = new GameObject("Sound Instances").transform;
                m_InstanceRoot.SetParent(gameObject.transform);
                m_InstanceRoot.localScale = Vector3.one;
            }

            for (int i = 0; i < m_SoundGroups.Length; i++)
            {
                if (!AddSoundGroup(m_SoundGroups[i].Name, m_SoundGroups[i].AvoidBeingReplacedBySamePriority, m_SoundGroups[i].Mute, m_SoundGroups[i].Volume, m_SoundGroups[i].AgentHelperCount))
                {
                    Log.Warning("Add sound group '{0}' failure.", m_SoundGroups[i].Name);
                    continue;
                }
            }
        }

        private void OnDestroy()
        {
#if UNITY_5_4_OR_NEWER
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneUnloaded -= OnSceneUnloaded;
#endif
        }

        public bool HasSoundGroup(string soundGroupName)
        {
            return m_SoundManager.HasSoundGroup(soundGroupName);
        }

        public ISoundGroup GetSoundGroup(string soundGroupName)
        {
            return m_SoundManager.GetSoundGroup(soundGroupName);
        }

        public ISoundGroup[] GetAllSoundGroups()
        {
            return m_SoundManager.GetAllSoundGroups();
        }

        public void GetAllSoundGroups(List<ISoundGroup> results)
        {
            m_SoundManager.GetAllSoundGroups(results);
        }

        public bool AddSoundGroup(string soundGroupName, int soundAgentHelperCount)
        {
            return AddSoundGroup(soundGroupName, false, false, 1f, soundAgentHelperCount);
        }

        public bool AddSoundGroup(string soundGroupName, bool soundGroupAvoidBeingReplacedBySamePriority, bool soundGroupMute, float soundGroupVolume, int soundAgentHelperCount)
        {
            if (m_SoundManager.HasSoundGroup(soundGroupName))
            {
                return false;
            }

            SoundGroupHelperBase soundGroupHelper = Helper.CreateHelper(m_SoundGroupHelperTypeName, m_CustomSoundGroupHelper, SoundGroupCount);
            if (soundGroupHelper == null)
            {
                Log.Error("Can not create sound group helper.");
                return false;
            }

            soundGroupHelper.name = Utility.Text.Format("Sound Group - {0}", soundGroupName);
            Transform transform = soundGroupHelper.transform;
            transform.SetParent(m_InstanceRoot);
            transform.localScale = Vector3.one;

            if (m_AudioMixer != null)
            {
                AudioMixerGroup[] audioMixerGroups = m_AudioMixer.FindMatchingGroups(Utility.Text.Format("Master/{0}", soundGroupName));
                if (audioMixerGroups.Length > 0)
                {
                    soundGroupHelper.AudioMixerGroup = audioMixerGroups[0];
                }
                else
                {
                    soundGroupHelper.AudioMixerGroup = m_AudioMixer.FindMatchingGroups("Master")[0];
                }
            }

            if (!m_SoundManager.AddSoundGroup(soundGroupName, soundGroupAvoidBeingReplacedBySamePriority, soundGroupMute, soundGroupVolume, soundGroupHelper))
            {
                return false;
            }

            for (int i = 0; i < soundAgentHelperCount; i++)
            {
                if (!AddSoundAgentHelper(soundGroupName, soundGroupHelper, i))
                {
                    return false;
                }
            }

            return true;
        }

        public int[] GetAllLoadingSoundSerialIds()
        {
            return m_SoundManager.GetAllLoadingSoundSerialIds();
        }

        public void GetAllLoadingSoundSerialIds(List<int> results)
        {
            m_SoundManager.GetAllLoadingSoundSerialIds(results);
        }

        public bool IsLoadingSound(int serialId)
        {
            return m_SoundManager.IsLoadingSound(serialId);
        }

        public int PlaySound(string soundAssetName, string soundGroupName)
        {
            return PlaySound(soundAssetName, soundGroupName, DefaultPriority, null, null, null);
        }

        public int PlaySound(string soundAssetName, string soundGroupName, int priority)
        {
            return PlaySound(soundAssetName, soundGroupName, priority, null, null, null);
        }

        public int PlaySound(string soundAssetName, string soundGroupName, PlaySoundParams playSoundParams)
        {
            return PlaySound(soundAssetName, soundGroupName, DefaultPriority, playSoundParams, null, null);
        }

        public int PlaySound(string soundAssetName, string soundGroupName, Entity bindingEntity)
        {
            return PlaySound(soundAssetName, soundGroupName, DefaultPriority, null, bindingEntity, null);
        }

        public int PlaySound(string soundAssetName, string soundGroupName, Vector3 worldPosition)
        {
            return PlaySound(soundAssetName, soundGroupName, DefaultPriority, null, worldPosition, null);
        }

        public int PlaySound(string soundAssetName, string soundGroupName, object userData)
        {
            return PlaySound(soundAssetName, soundGroupName, DefaultPriority, null, null, userData);
        }

        public int PlaySound(string soundAssetName, string soundGroupName, int priority, PlaySoundParams playSoundParams)
        {
            return PlaySound(soundAssetName, soundGroupName, priority, playSoundParams, null, null);
        }

        public int PlaySound(string soundAssetName, string soundGroupName, int priority, PlaySoundParams playSoundParams, object userData)
        {
            return PlaySound(soundAssetName, soundGroupName, priority, playSoundParams, null, userData);
        }

        public int PlaySound(string soundAssetName, string soundGroupName, int priority, PlaySoundParams playSoundParams, Entity bindingEntity)
        {
            return PlaySound(soundAssetName, soundGroupName, priority, playSoundParams, bindingEntity, null);
        }

        public int PlaySound(string soundAssetName, string soundGroupName, int priority, PlaySoundParams playSoundParams, Entity bindingEntity, object userData)
        {
            return m_SoundManager.PlaySound(soundAssetName, soundGroupName, priority, playSoundParams, PlaySoundInfo.Create(bindingEntity, Vector3.zero, userData));
        }

        public int PlaySound(string soundAssetName, string soundGroupName, int priority, PlaySoundParams playSoundParams, Vector3 worldPosition)
        {
            return PlaySound(soundAssetName, soundGroupName, priority, playSoundParams, worldPosition, null);
        }

        public int PlaySound(string soundAssetName, string soundGroupName, int priority, PlaySoundParams playSoundParams, Vector3 worldPosition, object userData)
        {
            return m_SoundManager.PlaySound(soundAssetName, soundGroupName, priority, playSoundParams, PlaySoundInfo.Create(null, worldPosition, userData));
        }

        public bool StopSound(int serialId)
        {
            return m_SoundManager.StopSound(serialId);
        }

        public bool StopSound(int serialId, float fadeOutSeconds)
        {
            return m_SoundManager.StopSound(serialId, fadeOutSeconds);
        }

        public void StopAllLoadedSounds()
        {
            m_SoundManager.StopAllLoadedSounds();
        }

        public void StopAllLoadedSounds(float fadeOutSeconds)
        {
            m_SoundManager.StopAllLoadedSounds(fadeOutSeconds);
        }

        public void StopAllLoadingSounds()
        {
            m_SoundManager.StopAllLoadingSounds();
        }

        public void PauseSound(int serialId)
        {
            m_SoundManager.PauseSound(serialId);
        }

        public void PauseSound(int serialId, float fadeOutSeconds)
        {
            m_SoundManager.PauseSound(serialId, fadeOutSeconds);
        }

        public void ResumeSound(int serialId)
        {
            m_SoundManager.ResumeSound(serialId);
        }

        public void ResumeSound(int serialId, float fadeInSeconds)
        {
            m_SoundManager.ResumeSound(serialId, fadeInSeconds);
        }

        private bool AddSoundAgentHelper(string soundGroupName, SoundGroupHelperBase soundGroupHelper, int index)
        {
            SoundAgentHelperBase soundAgentHelper = Helper.CreateHelper(m_SoundAgentHelperTypeName, m_CustomSoundAgentHelper, index);
            if (soundAgentHelper == null)
            {
                Log.Error("Can not create sound agent helper.");
                return false;
            }

            soundAgentHelper.name = Utility.Text.Format("Sound Agent Helper - {0} - {1}", soundGroupName, index);
            Transform transform = soundAgentHelper.transform;
            transform.SetParent(soundGroupHelper.transform);
            transform.localScale = Vector3.one;

            if (m_AudioMixer != null)
            {
                AudioMixerGroup[] audioMixerGroups = m_AudioMixer.FindMatchingGroups(Utility.Text.Format("Master/{0}/{1}", soundGroupName, index));
                if (audioMixerGroups.Length > 0)
                {
                    soundAgentHelper.AudioMixerGroup = audioMixerGroups[0];
                }
                else
                {
                    soundAgentHelper.AudioMixerGroup = soundGroupHelper.AudioMixerGroup;
                }
            }

            m_SoundManager.AddSoundAgentHelper(soundGroupName, soundAgentHelper);

            return true;
        }

        private void OnPlaySoundSuccess(object sender, GameFramework.Sound.PlaySoundSuccessEventArgs e)
        {
            PlaySoundInfo playSoundInfo = (PlaySoundInfo)e.UserData;
            if (playSoundInfo != null)
            {
                SoundAgentHelperBase soundAgentHelper = (SoundAgentHelperBase)e.SoundAgent.Helper;
                if (playSoundInfo.BindingEntity != null)
                {
                    soundAgentHelper.SetBindingEntity(playSoundInfo.BindingEntity);
                }
                else
                {
                    soundAgentHelper.SetWorldPosition(playSoundInfo.WorldPosition);
                }
            }

            m_EventComponent.Fire(this, PlaySoundSuccessEventArgs.Create(e));
        }

        private void OnPlaySoundFailure(object sender, GameFramework.Sound.PlaySoundFailureEventArgs e)
        {
            string logMessage = Utility.Text.Format("Play sound failure, asset name '{0}', sound group name '{1}', error code '{2}', error message '{3}'.", e.SoundAssetName, e.SoundGroupName, e.ErrorCode, e.ErrorMessage);
            if (e.ErrorCode == PlaySoundErrorCode.IgnoredDueToLowPriority)
            {
                Log.Info(logMessage);
            }
            else
            {
                Log.Warning(logMessage);
            }

            m_EventComponent.Fire(this, PlaySoundFailureEventArgs.Create(e));
        }

        private void OnPlaySoundUpdate(object sender, GameFramework.Sound.PlaySoundUpdateEventArgs e)
        {
            m_EventComponent.Fire(this, PlaySoundUpdateEventArgs.Create(e));
        }

        private void OnPlaySoundDependencyAsset(object sender, GameFramework.Sound.PlaySoundDependencyAssetEventArgs e)
        {
            m_EventComponent.Fire(this, PlaySoundDependencyAssetEventArgs.Create(e));
        }

        private void OnLoadSceneSuccess(object sender, GameFramework.Scene.LoadSceneSuccessEventArgs e)
        {
            RefreshAudioListener();
        }

        private void OnLoadSceneFailure(object sender, GameFramework.Scene.LoadSceneFailureEventArgs e)
        {
            RefreshAudioListener();
        }

        private void OnUnloadSceneSuccess(object sender, GameFramework.Scene.UnloadSceneSuccessEventArgs e)
        {
            RefreshAudioListener();
        }

        private void OnUnloadSceneFailure(object sender, GameFramework.Scene.UnloadSceneFailureEventArgs e)
        {
            RefreshAudioListener();
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode loadSceneMode)
        {
            RefreshAudioListener();
        }

        private void OnSceneUnloaded(Scene scene)
        {
            RefreshAudioListener();
        }

        private void RefreshAudioListener()
        {
            m_AudioListener.enabled = FindObjectsOfType<AudioListener>().Length <= 1;
        }
    }
}
