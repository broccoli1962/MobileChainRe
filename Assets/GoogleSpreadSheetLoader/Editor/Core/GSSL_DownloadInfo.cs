using GoogleSpreadSheetLoader.Setting;
using UnityEngine.Networking;

namespace GoogleSpreadSheetLoader.Download
{
    public class GSSL_DownloadInfo
    {
        public GSSL_DownloadInfo(string spreadSheetId, string sheetName)
        {
            _spreadSheetId = spreadSheetId;
            _sheetName = sheetName;

            var url = string.Format(GSSL_Download.downloadSheetUrl, _spreadSheetId, _sheetName,
                GSSL_Setting.SettingData.apiKey);
            _webRequest = UnityWebRequest.Get(url);
        }

        private string _spreadSheetId;
        private string _sheetName;
        private readonly UnityWebRequest _webRequest;

        public bool IsDone()
        {
            return _webRequest.isDone;
        }
        
        public UnityWebRequestAsyncOperation SendAndGetAsyncOperation()
        {
            return _webRequest.SendWebRequest();
        }

        public string GetSheetName()
        {
            return _sheetName;
        }

        public string GetDownloadText()
        {
            return _webRequest.downloadHandler.text;
        }
    }
}