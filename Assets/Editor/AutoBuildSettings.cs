using UnityEditor;
using UnityEngine;

#if UNITY_WEBGL
using UnityEditor.WebGL;
#endif

[InitializeOnLoad]
public static class AutoBuildSettings
{
    private const string AppliedKey = "AutoBuildSettings_Applied";

    static AutoBuildSettings()
    {
        if (!EditorPrefs.GetBool(AppliedKey, false))
        {
            Debug.Log("🔄 최초 실행: 커스텀 빌드 세팅 적용 중...");
            ApplyBuildSettings();
            EditorPrefs.SetBool(AppliedKey, true);
        }
        else
        {
            Debug.Log("⏭ 이미 자동 세팅이 적용되어 있으므로 건너뜀.");
        }
    }

    public static void ApplyBuildSettings()
    {
#if UNITY_WEBGL
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
        PlayerSettings.WebGL.template = "Minimal";
        Debug.Log("✔ WebGL 세팅 적용됨");
#endif

        PlayerSettings.defaultScreenWidth = 1655;
        PlayerSettings.defaultScreenHeight = 892;
        PlayerSettings.runInBackground = true;

        PlayerSettings.SplashScreen.show = true;
        PlayerSettings.SplashScreen.showUnityLogo = false;
        
        // 백그라운드 색상 설정: 흰색
        PlayerSettings.SplashScreen.backgroundColor = Color.white;

        // 오버레이 투명도 설정: 0 (완전 투명)
        PlayerSettings.SplashScreen.overlayOpacity = 0;

        AssetDatabase.SaveAssets();
        Debug.Log("✅ 모든 커스텀 빌드 세팅 완료!");
    }

    [MenuItem("Tools/Reapply Custom Build Settings")]
    public static void ResetAndApply()
    {
        EditorPrefs.DeleteKey(AppliedKey);
        ApplyBuildSettings();
        EditorPrefs.SetBool(AppliedKey, true);
        Debug.Log("🔁 수동 재적용 완료");
    }
}
