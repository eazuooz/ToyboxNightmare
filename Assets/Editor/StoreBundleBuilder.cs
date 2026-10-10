using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

namespace ToyBoxNightmare.EditorTools
{
    /// <summary>
    /// 에셋스토어 제출용 묶음을 만든다. 들어가는 것은 셋뿐이다 —
    /// 프레임워크 코어, Unity 래퍼, 그리고 예제(이 게임).
    ///
    /// <b>개발 프로젝트는 건드리지 않는다.</b> 필요한 파일만 <c>StoreBundle/</c> 로 복사하면서
    /// 스토어 레이아웃(루트 폴더 하나)으로 재배치한다. <c>.meta</c> 를 함께 복사하므로 GUID 가
    /// 유지되고, 그래서 경로가 바뀌어도 프리팹·씬·Addressables 참조가 끊기지 않는다.
    ///
    /// 예제에 무엇이 들어갈지는 손으로 고르지 않는다. 빌드 씬 + Addressables 엔트리 + 게임 코드를
    /// 뿌리로 의존성 폐포를 구한다 — 게임이 실제로 쓰는 것만 들어가고, 프로젝트에 같이 있는
    /// 유료 에셋은 참조되지 않는 한 따라오지 않는다.
    /// </summary>
    public static class StoreBundleBuilder
    {
        /// <summary>프로젝트 루트 기준 출력 폴더. 통째로 지우고 다시 만든다.</summary>
        private const string OutputFolder = "StoreBundle";

        /// <summary>묶음 안의 단일 루트. 스토어 가이드라인 2.1.a 가 루트 하나를 요구한다.</summary>
        private const string BundleRoot      = "Assets/GameFramework";
        private const string FrameworkFolder = BundleRoot + "/Framework";
        private const string ExampleFolder   = BundleRoot + "/Example";

        private const string PackagePath    = "Packages/com.yamyamcoding.gameframework";
        private const string PackageRuntime = PackagePath + "/Runtime";

        /// <summary>게임 코드. 폴더 전체가 예제의 뿌리다.</summary>
        private const string GameCodeFolder = "Assets/GameMain";

        /// <summary>Addressables 설정은 위치를 옮기지 않는다. 기본 경로에 있어야 다루기 쉽다.</summary>
        private const string AddressablesDataFolder = "Assets/AddressableAssetsData";

        /// <summary>
        /// 의존성 폐포에 잡히지 않지만 필요한 폴더.
        /// 에셋 참조가 아니라 ProjectSettings 나 코드가 가리키는 것들이다.
        /// </summary>
        private static readonly string[] sExtraFolders =
        {
            "Assets/Settings",      // URP 파이프라인 에셋 — GraphicsSettings/QualitySettings 가 참조한다
            "Assets/AssetManager",  // Environment.prefab 의 컴포넌트가 같은 폴더의 로더를 코드로 참조한다
        };

        /// <summary>
        /// 재배포할 수 없는 에셋. 폐포에 하나라도 섞이면 묶음을 만들지 않고 멈춘다.
        /// 누군가 예제에서 이 에셋을 참조하기 시작하면 여기서 걸린다.
        /// </summary>
        private static readonly string[] sForbiddenFolders =
        {
            "Assets/Art/Effect/Epic Toon FX",
            "Assets/Art/Effect/JMO Assets",
            "Assets/Art/Effect/118 sprite effects bundle",
        };

        /// <summary>패키지 루트에서 묶음 루트로 그대로 옮기는 문서.</summary>
        private static readonly string[] sPackageDocs =
        {
            "LICENSE.md",
            "README.md",
            "CHANGELOG.md",
        };

        [MenuItem("Tools/Store Bundle/Build")]
        public static void Build()
        {
            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            string output      = Path.Combine(projectRoot, OutputFolder);

            List<string> exampleAssets = CollectExampleAssets();

            string forbidden = FindForbidden(exampleAssets);
            if (forbidden != null)
            {
                Debug.LogError("StoreBundle: 재배포할 수 없는 에셋이 예제에 참조돼 있어 중단한다 → " + forbidden);
                return;
            }

            if (Directory.Exists(output))
            {
                Directory.Delete(output, true);
            }

            int exampleCount   = CopyAssets(projectRoot, output, exampleAssets);
            int frameworkCount = CopyAssets(projectRoot, output, CollectFolderAssets(PackageRuntime));

            CopyPackageDocs(projectRoot, output);
            WriteThirdPartyNotices(output);
            CopyProjectSettings(projectRoot, output);
            WriteManifest(projectRoot, output, exampleAssets);

            Debug.Log(string.Format("StoreBundle: 완료 — 프레임워크 {0}개, 예제 {1}개 → {2}",
                frameworkCount, exampleCount, output));
        }

        // ─── 무엇을 넣을지 ───

        /// <summary>예제가 실제로 쓰는 에셋 전부(프로젝트 상대 경로). 패키지 쪽 경로는 뺀다.</summary>
        private static List<string> CollectExampleAssets()
        {
            var roots = new List<string>();

            foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
            {
                if (scene.enabled) roots.Add(scene.path);
            }

            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings != null)
            {
                foreach (AddressableAssetGroup group in settings.groups)
                {
                    if (group == null) continue;

                    foreach (AddressableAssetEntry entry in group.entries)
                    {
                        roots.Add(entry.AssetPath);
                    }
                }
            }

            roots.AddRange(CollectFolderAssets(GameCodeFolder));

            var result = new SortedSet<string>(System.StringComparer.Ordinal);
            foreach (string path in AssetDatabase.GetDependencies(roots.ToArray(), true))
            {
                if (IsBundledAssetFile(path)) result.Add(path);
            }

            foreach (string path in roots)
            {
                if (IsBundledAssetFile(path)) result.Add(path);
            }

            foreach (string folder in sExtraFolders)
            {
                result.UnionWith(CollectFolderAssets(folder));
            }

            result.UnionWith(CollectFolderAssets(AddressablesDataFolder));

            return new List<string>(result);
        }

        /// <summary>Assets 아래의 실제 파일인가. Unity 패키지 쪽 의존성은 manifest 로 해결한다.</summary>
        private static bool IsBundledAssetFile(string path)
        {
            return path.StartsWith("Assets/") && File.Exists(path);
        }

        private static List<string> CollectFolderAssets(string folder)
        {
            var result = new List<string>();
            if (!AssetDatabase.IsValidFolder(folder)) return result;

            foreach (string guid in AssetDatabase.FindAssets(string.Empty, new[] { folder }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (File.Exists(path)) result.Add(path);
            }

            return result;
        }

        private static string FindForbidden(List<string> paths)
        {
            foreach (string path in paths)
            {
                foreach (string folder in sForbiddenFolders)
                {
                    if (path.StartsWith(folder + "/")) return path;
                }
            }

            return null;
        }

        // ─── 어디에 놓을지 ───

        /// <summary>개발 프로젝트 경로를 묶음 안 경로로 바꾼다. 묶음에 넣지 않는 경로면 null.</summary>
        private static string MapPath(string path)
        {
            if (path == AddressablesDataFolder || path.StartsWith(AddressablesDataFolder + "/")) return path;

            if (path.StartsWith(PackageRuntime + "/"))
            {
                return FrameworkFolder + "/" + path.Substring(PackageRuntime.Length + 1);
            }

            if (path.StartsWith("Assets/"))
            {
                return ExampleFolder + "/" + path.Substring("Assets/".Length);
            }

            return null;
        }

        /// <summary>이 경로에서 위로 올라가기를 멈출 지점인가. 그 위 폴더의 .meta 는 가져오지 않는다.</summary>
        private static bool IsMappingRoot(string folder)
        {
            return folder == "Assets" || folder == PackageRuntime;
        }

        private static int CopyAssets(string projectRoot, string output, List<string> paths)
        {
            int count = 0;

            foreach (string path in paths)
            {
                string mapped = MapPath(path);
                if (mapped == null) continue;

                CopyWithMeta(projectRoot, output, path, mapped);
                CopyParentFolderMetas(projectRoot, output, path);
                count++;
            }

            return count;
        }

        private static void CopyWithMeta(string projectRoot, string output, string path, string mapped)
        {
            string source = Path.Combine(projectRoot, path);
            string target = Path.Combine(output, mapped);

            Directory.CreateDirectory(Path.GetDirectoryName(target));
            File.Copy(source, target, true);

            if (File.Exists(source + ".meta"))
            {
                File.Copy(source + ".meta", target + ".meta", true);
            }
        }

        /// <summary>상위 폴더의 .meta 도 옮겨 폴더 GUID 를 유지한다.</summary>
        private static void CopyParentFolderMetas(string projectRoot, string output, string path)
        {
            string folder = Path.GetDirectoryName(path).Replace('\\', '/');

            while (!string.IsNullOrEmpty(folder) && !IsMappingRoot(folder))
            {
                string mapped = MapPath(folder + "/x");
                string meta   = Path.Combine(projectRoot, folder + ".meta");

                if (mapped != null && File.Exists(meta))
                {
                    string mappedFolder = mapped.Substring(0, mapped.Length - "/x".Length);
                    string target       = Path.Combine(output, mappedFolder + ".meta");

                    Directory.CreateDirectory(Path.GetDirectoryName(target));
                    File.Copy(meta, target, true);
                }

                folder = Path.GetDirectoryName(folder).Replace('\\', '/');
            }
        }

        // ─── 문서·설정 ───

        private static void CopyPackageDocs(string projectRoot, string output)
        {
            foreach (string doc in sPackageDocs)
            {
                string source = Path.Combine(projectRoot, PackagePath, doc);
                if (!File.Exists(source)) continue;

                string target = Path.Combine(output, BundleRoot, doc);
                Directory.CreateDirectory(Path.GetDirectoryName(target));
                File.Copy(source, target, true);
            }
        }

        /// <summary>
        /// 스토어 가이드라인 1.2.a 가 요구하는 고지 파일. 프레임워크 고지에 예제가 들고 온
        /// 서드파티(폰트, Unity 샘플 에셋)를 더한다.
        /// </summary>
        private static void WriteThirdPartyNotices(string output)
        {
            var sb = new StringBuilder();
            sb.AppendLine("This package contains third-party software components governed by the license(s) indicated below.");
            sb.AppendLine();
            AppendNotice(sb, "Game Framework", "MIT",
                "Copyright (c) 2013-2021 Jiang Yin (EllanJiang)", "https://github.com/EllanJiang/GameFramework",
                "Framework/Core (modified). Full text: LICENSE.md");
            AppendNotice(sb, "Unity Game Framework", "MIT",
                "Copyright (c) 2013-2021 Jiang Yin (EllanJiang)", "https://github.com/EllanJiang/UnityGameFramework",
                "Framework/UnityGameFramework (modified). Full text: LICENSE.md");
            AppendNotice(sb, "Luckiest Guy (font)", "Apache License 2.0",
                "Copyright (c) Astigmatic", "https://fonts.google.com/specimen/Luckiest+Guy",
                "Example/Fonts. Full text: Example/Fonts/LICENSE.txt");
            AppendNotice(sb, "Zombie Toys (art, audio, and the Countdown/FlashFade UI scripts)", "Unity Asset Store EULA",
                "Copyright (c) Unity Technologies", "https://unity.com/legal/as-terms",
                "Example/Art, Example/Audio, Example/Sample. Included for demonstration purposes only.");

            string target = Path.Combine(output, BundleRoot, "Third-Party Notices.txt");
            Directory.CreateDirectory(Path.GetDirectoryName(target));
            File.WriteAllText(target, sb.ToString(), new UTF8Encoding(false));
        }

        private static void AppendNotice(StringBuilder sb, string name, string license, string copyright, string url, string usedIn)
        {
            sb.AppendLine("---");
            sb.AppendLine();
            sb.AppendLine("Component Name: " + name);
            sb.AppendLine("License Type: " + license);
            sb.AppendLine(copyright);
            sb.AppendLine(url);
            sb.AppendLine("Used in: " + usedIn);
            sb.AppendLine();
        }

        private static void CopyProjectSettings(string projectRoot, string output)
        {
            string source = Path.Combine(projectRoot, "ProjectSettings");
            string target = Path.Combine(output, "ProjectSettings");
            Directory.CreateDirectory(target);

            foreach (string file in Directory.GetFiles(source))
            {
                File.Copy(file, Path.Combine(target, Path.GetFileName(file)), true);
            }

            RemapBuildScenePaths(Path.Combine(target, "EditorBuildSettings.asset"));
        }

        /// <summary>
        /// 빌드 씬 목록은 경로와 GUID 를 함께 들고 있다. 묶음에서는 씬이 다른 폴더로 가므로
        /// 경로 쪽도 맞춰 둔다(이 파일은 방금 만든 복사본이다).
        /// </summary>
        private static void RemapBuildScenePaths(string file)
        {
            if (!File.Exists(file)) return;

            const string Key = "path: ";
            string[] lines = File.ReadAllLines(file);

            for (int i = 0; i < lines.Length; i++)
            {
                int at = lines[i].IndexOf(Key + "Assets/", System.StringComparison.Ordinal);
                if (at < 0) continue;

                string path   = lines[i].Substring(at + Key.Length).Trim();
                string mapped = MapPath(path);
                if (mapped == null) continue;

                lines[i] = lines[i].Substring(0, at + Key.Length) + mapped;
            }

            File.WriteAllLines(file, lines);
        }

        /// <summary>
        /// 묶음의 Packages/manifest.json. 예제가 실제로 참조하는 Unity 패키지와
        /// 프레임워크가 요구하는 패키지만 남긴다 — 개발용 패키지(IDE, 테스트 등)는 뺀다.
        /// </summary>
        private static void WriteManifest(string projectRoot, string output, List<string> exampleAssets)
        {
            var wanted = new SortedSet<string>(System.StringComparer.Ordinal)
            {
                // 프레임워크가 요구하는 것(package.json 의 dependencies 와 같아야 한다).
                "com.unity.addressables",
                "com.unity.sharp-zip-lib",
                // 예제 코드가 using 으로 쓰는 것. 에셋 참조가 아니라 폐포에 안 잡힐 수 있다.
                "com.unity.inputsystem",
                "com.unity.ugui",
            };

            // 예제 에셋이 참조하는 Unity 패키지(URP 셰이더 등).
            foreach (string path in AssetDatabase.GetDependencies(exampleAssets.ToArray(), true))
            {
                if (!path.StartsWith("Packages/com.unity.")) continue;

                wanted.Add(path.Split('/')[1]);
            }

            var sb = new StringBuilder();
            sb.AppendLine("{");
            sb.AppendLine("  \"dependencies\": {");

            var lines = new List<string>();
            foreach (UnityEditor.PackageManager.PackageInfo info in UnityEditor.PackageManager.PackageInfo.GetAllRegisteredPackages())
            {
                if (!info.isDirectDependency) continue;

                bool isModule = info.name.StartsWith("com.unity.modules.");
                if (!isModule && !wanted.Contains(info.name)) continue;

                lines.Add(string.Format("    \"{0}\": \"{1}\"", info.name, info.version));
            }

            lines.Sort(System.StringComparer.Ordinal);
            sb.AppendLine(string.Join(",\n", lines.ToArray()));
            sb.AppendLine("  }");
            sb.AppendLine("}");

            string target = Path.Combine(output, "Packages", "manifest.json");
            Directory.CreateDirectory(Path.GetDirectoryName(target));
            File.WriteAllText(target, sb.ToString(), new UTF8Encoding(false));
        }
    }
}
