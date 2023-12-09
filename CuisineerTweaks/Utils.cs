namespace CuisineerTweaks;

public static class Utils
{
    internal static void WriteLog(string message, bool ignoreDebug = false)
    {
        if (Plugin.DebugMode.Value || ignoreDebug)
        {
            Plugin.Logger.LogInfo(message);
        }
    }

    internal static void FastForwardBrewCraft(UI_BrewArea.StateData stateData)
    {
        var currDate = SimpleSingleton<CalendarManager>.Instance.CurrDate;
        stateData.m_BrewDate = currDate - 2;
    }

    internal static UI_GameplayOptions GameplayOptionsInstance { get; set; }


    internal static void UpdateResolutionData(UI_GameplayOptions __instance, bool changeRes = false)
    {
        if (__instance == null)
        {
            return;
        }
        GameplayOptionsInstance = __instance;
        var resData = UI_GameplayOptions.ResolutionDatas[__instance.m_ResolutionSelection.DropDown.value];
        Fixes.ResolutionWidth = resData.m_Width;
        Fixes.ResolutionHeight = resData.m_Height;
        var fsData = UI_GameplayOptions.FullscreenDatas[__instance.m_FullscreenSelection.DropDown.value];
        Fixes.FullScreenMode = fsData.m_FullScreenMode;
        Fixes.MaxRefreshRate = UI_GameplayOptions.FramerateDatas[__instance.m_FramerateSelection.DropDown.value].m_FPS;


        WriteLog($"Chosen Display Settings: {Fixes.ResolutionWidth}x{Fixes.ResolutionHeight}@{Fixes.MaxRefreshRate}Hz in {Fixes.FullScreenMode} mode", true);
        if (!changeRes) return;
        Fixes.UpdateResolutionFrameRate();
    }

    internal static int FindLowestFrameRateMultipleAboveFifty(int originalRate)
    {
        // Start from half of the original rate and decrement by one to find the highest multiple above 50.
        for (var rate = originalRate / 2; rate > 50; rate--)
        {
            if (originalRate % rate == 0)
            {
                return rate;
            }
        }

        // Fallback, though this scenario is unlikely with standard monitor refresh rates
        return originalRate;
    }

    internal static void ScaleElement(string path, bool maskCheck, float scaleFactor = 1f)
    {
        var element = GameObject.Find(path);
        if (element == null) return;
        if (maskCheck)
        {
            var maskComponent = element.GetComponent<Mask>();
            if (maskComponent != null)
            {
                maskComponent.enabled = false;
            }
        }

        element.transform.localScale = element.transform.localScale with {x = scaleFactor};
    }
}