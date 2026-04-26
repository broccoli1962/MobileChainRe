using System;
using System.Collections.Generic;
using System.IO;
using TableData;
using Unity.Plastic.Newtonsoft.Json;
using UnityEditor;
using UnityEngine;
// ReSharper disable CheckNamespace
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable InconsistentNaming
// ReSharper disable CollectionNeverQueried.Local

namespace GoogleSpreadSheetLoader.Generate
{
    public partial class GSSL_Generate
    {
        public static void GenerateTableScripts(List<SheetData> sheets)
        {
            CheckAndCreateDirectory();

            foreach (var sheet in sheets)
            {
                var tableTitle = sheet.title;
                var dataClassName = tableTitle + "Data";
                var tableClassName = tableTitle + "Table";
                var dataFilePath = dataScriptSavePath + dataClassName + ".cs";
                var tableFilePath = tableScriptSavePath + tableClassName + ".cs";

                var variableDeclarations = new List<string>();

                var sheetRows = JsonConvert.DeserializeObject<List<List<string>>>(sheet.data);

                if (sheetRows == null || sheetRows.Count < 2) continue;

                var setData = "";
                var headers = sheetRows[0];
                for (var i = 0; i < headers.Count; i++)
                {
                    if (string.IsNullOrWhiteSpace(headers[i]) || !headers[i].Contains("-"))
                        continue;

                    var splitHeader = headers[i].Split('-');
                    if (splitHeader.Length < 2) continue;

                    var varName = splitHeader[0].Trim();
                    var varType = ConvertToCSharpType(splitHeader[1].Trim());

                    variableDeclarations.Add($"    public {varType} {varName} => _{varName};\n");
                    variableDeclarations.Add($"    [SerializeField] private {varType} _{varName};\n\n");

                    if (varType == "string")
                    {
                        setData += $"\t\t_{varName} = data[{i}].ToString();\n";
                    }
                    else if (varType.Contains("Type"))
                    {
                        setData += $"\t\t_{varName} = {varType}.Parse<{varType}>(data[{i}]);\n";
                    }
                    else
                    {
                        setData += $"\t\t_{varName} = {varType}.Parse(data[{i}]);\n";
                    }
                }

                setData = "\tpublic void SetData(List<string> data)\n\t{\n" + $"{setData}" + "\t}\n";

                var dataClassTemplate = $"using System;\n"
                                           + $"using System.Collections.Generic;\n"
                                           + "using TableData;\n"
                                           + "using UnityEngine;\n"
                                           + "\n"
                                           + "[Serializable]\n"
                                           + $"public partial class {dataClassName} : {nameof(IData)}\n{{\n"
                                           + string.Join("", variableDeclarations)
                                           + string.Join("", setData)
                                           + "}\n";

                File.WriteAllText(dataFilePath, dataClassTemplate);

                setData = "\tpublic void SetData(List<List<string>> data)\n\t{\n"
                          + $"\t\tdataList = new List<{dataClassName}>();\n"
                          + $"\t\tforeach (var item in data)\n"
                          + $"\t\t{{\n\t\t\t{dataClassName} newData = new();\n"
                          + $"\t\t\tnewData.SetData(item);\n"
                          + $"\t\t\tdataList.Add(newData);\n\t\t}}"
                          + "\n\t}\n";

                var tableTemplate = $"using System.Collections.Generic;\n"
                                       + "using TableData;\n"
                                       + "using UnityEngine;\n"
                                       + "\n"
                                       + $"[CreateAssetMenu(fileName = \"{tableClassName}\", menuName = \"Tables/{tableClassName}\")]\n"
                                       + $"public class {tableClassName} : ScriptableObject, {nameof(ITable)}\n{{\n"
                                       + $"    public List<{dataClassName}> dataList = new List<{dataClassName}>();\n\n"
                                       + string.Join("", setData)
                                       + "}\n";

                File.WriteAllText(tableFilePath, tableTemplate);
            }
        }

        public static void GenerateTableData(List<SheetData> listSheet)
        {
            CheckAndCreateDirectory();

            foreach (var sheet in listSheet)
            {
                var tableAssetPath = dataSavePath + sheet.title + "Table.asset";
                var tableClassName = sheet.title + "Table";

                var sheetRows = JsonConvert.DeserializeObject<List<List<string>>>(sheet.data);

                if (sheetRows == null || sheetRows.Count < 2) return;

                var tableAsset = ScriptableObject.CreateInstance(Type.GetType(tableClassName));
                tableAsset.hideFlags = HideFlags.None;
                if (tableAsset == null)
                {
                    Debug.LogError($"Failed to create instance of {tableClassName}");
                    return;
                }

                sheetRows.RemoveAt(0);

                ((ITable)tableAsset).SetData(sheetRows);

                AssetDatabase.CreateAsset(tableAsset, tableAssetPath);
            }
        }

        private static string ConvertToCSharpType(string type)
        {
            return type switch
            {
                "int" => "int",
                "float" => "float",
                "bool" => "bool",
                "long" => "long",
                "double" => "double",
                "string" => "string",
                // _ when type.Contains(".") => type, // 네임스페이스가 포함된 사용자 정의 타입 처리
                // _ => "string"
                _ => type,
            };
        }
    }
}