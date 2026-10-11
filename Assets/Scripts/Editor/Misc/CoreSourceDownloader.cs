//------------------------------------------------------------
// Game Framework (YamYamCoding Edition) - MIT License
// Copyright © 2025 얌얌코딩
// Homepage: https://www.yamyamcoding.com/
// Feedback: mailto:eazuooz@gmail.com
//------------------------------------------------------------

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace UnityGameFramework.Editor
{
    // 코어(Game Framework)는 Unity 프로젝트 밖의 별도 C# 프로젝트이고, 이 프로젝트에는 빌드 결과인
    // Assets/Plugins/GameFramework.dll 만 들어 있다. DLL 만으로도 프레임워크는 그대로 동작한다.
    //
    // 코어를 디버깅하거나 고치고 싶을 때 이 메뉴로 소스를 받는다. 저장소를
    // <프로젝트>/External/GameFramework 에 클론하며, 그 프로젝트를 빌드하면 빌드 후 이벤트가
    // DLL 을 Assets/Plugins 로 복사한다.
    //
    // 사용자가 메뉴를 고르고 확인 창에서 동의했을 때만 받는다. 임포트만으로는 아무것도 받지 않는다.
    public static class CoreSourceDownloader
    {
        private const string RepositoryUrl = "https://github.com/eazuooz/GameFramework.git";

        private const string ExternalFolderName = "External";
        private const string CoreFolderName = "GameFramework";
        private const string SolutionFileName = "GameFramework.sln";

        private const string DialogTitle = "Game Framework Core Source";

        [MenuItem("Game Framework/Download Core Source", false, 85)]
        public static void DownloadCoreSource()
        {
            string targetDirectory = GetTargetDirectory();

            if (Directory.Exists(targetDirectory))
            {
                bool reveal = EditorUtility.DisplayDialog(DialogTitle,
                    "The core source is already here:\n" + targetDirectory + "\n\nNothing was downloaded. To update it, pull the repository yourself.",
                    "Show Folder", "Close");
                if (reveal)
                {
                    EditorUtility.RevealInFinder(targetDirectory);
                }

                return;
            }

            bool agreed = EditorUtility.DisplayDialog(DialogTitle,
                "This will download the Game Framework core source with git:\n\n  git clone " + RepositoryUrl + "\n\ninto:\n" + targetDirectory
                + "\n\nIt needs git and an internet connection. The framework already works without it (Assets/Plugins/GameFramework.dll); download it only if you want to debug or modify the core.",
                "Download", "Cancel");
            if (!agreed)
            {
                return;
            }

            string message = null;
            bool succeeded = false;
            try
            {
                EditorUtility.DisplayProgressBar(DialogTitle, "Cloning " + RepositoryUrl + " ...", 0.5f);
                succeeded = Clone(targetDirectory, out message);
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            if (!succeeded)
            {
                UnityEngine.Debug.LogError("Download core source failure: " + message);
                EditorUtility.DisplayDialog(DialogTitle,
                    "Download failed.\n\n" + message + "\n\nYou can clone it yourself:\n  git clone " + RepositoryUrl + " \"" + targetDirectory + "\"",
                    "Close");
                return;
            }

            UnityEngine.Debug.Log("Download core source complete: " + targetDirectory);

            bool show = EditorUtility.DisplayDialog(DialogTitle,
                "Downloaded to:\n" + targetDirectory + "\n\nOpen " + SolutionFileName + " and build it. The build copies GameFramework.dll to Assets/Plugins, and Unity picks it up.",
                "Show Folder", "Close");
            if (show)
            {
                EditorUtility.RevealInFinder(Path.Combine(targetDirectory, SolutionFileName));
            }
        }

        internal static string GetTargetDirectory()
        {
            string projectRoot = Directory.GetParent(Application.dataPath).FullName;

            return Path.Combine(projectRoot, ExternalFolderName, CoreFolderName);
        }

        // 창을 띄우지 않고 클론만 한다. 실패하면 false 와 함께 이유를 돌려준다.
        internal static bool Clone(string targetDirectory, out string message)
        {
            if (Directory.Exists(targetDirectory))
            {
                message = "The target directory already exists: " + targetDirectory;
                return false;
            }

            ProcessStartInfo startInfo = new ProcessStartInfo("git", "clone " + RepositoryUrl + " \"" + targetDirectory + "\"")
            {
                UseShellExecute = false,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            try
            {
                using (Process process = Process.Start(startInfo))
                {
                    // git 은 진행 상황과 오류를 모두 표준 오류로 쓴다.
                    string output = process.StandardError.ReadToEnd();
                    process.WaitForExit();

                    if (process.ExitCode != 0)
                    {
                        message = "git exited with code " + process.ExitCode + ". " + output.Trim();
                        return false;
                    }
                }
            }
            catch (Win32Exception)
            {
                message = "Can not run git. Install git and make sure it is on the PATH.";
                return false;
            }
            catch (Exception exception)
            {
                message = exception.Message;
                return false;
            }

            message = null;
            return true;
        }
    }
}
