using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GoogleSpreadSheetLoader.Download;
using GoogleSpreadSheetLoader.Generate;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;
using static GoogleSpreadSheetLoader.SheetData;

namespace GoogleSpreadSheetLoader.OneButton
{
    public class GSSL_OneButton
    {

        public static bool GenerateDataFlag => EditorPrefs.HasKey("GenerateData");
        public static string GenerateDataString
        {
            get => EditorPrefs.HasKey("GenerateData") ? EditorPrefs.GetString("GenerateData") : string.Empty;
            set
            {
                if(string.IsNullOrEmpty(value))
                    EditorPrefs.DeleteKey("GenerateData");
                else
                    EditorPrefs.SetString("GenerateData", value);
            }
        }

        public static bool TableLinkerFlag
        {
            get => EditorPrefs.HasKey("TableLinkerLink");
            set
            {
                if (value)
                {
                    EditorPrefs.SetString("TableLinkerLink", true.ToString());
                }
                else
                {
                    EditorPrefs.DeleteKey("TableLinkerLink");
                }
            }
        }

        // ReSharper disable Unity.PerformanceAnalysis
        public static async Awaitable OneButtonProcess(List<GSSL_DownloadInfo> listDownloadInfo)
        {
            await GSSL_Download.DownloadSheet(listDownloadInfo);

            var listSheetData = GSSL_Generate.GetSheetDataList()
                .Where(x => listDownloadInfo.Any(y => y.GetSheetName() == x.title));

            var dicSheetData = new Dictionary<eTableStyle, List<SheetData>>();

            dicSheetData.TryAdd(eTableStyle.EnumType, new());
            dicSheetData.TryAdd(eTableStyle.None, new());
            dicSheetData.TryAdd(eTableStyle.Localization, new());

            foreach (var sheetData in listSheetData)
            {
                dicSheetData[sheetData.tableStyle].Add(sheetData);
            }

            foreach ((eTableStyle tableStyle, var list) in dicSheetData)
            {
                switch (tableStyle)
                {
                    case eTableStyle.None:
                        GSSL_Generate.GenerateTableScripts(list);
                        break;
                    case eTableStyle.EnumType:
                        GSSL_Generate.GenerateEnumDef(list);
                        break;
                    case eTableStyle.Localization:
                        GSSL_Generate.GenerateLocalize(list);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            dicSheetData.Remove(eTableStyle.EnumType);
            dicSheetData.Remove(eTableStyle.Localization);
            var str = JsonConvert.SerializeObject(dicSheetData);
            GenerateDataString = str;
            TableLinkerFlag = true;
            
            GSSL_Generate.GenerateTableLinkerScript(dicSheetData[eTableStyle.None]);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            CheckPrefsAndGenerateTableData();
        }

        public static async Awaitable GenerateTableLinkerAsync()
        {
            await Task.Delay(100);

            GSSL_Generate.GenerateTableLinkerData();
        }
        
        [InitializeOnLoadMethod]
        private static void CheckPrefsAndGenerateTableData()
        {
            if (!GenerateDataFlag) return;
            
            var str = GenerateDataString;

            GenerateData(str);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            if (TableLinkerFlag)
            {
                GenerateTableLinkerAsync();

                TableLinkerFlag = false;
            }
            
            GenerateDataString = string.Empty;
        }

        private static void GenerateData(string str)
        {
            var dic = JsonConvert.DeserializeObject<Dictionary<eTableStyle, List<SheetData>>>(str);

            foreach ((eTableStyle tableStyle, var list) in dic)
            {
                switch (tableStyle)
                {
                    case eTableStyle.None:
                        GSSL_Generate.GenerateTableData(list);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }
    }
}