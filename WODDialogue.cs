using UnityEngine;
using DaggerfallWorkshop.Game;
using DaggerfallWorkshop.Game.Utility.ModSupport;
using DaggerfallWorkshop.Game.UserInterfaceWindows;
using DaggerfallWorkshop.Game.UserInterface;
using DaggerfallWorkshop.Utility.AssetInjection;
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

        // Assuming you want this always enabled for now, since there's no mod settings check.
        bool dialogueWindowEnabled = true; // This should be set based on your mod's requirements.

        if(dialogueWindowEnabled)
        {
            UIWindowFactory.RegisterCustomUIWindow(UIWindowType.Talk, typeof(WODTalkWindow));
        }
    }
}

