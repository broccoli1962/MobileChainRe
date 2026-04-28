using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;
// ReSharper disable MemberCanBePrivate.Global

public static class LocalizeTable
{
    private static Dictionary<string, string> dicLocalize = new();

    public static void Initialize(string countryCode)
    {
        var assetName = $"Localize_";
        switch (countryCode)
        {
            case "kr":
            case "en":
                assetName += $"{countryCode}";
                break;
            default:
                Debug.LogError($"해당 국가코드 ({countryCode}) 는 정의 되지 않아 en으로 대체합니다.");
                break;
        }

        var obj = Resources.Load<TextAsset>(assetName);

        dicLocalize = JsonConvert.DeserializeObject<Dictionary<string, string>>(obj.text);
    }

    public static string GetLocalize(this string str)
    {
        if (dicLocalize.TryGetValue(str, out var result))
        {
            return result;
        }

        return "!" + str;
    }
}