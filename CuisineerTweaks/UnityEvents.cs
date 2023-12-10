namespace CuisineerTweaks;

public class UnityEvents : MonoBehaviour
{
    private void Awake()
    {
        Plugin.Logger.LogInfo("UnityEvents Awake");
    }

    private void Update()
    {
        // Handle DebugR key action
        if (CuisineerInputWrapper.GetGameActionKeyUp(BattlebrewGameAction.DebugR))
        {
            Plugin.Instance.Config.Reload();
        }

        // Handle DebugK key action
        if (CuisineerSaveManager.m_Instance != null && CuisineerInputWrapper.GetGameActionKeyUp(BattlebrewGameAction.DebugK))
        {
            CuisineerSaveManager.SaveCurrent();
            Plugin.Logger.LogInfo("Saved current game.");
        }

        // Handle zoom level adjustment
        if (Plugin.AdjustableZoomLevel.Value)
        {
            var change = 0f;
            if (CuisineerInputWrapper.GetGameActionKeyUp(BattlebrewGameAction.DebugO))
            {
                change = -0.1f;
            }
            else if (CuisineerInputWrapper.GetGameActionKeyUp(BattlebrewGameAction.DebugL))
            {
                change = 0.1f;
            }

            if (change != 0f)
            {
                var newValue = Mathf.Round((Plugin.UseStaticZoomLevel.Value ? Plugin.StaticZoomAdjustment.Value : Plugin.RelativeZoomAdjustment.Value + change) * 10f) / 10f;
                var newZoom = Fixes.GetNewZoomValue(newValue);

                if (newZoom > 0.5f)
                {
                    if (Plugin.UseStaticZoomLevel.Value)
                    {
                        Plugin.StaticZoomAdjustment.Value = newValue;
                    }
                    else
                    {
                        Plugin.RelativeZoomAdjustment.Value = newValue;
                    }
                    Fixes.UpdateCameraZoom();
                }
            }
        }

        // Handle time pause when viewing inventories
        if (TimeManager.m_Instance == null) return;
        var shouldPause = Plugin.PauseTimeWhenViewingInventories.Value && UI_InventoryViewBase.AnyInventoryActive;
        TimeManager.ToggleTimePause(shouldPause);
    }

}