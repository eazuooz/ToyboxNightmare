//------------------------------------------------------------
// Game Framework - MIT License
// Copyright © 2013–2021 Jiang Yin (EllanJiang)
// Modified © 2025 얌얌코딩
// Homepage: https://www.yamyamcoding.com/
// Feedback: mailto:eazuooz@gmail.com
//------------------------------------------------------------

using UnityEditor;
using UnityEngine;

namespace UnityGameFramework.Editor
{
    public static class Help
    {
        [MenuItem("Game Framework/Documentation", false, 90)]
        public static void ShowDocumentation()
        {
            ShowHelp("https://www.yamyamcoding.com/");
        }

        // 원본 Game Framework 의 문서(중국어). 이 에디션은 리소스 계층이 Addressables 로 바뀌어
        // Resource 관련 설명은 맞지 않는다. 그 밖의 모듈 설명은 그대로 참고할 수 있다.
        [MenuItem("Game Framework/Original Game Framework (EllanJiang)", false, 91)]
        public static void ShowOriginalDocumentation()
        {
            ShowHelp("https://gameframework.cn/document/");
        }

        private static void ShowHelp(string uri)
        {
            Application.OpenURL(uri);
        }
    }
}
