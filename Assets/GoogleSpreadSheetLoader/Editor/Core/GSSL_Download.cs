using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using GoogleSpreadSheetLoader.Setting;
using Newtonsoft.Json;
using Unity.Plastic.Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;

// ReSharper disable CheckNamespace
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable InconsistentNaming
// ReSharper disable PossibleNullReferenceException
#pragma warning disable CS0414 // 필드가 대입되었으나 값이 사용되지 않습니다

namespace GoogleSpreadSheetLoader.Download
{
    internal class GSSL_Download
    {
        public enum eDownloadState
        {
            None,
            Downloading,
            Complete,
        }

        internal static readonly string downloadSheetUrl =
            "https://sheets.googleapis.com/v4/spreadsheets/{0}/values/{1}?key={2}";

        private static readonly string downloadSpreadSheetUrl =
            "https://sheets.googleapis.com/v4/spreadsheets/{0}?key={1}";

        private static string _spreadSheetOpenUrl = "https://docs.google.com/spreadsheets/d/{0}/edit?key={1}";
        
        public static readonly string sheetDataAssetPath = "Assets/GoogleSpreadSheetLoader/Generated/SerializeObject/Sheet";
        
        public static List<GSSL_DownloadInfo> GetDownloadInfoList(Dictionary<string, Dictionary<string, bool>> sheetCheck)
        {
            var listDownloadInfo = new List<GSSL_DownloadInfo>();

            foreach (var (spreadSheetId, dicSheet) in sheetCheck)
            {
                listDownloadInfo.AddRange(from pair in dicSheet where pair.Value select new GSSL_DownloadInfo(spreadSheetId, pair.Key));
            }

            return listDownloadInfo;
        }
        
        #region 개별

        public static async Awaitable DownloadSpreadSheet(
            Dictionary<int, bool> dicCheck,
            Dictionary<string, List<string>> dicSheetName)
        {
            // 다운로드 대상 정리
            var listDownloadTarget = GSSL_Setting.SettingData.listSpreadSheetInfo.Where((t, i) => dicCheck[i]).ToList();

            DownloadView.spreadSheetDownloadState = eDownloadState.Downloading;
            EditorWindow.focusedWindow.Repaint();

            // 다운로드
            var listInfoOperPair = new List<(SpreadSheetInfo info, UnityWebRequestAsyncOperation oper)>();
            try
            {
                for (var index = 0; index < listDownloadTarget.Count; index++)
                {
                    SpreadSheetInfo info = listDownloadTarget[index];
                    var url = string.Format(downloadSpreadSheetUrl, info.spreadSheetId,
                        GSSL_Setting.SettingData.apiKey);

                    UnityWebRequest webRequest = UnityWebRequest.Get(url);

                    UnityWebRequestAsyncOperation asyncOperator = webRequest.SendWebRequest();
                    listInfoOperPair.Add((info, asyncOperator));
                }

                do
                {
                    DownloadView.spreadSheetDownloadMessage =
                        $"다운로드 중 ({listInfoOperPair.Count(x => x.oper.isDone)}/{listInfoOperPair.Count})";
                    EditorWindow.focusedWindow.Repaint();
                    await Task.Delay(100);
                } while (listInfoOperPair.Any(x => !x.oper.isDone));
            }
            finally
            {
                DownloadView.spreadSheetDownloadMessage ="다운로드 완료";
                DownloadView.spreadSheetDownloadState = eDownloadState.Complete;
                EditorWindow.focusedWindow.Repaint();
                await Task.Delay(1000);
                dicCheck.Clear();
                DownloadView.spreadSheetDownloadMessage ="";
                DownloadView.spreadSheetDownloadState = (eDownloadState.None);
                EditorWindow.focusedWindow.Repaint();
            }

            // 다운로드 받은 데이터 정리
            foreach ((SpreadSheetInfo info, UnityWebRequestAsyncOperation oper) pair in listInfoOperPair)
            {
                JObject jObj = JObject.Parse(pair.oper.webRequest.downloadHandler.text);
                if (jObj.TryGetValue("sheets", out var sheetsJToken))
                {
                    dicSheetName.TryAdd(pair.info.spreadSheetId, new List<string>());
                    dicSheetName[pair.info.spreadSheetId].Clear();

                    IEnumerable<JToken> enumTitle = sheetsJToken.Select(x => x["properties"]["title"]);
                    foreach (JToken title in enumTitle)
                    {
                        var titleString = title.ToString();

                        var isContains = titleString.Contains(GSSL_Setting.SettingData.sheetTargetStr);

                        switch (isContains)
                        {
                            // 포함 되어있는데 '제외'설정 되어있으면 추가되지 않음.
                            case true when GSSL_Setting.SettingData.sheetTarget == SettingData.eSheetTargetStandard.제외:
                            // 포함되어 있지 않은데 '포함'설정 되어있으면 추가되지 않음.
                            case false when GSSL_Setting.SettingData.sheetTarget == SettingData.eSheetTargetStandard.포함:
                                continue;
                        }

                        if (dicSheetName[pair.info.spreadSheetId].Contains(titleString))
                        {
                            Debug.LogError(
                                $"중복 시트 이름 : {pair.info.spreadSheetName}에서 {titleString}의 중복 이름이 존재!");

                            continue;
                        }

                        dicSheetName[pair.info.spreadSheetId].Add(titleString);
                    }
                }
            }
        }

        public static async Awaitable DownloadSheet(List<GSSL_DownloadInfo> listDownloadInfo)
        {
            // 다운로드 대상 정리
            DownloadView.sheetDownloadState = eDownloadState.Downloading;

            // 다운로드
            try
            {
                foreach (var info in listDownloadInfo)
                {
                    info.SendAndGetAsyncOperation();
                }

                var totalCount = listDownloadInfo.Count;
                do
                {
                    await Task.Delay(100);
                    DownloadView.sheetDownloadMessage = $"다운로드 중 ({listDownloadInfo.Count(x=>x.IsDone())}/{totalCount})";
                    EditorWindow.focusedWindow.Repaint();
                } while (listDownloadInfo.Any(x => !x.IsDone()));
            }
            finally
            {
                DownloadView.sheetDownloadMessage = "다운로드 완료";
                DownloadView.sheetDownloadState = eDownloadState.Complete;
                EditorWindow.focusedWindow.Repaint();
                await Task.Delay(1000);
                DownloadView.sheetDownloadMessage = "";
                DownloadView.sheetDownloadState = eDownloadState.None;
                EditorWindow.focusedWindow.Repaint();
            }

            if (!Directory.Exists(sheetDataAssetPath))
            {
                Directory.CreateDirectory(sheetDataAssetPath);
            }

            // 다운로드 받은 데이터 정리
            foreach (var info in listDownloadInfo)
            {
                SheetData sheetData = ScriptableObject.CreateInstance<SheetData>();

                sheetData.title = info.GetSheetName();

                if (sheetData.title.Contains(GSSL_Setting.SettingData.sheet_enumTypeStr))
                    sheetData.tableStyle = SheetData.eTableStyle.EnumType;
                else if (sheetData.title.Contains(GSSL_Setting.SettingData.sheet_localizationTypeStr))
                    sheetData.tableStyle = SheetData.eTableStyle.Localization;
                else
                    sheetData.tableStyle = SheetData.eTableStyle.None;

                JObject jObj = JObject.Parse(info.GetDownloadText());

                if (!jObj.TryGetValue("values", out var values))
                {
                    Debug.LogError($"변환 실패 - {info.GetSheetName()} \n{info.GetDownloadText()}");

                    continue;
                }

                sheetData.data = values.ToString();

                AssetDatabase.CreateAsset(sheetData, $"{sheetDataAssetPath}/{sheetData.title}.asset");
            }
        }

        #endregion
    }
}