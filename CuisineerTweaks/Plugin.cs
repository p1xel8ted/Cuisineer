using System.Diagnostics;
using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using Il2CppSystem.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CuisineerTweaks;

[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
public class Plugin : BasePlugin
{
    private const string PluginGuid = "p1xel8ted.cuisineer.cuisineertweaks";
    private const string PluginName = "Cuisineer Tweaks (IL2CPP)";
    internal const string PluginVersion = "0.1.6";
    internal static ManualLogSource Logger { get; private set; }

    private static ConfigEntry<bool> CorrectMainMenuAspect { get; set; }
    private static ConfigEntry<bool> CorrectFixedUpdateRate { get; set; }
    private static ConfigEntry<bool> UseRefreshRateForFixedUpdateRate { get; set; }
    internal static ConfigEntry<bool> IncreaseStackSize { get; private set; }
    internal static ConfigEntry<int> IncreaseStackSizeValue { get; private set; }
    internal static ConfigEntry<bool> InstantText { get; private set; }
    private static ConfigEntry<bool> EnableAutoSave { get; set; }

    internal static ConfigEntry<bool> LoadToSaveMenu { get; private set; }
    internal static ConfigEntry<bool> AutoLoadSpecifiedSave { get; private set; }
    internal static ConfigEntry<int> AutoLoadSpecifiedSaveSlot { get; private set; }

    private static ConfigEntry<bool> PauseTimeWhenViewingInventories { get; set; }
    internal static ConfigEntry<bool> UnlockBothModSlots { get; private set; }
    internal static ConfigEntry<bool> InstantRestaurantUpgrades { get; private set; }
    internal static ConfigEntry<bool> InstantBrew { get; private set; }
    internal static ConfigEntry<bool> ModifyPlayerMaxHp { get; private set; }
    internal static ConfigEntry<float> ModifyPlayerMaxHpMultiplier { get; private set; }
    internal static ConfigEntry<bool> RegenPlayerHp { get; private set; }
    internal static ConfigEntry<int> RegenPlayerHpAmount { get; private set; }
    internal static ConfigEntry<float> RegenPlayerHpTick { get; private set; }
    internal static ConfigEntry<bool> RegenPlayerHpShowFloatingText { get; private set; }

    private static ConfigEntry<bool> AutomaticPayment { get; set; }

    internal static ConfigEntry<bool> IncreaseCustomerMoveSpeed { get; private set; }
    internal static ConfigEntry<bool> IncreaseCustomerMoveSpeedAnimation { get; private set; }
    internal static ConfigEntry<float> CustomerMoveSpeedValue { get; private set; }

    internal static ConfigEntry<bool> IncreasePlayerMoveSpeed { get; private set; }
    internal static ConfigEntry<float> PlayerMoveSpeedValue { get; private set; }
    
    private static ConfigEntry<int> AutoSaveFrequency { get; set; }
    internal static int MaxRefresh => Screen.resolutions.Max(a => a.refreshRate);

    private static int TimeScale => FindLowestFrameRateMultipleAboveFifty(MaxRefresh);

    internal static Dictionary<BaseItemSO, int> OriginalItemStackSizes { get; } = new();

    private static int FindLowestFrameRateMultipleAboveFifty(int originalRate)
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

    private void InitConfig()
    {
        // Group 1: Performance Settings
        CorrectFixedUpdateRate = Config.Bind("01. Performance", "CorrectFixedUpdateRate", true,
            new ConfigDescription("Adjusts the fixed update rate to minimum amount to reduce camera judder based on your refresh rate."));
        UseRefreshRateForFixedUpdateRate = Config.Bind("01. Performance", "UseRefreshRateForFixedUpdateRate", false,
            new ConfigDescription("Sets the fixed update rate based on the monitor's refresh rate for smoother gameplay. If you're playing on a potato, this may have performance impacts."));
        CorrectMainMenuAspect = Config.Bind("01. Performance", "CorrectMainMenuAspect", true,
            new ConfigDescription("Adjusts the main menu images to fit the screen. Will only be applied on aspect ratios wider than 16:9."));

        // Group 2: User Interface Enhancements
        InstantText = Config.Bind("02. User Interface", "InstantDialogueText", true,
            new ConfigDescription("Dialogue text is instantly displayed, skipping the typewriter effect."));

        // Group 3: Inventory Management
        IncreaseStackSize = Config.Bind("03. Inventory", "IncreaseStackSize", true,
            new ConfigDescription("Enables increasing the item stack size, allowing for more efficient inventory management."));
        IncreaseStackSizeValue = Config.Bind("03. Inventory", "IncreaseStackSizeValue", 999,
            new ConfigDescription("Determines the maximum number of items in a single stack.", new AcceptableValueRange<int>(1, 999)));

        // Group 4: Save System Customization
        EnableAutoSave = Config.Bind("04. Save System", "EnableAutoSave", true,
            new ConfigDescription("Activates the auto-save feature, automatically saving game progress at set intervals."));
        AutoSaveFrequency = Config.Bind("04. Save System", "AutoSaveFrequency", 300,
            new ConfigDescription("Sets the frequency of auto-saves in seconds.", new AcceptableValueRange<int>(30, 600)));

        LoadToSaveMenu = Config.Bind("04. Save System", "LoadToSaveMenu", true,
            new ConfigDescription("Changes the initial game load screen to the save menu, streamlining the game start."));
        AutoLoadSpecifiedSave = Config.Bind("04. Save System", "AutoLoadSpecifiedSave", false,
            new ConfigDescription("Automatically loads a pre-selected save slot when starting the game."));
        AutoLoadSpecifiedSaveSlot = Config.Bind("04. Save System", "AutoLoadSpecifiedSaveSlot", 1,
            new ConfigDescription("Determines which save slot to auto-load.", new AcceptableValueRange<int>(1, 5)));

        // Group 5: Gameplay Enhancements
        PauseTimeWhenViewingInventories = Config.Bind("05. Gameplay", "PauseTimeWhenViewingInventories", true,
            new ConfigDescription("Pauses the game when accessing inventory screens, excluding cooking interfaces."));
        UnlockBothModSlots = Config.Bind("05. Gameplay", "UnlockBothModSlots", false,
            new ConfigDescription("Both mod slots on gears/weapons remain unlocked."));
        InstantRestaurantUpgrades = Config.Bind("05. Gameplay", "InstantRestaurantUpgrades", false,
            new ConfigDescription("Allows for immediate upgrades to the restaurant, bypassing build times."));
        InstantBrew = Config.Bind("05. Gameplay", "InstantBrew", false,
            new ConfigDescription("Enables instant brewing processes, eliminating the usual brewing duration."));

        // Group 6: Player Health Customization
        ModifyPlayerMaxHp = Config.Bind("06. Player Health", "ModifyPlayerMaxHp", false,
            new ConfigDescription("Enables the modification of the player's maximum health."));
        ModifyPlayerMaxHpMultiplier = Config.Bind("06. Player Health", "ModifyPlayerMaxHpMultiplier", 1.25f,
            new ConfigDescription("Sets the multiplier for the player's maximum health. 1.25 would be 25% more health.", new AcceptableValueList<float>(0.5f, 1.25f, 1.5f, 1.75f, 2f)));
        RegenPlayerHp = Config.Bind("06. Player Health", "RegenPlayerHp", true,
            new ConfigDescription("Activates health regeneration for the player, gradually restoring health over time."));
        RegenPlayerHpAmount = Config.Bind("06. Player Health", "RegenPlayerHpAmount", 1,
            new ConfigDescription("Specifies the amount of health regenerated per tick.", new AcceptableValueList<int>(1, 2, 3, 4, 5)));
        RegenPlayerHpTick = Config.Bind("06. Player Health", "RegenPlayerHpTick", 3f,
            new ConfigDescription("Determines the time interval in seconds for each health regeneration tick.", new AcceptableValueList<float>(1f, 2f, 3f, 4f, 5f, 6f, 7f, 8f, 9f, 10f)));
        RegenPlayerHpShowFloatingText = Config.Bind("06. Player Health", "RegenPlayerHpShowFloatingText", true,
            new ConfigDescription("Displays a floating text notification during health regeneration."));

        //Group 7: Player Movement
        IncreasePlayerMoveSpeed = Config.Bind("07. Player Movement", "IncreasePlayerMoveSpeed", false,
            new ConfigDescription("Increases the speed of the player."));
        PlayerMoveSpeedValue = Config.Bind("07. Player Movement", "PlayerMoveSpeedValue", 1.25f, 
            new ConfigDescription("Determines the speed of the player. Good luck playing at 5.", new AcceptableValueRange<float>(1.10f, 5f)));
        
        // Group 8: Restaurant Management
        AutomaticPayment = Config.Bind("08. Restaurant", "AutomaticPayment", false,
            new ConfigDescription("Automatically accept payment for customer."));
        IncreaseCustomerMoveSpeed = Config.Bind("08. Restaurant", "IncreaseCustomerMoveSpeed", false,
            new ConfigDescription("Increases the speed of customers."));
        IncreaseCustomerMoveSpeedAnimation = Config.Bind("08. Restaurant", "IncreaseCustomerMoveSpeedAnimation", false,
            new ConfigDescription("Increases the speed of customers move animation. Test it out, see how you go."));
        CustomerMoveSpeedValue = Config.Bind("08. Restaurant", "CustomerMoveSpeedValue", 1.25f,
            new ConfigDescription("Determines the speed of customers. Setting too high will cause momentum issues.", new AcceptableValueRange<float>(1.10f, 5f)));
    }

    private static Plugin Instance { get; set; }

    public override void Load()
    {
        Instance = this;
        Logger = Log;
        Config.ConfigReloaded += (_, _) =>
        {
            InitConfig();
            Logger.LogInfo("Reloaded configuration.");
        };
        InitConfig();
        SceneManager.sceneLoaded += (UnityAction<Scene, LoadSceneMode>) OnSceneLoaded;
        Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), PluginGuid);
        Logger.LogInfo($"Plugin {PluginGuid} is loaded!");
        AddComponent<UnityEvents>();
    }

    private static void OnSceneLoaded(Scene arg0, LoadSceneMode arg1)
    {
        Logger.LogInfo($"Scene loaded: {arg0.name}: Running Fixes");
        Screen.SetResolution(Display.main.systemWidth, Display.main.systemHeight, FullScreenMode.FullScreenWindow, MaxRefresh);
        Logger.LogInfo($"Set resolution to {Screen.currentResolution}");

        if (Application.targetFrameRate != MaxRefresh)
        {
            Application.targetFrameRate = MaxRefresh;
            Logger.LogInfo($"Set targetFrameRate to {Application.targetFrameRate}.");
        }
        else
        {
            Logger.LogInfo($"targetFrameRate is already {Application.targetFrameRate}. No update necessary.");
        }

        if (CorrectFixedUpdateRate.Value)
        {
            var originalTime = Time.fixedDeltaTime;
            var scale = UseRefreshRateForFixedUpdateRate.Value ? MaxRefresh : TimeScale;
            var newValue = 1f / scale;
            if (Mathf.Approximately(newValue, originalTime))
            {
                Logger.LogInfo($"fixedDeltaTime is already {newValue} ({scale}fps). No update necessary.");
                return;
            }

            Time.fixedDeltaTime = newValue;
            Logger.LogInfo($"Set fixedDeltaTime to {newValue} ({scale}fps). Original is {originalTime} ({Mathf.Round(1f / originalTime)}fps).");
        }


        UpdateAutoSave();
        UpdateInventoryStackSize();

        if (arg0.name.Equals("MainMenuScene"))
        {
            UpdateMainMenu();
        }

        Cheats.Customer.AutoCollectPayment = AutomaticPayment.Value;
    }

    private static void UpdateMainMenu()
    {
        if (!CorrectMainMenuAspect.Value) return;
        const float baseAspect = 16f / 9f;
        var currentAspect = Display.main.systemWidth / (float) Display.main.systemHeight;
        if (currentAspect <= baseAspect) return;

        var positiveScaleFactor = currentAspect / baseAspect;
        var negativeScaleFactor = 1f / positiveScaleFactor;
        Logger.LogInfo($"Current aspect ratio ({currentAspect}) is greater than base aspect ratio ({baseAspect}). Resizing UI elements.");

        ScaleElement("UI_MainMenuCanvas/Mask", true);
        ScaleElement("UI_MainMenuCanvas/Mask/UI_MainMenu/Container", false, positiveScaleFactor);
        ScaleElement("UI_MainMenuCanvas/Mask/UI_MainMenu/Container/Centre/MC", false, negativeScaleFactor);
        ScaleElement("UI_MainMenuCanvas/Mask/UI_MainMenu/Container/Press any key text", false, negativeScaleFactor);
        ScaleElement("UI_MainMenuCanvas/Mask/UI_MainMenu/Container/CuisineerLogo", false, negativeScaleFactor);
        ScaleElement("UI_MainMenuCanvas/Mask/UI_MainMenu/Container/ButtonsBacking", false, negativeScaleFactor);
        ScaleElement("UI_MainMenuCanvas/Mask/UI_MainMenu/Container/UI_SaveSlotDetail", false, negativeScaleFactor);
    }

    private static void ScaleElement(string path, bool maskCheck, float scaleFactor = 1f)
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


    private static void UpdateInventoryStackSize()
    {
        if (InventoryManager.m_Instance == null) return;

        var s = new Stopwatch();
        s.Start();
        Logger.LogInfo("Updating Inventory Stack Sizes");
        var count = 0;
        foreach (var instanceMInventory in InventoryManager.Instance.m_Inventories)
        {
            if (instanceMInventory.Value == null) continue;

            foreach (var valueMSlot in instanceMInventory.Value.m_Slots)
            {
                if (valueMSlot?.ItemSO == null) continue;

                if (!OriginalItemStackSizes.TryGetValue(valueMSlot.ItemSO, out var maxStack))
                {
                    maxStack = valueMSlot.ItemSO.m_MaxStack;
                    OriginalItemStackSizes[valueMSlot.ItemSO] = maxStack;
                }

                if (IncreaseStackSizeValue.Value <= maxStack)
                {
                    Logger.LogInfo($"Item {valueMSlot.ItemSO.name} already has a stack size of {maxStack}.");
                    continue;
                }

                valueMSlot.ItemSO.m_MaxStack = IncreaseStackSizeValue.Value;
                count++;
            }
        }

        s.Stop();
        Logger.LogInfo($"Updated {count} item's stack sizes in {s.ElapsedMilliseconds}ms, {s.ElapsedTicks} ticks");
    }


    private static void UpdateAutoSave()
    {
        if (CuisineerSaveManager.m_Instance == null) return;
        Logger.LogInfo("Initiating AutoSave");
        CuisineerSaveManager.Instance.m_AutoSave = EnableAutoSave.Value;
        CuisineerSaveManager.Instance.m_AutoSaveFrequency = AutoSaveFrequency.Value;
        Logger.LogInfo($"AutoSave: {CuisineerSaveManager.Instance.m_AutoSave} ({CuisineerSaveManager.Instance.m_AutoSaveFrequency / 60f} minutes)");
    }


    public class UnityEvents : MonoBehaviour
    {
        private void Awake()
        {
            Logger.LogInfo("UnityEvents Awake");
        }

        private void Update()
        {
            if (CuisineerInputWrapper.GetGameActionKeyUp(BattlebrewGameAction.DebugR))
            {
                Instance.Config.Reload();
            }

            if (CuisineerSaveManager.m_Instance != null && CuisineerInputWrapper.GetGameActionKeyUp(BattlebrewGameAction.DebugK))
            {
                CuisineerSaveManager.SaveCurrent();
                Logger.LogInfo("Saved current game.");
            }

            if (TimeManager.m_Instance == null || !PauseTimeWhenViewingInventories.Value) return;
            TimeManager.ToggleTimePause(UI_InventoryViewBase.AnyInventoryActive);
        }
    }
}