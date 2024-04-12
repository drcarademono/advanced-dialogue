using System;
using System.Collections;
using System.Collections.Generic;
using DaggerfallWorkshop.Game.Utility.ModSupport;
using DaggerfallWorkshop.Game.UserInterface;


public class WODSaveDataHandler : IHasModSaveData
{
    private static WODSaveDataHandler instance;
    public static WODSaveDataHandler Instance
    {
        get
        {
            if (instance == null)
                instance = new WODSaveDataHandler();
            return instance;
        }
    }

    public Type SaveDataType => typeof(WODTalkWindow.WODTalkWindowSaveData);

    public object NewSaveData()
    {
        return new WODTalkWindow.WODTalkWindowSaveData
        {
            knownCaptions = new List<string> { "any advice?" }
        };
    }

    public object GetSaveData()
    {
        return new WODTalkWindow.WODTalkWindowSaveData
        {
            knownCaptions = WODTalkWindow.knownCaptions
        };
    }

    public void RestoreSaveData(object saveData)
    {
        var data = saveData as WODTalkWindow.WODTalkWindowSaveData;
        if (data != null)
        {
            WODTalkWindow.knownCaptions = data.knownCaptions;
        }
    }
}


