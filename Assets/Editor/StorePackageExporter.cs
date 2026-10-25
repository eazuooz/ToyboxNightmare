using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

namespace ToyBoxNightmare.EditorTools
{
    /// <summary>
    /// 배포용 <c>.unitypackage</c> 세 개를 만든다.
    ///
    /// <list type="bullet">
    /// <item><b>코어</b> — 코어 DLL(<c>Assets/Plugins/GameFramework.dll</c>)과 라이선스 문서뿐. Unity 를 모르는 순수 C# 라이브러리다.</item>
    /// <item><b>유니티</b> — 코어 DLL + UnityGameFramework(<c>Assets/Scripts</c>, 시작 예제 <c>Starter</c> 포함)
    /// + <c>GameFramework.prefab</c>. 아무 프로젝트에 넣고 시작 씬을 Play 하면 돈다.</item>
    /// <item><b>예제</b> — 유니티 패키지에 예제 게임이 실제로 쓰는 에셋과 프로젝트 설정을 더한 것. 빈 프로젝트용.</item>
    /// </list>
    ///
    /// <b>경로를 바꾸지 않는다.</b> 패키지를 열면 이 프로젝트와 똑같은 위치(<c>Assets/…</c>)에 풀린다.
    /// <c>.meta</c> 를 함께 담으므로 GUID 가 유지되어 프리팹·씬·Addressables 참조가 끊기지 않는다.
    ///
    /// 예제에 무엇이 들어갈지는 손으로 고르지 않는다. 빌드 씬 + Addressables 엔트리 + 게임 코드를
    /// 뿌리로 의존성 폐포를 구한다 — 게임이 실제로 쓰는 것만 들어가고, 프로젝트에 같이 있는
    /// 유료 에셋은 참조되지 않는 한 따라오지 않는다.
    /// </summary>
    public static class StorePackageExporter
    {
        /// <summary>프로젝트 루트 기준 출력 폴더. .gitignore 의 /Builds/ 에 걸린다.</summary>
        private const string OutputFolder = "Builds/StorePackages";

        private const string CorePackageName    = "GameFramework-Core";
        private const string UnityPackageName   = "GameFramework-Unity";
        private const string ExamplePackageName = "GameFramework-Example";

        /// <summary>UnityGameFramework(런타임 + Editor). 폴더 전체가 프레임워크다.</summary>
        private const string FrameworkFolder = "Assets/Scripts";

        /// <summary>코어 DLL. External/GameFramework 의 빌드 후 이벤트가 여기에 복사한다.</summary>
        private const string CoreAssembly = "Assets/Plugins/GameFramework.dll";

        /// <summary>코어 패키지의 문서 자리. 코어 패키지에는 Assets/Scripts 가 없어서 DLL 옆에 이름을 붙여 둔다.</summary>
        private const string CoreDocsFolder = "Assets/Plugins";
        private const string CoreDocsPrefix = "GameFramework-";

        /// <summary>프레임워크 컴포넌트가 모두 붙은 프리팹. 씬에 이것 하나를 놓고 시작한다.</summary>
        private const string FrameworkPrefab = "Assets/Prefabs/GameFramework.prefab";

        /// <summary>게임 코드. 폴더 전체가 예제의 뿌리다.</summary>
        private const string GameCodeFolder = "Assets/GameMain";

        private const string AddressablesDataFolder = "Assets/AddressableAssetsData";

        /// <summary>프로젝트 루트의 고지 문서 폴더. Assets 밖에 있어서 패키지에는 따로 넣어 준다.</summary>
        private const string DocsFolder = "Docs/GameFramework";

        private const string NoticesDoc = "Third Party Notices.md";

        /// <summary>패키지에 넣는 문서. LICENSE 와 고지는 MIT 재배포 조건이라 빠지면 안 된다.</summary>
        private static readonly string[] sPackageDocs =
        {
            "LICENSE.md",
            NoticesDoc,
            "README.md",
        };

        /// <summary>
        /// 의존성 폐포에 잡히지 않지만 예제에 필요한 폴더.
        /// 에셋 참조가 아니라 ProjectSettings 나 코드가 가리키는 것들이다.
        /// </summary>
        private static readonly string[] sExtraFolders =
        {
            "Assets/Settings",      // URP 파이프라인 에셋 — GraphicsSettings/QualitySettings 가 참조한다
        };

        /// <summary>
        /// 재배포할 수 없는 에셋. 폐포에 하나라도 섞이면 패키지를 만들지 않고 멈춘다.
        /// 누군가 예제에서 이 에셋을 참조하기 시작하면 여기서 걸린다.
        /// </summary>
        private static readonly string[] sForbiddenFolders =
        {
            "Assets/Art/Effect/Epic Toon FX",
            "Assets/Art/Effect/JMO Assets",
            "Assets/Art/Effect/118 sprite effects bundle",
        };

        /// <summary>프레임워크가 요구하는 Unity 패키지.</summary>
        private static readonly string[] sFrameworkDependencies =
        {
            "com.unity.addressables",
            "com.unity.sharp-zip-lib",
        };

        /// <summary>예제 코드가 using 으로 쓰는 Unity 패키지. 에셋 참조가 아니라 폐포에 안 잡힐 수 있다.</summary>
        private static readonly string[] sExampleDependencies =
        {
            "com.unity.inputsystem",
            "com.unity.ugui",
        };

        /// <summary>임포트하는 쪽 Package Manager 에 의존성을 알리는 항목. Unity 가 정한 이름이다.</summary>
        private const string ManifestEntryName = "packagemanagermanifest";
        private const string ManifestPathname  = "Packages/manifest.json";

        private const string SettingsFolder = "ProjectSettings";

        /// <summary>받는 쪽 에디터 버전을 바꿔 버리므로 넣지 않는다.</summary>
        private const string ProjectVersionFile = "ProjectVersion.txt";

        private const string UnityPackagePrefix = "Packages/com.unity.";
        private const string ModulePrefix       = "com.unity.modules.";

        /// <summary>Assets 밖에서 가져와 넣는 파일에 붙일 GUID 의 씨앗. 바꾸면 그 GUID 가 바뀐다.</summary>
        private const string GuidSeed = "yamyamcoding.gameframework:";

        private const int TarBlockSize = 512;

        /// <summary>항목 시각을 고정해 같은 입력이면 같은 파일이 나오게 한다(2025-01-01 UTC).</summary>
        private const long TarTimestamp = 1735689600L;

        [MenuItem("Tools/Export Packages")]
        public static void Export()
        {
            string projectRoot = Directory.GetParent(Application.dataPath).FullName;

            if (!File.Exists(Path.Combine(projectRoot, CoreAssembly)))
            {
                Debug.LogError("StorePackage: 코어 DLL 이 없어 중단한다. External/GameFramework 를 먼저 빌드할 것 → " + CoreAssembly);
                return;
            }

            List<string> frameworkAssets = CollectFrameworkAssets();
            List<string> exampleAssets   = CollectExampleAssets(frameworkAssets);

            string forbidden = FindForbidden(exampleAssets);
            if (forbidden != null)
            {
                Debug.LogError("StorePackage: 재배포할 수 없는 에셋이 예제에 참조돼 있어 중단한다 → " + forbidden);
                return;
            }

            string outputDir = Path.Combine(projectRoot, OutputFolder);
            Directory.CreateDirectory(outputDir);

            string version = PlayerSettings.bundleVersion;

            // 코어는 Unity 패키지 의존성이 없어 manifest 항목을 넣지 않는다.
            string coreFile = Path.Combine(outputDir, CorePackageName + "-" + version + ".unitypackage");
            using (var tar = new TarWriter(coreFile))
            {
                AddAssets(tar, projectRoot, new List<string> { CoreAssembly });
                AddPackageDocs(tar, projectRoot, CoreDocsFolder, CoreDocsPrefix, false);
            }

            string frameworkFile = Path.Combine(outputDir, UnityPackageName + "-" + version + ".unitypackage");
            using (var tar = new TarWriter(frameworkFile))
            {
                AddAssets(tar, projectRoot, frameworkAssets);
                AddPackageDocs(tar, projectRoot, FrameworkFolder, string.Empty, false);
                AddManifest(tar, BuildManifest(sFrameworkDependencies, null, false));
            }

            string exampleFile = Path.Combine(outputDir, ExamplePackageName + "-" + version + ".unitypackage");
            using (var tar = new TarWriter(exampleFile))
            {
                AddAssets(tar, projectRoot, exampleAssets);
                AddPackageDocs(tar, projectRoot, FrameworkFolder, string.Empty, true);
                AddProjectSettings(tar, projectRoot);
                AddManifest(tar, BuildManifest(sFrameworkDependencies, exampleAssets, true));
            }

            Debug.Log(string.Format("StorePackage: 완료 — 코어 → {0} / 유니티 {1}개 → {2} / 예제 {3}개 → {4}",
                coreFile, frameworkAssets.Count, frameworkFile, exampleAssets.Count, exampleFile));
        }

        // ─── 무엇을 넣을지 ───

        private static List<string> CollectFrameworkAssets()
        {
            var result = new SortedSet<string>(StringComparer.Ordinal);
            result.UnionWith(CollectFolderAssets(FrameworkFolder));
            result.Add(CoreAssembly);
            result.Add(FrameworkPrefab);

            return new List<string>(result);
        }

        /// <summary>프레임워크 + 예제가 실제로 쓰는 에셋 전부(프로젝트 상대 경로).</summary>
        private static List<string> CollectExampleAssets(List<string> frameworkAssets)
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

            var result = new SortedSet<string>(StringComparer.Ordinal);
            result.UnionWith(frameworkAssets);

            foreach (string path in AssetDatabase.GetDependencies(roots.ToArray(), true))
            {
                if (IsProjectAssetFile(path)) result.Add(path);
            }

            foreach (string path in roots)
            {
                if (IsProjectAssetFile(path)) result.Add(path);
            }

            foreach (string folder in sExtraFolders)
            {
                result.UnionWith(CollectFolderAssets(folder));
            }

            result.UnionWith(CollectFolderAssets(AddressablesDataFolder));

            return new List<string>(result);
        }

        /// <summary>Assets 아래의 실제 파일인가. Unity 패키지 쪽 의존성은 manifest 로 해결한다.</summary>
        private static bool IsProjectAssetFile(string path)
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

        /// <summary>
        /// 임포트하는 쪽에 설치돼야 하는 Unity 패키지 목록(manifest.json 형식).
        /// 버전은 지금 이 프로젝트에 설치된 것을 적는다. 내장 모듈은 예제 쪽에만 넣는다 —
        /// 프레임워크만 받는 프로젝트에는 이미 켜져 있고, 넣으면 설치 목록만 길어진다.
        /// </summary>
        private static string BuildManifest(string[] required, List<string> exampleAssets, bool includeExample)
        {
            var wanted = new SortedSet<string>(StringComparer.Ordinal);
            wanted.UnionWith(required);

            if (includeExample)
            {
                wanted.UnionWith(sExampleDependencies);

                // 예제 에셋이 참조하는 Unity 패키지(URP 셰이더 등).
                foreach (string path in AssetDatabase.GetDependencies(exampleAssets.ToArray(), true))
                {
                    if (!path.StartsWith(UnityPackagePrefix)) continue;

                    wanted.Add(path.Split('/')[1]);
                }
            }

            var lines = new List<string>();
            foreach (UnityEditor.PackageManager.PackageInfo info in UnityEditor.PackageManager.PackageInfo.GetAllRegisteredPackages())
            {
                if (!info.isDirectDependency) continue;

                bool isModule = info.name.StartsWith(ModulePrefix);
                if (isModule && !includeExample) continue;
                if (!isModule && !wanted.Contains(info.name)) continue;

                lines.Add(string.Format("    \"{0}\": \"{1}\"", info.name, info.version));
            }

            lines.Sort(StringComparer.Ordinal);

            var sb = new StringBuilder();
            sb.Append("{\n  \"dependencies\": {\n");
            sb.Append(string.Join(",\n", lines.ToArray()));
            sb.Append("\n  }\n}\n");

            return sb.ToString();
        }

        /// <summary>예제가 들고 오는 서드파티. 프레임워크만 내보낼 때는 적지 않는다 — 들어 있지 않기 때문이다.</summary>
        private static string BuildExampleNotices()
        {
            var sb = new StringBuilder();
            sb.Append("\n---\n\nComponent Name: Luckiest Guy (font)\n\nLicense Type: Apache License 2.0\n\n");
            sb.Append("Copyright (c) Astigmatic\n\nhttps://fonts.google.com/specimen/Luckiest+Guy\n\n");
            sb.Append("Used in: `Assets/Fonts`. Full text: `Assets/Fonts/LICENSE.txt`\n");
            sb.Append("\n---\n\nComponent Name: Zombie Toys (art and audio)\n\n");
            sb.Append("License Type: Unity Asset Store EULA\n\nCopyright (c) Unity Technologies\n\nhttps://unity.com/legal/as-terms\n\n");
            sb.Append("Used in: `Assets/Art`, `Assets/Audio`. Included for demonstration purposes only.\n");

            return sb.ToString();
        }

        // ─── 패키지 항목 쓰기 ───

        /// <summary>에셋들과, 그 에셋이 놓인 폴더들(폴더 GUID 를 유지하려면 폴더의 .meta 도 가야 한다).</summary>
        private static void AddAssets(TarWriter tar, string projectRoot, List<string> assets)
        {
            var folders = new SortedSet<string>(StringComparer.Ordinal);
            foreach (string asset in assets)
            {
                string folder = GetParentFolder(asset);
                while (folder != "Assets" && !string.IsNullOrEmpty(folder))
                {
                    folders.Add(folder);
                    folder = GetParentFolder(folder);
                }
            }

            foreach (string folder in folders)
            {
                AddAsset(tar, projectRoot, folder, true);
            }

            foreach (string asset in assets)
            {
                AddAsset(tar, projectRoot, asset, false);
            }
        }

        private static string GetParentFolder(string path)
        {
            int slash = path.LastIndexOf('/');

            return slash < 0 ? string.Empty : path.Substring(0, slash);
        }

        private static void AddAsset(TarWriter tar, string projectRoot, string asset, bool isFolder)
        {
            string fullPath = Path.Combine(projectRoot, asset);
            string metaPath = fullPath + ".meta";

            string guid = ReadGuid(metaPath);
            if (guid == null) throw new InvalidOperationException("StorePackage: .meta 에서 GUID 를 읽지 못했다 → " + asset);

            tar.AddDirectory(guid);
            if (!isFolder) tar.AddFile(guid + "/asset", File.ReadAllBytes(fullPath));
            tar.AddFile(guid + "/asset.meta", File.ReadAllBytes(metaPath));
            tar.AddFile(guid + "/pathname", Encoding.UTF8.GetBytes(asset));
        }

        private static string ReadGuid(string metaPath)
        {
            const string Key = "guid: ";

            if (!File.Exists(metaPath)) return null;

            foreach (string line in File.ReadAllLines(metaPath))
            {
                if (line.StartsWith(Key)) return line.Substring(Key.Length).Trim();
            }

            return null;
        }

        /// <summary>
        /// 문서는 Assets 밖(<see cref="DocsFolder"/>)에 있어 .meta 가 없다. 패키지마다 정한 폴더에
        /// 놓이도록 넣고(이름 앞에 prefix 를 붙인다), GUID 는 경로에서 계산해 다시 내보내도 같게 한다.
        /// </summary>
        private static void AddPackageDocs(TarWriter tar, string projectRoot, string destinationFolder, string prefix, bool includeExample)
        {
            foreach (string doc in sPackageDocs)
            {
                string source = Path.Combine(projectRoot, DocsFolder, doc);
                if (!File.Exists(source)) throw new InvalidOperationException("StorePackage: 문서가 없다 → " + DocsFolder + "/" + doc);

                string text = File.ReadAllText(source);
                if (includeExample && doc == NoticesDoc) text += BuildExampleNotices();

                string pathname = destinationFolder + "/" + prefix + doc;
                string guid     = Hash128.Compute(GuidSeed + pathname).ToString();
                string meta     = "fileFormatVersion: 2\nguid: " + guid + "\n";

                tar.AddDirectory(guid);
                tar.AddFile(guid + "/asset", new UTF8Encoding(false).GetBytes(text));
                tar.AddFile(guid + "/asset.meta", Encoding.UTF8.GetBytes(meta));
                tar.AddFile(guid + "/pathname", Encoding.UTF8.GetBytes(pathname));
            }
        }

        /// <summary>
        /// 프로젝트 설정. 레이어 이름, 빌드 씬, 렌더 파이프라인, 로그 심볼이 여기 있어서
        /// 이것 없이는 예제가 돌지 않는다. 설정 파일은 .meta 가 없고 Unity 가 정한 고정 GUID 를 쓴다 —
        /// 그 GUID 를 에디터에 물어 얻고, 에디터가 모르는 파일(패키지가 직접 쓰는 설정)은 넣지 않는다.
        /// </summary>
        private static void AddProjectSettings(TarWriter tar, string projectRoot)
        {
            string[] files = Directory.GetFiles(Path.Combine(projectRoot, SettingsFolder));
            Array.Sort(files, StringComparer.Ordinal);

            foreach (string file in files)
            {
                string name = Path.GetFileName(file);
                if (name == ProjectVersionFile) continue;

                string pathname = SettingsFolder + "/" + name;
                string guid     = AssetDatabase.AssetPathToGUID(pathname);
                if (string.IsNullOrEmpty(guid)) continue;

                tar.AddDirectory(guid);
                tar.AddFile(guid + "/asset", File.ReadAllBytes(file));
                tar.AddFile(guid + "/pathname", Encoding.UTF8.GetBytes(pathname));
            }
        }

        private static void AddManifest(TarWriter tar, string json)
        {
            tar.AddDirectory(ManifestEntryName);
            tar.AddFile(ManifestEntryName + "/asset", new UTF8Encoding(false).GetBytes(json));
            tar.AddFile(ManifestEntryName + "/pathname", Encoding.UTF8.GetBytes(ManifestPathname));
        }

        // ─── tar.gz ───

        /// <summary>
        /// .unitypackage 는 에셋마다 "GUID/asset", "GUID/asset.meta", "GUID/pathname" 을 담은 tar.gz 다.
        /// ustar 형식으로 쓴다. 항목 이름이 전부 짧아서 긴 이름 확장(100바이트 초과)은 다루지 않는다.
        /// </summary>
        private sealed class TarWriter : IDisposable
        {
            private const int NameFieldLength = 100;
            private const int ChecksumOffset  = 148;
            private const int ChecksumLength  = 8;

            private const byte TypeFile      = (byte)'0';
            private const byte TypeDirectory = (byte)'5';

            private readonly FileStream mFile;
            private readonly GZipStream mGzip;

            public TarWriter(string path)
            {
                mFile = new FileStream(path, FileMode.Create, FileAccess.Write);
                mGzip = new GZipStream(mFile, System.IO.Compression.CompressionLevel.Optimal);
            }

            public void AddDirectory(string name)
            {
                WriteHeader(name + "/", 0, TypeDirectory, "0000755");
            }

            public void AddFile(string name, byte[] data)
            {
                WriteHeader(name, data.Length, TypeFile, "0000644");
                mGzip.Write(data, 0, data.Length);

                int remainder = data.Length % TarBlockSize;
                if (remainder == 0) return;

                byte[] padding = new byte[TarBlockSize - remainder];
                mGzip.Write(padding, 0, padding.Length);
            }

            public void Dispose()
            {
                // tar 의 끝은 빈 블록 두 개다.
                byte[] end = new byte[TarBlockSize * 2];
                mGzip.Write(end, 0, end.Length);

                mGzip.Dispose();
                mFile.Dispose();
            }

            private void WriteHeader(string name, long size, byte type, string mode)
            {
                byte[] nameBytes = Encoding.ASCII.GetBytes(name);
                if (nameBytes.Length > NameFieldLength) throw new InvalidOperationException("StorePackage: tar 항목 이름이 너무 길다 → " + name);

                byte[] header = new byte[TarBlockSize];
                Array.Copy(nameBytes, 0, header, 0, nameBytes.Length);

                PutText(header, 100, mode);                 // mode
                PutOctal(header, 108, 8, 0);                // uid
                PutOctal(header, 116, 8, 0);                // gid
                PutOctal(header, 124, 12, size);            // size
                PutOctal(header, 136, 12, TarTimestamp);    // mtime
                header[156] = type;
                PutText(header, 257, "ustar");              // magic(끝에 NUL)
                PutText(header, 263, "00");                 // version

                // 체크섬은 체크섬 칸을 공백으로 채운 상태의 헤더 바이트 합이다.
                for (int i = 0; i < ChecksumLength; i++)
                {
                    header[ChecksumOffset + i] = (byte)' ';
                }

                long checksum = 0;
                for (int i = 0; i < header.Length; i++)
                {
                    checksum += header[i];
                }

                PutOctal(header, ChecksumOffset, 7, checksum);

                mGzip.Write(header, 0, header.Length);
            }

            private static void PutText(byte[] header, int offset, string text)
            {
                byte[] bytes = Encoding.ASCII.GetBytes(text);
                Array.Copy(bytes, 0, header, offset, bytes.Length);
            }

            /// <summary>length 칸에 8진수를 0으로 채워 쓰고 마지막 한 칸은 NUL 로 둔다.</summary>
            private static void PutOctal(byte[] header, int offset, int length, long value)
            {
                string text = Convert.ToString(value, 8).PadLeft(length - 1, '0');
                PutText(header, offset, text);
                header[offset + length - 1] = 0;
            }
        }
    }
}
