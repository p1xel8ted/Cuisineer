using UnityEngine;

namespace CuisineerTweaks;

public class UnityEvents : MonoBehaviour
{
    private void Awake()
    {
        Plugin.Logger.LogInfo("UnityEvents Awake");
    }

    private void Update()
    {
        if (CuisineerInputWrapper.GetGameActionKeyUp(BattlebrewGameAction.DebugR))
        {
            Plugin.Instance.Config.Reload();
        }

        if (CuisineerSaveManager.m_Instance != null && CuisineerInputWrapper.GetGameActionKeyUp(BattlebrewGameAction.DebugK))
        {
            CuisineerSaveManager.SaveCurrent();
            Plugin.Logger.LogInfo("Saved current game.");
        }

        if (Plugin.PauseTimeWhenViewingInventories.Value)
        {
            if (TimeManager.m_Instance != null)
            {
                TimeManager.ToggleTimePause(UI_InventoryViewBase.AnyInventoryActive);
            }
        }
        else
        {
            if (TimeManager.m_Instance != null)
            {
                TimeManager.ToggleTimePause(false);
            }
        }
    }
}