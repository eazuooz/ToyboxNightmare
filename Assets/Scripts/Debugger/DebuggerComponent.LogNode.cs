//------------------------------------------------------------
// Game Framework - MIT License
// Copyright © 2013–2021 Jiang Yin (EllanJiang)
// Modified © 2025 얌얌코딩
// Homepage: https://www.yamyamcoding.com/
// Feedback: mailto:eazuooz@gmail.com
//------------------------------------------------------------


using GameFramework;
using System;
using UnityEngine;

namespace UnityGameFramework.Runtime
{
    public sealed partial class DebuggerComponent : GameFrameworkComponent
    {
        public sealed class LogNode : IReference
        {
            private DateTime m_LogTime;
            private int m_LogFrameCount;
            private LogType m_LogType;
            private string m_LogMessage;
            private string m_StackTrack;

            public LogNode()
            {
                m_LogTime = default(DateTime);
                m_LogFrameCount = 0;
                m_LogType = LogType.Error;
                m_LogMessage = null;
                m_StackTrack = null;
            }

            public DateTime LogTime
            {
                get
                {
                    return m_LogTime;
                }
            }

            public int LogFrameCount
            {
                get
                {
                    return m_LogFrameCount;
                }
            }

            public LogType LogType
            {
                get
                {
                    return m_LogType;
                }
            }

            public string LogMessage
            {
                get
                {
                    return m_LogMessage;
                }
            }

            public string StackTrack
            {
                get
                {
                    return m_StackTrack;
                }
            }

            public static LogNode Create(LogType logType, string logMessage, string stackTrack)
            {
                LogNode logNode = ReferencePool.Acquire<LogNode>();
                logNode.m_LogTime = DateTime.UtcNow;
                logNode.m_LogFrameCount = Time.frameCount;
                logNode.m_LogType = logType;
                logNode.m_LogMessage = logMessage;
                logNode.m_StackTrack = stackTrack;
                return logNode;
            }

            public void Clear()
            {
                m_LogTime = default(DateTime);
                m_LogFrameCount = 0;
                m_LogType = LogType.Error;
                m_LogMessage = null;
                m_StackTrack = null;
            }
        }
    }
}
