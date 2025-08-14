//------------------------------------------------------------
// Game Framework - MIT License
// Copyright © 2013–2021 Jiang Yin (EllanJiang)
// Modified © 2025 얌얌코딩
// Homepage: https://www.yamyamcoding.com/
// Feedback: mailto:eazuooz@gmail.com
//------------------------------------------------------------


using UnityEngine;

namespace UnityGameFramework.Runtime
{
    public sealed class UIStringKey : MonoBehaviour
    {
        [SerializeField]
        private string m_Key = null;

        public string Key
        {
            get
            {
                return m_Key ?? string.Empty;
            }
            set
            {
                m_Key = value;
            }
        }
    }
}
