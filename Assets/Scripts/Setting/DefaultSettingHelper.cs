//------------------------------------------------------------
// Game Framework - MIT License
// Copyright © 2013–2021 Jiang Yin (EllanJiang)
// Modified © 2025 얌얌코딩
// Homepage: https://www.yamyamcoding.com/
// Feedback: mailto:eazuooz@gmail.com
//------------------------------------------------------------


using GameFramework;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace UnityGameFramework.Runtime
{
    public class DefaultSettingHelper : SettingHelperBase
    {
        private const string SettingFileName = "GameFrameworkSetting.dat";

        private string m_FilePath = null;
        private DefaultSetting m_Settings = null;
        private DefaultSettingSerializer m_Serializer = null;

        public override int Count
        {
            get
            {
                return m_Settings != null ? m_Settings.Count : 0;
            }
        }

        public string FilePath
        {
            get
            {
                return m_FilePath;
            }
        }

        public DefaultSetting Setting
        {
            get
            {
                return m_Settings;
            }
        }

        public DefaultSettingSerializer Serializer
        {
            get
            {
                return m_Serializer;
            }
        }

        public override bool Load()
        {
            try
            {
                if (!File.Exists(m_FilePath))
                {
                    return true;
                }

                using (FileStream fileStream = new FileStream(m_FilePath, FileMode.Open, FileAccess.Read))
                {
                    m_Serializer.Deserialize(fileStream);
                    return true;
                }
            }
            catch (Exception exception)
            {
                Log.Warning("Load settings failure with exception '{0}'.", exception);
                return false;
            }
        }

        public override bool Save()
        {
            try
            {
                using (FileStream fileStream = new FileStream(m_FilePath, FileMode.Create, FileAccess.Write))
                {
                    return m_Serializer.Serialize(fileStream, m_Settings);
                }
            }
            catch (Exception exception)
            {
                Log.Warning("Save settings failure with exception '{0}'.", exception);
                return false;
            }
        }

        public override string[] GetAllSettingNames()
        {
            return m_Settings.GetAllSettingNames();
        }

        public override void GetAllSettingNames(List<string> results)
        {
            m_Settings.GetAllSettingNames(results);
        }

        public override bool HasSetting(string settingName)
        {
            return m_Settings.HasSetting(settingName);
        }

        public override bool RemoveSetting(string settingName)
        {
            return m_Settings.RemoveSetting(settingName);
        }

        public override void RemoveAllSettings()
        {
            m_Settings.RemoveAllSettings();
        }

        public override bool GetBool(string settingName)
        {
            return m_Settings.GetBool(settingName);
        }

        public override bool GetBool(string settingName, bool defaultValue)
        {
            return m_Settings.GetBool(settingName, defaultValue);
        }

        public override void SetBool(string settingName, bool value)
        {
            m_Settings.SetBool(settingName, value);
        }

        public override int GetInt(string settingName)
        {
            return m_Settings.GetInt(settingName);
        }

        public override int GetInt(string settingName, int defaultValue)
        {
            return m_Settings.GetInt(settingName, defaultValue);
        }

        public override void SetInt(string settingName, int value)
        {
            m_Settings.SetInt(settingName, value);
        }

        public override float GetFloat(string settingName)
        {
            return m_Settings.GetFloat(settingName);
        }

        public override float GetFloat(string settingName, float defaultValue)
        {
            return m_Settings.GetFloat(settingName, defaultValue);
        }

        public override void SetFloat(string settingName, float value)
        {
            m_Settings.SetFloat(settingName, value);
        }

        public override string GetString(string settingName)
        {
            return m_Settings.GetString(settingName);
        }

        public override string GetString(string settingName, string defaultValue)
        {
            return m_Settings.GetString(settingName, defaultValue);
        }

        public override void SetString(string settingName, string value)
        {
            m_Settings.SetString(settingName, value);
        }

        public override T GetObject<T>(string settingName)
        {
            return Utility.Json.ToObject<T>(GetString(settingName));
        }

        public override object GetObject(Type objectType, string settingName)
        {
            return Utility.Json.ToObject(objectType, GetString(settingName));
        }

        public override T GetObject<T>(string settingName, T defaultObj)
        {
            string json = GetString(settingName, null);
            if (json == null)
            {
                return defaultObj;
            }

            return Utility.Json.ToObject<T>(json);
        }

        public override object GetObject(Type objectType, string settingName, object defaultObj)
        {
            string json = GetString(settingName, null);
            if (json == null)
            {
                return defaultObj;
            }

            return Utility.Json.ToObject(objectType, json);
        }

        public override void SetObject<T>(string settingName, T obj)
        {
            SetString(settingName, Utility.Json.ToJson(obj));
        }

        public override void SetObject(string settingName, object obj)
        {
            SetString(settingName, Utility.Json.ToJson(obj));
        }

        private void Awake()
        {
            m_FilePath = Utility.Path.GetRegularPath(Path.Combine(Application.persistentDataPath, SettingFileName));
            m_Settings = new DefaultSetting();
            m_Serializer = new DefaultSettingSerializer();
            m_Serializer.RegisterSerializeCallback(0, SerializeDefaultSettingCallback);
            m_Serializer.RegisterDeserializeCallback(0, DeserializeDefaultSettingCallback);
        }

        private bool SerializeDefaultSettingCallback(Stream stream, DefaultSetting defaultSetting)
        {
            m_Settings.Serialize(stream);
            return true;
        }

        private DefaultSetting DeserializeDefaultSettingCallback(Stream stream)
        {
            m_Settings.Deserialize(stream);
            return m_Settings;
        }
    }
}
