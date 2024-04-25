using UnityEngine;
using DaggerfallWorkshop.Game;
using DaggerfallWorkshop.Game.Utility.ModSupport;
using DaggerfallWorkshop.Game.UserInterfaceWindows;
using DaggerfallWorkshop.Game.UserInterface;
using DaggerfallWorkshop.Utility.AssetInjection;
using System;
using System.IO;
using Wenzil.Console;

public class WODDialogue : MonoBehaviour
{
    public static WODDialogue instance;
    public static Mod Mod { get; private set; }

    public static bool AD_Log = false;

    [Invoke(StateManager.StateTypes.Start, 0)]
    public static void Init(InitParams initParams)
    {
        Mod = initParams.Mod;
        var go = new GameObject(Mod.Title);
        instance = go.AddComponent<WODDialogue>();

        // Register custom UI window if needed
        bool dialogueWindowEnabled = true; // Adjust based on mod settings or conditions
        if(dialogueWindowEnabled)
        {
            UIWindowFactory.RegisterCustomUIWindow(UIWindowType.Talk, typeof(WODTalkWindow));
        }

        // Set the singleton save data handler as the mod's save data interface
        Mod.SaveDataInterface = WODSaveDataHandler.Instance; // Set up save data handler

        ConsoleCommandsDatabase.RegisterCommand("AD_Log", "Toggles dialogue system logging for filter data and condition evaluations.", "", ToggleADLogging);
    }

    public static string ToggleADLogging(string[] args)
    {
        // Toggle the logging state
        AD_Log = !AD_Log;

        // Return the current state as a string to be displayed in the console
        return $"Advanced Dialogue logging is now {(AD_Log ? "enabled" : "disabled")}";
    }

}
