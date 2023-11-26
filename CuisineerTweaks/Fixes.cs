﻿using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using UnityEngine;

namespace CuisineerTweaks;

[SuppressMessage("ReSharper", "InconsistentNaming")]
public static class Fixes
{
    private const string MainMenuScene = "MainMenuScene";
    internal static int MaxRefresh => Screen.resolutions.Max(a => a.refreshRate);
    private static int TimeScale => Utils.FindLowestFrameRateMultipleAboveFifty(MaxRefresh);
    private static Dictionary<string, int> OriginalItemStackSizes { get; } = new();
    private static Dictionary<string, float> OriginalWeaponTimes { get; } = new();

    internal static void UpdateWeaponCooldowns()
    {
        if (Player.m_Instance == null) return;
        foreach (var slot in Player.RuntimeData.m_Weapons)
        {
            var weapon = slot.m_Weapon;
            if (weapon == null) continue;

            if (!OriginalWeaponTimes.TryGetValue(slot.m_Weapon.m_WeaponName, out var cooldown))
            {
                OriginalWeaponTimes[slot.m_Weapon.m_WeaponName] = weapon.m_SpecialAttackCooldown;
            }

            if (!Plugin.ModifyWeaponSpecialCooldown.Value)
            {
                weapon.m_SpecialAttackCooldown = OriginalWeaponTimes[slot.m_Weapon.m_WeaponName];
                Utils.WriteLog($"Reset weapon cooldown for {slot.m_Weapon.m_WeaponName} to {weapon.m_SpecialAttackCooldown}", true);

                continue;
            }

            var weaponCooldownPercent = Plugin.WeaponSpecialCooldownValue.Value;
            var newCooldown = cooldown * (1f - weaponCooldownPercent / 100f);

            weapon.m_SpecialAttackCooldown = newCooldown;
            Utils.WriteLog($"Updated weapon cooldown for {slot.m_Weapon.m_WeaponName} from {cooldown} to {newCooldown}", true);
        }
    }

    internal static void UpdateItemStackSize(ItemInstance __instance)
    {
        if (__instance.ItemSO.Type is not (ItemType.Ingredient or ItemType.Potion or ItemType.Material)) return;

        if (!OriginalItemStackSizes.TryGetValue(__instance.ItemID, out var maxStack))
        {
            maxStack = __instance.ItemSO.m_MaxStack;
            OriginalItemStackSizes[__instance.ItemID] = maxStack;
        }

        if (!Plugin.IncreaseStackSize.Value)
        {
            __instance.ItemSO.m_MaxStack = OriginalItemStackSizes[__instance.ItemID];
            Utils.WriteLog($"Reset Stack Size: {__instance.ItemID} stack size: {maxStack} -> {OriginalItemStackSizes[__instance.ItemID]}");
            return;
        }

        if (Plugin.IncreaseStackSizeValue.Value > maxStack)
        {
            __instance.ItemSO.m_MaxStack = Plugin.IncreaseStackSizeValue.Value;
            Utils.WriteLog($"ItemInstance: {__instance.ItemID} stack size: {maxStack} -> {Plugin.IncreaseStackSizeValue.Value}");
        }
        else
        {
            Utils.WriteLog($"ItemInstance: {__instance.ItemID} stack size: {maxStack} (unchanged)");
        }
    }

    internal static void RunFixes(string scene, bool refresh = false)
    {
        Utils.WriteLog(!refresh ? $"New Scene {scene} Loaded: Running Fixes" : $"Refresh Requested: Running Fixes", true);

        UpdateResolutionFrameRate();
        UpdateFixedDeltaTime();
        UpdateAutoSave();
        UpdateInventoryStackSize();
        UpdateWeaponCooldowns();
        UpdateMainMenu(scene);
        UpdateCheats();
    }

    private static void UpdateResolutionFrameRate()
    {
        Screen.SetResolution(Display.main.systemWidth, Display.main.systemHeight, FullScreenMode.FullScreenWindow, MaxRefresh);
        Utils.WriteLog($"Set resolution to {Screen.currentResolution}");

        if (Application.targetFrameRate != MaxRefresh)
        {
            Application.targetFrameRate = MaxRefresh;
            Utils.WriteLog($"Set targetFrameRate to {Application.targetFrameRate}.");
        }
        else
        {
            Utils.WriteLog($"targetFrameRate is already {Application.targetFrameRate}. No update necessary.");
        }
    }

    private static void UpdateFixedDeltaTime()
    {
        if (!Plugin.CorrectFixedUpdateRate.Value) return;
        var originalTime = Time.fixedDeltaTime;
        var scale = Plugin.UseRefreshRateForFixedUpdateRate.Value ? MaxRefresh : TimeScale;
        var newValue = 1f / scale;
        if (Mathf.Approximately(newValue, originalTime))
        {
            Utils.WriteLog($"fixedDeltaTime is already {newValue} ({scale}fps). No update necessary.");
            return;
        }

        Time.fixedDeltaTime = newValue;
        Utils.WriteLog($"Set fixedDeltaTime to {newValue} ({scale}fps). Original is {originalTime} ({Mathf.Round(1f / originalTime)}fps).");
    }

    private static void UpdateCheats()
    {
        Cheats.Customer.DisableDineAndDash = !Plugin.DineAndDash.Value;
        Cheats.Customer.AutoCollectPayment = Plugin.AutomaticPayment.Value;
        Cheats.CookingTools.AutoCook = Plugin.AutoCook.Value;
        Cheats.CookingTools.FreeCook = Plugin.FreeCook.Value;
        Player.OnePunchMode = Plugin.InstantKill.Value;
    }

    private static void UpdateMainMenu(string scene)
    {
        if (!scene.Equals(MainMenuScene)) return;
        if (!Plugin.CorrectMainMenuAspect.Value) return;
        const float baseAspect = 16f / 9f;
        var currentAspect = Display.main.systemWidth / (float) Display.main.systemHeight;
        if (currentAspect <= baseAspect) return;

        var positiveScaleFactor = currentAspect / baseAspect;
        var negativeScaleFactor = 1f / positiveScaleFactor;
        Utils.WriteLog($"Current aspect ratio ({currentAspect}) is greater than base aspect ratio ({baseAspect}). Resizing UI elements.");

        Utils.ScaleElement("UI_MainMenuCanvas/Mask", true);
        Utils.ScaleElement("UI_MainMenuCanvas/Mask/UI_MainMenu/Container", false, positiveScaleFactor);
        Utils.ScaleElement("UI_MainMenuCanvas/Mask/UI_MainMenu/Container/Centre/MC", false, negativeScaleFactor);
        Utils.ScaleElement("UI_MainMenuCanvas/Mask/UI_MainMenu/Container/Press any key text", false, negativeScaleFactor);
        Utils.ScaleElement("UI_MainMenuCanvas/Mask/UI_MainMenu/Container/CuisineerLogo", false, negativeScaleFactor);
        Utils.ScaleElement("UI_MainMenuCanvas/Mask/UI_MainMenu/Container/ButtonsBacking", false, negativeScaleFactor);
        Utils.ScaleElement("UI_MainMenuCanvas/Mask/UI_MainMenu/Container/UI_SaveSlotDetail", false, negativeScaleFactor);
    }

    internal static void UpdateInventoryStackSize()
    {
        if (InventoryManager.m_Instance == null) return;

        Utils.WriteLog("Updating Inventory Stack Sizes");

        foreach (var instanceMInventory in InventoryManager.Instance.m_Inventories)
        {
            if (instanceMInventory.Value == null) continue;

            foreach (var valueMSlot in instanceMInventory.Value.m_Slots)
            {
                if (valueMSlot?.ItemSO == null) continue;

                UpdateItemStackSize(valueMSlot);
            }
        }
    }


    private static void UpdateAutoSave()
    {
        if (CuisineerSaveManager.m_Instance == null) return;
        Utils.WriteLog("Initiating AutoSave");
        CuisineerSaveManager.Instance.m_AutoSave = Plugin.EnableAutoSave.Value;
        CuisineerSaveManager.Instance.m_AutoSaveFrequency = Plugin.AutoSaveFrequency.Value;
        Utils.WriteLog($"AutoSave: {CuisineerSaveManager.Instance.m_AutoSave} ({CuisineerSaveManager.Instance.m_AutoSaveFrequency / 60f} minutes)");
    }
}