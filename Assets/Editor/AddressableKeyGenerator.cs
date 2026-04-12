using Backend.Util.Addressable;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEngine;

namespace Backend.Editor
{
    public static class AddressableKeyGenerator
    {
        private const string FileName = "AddressableKeys.cs";
        private const string ClassName = "AddressableKeys";

        [MenuItem("Tools/Addressables/Force Generate Keys")]
        public static void Generate()
        {
            string[] guids = AssetDatabase.FindAssets("t:AddressableGenSettings");
            if (guids.Length == 0)
            {
                Debug.LogError("AddressableGenSettings 에셋을 생성해주세요.");
                return;
            }

            var genSettings = AssetDatabase.LoadAssetAtPath<AddressableGenSettings>(AssetDatabase.GUIDToAssetPath(guids[0]));
            var addrSettings = AddressableAssetSettingsDefaultObject.Settings;

            if (addrSettings == null) return;

            string folderPath = genSettings.GetFolderPath();
            string nameSpace = GetNamespaceFromPath(folderPath);
            bool useNamespace = !string.IsNullOrEmpty(nameSpace);
            string tap = useNamespace ? "    " : "";

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("// 자동 생성된 코드입니다. 수동 수정을 피하세요.");

            if (useNamespace)
            {
                sb.AppendLine($"namespace {nameSpace}");
                sb.AppendLine("{");
            }

            sb.AppendLine($"{tap}public static class {ClassName}");
            sb.AppendLine($"{tap}{{");

            foreach (var group in addrSettings.groups)
            {
                if (group == null || group.ReadOnly) continue;

                sb.AppendLine($"{tap}    public static class {FormatName(group.Name)}");
                sb.AppendLine($"{tap}    {{");

                // 생성된 키 추적 (이름이 같은 파일이 다른 하위 폴더에 있을 때 변수명 중복 방지)
                HashSet<string> generatedKeys = new HashSet<string>();

                foreach (var entry in group.entries)
                {
                    // 💡 엔트리가 '폴더'인지 검사
                    if (AssetDatabase.IsValidFolder(entry.AssetPath))
                    {
                        // 폴더 내부의 모든 파일 탐색
                        string[] assetGuids = AssetDatabase.FindAssets("", new[] { entry.AssetPath });
                        foreach (var guid in assetGuids)
                        {
                            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                            if (AssetDatabase.IsValidFolder(assetPath)) continue; // 하위 폴더 자체는 스킵

                            // 파일 이름을 추출해 변수명으로 사용 (예: "Hero.prefab" -> "Hero")
                            string fileName = Path.GetFileNameWithoutExtension(assetPath);
                            string keyName = FormatName(fileName);

                            // 중복 이름 방지 로직 (같은 이름이면 Hero_1, Hero_2 식으로 넘버링)
                            int suffix = 1;
                            string finalKeyName = keyName;
                            while (generatedKeys.Contains(finalKeyName))
                            {
                                finalKeyName = $"{keyName}_{suffix}";
                                suffix++;
                            }
                            generatedKeys.Add(finalKeyName);

                            // 폴더로 넣었을 때 내부 에셋의 어드레스는 '전체 경로'임
                            sb.AppendLine($"{tap}        public const string {finalKeyName} = \"{assetPath}\";");
                        }
                    }
                    else
                    {
                        // 💡 엔트리가 '개별 파일'인 경우 기존 로직
                        string keyName = FormatName(entry.address);
                        if (!generatedKeys.Contains(keyName))
                        {
                            generatedKeys.Add(keyName);
                            sb.AppendLine($"{tap}        public const string {keyName} = \"{entry.address}\";");
                        }
                    }
                }
                sb.AppendLine($"{tap}    }}");
            }
            sb.AppendLine($"{tap}}}");

            if (useNamespace) sb.AppendLine("}");

            string relativePath = Path.Combine(folderPath, FileName).Replace('\\', '/');
            string fullPath = Path.Combine(Directory.GetCurrentDirectory(), relativePath);

            File.WriteAllText(fullPath, sb.ToString(), Encoding.UTF8);

            AssetDatabase.ImportAsset(relativePath);
            AssetDatabase.Refresh();

            Debug.Log($"[Addressables] {relativePath} 생성 완료!");
        }

        private static string GetNamespaceFromPath(string path)
        {
            if (string.IsNullOrEmpty(path) || path == "Assets") return string.Empty;
            if (path.StartsWith("Assets/")) path = path.Substring(7);

            string[] parts = path.Split(new char[] { '/', '\\' }, System.StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < parts.Length; i++) parts[i] = FormatName(parts[i]);
            return string.Join(".", parts);
        }

        private static string FormatName(string name)
        {
            if (string.IsNullOrEmpty(name)) return "Unknown";
            string result = name.Replace(" ", "_").Replace("-", "_").Replace(".", "_").Replace("/", "_");
            if (char.IsDigit(result[0])) result = "_" + result;
            return result;
        }
    }
}