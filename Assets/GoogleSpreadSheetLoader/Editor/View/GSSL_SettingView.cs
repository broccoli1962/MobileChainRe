using UnityEditor;
using UnityEngine;
// ReSharper disable CheckNamespace

// ReSharper disable FieldCanBeMadeReadOnly.Local

namespace GoogleSpreadSheetLoader.Setting
{
    public class SettingView
    {
        private bool _apiKeyEditToggle;
        private bool _sheetInfoEditToggle;
        private bool _sheetTargetEditToggle;

        public void DrawSettingView()
        {
            DrawApiKey();

            DrawSpreadSheetsInfos();

            DrawAddRemoveButtons();

            DrawSheetSettings();

            SaveSettingData();
        }


        private void DrawApiKey()
        {
            EditorGUILayout.Separator();

            _apiKeyEditToggle = EditorGUILayout.ToggleLeft("  API 키", _apiKeyEditToggle, EditorStyles.whiteLargeLabel);

            if (_apiKeyEditToggle)
                GSSL_Setting.SettingData.apiKey = EditorGUILayout.TextField(GSSL_Setting.SettingData.apiKey);
            else
                EditorGUILayout.LabelField(GSSL_Setting.SettingData.apiKey);
        }

        private void DrawSpreadSheetsInfos()
        {
            EditorGUILayout.Separator();

            _sheetInfoEditToggle =
                EditorGUILayout.ToggleLeft("  스프레드 시트 데이터 입력", _sheetInfoEditToggle, EditorStyles.whiteLargeLabel);

            int sheetNameWidth = 100;
            for (int i = 0; i < GSSL_Setting.SettingData.listSpreadSheetInfo.Count; i++)
            {
                var info = GSSL_Setting.SettingData.listSpreadSheetInfo[i];

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"{i + 1}. ", GUILayout.Width(20));

                if (_sheetInfoEditToggle)
                {
                    info.spreadSheetName =
                        EditorGUILayout.TextField(info.spreadSheetName, GUILayout.Width(sheetNameWidth));
                    info.spreadSheetId = EditorGUILayout.TextField(info.spreadSheetId);
                }
                else
                {
                    EditorGUILayout.LabelField(info.spreadSheetName, GUILayout.Width(sheetNameWidth));
                    EditorGUILayout.LabelField(info.spreadSheetId);
                    GUILayout.FlexibleSpace();
                }

                EditorGUILayout.EndHorizontal();
            }
        }

        private void DrawAddRemoveButtons()
        {
            if (!_sheetInfoEditToggle) return;

            EditorGUILayout.Separator();

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            if (GSSL_Setting.SettingData.listSpreadSheetInfo.Count > 0 && GUILayout.Button("-", GUILayout.Width(100)))
            {
                GSSL_Setting.SettingData.listSpreadSheetInfo.RemoveAt(
                    GSSL_Setting.SettingData.listSpreadSheetInfo.Count - 1);
            }

            if (GUILayout.Button("+", GUILayout.Width(100)))
            {
                GSSL_Setting.SettingData.listSpreadSheetInfo.Add(new SpreadSheetInfo()
                    { spreadSheetName = "Name Here", spreadSheetId = "Id Here" });
            }

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }

        private void DrawSheetSettings()
        {
            EditorGUILayout.Separator();

            _sheetTargetEditToggle =
                EditorGUILayout.ToggleLeft("  시트 설정", _sheetTargetEditToggle, EditorStyles.whiteLargeLabel);
            if (_sheetTargetEditToggle)
            {
                // 무시
                EditorGUILayout.BeginHorizontal();
                GUILayout.Box(" 시트의 제목에서", GUILayout.Width(100));
                EditorGUILayout.TextField(GSSL_Setting.SettingData.sheetTargetStr, GUILayout.Width(50));
                // EditorGUILayout.EndHorizontal();
                // EditorGUILayout.BeginHorizontal();
                GUILayout.Box("문구가 포함되어 있을 경우, 대상 시트에서 ", GUILayout.Width(250));
                GSSL_Setting.SettingData.sheetTarget =
                    (SettingData.eSheetTargetStandard)EditorGUILayout.EnumPopup(GSSL_Setting.SettingData.sheetTarget,
                        GUILayout.Width(60));
                EditorGUILayout.EndHorizontal();

                // 이넘 타입
                EditorGUILayout.BeginHorizontal();
                GUILayout.Box(" 다음의 문구가 있다면 enum타입을 정의하는 테이블로 분류합니다.");
                GSSL_Setting.SettingData.sheet_enumTypeStr =
                    EditorGUILayout.TextField(GSSL_Setting.SettingData.sheet_enumTypeStr, GUILayout.Width(100));
                EditorGUILayout.EndHorizontal();

                // 로컬라이제이션 타입
                EditorGUILayout.BeginHorizontal();
                GUILayout.Box(" 다음의 같은 문구가 있다면 Localization테이블로 분류합니다.");
                GSSL_Setting.SettingData.sheet_localizationTypeStr =
                    EditorGUILayout.TextField(GSSL_Setting.SettingData.sheet_localizationTypeStr, GUILayout.Width(100));
                EditorGUILayout.EndHorizontal();
            }
            else
            {
                // 무시
                EditorGUILayout.LabelField(
                    $" 시트의 제목에서 \"{GSSL_Setting.SettingData.sheetTargetStr}\" 문구가 포함되어 있을경우, 대상 시트에서 \"{GSSL_Setting.SettingData.sheetTarget.ToString()}\"");

                // 이넘 타입
                EditorGUILayout.LabelField(
                    $" 다음의 문구가 있다면 enum타입을 정의하는 테이블로 분류합니다. \"{GSSL_Setting.SettingData.sheet_enumTypeStr}\"");

                // 로컬라이제이션 타입
                EditorGUILayout.LabelField(
                    $" 다음의 같은 문구가 있다면 Localization테이블로 분류합니다. \"{GSSL_Setting.SettingData.sheet_localizationTypeStr}\"");
            }
        }

        private void SaveSettingData()
        {
            GUILayout.FlexibleSpace();

            EditorGUILayout.Separator();

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("세팅 데이터 저장", GUILayout.Width(200)))
            {
                EditorUtility.SetDirty(GSSL_Setting.SettingData);
                AssetDatabase.SaveAssets();
            }


            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(30);
        }
    }
}