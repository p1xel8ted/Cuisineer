using System.Diagnostics.CodeAnalysis;
using System.Linq;
using HarmonyLib;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppSystem;
using UnityEngine;
using Environment = System.Environment;
using StringComparison = System.StringComparison;

namespace CuisineerTweaks;

[Harmony]
[SuppressMessage("ReSharper", "InconsistentNaming")]
public static class Patches
{
    private const string UpgradeDateRestaurant = "UPGRADE_DATE_RESTAURANT";

    private static RestaurantExt RestaurantExtInstance { get; set; }
    private static float NextRegen { get; set; }


    [HarmonyPrefix]
    [HarmonyPatch(typeof(ItemInstance), nameof(ItemInstance.Clone))]
    [HarmonyPatch(typeof(ItemInstance), nameof(ItemInstance.CloneWithCount))]
    [HarmonyPatch(typeof(ItemInstance), nameof(ItemInstance.CloneWithMods), typeof(Il2CppReferenceArray<ItemModSO>))]
    [HarmonyPatch(typeof(ItemInstance), nameof(ItemInstance.Insert))]
    [HarmonyPatch(typeof(ItemInstance), nameof(ItemInstance.Insert_IgnoreMaxStack))]
    [HarmonyPatch(typeof(ItemInstance), nameof(ItemInstance.Merge))]
    [HarmonyPatch(typeof(ItemInstance), nameof(ItemInstance.Remove))]
    [HarmonyPatch(typeof(ItemInstance), nameof(ItemInstance.SameAs))]
    [HarmonyPatch(typeof(ItemInstance), nameof(ItemInstance.SplitOutStack))]
    public static void ItemInstance_Patches(ref ItemInstance __instance)
    {
        Fixes.UpdateItemStackSize(__instance);
    }


    [HarmonyPrefix]
    [HarmonyPatch(typeof(UI_EquippedSlot_Belt), nameof(UI_EquippedSlot_Belt.SetupUI))]
    public static void UI_EquippedSlot_Belt_SetupUI(ref UI_EquippedSlot_Belt __instance, ref ItemInstance data)
    {
        Fixes.UpdateWeaponCooldowns();
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(Inventory), nameof(Inventory.Sort))]
    public static void Inventory_Sort()
    {
        Fixes.UpdateInventoryStackSize();
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(Inventory), nameof(Inventory.InsertHelper))]
    public static void Inventory_SetSlotData(ref ItemInstance item)
    {
        if (!Plugin.IncreaseStackSize.Value || item == null) return;

        Fixes.UpdateItemStackSize(item);
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
    [HarmonyPatch(typeof(Player), nameof(Player.LateUpdate))]
    public static void Player_OnEnable(ref Player __instance)
    {
        if (!Plugin.IncreasePlayerMoveSpeed.Value) return;
        __instance.m_RuntimeData.m_MovementModifier = Plugin.PlayerMoveSpeedValue.Value;
        __instance.m_AnimHandler.Anim.speed = Plugin.PlayerMoveSpeedValue.Value;
    }


    [HarmonyPrefix]
    [HarmonyPatch(typeof(Customer), nameof(Customer.FixedUpdate))]
    public static void Customer_FixedUpdate(ref Customer __instance)
    {
        if (!Plugin.IncreaseCustomerMoveSpeed.Value) return;

        var newSpeed = __instance.Data.m_MovementSpeed * Plugin.CustomerMoveSpeedValue.Value;
        __instance.m_Agent.speed = newSpeed;
        __instance.m_Agent.maxSpeed = newSpeed;

        if (Plugin.IncreaseCustomerMoveSpeedAnimation.Value)
        {
            __instance.m_WalkSpeedAnimMultiplier = newSpeed;
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(UI_WeaponUpgrade), nameof(UI_WeaponUpgrade.ToggleClaimMode))]
    public static void UI_WeaponUpgrade_ToggleClaimMode(ref UI_WeaponUpgrade __instance)
    {
        if (!Plugin.InstantWeaponUpgrades.Value) return;
        __instance.ClaimEquipment();
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(ItemDrop), nameof(ItemDrop.Setup), typeof(ItemInstance), typeof(Vector3), typeof(float), typeof(float), typeof(float), typeof(float))]
    public static void ItemDropManager_PickupItem(ref ItemDrop __instance)
    {
        if (!Plugin.ItemDropMultiplier.Value) return;
        if (__instance == null || __instance.m_ItemInstance == null) return;
        __instance.m_ItemInstance.m_Stack = Mathf.RoundToInt(__instance.m_ItemInstance.m_Stack * Plugin.ItemDropMultiplierValue.Value);
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(BaseAttack), nameof(BaseAttack.HandleCollision))]
    public static void BaseAttack_HandleCollision(ref BaseAttack __instance, ref Collider collider)
    {
        if (!Plugin.OneHitDestructible.Value) return;
        if (__instance == null || collider == null) return;
        var prop = collider.GetComponent<Prop>();
        if (prop == null) return;

        const int maxIterations = 10;
        for (var i = 0; i < maxIterations; i++)
        {
            __instance.HandleHitDestructible(prop);
        }
    }

    [HarmonyFinalizer]
    [HarmonyPatch(typeof(BaseAttack), nameof(BaseAttack.HandleCollision))]
    public static Exception Finalizer()
    {
        return null;
    }

    [HarmonyPrefix]
    [HarmonyPostfix]
    [HarmonyPatch(typeof(BaseAttack), nameof(BaseAttack.Update))]
    [HarmonyPatch(typeof(BaseAttack), nameof(BaseAttack.FixedUpdate))]
    [HarmonyPatch(typeof(BaseAttack), nameof(BaseAttack.FixedUpdate_ChainAttack))]
    [HarmonyPatch(typeof(BaseAttack), nameof(BaseAttack.FixedUpdate_ActiveNestedAttacks))]
    public static void BaseAttack_Update(ref BaseAttack __instance)
    {
        if (!Plugin.RemoveChainAttackDelay.Value) return;
        if (!__instance._IsPlayer_k__BackingField) return;
        __instance.m_ActiveNestedAttInterval = 0f;
        __instance.m_CurActiveNestedAttInterval = 0f;
        __instance.m_ChainAttDelay = 0f;
        __instance.m_ActiveFXDelay = 0f;
        __instance.m_ActiveNestedAttDelay = 0f;
        __instance.m_CurrChainAttackDelay = 0f;
        __instance.m_RandAddActiveDelay = 0f;
        __instance.m_ActiveDelayTimer = 0f;
        __instance.m_CurActiveNestedAttDelay = 0f;
        __instance.m_ActiveFXDelayTimer = 0f;
        __instance.m_ActiveDelayTime = 0f;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(UI_BrewArea), nameof(UI_BrewArea.EnterBrew))]
    public static void UI_BrewArea_EnterBrew(ref UI_BrewArea __instance)
    {
        if (!Plugin.InstantBrew.Value) return;
        Utils.FastForwardBrewCraft(__instance.m_BrewConfirmationData);
        __instance.SwitchState(UI_BrewArea.Stage.Claim);
    }
    
    [HarmonyPrefix]
    [HarmonyPatch(typeof(UI_BrewArea), nameof(UI_BrewArea.Show))]
    public static void UI_BrewArea_Show(ref UI_BrewArea __instance)
    {
        if (!Plugin.InstantBrew.Value) return;
        Utils.FastForwardBrewCraft(__instance.m_BrewConfirmationData);
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(UI_MainMenu), nameof(UI_MainMenu.HandleDependenciesReady))]
    public static void UI_MainMenu_Awake(ref UI_MainMenu __instance)
    {
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
        Utils.WriteLog($"Player.GetMaxHP: Player max HP: {originalHP} -> {newHP} ({Plugin.ModifyPlayerMaxHpMultiplier.Value}x)");
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
        Screen.SetResolution(Display.main.systemWidth, Display.main.systemHeight, FullScreenMode.FullScreenWindow, Fixes.MaxRefresh);
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
            Utils.WriteLog($"Main display resolution not detected; added {myResData.m_Width}x{myResData.m_Height} to resolution list");
        }

        UI_GameplayOptions.ResolutionDatas = resDatas.ToArray();
    }


#if DEBUG
    [HarmonyPostfix]
    [HarmonyPatch(typeof(CurrencyManager), nameof(CurrencyManager.CanAfford), typeof(int))]
    [HarmonyPatch(typeof(CurrencyManager), nameof(CurrencyManager.CanAfford), typeof(ShopInventory), typeof(int))]
    [HarmonyPatch(typeof(CurrencyManager), nameof(CurrencyManager.CanAfford), typeof(Cost), typeof(int), typeof(int))]
    [HarmonyPatch(typeof(CurrencyManager), nameof(CurrencyManager.CanAffordMaterials))]
    public static void CurrencyManager_CanAfford(ref bool __result)
    {
        __result = true;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(CurrencyManager), nameof(CurrencyManager.Spend), typeof(Cost), typeof(int), typeof(int))]
    [HarmonyPatch(typeof(CurrencyManager), nameof(CurrencyManager.Spend), typeof(ShopInventory), typeof(int))]
    [HarmonyPatch(typeof(CurrencyManager), nameof(CurrencyManager.SpendCoins))]
    public static bool CurrencyManager_Spend()
    {
        return false;
    }
#endif
}