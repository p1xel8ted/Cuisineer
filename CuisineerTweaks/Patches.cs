using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using HarmonyLib;
using UnityEngine;

namespace CuisineerTweaks;

[Harmony]
[SuppressMessage("ReSharper", "InconsistentNaming")]
public static class Patches
{
    private const string UpgradeDateRestaurant = "UPGRADE_DATE_RESTAURANT";
    private const string BattleBrewProductions = "BattleBrew Productions";
    private static RestaurantExt RestaurantExtInstance { get; set; }
    private static float NextRegen { get; set; }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(Inventory), nameof(Inventory.InsertHelper))]
    public static void Inventory_SetSlotData(ref ItemInstance item)
    {
        if (!Plugin.IncreaseStackSize.Value || item == null) return;

        if (!Plugin.OriginalItemStackSizes.TryGetValue(item.ItemSO, out var maxStack))
        {
            maxStack = item.ItemSO.m_MaxStack;
            Plugin.OriginalItemStackSizes[item.ItemSO] = maxStack;
        }

        if (Plugin.IncreaseStackSizeValue.Value > maxStack)
        {
            item.ItemSO.m_MaxStack = Plugin.IncreaseStackSizeValue.Value;
        }
        else
        {
            Plugin.Logger.LogWarning($"Inventory.InsertHelper: Tried to increase stack size of {item.ItemSO.name} to {Plugin.IncreaseStackSizeValue.Value}, but it's already {maxStack}!");
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(UI_CarpenterUpgradeArea), nameof(UI_CarpenterUpgradeArea.HandleUpgradeRestaurantClicked))]
    public static void UI_CarpenterUpgradeArea_HandleUpgradeRestaurantClicked(ref UI_CarpenterUpgradeArea __instance)
    {
        if (!Plugin.InstantRestaurantUpgrades.Value) return;
        var currentDateInt = SimpleSingleton<CalendarManager>.Instance.CurrDate;
        GlobalEvents.Narrative.OnFlagTrigger.Invoke(UpgradeDateRestaurant, FlagType.Persisting, currentDateInt - 2);
        SimpleSingleton<RestaurantDataManager>.Instance.HandleDayChanged();
        if (RestaurantExtInstance != null)
        {
            RestaurantExtInstance.LoadRestaurantExterior();
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(RestaurantExt), nameof(RestaurantExtInstance.Awake))]
    [HarmonyPatch(typeof(RestaurantExt), nameof(RestaurantExtInstance.LoadRestaurantExterior))]
    public static void RestaurantExt_Awake(ref RestaurantExt __instance)
    {
        RestaurantExtInstance ??= __instance;
    }


    [HarmonyPostfix]
    [HarmonyPatch(typeof(UI_BrewArea), nameof(UI_BrewArea.EnterBrew))]
    public static void UI_BrewArea_EnterBrew(ref UI_BrewArea __instance)
    {
        if (!Plugin.InstantBrew.Value) return;
        var brewConfirmationData = __instance.m_BrewConfirmationData;
        var currDate = SimpleSingleton<CalendarManager>.Instance.CurrDate;
        brewConfirmationData.m_BrewDate = currDate - 2;
        __instance.SwitchState(UI_BrewArea.Stage.Claim);
    }

    // internal static UI_MainMenu MainMenuInstance { get; set; }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(UI_MainMenu), nameof(UI_MainMenu.HandleDependenciesReady))]
    public static void UI_MainMenu_Awake(ref UI_MainMenu __instance)
    {
        // MainMenuInstance ??= __instance;
    
        __instance.m_FadeDuration = 0f;
        __instance.m_CreditBtn.gameObject.SetActive(false);
        __instance.m_QuitGameBtn.transform.localPosition = __instance.m_CreditBtn.transform.localPosition;
        __instance.m_ButtonsBackingAnimator.Activate();
        __instance.m_WaitingForInput = false;
        __instance.m_PressAnyButtonText.CrossFadeAlpha(0, 0, true);
        if (Plugin.LoadToSaveMenu.Value)
        {
            __instance.StartGameBtnClicked();
        }

        if (Plugin.AutoLoadSpecifiedSave.Value)
        {
            var slot = Plugin.AutoLoadSpecifiedSaveSlot.Value - 1;
            if (!__instance.m_SaveSlotMenu.m_SaveSlots[slot].m_NewSave)
            {
                __instance.m_SaveSlotMenu.m_SaveSlots[slot].HandleClicked();
            }
            else
            {
                Plugin.Logger.LogError($"AutoLoadSpecifiedSaveSlot: Chosen save slot {slot + 1} is empty!");
            }
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(Player), nameof(Player.GetMaxHP))]
    public static void Player_MaxHP(ref Player __instance, ref int __result)
    {
        if (!Plugin.ModifyPlayerMaxHp.Value) return;
        var originalHP = __result;
        var newHP = originalHP * Plugin.ModifyPlayerMaxHpMultiplier.Value;
        __result = Mathf.RoundToInt(newHP);
        Plugin.Logger.LogInfo($"Player.GetMaxHP: Player max HP: {originalHP} -> {newHP} ({Plugin.ModifyPlayerMaxHpMultiplier.Value}x)");
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(Player), nameof(Player.LateUpdate))]
    public static void Player_LateUpdate(ref Player __instance)
    {
        if (!Plugin.RegenPlayerHp.Value) return;

        if (Time.time > NextRegen && __instance.m_RuntimeData.m_PlayerHP < __instance.m_RuntimeData.MaxAvailableHP)
        {
            NextRegen = Time.time + Plugin.RegenPlayerHpTick.Value;
            __instance.Heal(Plugin.RegenPlayerHpAmount.Value, !Plugin.RegenPlayerHpShowFloatingText.Value);
        }
    }
    
    [HarmonyPrefix]
    [HarmonyPatch(typeof(GUI), nameof(GUI.Label), typeof(Rect), typeof(string), typeof(GUIStyle))]
    public static void GUI_Label(ref Rect position, ref string text, ref GUIStyle style)
    {
  
        var version = Application.version;
        var versionText = "v" + version;
        if (text.Contains(versionText, StringComparison.OrdinalIgnoreCase))
        {
            style.alignment = TextAnchor.UpperLeft;
            position.y -= 17;
            style.wordWrap = true;
            text += $"{Environment.NewLine}Cuisineer Tweaks v{Plugin.PluginVersion}"; 
        }
    
      
    }


    [HarmonyPostfix]
    [HarmonyPatch(typeof(UI_Cutscene), nameof(UI_Cutscene.RefreshText))]
    public static void UI_Cutscene_RefreshText(ref UI_Cutscene __instance)
    {
        if (!Plugin.InstantText.Value || __instance == null) return;
        var speaker = __instance.m_CurrLine.m_MainSpeaker;
        if (speaker is not (SpeakerType.Left or SpeakerType.Right)) return;
        __instance.Skip();
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(UI_GearModDisplay), nameof(UI_GearModDisplay.SetLockedState))]
    public static void UI_GearModDisplay_SetLockedState(ref bool state)
    {
        if (!Plugin.UnlockBothModSlots.Value) return;
        state = false;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(LoadingScreenProgressComponent), nameof(LoadingScreenProgressComponent.OnEnable))]
    [HarmonyPatch(typeof(LoadingScreenProgressComponent), nameof(LoadingScreenProgressComponent.OnDisable))]
    public static void LoadingScreenProgressComponent_OnDisable()
    {
        Screen.SetResolution(Display.main.systemWidth, Display.main.systemHeight, FullScreenMode.FullScreenWindow, Plugin.MaxRefresh);
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(UI_GameplayOptions), nameof(UI_GameplayOptions.Setup))]
    public static void UI_GameplayOptions_Setup()
    {
        var myResData = new ResolutionData
        {
            m_Height = Display._mainDisplay.systemHeight,
            m_Width = Display._mainDisplay.systemWidth
        };
        var resDatas = UI_GameplayOptions.ResolutionDatas.ToList();
        if (!resDatas.Exists(a => a.m_Height == Display._mainDisplay.systemHeight && a.m_Width == Display._mainDisplay.systemWidth))
        {
            resDatas.Add(myResData);
            Plugin.Logger.LogInfo($"Main display resolution not detected; added {myResData.m_Width}x{myResData.m_Height} to resolution list");
        }

        UI_GameplayOptions.ResolutionDatas = resDatas.ToArray();
    }
}