using UnityEngine;
using DaggerfallWorkshop.Game;
using DaggerfallConnect;
using DaggerfallConnect.Arena2;
using DaggerfallConnect.Utility;
using DaggerfallWorkshop;
using DaggerfallWorkshop.Utility;
using DaggerfallWorkshop.Game.Formulas;
using DaggerfallWorkshop.Game.Utility.ModSupport;
using DaggerfallWorkshop.Game.UserInterfaceWindows;
using DaggerfallWorkshop.Game.UserInterface;
using DaggerfallWorkshop.Utility.AssetInjection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Wenzil.Console;

public class ADDialogue : MonoBehaviour
{
    public static ADDialogue instance;
    public static Mod Mod { get; private set; }

    public static bool AD_Log = false;

    public static Mod CnCMod;

    public static bool CnCModEnabled;

    public static Dictionary<string, object> LocalizationKeys = new Dictionary<string, object>();

    public class DialogueListItem
    {
        public TalkManager.ListItem ListItem { get; set; }
        public Dictionary<string, object> DialogueData { get; set; }

        public DialogueListItem(TalkManager.ListItem listItem)
        {
            ListItem = listItem;
            DialogueData = new Dictionary<string, object>();
        }
    }

    [Invoke(StateManager.StateTypes.Start, 0)]
    public static void Init(InitParams initParams)
    {
        Mod = initParams.Mod;
        var go = new GameObject(Mod.Title);
        instance = go.AddComponent<ADDialogue>();

        // Register custom UI window if needed
        bool dialogueWindowEnabled = true; // Adjust based on mod settings or conditions
        if(dialogueWindowEnabled)
        {
            UIWindowFactory.RegisterCustomUIWindow(UIWindowType.Talk, typeof(ADTalkWindow));
        }

        // Set the singleton save data handler as the mod's save data interface
        Mod.SaveDataInterface = ADSaveDataHandler.Instance; // Set up save data handler

        instance.LoadLocalizationKeys();
        instance.LoadDialogueTopicsFromCSV();

        CnCMod = ModManager.Instance.GetModFromGUID("7975b109-1381-485b-bdfd-8d076bb5d0c9");
        if (CnCMod != null && CnCMod.Enabled)
        {
            CnCModEnabled = true;
            Debug.Log("AD: Climates & Calories Mod is active");
        }

        ConsoleCommandsDatabase.RegisterCommand("AD_Log", "Toggles dialogue system logging for filter data and condition evaluations.", "", ToggleADLogging);
        ConsoleCommandsDatabase.RegisterCommand("AD_Reload", "Reloads all dialogue data and localization keys.", "", ReloadDialogueData);
    }

    public void LoadLocalizationKeys()
    {
        string filePath = "AD_Keys.csv";
        if (ModManager.Instance.TryGetAsset<TextAsset>(filePath, false, out TextAsset csvAsset))
        {
            using (StringReader reader = new StringReader(csvAsset.text))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    string[] values = line.Split('\t'); // Using tab delimiter for TSV
                    if (values.Length < 2) continue;

                    string key = values[0];
                    string text = values[1];

                    // If the value contains '|', split into a list; otherwise, store it as a single string
                    if (text.Contains("|"))
                    {
                        LocalizationKeys[key] = text.Split('|').ToList();
                    }
                    else
                    {
                        LocalizationKeys[key] = text;
                    }
                }
            }
            Debug.Log("Localization keys loaded successfully.");
        }
        else
        {
            Debug.LogError("Localization keys file not found.");
        }
    }

    public List<DialogueListItem> dialogueListItems = new List<DialogueListItem>();

    public void LoadDialogueTopicsFromCSV()
    {
        string filePath = "Dialogue.csv";
        if (ModManager.Instance.TryGetAsset<TextAsset>(filePath, false, out TextAsset csvAsset))
        {
            using (StringReader reader = new StringReader(csvAsset.text))
            {
                string line = reader.ReadLine(); // Skip header
                int lineNumber = 1;
                while ((line = reader.ReadLine()) != null)
                {
                    string[] values = line.Split('\t');
                    TalkManager.ListItem item = new TalkManager.ListItem
                    {
                        type = TalkManager.ListItemType.Item,
                        caption = values[1],
                        questionType = TalkManager.QuestionType.OrganizationInfo,
                        index = lineNumber
                    };
                    DialogueListItem dialogueItem = new DialogueListItem(item);
                    dialogueItem.DialogueData.Add("DialogueIndex", lineNumber);
                    dialogueItem.DialogueData.Add("Answer", values[2]); // Store raw answer text
                    dialogueItem.DialogueData.Add("AddCaption", values[3]);
                    dialogueItem.DialogueData.Add("C1_Variable", values[4]);
                    dialogueItem.DialogueData.Add("C1_Comparison", values[5]);
                    dialogueItem.DialogueData.Add("C1_Value", values[6]);
                    dialogueItem.DialogueData.Add("C2_Variable", values[7]);
                    dialogueItem.DialogueData.Add("C2_Comparison", values[8]);
                    dialogueItem.DialogueData.Add("C2_Value", values[9]);
                    dialogueItem.DialogueData.Add("C3_Variable", values[10]);
                    dialogueItem.DialogueData.Add("C3_Comparison", values[11]);
                    dialogueItem.DialogueData.Add("C3_Value", values[12]);

                    dialogueListItems.Add(dialogueItem);
                    lineNumber++;
                }
            }
            Debug.Log("Dialogue topics loaded successfully.");
        }
        else
        {
            Debug.LogError("CSV asset not found.");
        }
    }

    public static string ToggleADLogging(string[] args)
    {
        // Toggle the logging state
        AD_Log = !AD_Log;

        // Return the current state as a string to be displayed in the console
        return $"Advanced Dialogue logging is now {(AD_Log ? "enabled" : "disabled")}";
    }

    public static string ReloadDialogueData(string[] args)
    {

        // Clear the custom topics
        if (instance.dialogueListItems != null)
        {
            instance.dialogueListItems.Clear();
        }
        // Reload dialogue data
        //instance.LoadLocalizationKeys();
        instance.LoadDialogueTopicsFromCSV();

        // Return the current state as a string to be displayed in the console
        return "Advanced Dialogue data has been reloaded.}";
    }

    /// <summary>
    /// Calculates the price of ale based on region, tavern quality, and holiday status.
    /// </summary>
    public static int CalculateAlePrice()
    {
        int alePrice = 1; // Default vanilla ale price

        if (CnCModEnabled) // Ensure Climates & Calories mod is active
        {
            // Get tavern quality
            int tavernQuality = GameManager.Instance.PlayerEnterExit.Interior.BuildingData.Quality;

            // Determine region type
            PlayerGPS playerGPS = GameManager.Instance.PlayerGPS;
            string regionType;

            switch (playerGPS.CurrentRegionIndex)
            {
                case Regions.Anticlere:
                case Regions.Betony:
                case Regions.Bhoraine:
                case Regions.Daenia:
                case Regions.Daggerfall:
                case Regions.Dwynnen:
                case Regions.Glenpoint:
                case Regions.GlenumbraMoors:
                case Regions.IlessanHills:
                case Regions.Kambria:
                case Regions.Northmoor:
                case Regions.Phrygias:
                case Regions.Shalgora:
                case Regions.Tulune:
                case Regions.Urvaius:
                case Regions.Ykalon:
                    regionType = "n"; // Northern region
                    break;

                case Regions.Alcaire:
                case Regions.Balfiera:
                case Regions.Gavaudon:
                case Regions.Koegria:
                case Regions.Menevia:
                case Regions.Wayrest:
                    regionType = "ne"; // Northeastern region
                    break;

                case Regions.Kozanset:
                case Regions.Lainlyn:
                case Regions.Mournoth:
                case Regions.Satakalaam:
                case Regions.Totambu:
                    regionType = "se"; // Southeastern region
                    break;

                case Regions.AbibonGora:
                case Regions.AlikrDesert:
                case Regions.Antipyllos:
                case Regions.Ayasofya:
                case Regions.Bergama:
                case Regions.Cybiades:
                case Regions.DakFron:
                case Regions.Dragontail:
                case Regions.Ephesus:
                case Regions.Kairou:
                case Regions.Myrkwasa:
                case Regions.Pothago:
                case Regions.Santaki:
                case Regions.Sentinel:
                case Regions.Tigonus:
                    regionType = "s"; // Southern region
                    break;

                case Regions.Orsinium:
                case Regions.Wrothgarian:
                    regionType = "wo"; // Orsinium & Wrothgarian
                    break;

                default:
                    // Fallback based on climate
                    switch (playerGPS.CurrentClimateIndex)
                    {
                        case (int)MapsFile.Climates.Desert2:
                        case (int)MapsFile.Climates.Desert:
                        case (int)MapsFile.Climates.Subtropical:
                            regionType = "s";
                            break;
                        case (int)MapsFile.Climates.Rainforest:
                        case (int)MapsFile.Climates.Swamp:
                            regionType = "se";
                            break;
                        case (int)MapsFile.Climates.Woodlands:
                        case (int)MapsFile.Climates.HauntedWoodlands:
                        case (int)MapsFile.Climates.MountainWoods:
                        case (int)MapsFile.Climates.Mountain:
                            regionType = "n";
                            break;
                        default:
                            regionType = "n";
                            break;
                    }
                    break;
            }

            // Set base ale price based on region and quality
            int baseAlePrice = 3; // Default base price
            switch (regionType)
            {
                case "n":
                    if (tavernQuality < 5)
                        baseAlePrice = 3;
                    else if (tavernQuality < 13)
                        baseAlePrice = 4;
                    else
                        baseAlePrice = 9;
                    break;
                case "ne":
                    if (tavernQuality < 5)
                        baseAlePrice = 3;
                    else if (tavernQuality < 13)
                        baseAlePrice = 4;
                    else
                        baseAlePrice = 9;
                    break;
                case "s":
                    if (tavernQuality < 5)
                        baseAlePrice = 2;
                    else if (tavernQuality < 13)
                        baseAlePrice = 4;
                    else
                        baseAlePrice = 9;
                    break;
                case "se":
                    if (tavernQuality < 5)
                        baseAlePrice = 3;
                    else if (tavernQuality < 13)
                        baseAlePrice = 4;
                    else
                        baseAlePrice = 9;
                    break;
                case "wo":
                    if (tavernQuality < 5)
                        baseAlePrice = 2;
                    else if (tavernQuality < 13)
                        baseAlePrice = 4;
                    else
                        baseAlePrice = 9;
                    break;
            }

            // Check for holiday adjustments
            uint gameMinutes = DaggerfallUnity.Instance.WorldTime.DaggerfallDateTime.ToClassicDaggerfallTime();
            int holidayID = FormulaHelper.GetHolidayId(gameMinutes, playerGPS.CurrentRegionIndex);

            if (holidayID == (int)DFLocation.Holidays.Harvest_End || holidayID == (int)DFLocation.Holidays.New_Life)
            {
                alePrice = 0; // Free ale during holidays
            }
            else
            {
                alePrice = baseAlePrice;
            }
        }

        return alePrice;
    }

    class Regions
    {
        public const int AlikrDesert = 0;
        public const int Dragontail = 1;
        public const int GlenpointF = 2;
        public const int DaggerfallBluffs = 3;
        public const int Yeorth = 4;
        public const int Dwynnen = 5;
        public const int Ravennian = 6;
        public const int Devilrock = 7;
        public const int Malekna = 8;
        public const int Balfiera = 9;
        public const int Bantha = 10;
        public const int DakFron = 11;
        public const int WesternIsles = 12;
        public const int Tamaril = 13;
        public const int LainlynC = 14;
        public const int Bjoulae = 15;
        public const int Wrothgarian = 16;
        public const int Daggerfall = 17;
        public const int Glenpoint = 18;
        public const int Betony = 19;
        public const int Sentinel = 20;
        public const int Anticlere = 21;
        public const int Lainlyn = 22;
        public const int Wayrest = 23;
        public const int GenTemHighRock = 24;
        public const int GenRaiHammerfell = 25;
        public const int Orsinium = 26;
        public const int SkeffingtonW = 27;
        public const int HammerfellBay = 28;
        public const int HammerfellCoast = 29;
        public const int HighRockBay = 30;
        public const int HighRockSea = 31;
        public const int Northmoor = 32;
        public const int Menevia = 33;
        public const int Alcaire = 34;
        public const int Koegria = 35;
        public const int Bhoraine = 36;
        public const int Kambria = 37;
        public const int Phrygias = 38;
        public const int Urvaius = 39;
        public const int Ykalon = 40;
        public const int Daenia = 41;
        public const int Shalgora = 42;
        public const int AbibonGora = 43;
        public const int Kairou = 44;
        public const int Pothago = 45;
        public const int Myrkwasa = 46;
        public const int Ayasofya = 47;
        public const int Tigonus = 48;
        public const int Kozanset = 49;
        public const int Satakalaam = 50;
        public const int Totambu = 51;
        public const int Mournoth = 52;
        public const int Ephesus = 53;
        public const int Santaki = 54;
        public const int Antipyllos = 55;
        public const int Bergama = 56;
        public const int Gavaudon = 57;
        public const int Tulune = 58;
        public const int GlenumbraMoors = 59;
        public const int IlessanHills = 60;
        public const int Cybiades = 61;
    }
}
