using UnityEngine;
using DaggerfallWorkshop.Game;
using DaggerfallWorkshop.Game.Utility.ModSupport;
using DaggerfallWorkshop.Game.UserInterfaceWindows;
using DaggerfallWorkshop.Game.UserInterface;
using DaggerfallWorkshop.Utility.AssetInjection;
using System;
using System.IO;

public class WODDialogue : MonoBehaviour
{
    public static WODDialogue instance;
    public static Mod Mod { get; private set; }

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
    }
}
