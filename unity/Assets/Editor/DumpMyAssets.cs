using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// 에디터 메뉴 Tools > Dump My Assets List
/// 로그인된 Unity 계정의 Asset Store 구매(My Assets) 목록 전체를
/// 프로젝트 루트의 myassets.json 으로 저장한다. (일회용 유틸리티)
/// </summary>
public static class DumpMyAssets
{
    [MenuItem("Tools/Dump My Assets List")]
    public static void Dump()
    {
        string token = CloudProjectSettings.accessToken;
        if (string.IsNullOrEmpty(token))
        {
            EditorUtility.DisplayDialog("실패", "로그인 토큰을 찾을 수 없습니다. Unity Hub 로그인 상태를 확인하세요.", "확인");
            return;
        }

        var sb = new StringBuilder();
        sb.Append("[");
        int offset = 0;
        const int limit = 100;
        int total = int.MaxValue;
        bool first = true;

        while (offset < total)
        {
            string url = "https://packages-v2.unity.com/-/api/purchases?offset=" + offset + "&limit=" + limit;
            using (var req = UnityWebRequest.Get(url))
            {
                req.SetRequestHeader("Authorization", "Bearer " + token);
                var op = req.SendWebRequest();
                while (!op.isDone) { } // 일회용 툴이라 동기 대기

                if (req.result != UnityWebRequest.Result.Success)
                {
                    EditorUtility.DisplayDialog("실패", "API 오류 (offset " + offset + "): " + req.error + "\n" + req.downloadHandler.text, "확인");
                    return;
                }

                string json = req.downloadHandler.text;
                if (!first) sb.Append(",");
                sb.Append(json);
                first = false;

                // total 파싱 (간단 추출)
                var m = System.Text.RegularExpressions.Regex.Match(json, "\"total\"\\s*:\\s*(\\d+)");
                if (m.Success) total = int.Parse(m.Groups[1].Value);

                offset += limit;
                EditorUtility.DisplayProgressBar("My Assets 목록 수집", offset + " / " + total, Mathf.Clamp01((float)offset / total));
            }
        }
        sb.Append("]");
        EditorUtility.ClearProgressBar();

        string path = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "myassets.json");
        File.WriteAllText(path, sb.ToString());
        EditorUtility.DisplayDialog("완료", "저장됨:\n" + path, "확인");
        Debug.Log("My Assets dumped to: " + path);
    }
}
