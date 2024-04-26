using System;
using System.Collections;
using System.Collections.Generic;
using DaggerfallWorkshop.Game.Utility.ModSupport;
using DaggerfallWorkshop.Game.UserInterface;


public class ADSaveDataHandler : IHasModSaveData
{
    private static ADSaveDataHandler instance;
    public static ADSaveDataHandler Instance
    {
        get
        {
            if (instance == null)
                instance = new ADSaveDataHandler();
            return instance;
        }
    }

    public Type SaveDataType => typeof(ADTalkWindow.ADTalkWindowSaveData);

    public object NewSaveData()
    {
        return new ADTalkWindow.ADTalkWindowSaveData
        {
            knownCaptions = new List<string> { "any advice?" },
            numAnswersGivenDialogue = new Dictionary<string, (int numAnswers, int dayOfYear)>()
        };
    }

    public object GetSaveData()
    {
        return new ADTalkWindow.ADTalkWindowSaveData
        {
            knownCaptions = ADTalkWindow.knownCaptions,
            numAnswersGivenDialogue = ADTalkWindow.numAnswersGivenDialogue // Ensure this static field exists and is updated properly in your ADTalkWindow class
        };
    }

    public void RestoreSaveData(object saveData)
    {
        var data = saveData as ADTalkWindow.ADTalkWindowSaveData;
        if (data != null)
        {
            ADTalkWindow.knownCaptions = data.knownCaptions;
            ADTalkWindow.numAnswersGivenDialogue = data.numAnswersGivenDialogue; // Restore this data back into the ADTalkWindow class
        }
    }
}



