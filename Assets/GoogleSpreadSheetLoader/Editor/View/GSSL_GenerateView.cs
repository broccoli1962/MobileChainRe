using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GoogleSpreadSheetLoader.Download;
using GoogleSpreadSheetLoader.Generate;
using GoogleSpreadSheetLoader.Setting;
using UnityEditor;
using UnityEngine;
using static GoogleSpreadSheetLoader.SheetData;

// ReSharper disable InconsistentNaming

namespace GoogleSpreadSheetLoader.Editor.View
{
    public class GenerateView
    {
        private enum eGenerateState
        {
            None,
            Progress,
            Complete,
        }

        private DateTime _checkTime;
        private readonly List<SheetData> _listSheetData = new();
        private readonly Dictionary<eTableStyle, Dictionary<SheetData, bool>> _dicTableDataGenerateCheck = new();
        private Vector2 _generateScrollPos = Vector2.zero;
        private readonly eGenerateState _generateScriptState = eGenerateState.None;
        private readonly string _generateScriptMessage = "";
        private readonly eGenerateState _generateDataState = eGenerateState.None;
        private readonly string _generateDataMessage = "";

        public void DrawGenerateView()
        {
            CheckAndLoad();

            DrawTableDataList();

            DrawGenerateButtons();
        }

        private void CheckAndLoad()
        {
            if ((DateTime.Now - _checkTime).TotalSeconds > 1)
            {
                _checkTime = DateTime.Now;

                if (!Directory.Exists(GSSL_Download.sheetDataAssetPath))
                    return;
                
                _listSheetData.Clear();

                var listData = GSSL_Generate.GetSheetDataList();

                if (listData == null)
                {
                    CheckAndClearDictionary();
                    return;
                }
                
                _listSheetData.AddRange(listData);
                
                CheckAndClearDictionary();
            }
        }

        private void DrawTableDataList()
        {
            EditorGUILayout.Separator();

            if (_listSheetData == null || _listSheetData.Count == 0)
            {
                EditorGUILayout.LabelField("  다운로드된 시트 데이터가 하나도 없음.", EditorStyles.whiteLargeLabel);
                return;
            }

            EditorGUILayout.LabelField($"  변환시킬 시트들 선택", EditorStyles.whiteLargeLabel);

            EditorGUILayout.Separator();

            // 요소 체크
            foreach (var sheetData in _listSheetData)
            {
                _dicTableDataGenerateCheck.TryAdd(sheetData.tableStyle, new Dictionary<SheetData, bool>());
                _dicTableDataGenerateCheck[sheetData.tableStyle].TryAdd(sheetData, true);
            }

            var boxStyle = new GUIStyle(GUI.skin.box);
            _generateScrollPos = EditorGUILayout.BeginScrollView(_generateScrollPos,
                GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            EditorGUILayout.BeginVertical(boxStyle);

            foreach (var categoryPair in _dicTableDataGenerateCheck)
            {
                var currCategory = categoryPair.Key;
                var currDic = categoryPair.Value;

                string categoryName = "";
                switch (currCategory)
                {
                    case eTableStyle.None: categoryName = "일반"; break;
                    case eTableStyle.EnumType: categoryName = "Enum"; break;
                    case eTableStyle.Localization: categoryName = "Localization"; break;
                }

                // 전체 체크 부분
                var isAllCheck = currDic.Count > 0 && currDic.All(x => x.Value);
                var selected = EditorGUILayout.ToggleLeft($" {categoryName}", isAllCheck);

                if (selected != isAllCheck)
                {
                    var keys = currDic.Keys.ToArray();

                    foreach (var sheetData in keys)
                    {
                        currDic[sheetData] = selected;
                    }
                }

                // 요소들
                foreach (var sheetData in _listSheetData)
                {
                    if (!currDic.ContainsKey(sheetData))
                        continue;

                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("", GUILayout.Width(10));
                    currDic[sheetData] =
                        EditorGUILayout.ToggleLeft(sheetData.title, currDic[sheetData]);
                    EditorGUILayout.EndHorizontal();
                }

                EditorGUILayout.Space(10);
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.EndScrollView();
        }

        // ReSharper disable Unity.PerformanceAnalysis
        private void DrawGenerateButtons()
        {
            var isGenerateAble = _dicTableDataGenerateCheck.Count > 0 &&
                                 _dicTableDataGenerateCheck.Values.Any(x => x.Values.Any(y => y));

            if (!isGenerateAble)
                return;

            GUILayout.FlexibleSpace();

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            switch (_generateScriptState)
            {
                case eGenerateState.None:
                    if (GUILayout.Button("스크립트 생성", GUILayout.Width(150)))
                    {
                        var list = new List<SheetData>();
                        foreach (var pair in _dicTableDataGenerateCheck)
                        {
                            list.Clear();
                            list = pair.Value.Where(x => x.Value)
                                .Select(x => x.Key).ToList();

                            switch (pair.Key)
                            {
                                case eTableStyle.None:
                                    GSSL_Generate.GenerateTableScripts(list);
                                    AssetDatabase.Refresh();
                                    break;
                                case eTableStyle.EnumType:
                                    GSSL_Generate.GenerateEnumDef(list);
                                    AssetDatabase.Refresh();
                                    break;
                                case eTableStyle.Localization:
                                    GSSL_Generate.GenerateLocalize(list);
                                    AssetDatabase.Refresh();
                                    break;
                                default:
                                    throw new ArgumentOutOfRangeException();
                            }
                        }
                    }

                    break;
                case eGenerateState.Progress:
                case eGenerateState.Complete:
                {
                    EditorGUILayout.LabelField(_generateScriptMessage, GUILayout.Width(150));
                }
                    break;
            }

            switch (_generateDataState)
            {
                case eGenerateState.None:
                {
                    if (GUILayout.Button("테이블 데이터 생성", GUILayout.Width(150)))
                    {
                        var enumTarget = _dicTableDataGenerateCheck[eTableStyle.None].Where(x => x.Value);
                        var listSheet = new List<SheetData>();
                        foreach (var pair in enumTarget)
                        {
                            listSheet.Add(pair.Key);
                        }

                        GSSL_Generate.GenerateTableData(listSheet);
                    }
                }
                    break;
                case eGenerateState.Progress:
                case eGenerateState.Complete:
                {
                    EditorGUILayout.LabelField(_generateDataMessage, GUILayout.Width(150));
                }
                    break;
            }

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(20);
        }

        private void CheckAndClearDictionary()
        {
            if (_listSheetData.All(x => x != null)) return;

            _listSheetData.Clear();
            _dicTableDataGenerateCheck.Clear();
        }
    }
}