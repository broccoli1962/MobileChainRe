using System;
using GoogleSpreadSheetLoader.Download;
using GoogleSpreadSheetLoader.Editor.View;
using GoogleSpreadSheetLoader.Setting;
using UnityEditor;
using UnityEngine;

namespace GoogleSpreadSheetLoader
{
    public partial class GSSL_EditorWindow : EditorWindow
    {
        private readonly SettingView _settingView = new();
        private readonly DownloadView _downloadView = new();
        private readonly GenerateView _generateView = new();
        
        private static int _selectedToolbar = 0;

        // 컴파일 후 이전 선택한 것 다시 선택
        [InitializeOnLoadMethod]
        private static void RefocusSelectedToolbarNum()
        {
            _selectedToolbar = LoadSelectedToolbarNum();
        }
        
        [MenuItem("Tools/Google Spread Sheet Loader")]
        public static void ShowWindow()
        {
            var window = GetWindow<GSSL_EditorWindow>(true, "Google Spread Sheet Loader");
            var editorWindow = (window as EditorWindow);
            editorWindow.minSize = new Vector2(530, 600);
            
            _selectedToolbar = LoadSelectedToolbarNum();

            window.ShowUtility();
        }

        private void OnEnable()
        {
            EditorApplication.update -= UpdateEditor;
            EditorApplication.update += UpdateEditor;
        }

        private void OnDisable()
        {
            EditorApplication.update -= UpdateEditor;
        }

        private DateTime _checkTime;
        
        private void UpdateEditor()
        {
            if((DateTime.Now - _checkTime).TotalSeconds > 1)
            {
                _checkTime = DateTime.Now;
                Repaint();
            }
        }
        
        private void OnGUI()
        {
            int prevSelected = _selectedToolbar;
            _selectedToolbar = GUILayout.Toolbar(_selectedToolbar, new[] { "Settings", "Download", "Create" });
            
            // 기존 번호랑 다르면 세이브
            if (prevSelected != _selectedToolbar)
                SaveSelectedToolbarNum();

            if (!GSSL_Setting.CheckAndCreate())
                return;
            
            switch (_selectedToolbar)
            {
                case 0:
                    _settingView.DrawSettingView();
                    break;
                case 1:
                    _downloadView.DrawDownloadView();
                    break;
                case 2:
                    _generateView.DrawGenerateView();
                    break;
            }
        }

        private static int LoadSelectedToolbarNum()
        {
            return EditorPrefs.GetInt("GSSL_SelectToolbarNum", 0);
        }
        private static void SaveSelectedToolbarNum()
        {
            EditorPrefs.SetInt("GSSL_SelectToolbarNum", _selectedToolbar);
        }
    }
}