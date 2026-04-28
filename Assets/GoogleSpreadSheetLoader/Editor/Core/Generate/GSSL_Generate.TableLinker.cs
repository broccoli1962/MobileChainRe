using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using GoogleSpreadSheetLoader.OneButton;
using TableData;
using UnityEditor;
using UnityEngine;

namespace GoogleSpreadSheetLoader.Generate
{
    public partial class GSSL_Generate
    {
        private static string tableLinkerScriptPath = "Assets/GoogleSpreadSheetLoader/Generated/Script/";
        private static string tableLinkerDataPath = "Assets/Resources/";
        
        public static void GenerateTableLinkerScript(List<SheetData> sheetDataList)
        {
            var declation = "";

            foreach (var sheetData in sheetDataList)
            {
                var className = sheetData.title + "Table";

                declation += $"\t\t public {className} {className};\n";
            }

            var contents =
                "using System.Collections.Generic;\n" +
                "using UnityEngine;\n" +
                "using UnityEngine.Serialization;\n\n" +
                "namespace TableData\n" +
                "{\n" +
                "    [CreateAssetMenu(fileName = \"TableLinker\", menuName = \"Tables/TableLinker\")]\n" +
                "    public partial class TableLinker : ScriptableObject\n" +
                "    {\n"
                + declation
                + "\n    }\n}";
            var path = tableLinkerScriptPath + "TableLinker.cs";

            File.WriteAllText(path, contents);
        }
        
        public static void GenerateTableLinkerData()
        {
            if (!Directory.Exists(tableLinkerDataPath))
            {
                Directory.CreateDirectory(tableLinkerDataPath);
            }
            
            var tableLinkerAssetPath = tableLinkerDataPath + "TableLinker.asset";
            
            var tableLinkerAsset = ScriptableObject.CreateInstance("TableLinker");
            tableLinkerAsset.hideFlags = HideFlags.None;
            
            AssetDatabase.CreateAsset(tableLinkerAsset, tableLinkerAssetPath);
    
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (AssignFirstMatchingAssets(tableLinkerAsset))
            {
                GSSL_OneButton.TableLinkerFlag = false;
                
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
        }

        public static void SearchAndAssign()
        {
            var guids = AssetDatabase.FindAssets($"t:TableLinker");
            
            if (guids.Length <= 0) return;
            
            var path = AssetDatabase.GUIDToAssetPath(guids[0]);
            var tableLinker = AssetDatabase.LoadAssetAtPath(path, typeof(UnityEngine.Object));
            
            AssignFirstMatchingAssets(tableLinker);
        }
        
        private static bool AssignFirstMatchingAssets(UnityEngine.Object target)
        {
            if (target == null)
            {
                Debug.LogWarning("타겟 오브젝트가 null입니다.");
                return false;
            }

            Type targetType = target.GetType();
            BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

            SerializedObject serializedObject = new SerializedObject(target);
            SerializedProperty property = serializedObject.GetIterator();

            property.Next(true);
            property.Next(true);

            var anySet = false;
            while (property.NextVisible(false))
            {
                if (property.propertyType != SerializedPropertyType.ObjectReference) continue;
                
                Type propertyType = GetFieldType(targetType, property.name);

                if (propertyType == null || !typeof(UnityEngine.Object).IsAssignableFrom(propertyType)) continue;
                
                string[] guids = AssetDatabase.FindAssets($"t:{propertyType.Name}");

                if (guids.Length <= 0) continue;
                
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                UnityEngine.Object asset = AssetDatabase.LoadAssetAtPath(path, propertyType);
                property.objectReferenceValue = asset;
                anySet = true;
            }

            serializedObject.ApplyModifiedProperties();

            return anySet;
        }

        private static Type GetFieldType(Type targetType, string propertyName)
        {
            FieldInfo field = targetType.GetField(propertyName, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
            if (field != null)
            {
                return field.FieldType;
            }

            // SerializedProperty는 필드명이 그대로 오지 않으므로 대체 시도
            foreach (var f in targetType.GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance))
            {
                if (ObjectNames.NicifyVariableName(f.Name) == propertyName)
                {
                    return f.FieldType;
                }
            }

            return null;
        }
    }
}