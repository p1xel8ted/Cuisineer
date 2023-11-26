using UnityEngine;
using UnityEngine.UI;

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