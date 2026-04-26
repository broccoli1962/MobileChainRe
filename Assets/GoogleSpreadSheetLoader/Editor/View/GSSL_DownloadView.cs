using System;
using System.Collections.Generic;
using System.Linq;
using GoogleSpreadSheetLoader.Generate;
using GoogleSpreadSheetLoader.OneButton;
using GoogleSpreadSheetLoader.Setting;
using UnityEditor;
using UnityEngine;
using static GoogleSpreadSheetLoader.Download.GSSL_Download;
// ReSharper disable CheckNamespace
// ReSharper disable FieldCanBeMadeReadOnly.Local
// ReSharper disable ConvertToConstant.Local

namespace GoogleSpreadSheetLoader.Download
{
    public class DownloadView
    {
        internal static string spreadSheetDownloadMessage = "";
        internal static string sheetDownloadMessage = "";
        internal static eDownloadState spreadSheetDownloadState = eDownloadState.None;
        internal static eDownloadState sheetDownloadState = eDownloadState.None;
        
        private Dictionary<int, bool> _dicDownloadSpreadSheetCheck = new();

        private Dictionary<string, Dictionary<string, bool>> _dicDownloadSheetCheck = new();

        // Key : SpreadSheetId , Value : SheetNames
        private Dictionary<string, List<string>> _dicSheetNames = new();
        
        private Vector2 _sheetDownloadScrollPos = new(0, 0);

        public void DrawDownloadView()
        {
            DrawSpreadSheetList();

            DrawDownloadSpreadSheetBtn();

            DrawSheetInfo();

            DrawDownloadSheetBtn();
        }

        private void DrawSpreadSheetList()
        {
            EditorGUILayout.Separator();
            EditorGUILayout.LabelField("  정보를 다운로드할 스프레드 시트 선택", EditorStyles.whiteLargeLabel);
            EditorGUILayout.Separator();

            for (var i = 0; i < GSSL_Setting.SettingData.listSpreadSheetInfo.Count; i++)
            {
                var info = GSSL_Setting.SettingData.listSpreadSheetInfo[i];
                
                _dicDownloadSpreadSheetCheck.TryAdd(i, false);

                EditorGUILayout.BeginHorizontal();
                _dicDownloadSpreadSheetCheck[i] =
                    EditorGUILayout.ToggleLeft("", _dicDownloadSpreadSheetCheck[i], GUILayout.Width(20));
                EditorGUILayout.LabelField($"{i + 1}. {info.spreadSheetName}", GUILayout.Width(150));
                EditorGUILayout.LabelField(info.spreadSheetId);
                EditorGUILayout.EndHorizontal();
            }
        }

        // ReSharper disable Unity.PerformanceAnalysis
        private void DrawDownloadSpreadSheetBtn()
        {
            EditorGUILayout.Separator();

            if (_dicDownloadSpreadSheetCheck.Count == 0)
                return;

            switch (spreadSheetDownloadState)
            {
                case eDownloadState.None:
                {
                    if (_dicDownloadSpreadSheetCheck.Values.Any(x => x))
                    {
                        if (GUILayout.Button("스프레드 시트 정보 다운로드"))
                        {
                            _ = DownloadSpreadSheet(_dicDownloadSpreadSheetCheck, _dicSheetNames);
                        }
                    }
                }
                    break;
                case eDownloadState.Complete:
                case eDownloadState.Downloading:
                {
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.FlexibleSpace();

                    EditorGUILayout.LabelField(spreadSheetDownloadMessage);

                    GUILayout.FlexibleSpace();
                    EditorGUILayout.EndHorizontal();
                }
                    break;
            }
        }

        private void DrawSheetInfo()
        {
            EditorGUILayout.Separator();

            EditorGUILayout.LabelField("  다운로드 받은 시트 정보들", EditorStyles.whiteLargeLabel);
            _sheetDownloadScrollPos = EditorGUILayout.BeginScrollView(_sheetDownloadScrollPos,
                GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            for (var index = 0; index < GSSL_Setting.SettingData.listSpreadSheetInfo.Count; index++)
            {
                SpreadSheetInfo info = GSSL_Setting.SettingData.listSpreadSheetInfo[index];
                EditorGUILayout.Separator();


                if (!_dicSheetNames.ContainsKey(info.spreadSheetId) || _dicSheetNames[info.spreadSheetId].Count == 0)
                {
                    EditorGUILayout.LabelField($"{index + 1}. {info.spreadSheetName}");
                    EditorGUILayout.LabelField(" 저장된 시트 이름이 없습니다.");

                    continue;
                }

                _dicDownloadSheetCheck.TryAdd(info.spreadSheetId, new Dictionary<string, bool>());

                if (_dicDownloadSheetCheck[info.spreadSheetId].Count != _dicSheetNames[info.spreadSheetId].Count)
                {
                    _dicDownloadSheetCheck[info.spreadSheetId].Clear();
                    foreach (var sheetName in _dicSheetNames[info.spreadSheetId])
                    {
                        _dicDownloadSheetCheck[info.spreadSheetId].Add(sheetName, true);
                    }
                }

                var isAllCheck = _dicDownloadSheetCheck[info.spreadSheetId].Count > 0 &&
                                 _dicDownloadSheetCheck[info.spreadSheetId].All(x => x.Value);
                var select = EditorGUILayout.ToggleLeft($"{index + 1}. {info.spreadSheetName}", isAllCheck);

                if (isAllCheck != select)
                {
                    var keys = _dicDownloadSheetCheck[info.spreadSheetId].Keys.ToArray();
                    foreach (var key in keys)
                    {
                        _dicDownloadSheetCheck[info.spreadSheetId][key] = select;
                    }
                }

                _dicSheetNames.TryAdd(info.spreadSheetId, new List<string>());
                var listTitleNames = _dicSheetNames[info.spreadSheetId];

                foreach (var sheetName in listTitleNames)
                {
                    _dicDownloadSheetCheck[info.spreadSheetId].TryAdd(sheetName, false);

                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("", GUILayout.Width(5));
                    _dicDownloadSheetCheck[info.spreadSheetId][sheetName] = EditorGUILayout.ToggleLeft($"  {sheetName}",
                        _dicDownloadSheetCheck[info.spreadSheetId][sheetName]);
                    EditorGUILayout.EndHorizontal();
                }
            }

            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space(50);
        }

        private void DrawDownloadSheetBtn()
        {
            var isDownloadable = _dicDownloadSheetCheck.Count > 0 &&
                                 _dicDownloadSheetCheck.Any(x => x.Value.Any(y => y.Value));

            if (!isDownloadable) return;

            GUILayout.FlexibleSpace();

            EditorGUILayout.Separator();

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            switch (sheetDownloadState)
            {
                case eDownloadState.None:
                {
                    EditorGUILayout.BeginHorizontal();
                    
                    if (GUILayout.Button("시트 데이터만 다운로드 (변환x)", GUILayout.Width(150)))
                    {
                        _ = DownloadSheet(GetDownloadInfoList(_dicDownloadSheetCheck));
                        
                        _dicDownloadSheetCheck.Clear();
                    }
                    
                    if (GUILayout.Button("원버튼 변환", GUILayout.Width(150)))
                    {
                        _ = GSSL_OneButton.OneButtonProcess(GetDownloadInfoList(_dicDownloadSheetCheck));
                        
                        _dicDownloadSheetCheck.Clear();
                    }
                    
                    EditorGUILayout.EndHorizontal();
                }
                    break;
                case eDownloadState.Downloading:
                case eDownloadState.Complete:
                {
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.FlexibleSpace();

                    EditorGUILayout.LabelField(sheetDownloadMessage);

                    GUILayout.FlexibleSpace();
                    EditorGUILayout.EndHorizontal();
                }
                    break;
            }

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(30);
        }
    }
}