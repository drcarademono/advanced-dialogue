// Project:         Daggerfall Unity
// Copyright:       Copyright (C) 2009-2023 Daggerfall Workshop
// Web Site:        http://www.dfworkshop.net
// License:         MIT License (http://www.opensource.org/licenses/mit-license.php)
// Source Code:     https://github.com/Interkarma/daggerfall-unity
// Original Author: Michael Rauter (Nystul)
// Contributors:    Numidium, TheExceptionist
//
// Notes:
//

using UnityEngine;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using DaggerfallWorkshop.Game.Entity;
using DaggerfallWorkshop.Game.Formulas;
using DaggerfallWorkshop.Game.Items;
using DaggerfallWorkshop.Game.Player;
using DaggerfallWorkshop.Game.Questing;
using DaggerfallWorkshop.Game.UserInterface;
using DaggerfallWorkshop.Game.UserInterfaceWindows;
using DaggerfallWorkshop.Game.Utility;
using DaggerfallWorkshop.Game.Utility.ModSupport;
using DaggerfallWorkshop.Localization;
using DaggerfallConnect;
using DaggerfallConnect.Arena2;
using DaggerfallConnect.Utility;
using DaggerfallWorkshop;
using DaggerfallWorkshop.Utility;
using DaggerfallWorkshop.Utility.AssetInjection;
using Wenzil.Console;

namespace DaggerfallWorkshop.Game.UserInterface
{
    public class ADTalkWindow : DaggerfallTalkWindow
    {

        public static List<string> knownCaptions { get; set; } = new List<string>();
        private string[] repeatedResponses;
        private string[] exceededMaxResponses;

        [Serializable]
        public class ADTalkWindowSaveData
        {
            public List<string> knownCaptions = new List<string>();
            public Dictionary<string, (int numAnswers, int dayOfYear)> numAnswersGivenDialogue = new Dictionary<string, (int numAnswers, int dayOfYear)>();
        }

        public Dictionary<string, object> filterVariables = new Dictionary<string, object>();

        protected HashSet<string> respondedCaptions = new HashSet<string>();

        protected int reactionToPlayer;
        protected int reactionToPlayer_0_1_2;

        public int totalReactionToPlayer;
        public int maxNumAnswersNpcGivesDialogue;

        /// From TalkManager, add reaction modifiers when using Etiquette
        /// and Streetwise in relation to social groups.
        readonly short[] etiquetteReactionMods = { -10, 5, 10, 15, -15 };
        readonly short[] streetwiseReactionMods = { 10, 5, -10, -15, 15 };

        int[] toneReactionForTalkSession = { 0, 0, 0 };
        int lastToneIndex = -1;

        // type of npc talk partners for a conversion
        public enum NPCType
        {
            Static,
            Mobile,
            Unset
        }
        MobilePersonNPC lastTargetMobileNPC = null; // the last mobile npc talk partner
        StaticNPC lastTargetStaticNPC = null; // the last static npc talk partner
        NPCType currentNPCType = NPCType.Unset; // current type of npc talk partner

        // Dictionary to keep track of answers given for each NPC by name
        public static Dictionary<string, (int numAnswers, int dayOfYear)> numAnswersGivenDialogue = new Dictionary<string, (int numAnswers, int dayOfYear)>();

        // To hold the current number of answers given during this dialogue
        public int currentNumAnswersGivenDialogue = 0;

        private bool isFirstAdviceRequest = true;
        private bool isFirstWhoAreYouRequest = true;
        private bool isFirstBuyAleRequest = true;
        private bool isFirstBuyBeerRequest = true;

        protected const string talkWindowImgName    = "TALK01I0.IMG";
        protected const string talkCategoriesImgName = "TALK02I0.IMG";
        protected const string highlightedOptionsImgName = "TALK03I0.IMG";

        protected const string portraitImgName = "TFAC00I0.RCI";
        protected const string facesImgName = "FACES.CIF";

        protected const string greenArrowsTextureName = "INVE06I0.IMG";       // Green up/down arrows when more items available
        protected const string redArrowsTextureName = "INVE07I0.IMG";         // Red up/down arrows when no more items available

        //const int maxNumTopicsShown = 13; // max number of items displayed in scrolling area of topics list
        const int maxNumCharactersOfTopicShown = 20; // max number of characters of a topic displayed in scrolling area of topics list

        //const int maxNumAnswerLinesShown = 15; // max number of lines displayed in scrolling area of answers

        protected Color textcolorPlayerSays = new Color(0.698f, 0.812f, 1.0f);

        //Color textcolorQuestionHighlighted = new Color(0.8f, 0.9f, 1.0f);
        protected Color textcolorHighlighted = Color.white;

        protected Color textcolorQuestionBackgroundModernConversationStyle = new Color(0.3f, 0.35f, 0.43f); // new Color(0.23f, 0.27f, 0.33f);
        protected Color textcolorAnswerBackgroundModernConversationStyle = new Color(0.32f, 0.31f, 0.06f); //  default text r: 243 (0.95f), g: 239 (0.93f), b: 44 (0.17)

        protected Color textcolorCaptionGotoParentList = new Color(0.698f, 0.812f, 1.0f);
        //Color textcolorCaptionGotoParentListHighlighted = Color.white;

        protected const float textScaleModernConversationStyle = 0.8f;
        protected const float textBlockSizeModernConversationStyle = 0.75f;

        protected enum TalkOption {
            None,
            TellMeAbout,
            WhereIs
        };
        protected TalkOption selectedTalkOption = TalkOption.WhereIs;
        protected TalkOption talkOptionLastUsed = TalkOption.None;

        protected enum TalkCategory
        {
            None,
            Location,
            People,
            Things,
            Work
        };
        protected TalkCategory selectedTalkCategory = TalkCategory.Location;
        protected TalkCategory talkCategoryLastUsed = TalkCategory.None;

        static public int TalkToneToIndex(TalkTone talkTone)
        {
            switch (talkTone)
            {
                case TalkTone.Polite:
                    return 0;
                default:
                case TalkTone.Normal:
                    return 1;
                case TalkTone.Blunt:
                    return 2;
            }
        }

        protected bool isSetup = false;

        protected List<TalkManager.ListItem> listCurrentTopics; // current topic list metadata of displayed topic list in topic frame

        protected Texture2D textureBackground;
        protected Texture2D textureHighlightedOptions;
        protected Texture2D textureGrayedOutCategories;
        protected Texture2D texturePortrait;

        protected Panel panelNameNPC;
        protected TextLabel labelNameNPC = null;

        protected Texture2D textureTellMeAboutGrayedOut; // tried without this texture by just setting button background to null, but then pixel-perfect fit is no longer achieved
        protected Texture2D textureWhereIsGrayedOut; // tried without this texture by just setting button background to null, but then pixel-perfect fit is no longer achieved
        protected Texture2D textureTellMeAboutHighlighted;
        protected Texture2D textureWhereIsHighlighted;

        protected Texture2D textureCategoryLocationGrayedOut;
        protected Texture2D textureCategoryPersonGrayedOut;
        protected Texture2D textureCategoryThingGrayedOut;
        protected Texture2D textureCategoryWorkGrayedOut;
        protected Texture2D textureCategoryLocationHighlighted; // tried without this texture by just setting button background to null, but then pixel-perfect fit is no longer achieved
        protected Texture2D textureCategoryPersonHighlighted; // tried without this texture by just setting button background to null, but then pixel-perfect fit is no longer achieved
        protected Texture2D textureCategoryThingHighlighted; // tried without this texture by just setting button background to null, but then pixel-perfect fit is no longer achieved
        protected Texture2D textureCategoryWorkHighlighted; // tried without this texture by just setting button background to null, but then pixel-perfect fit is no longer achieved

        protected Color[] colorsTellMeAboutGrayedOut;
        protected Color[] colorsWhereIsGrayedOut;
        protected Color[] colorsTellMeAboutHighlighted;
        protected Color[] colorsWhereIsHighlighted;

        protected Color[] colorsCategoryLocationGrayedOut;
        protected Color[] colorsCategoryPeopleGrayedOut;
        protected Color[] colorsCategoryThingGrayedOut;
        protected Color[] colorsCategoryWorkGrayedOut;
        protected Color[] colorsCategoryLocationHighlighted;
        protected Color[] colorsCategoryPeopleHighlighted;
        protected Color[] colorsCategoryThingHighlighted;
        protected Color[] colorsCategoryWorkHighlighted;


        protected Panel mainPanel;

        protected TextLabel textlabelPlayerSays;
        protected string currentQuestion = "";
        protected int selectionIndexLastUsed = -1;

        // alignment stuff for portrait
        protected Panel panelPortrait = null;
        protected Vector2 panelPortraitPos = new Vector2(119, 65);
        protected Vector2 panelPortraitSize = new Vector2(64f, 64f);

        // alignment stuff for checkbox buttons
        protected Panel panelTone; // used as selection marker
        protected Vector2 panelTonePolitePos = new Vector2(258, 18);
        protected Vector2 panelToneNormalPos = new Vector2(258, 28);
        protected Vector2 panelToneBluntPos = new Vector2(258, 38);
        protected Vector2 panelToneSize = new Vector2(6f, 6f);
        protected Color32 toggleColor = new Color32(162, 36, 12, 255);

        // positioning rects for checkbox buttons
        protected Rect rectButtonTonePolite = new Rect(258, 18, 6, 6);
        protected Rect rectButtonToneNormal = new Rect(258, 28, 6, 6);
        protected Rect rectButtonToneBlunt = new Rect(258, 38, 6, 6);

        // normal buttons
        protected Button buttonTellMeAbout;
        protected Button buttonWhereIs;
        protected Button buttonCategoryLocation;
        protected Button buttonCategoryPerson;
        protected Button buttonCategoryThings;
        protected Button buttonCategoryWork;
        protected Button buttonTopicUp;
        protected Button buttonTopicDown;
        protected Button buttonTopicLeft;
        protected Button buttonTopicRight;
        protected Button buttonConversationUp;
        protected Button buttonConversationDown;
        protected Button buttonOkay;
        protected Button buttonGoodbye;
        protected Button buttonLogbook;

        // checkbox buttons
        protected Button buttonCheckboxTonePolite;
        protected Button buttonCheckboxToneNormal;
        protected Button buttonCheckboxToneBlunt;

        protected int toneLastUsed = -1;

        // position rect of arrow images is src image
        protected DFSize arrowsFullSize = new DFSize(9, 152);
        protected Rect upArrowRectInSrcImg = new Rect(0, 0, 9, 16);
        protected Rect downArrowRectInSrcImg = new Rect(0, 136, 9, 16);

        // topic listbox and layout, scrollbar/slider and parameters
        protected ListBox listboxTopic;
        protected Rect rectButtonTopicUp = new Rect(102, 69, 9, 16);
        protected Rect rectButtonTopicDown = new Rect(102, 161, 9, 16);
        protected Rect rectButtonTopicLeft = new Rect(4, 177, 16, 9);
        protected Rect rectButtonTopicRight = new Rect(86, 177, 16, 9);
        protected VerticalScrollBar verticalScrollBarTopic = null;
        protected HorizontalSlider horizontalSliderTopic = null;
        //int lengthOfLongestItemInListBox;
        protected int widthOfLongestItemInListBox;

        // textures of green/red arrow buttons for topic frame
        protected Texture2D arrowTopicUpRed;
        protected Texture2D arrowTopicUpGreen;
        protected Texture2D arrowTopicDownRed;
        protected Texture2D arrowTopicDownGreen;
        protected Texture2D arrowTopicLeftRed;
        protected Texture2D arrowTopicLeftGreen;
        protected Texture2D arrowTopicRightRed;
        protected Texture2D arrowTopicRightGreen;

        // conversation listbox and layout, scrollbar
        protected ListBox listboxConversation = null;
        protected Rect rectButtonConversationUp = new Rect(303, 64, 9, 16);
        protected Rect rectButtonConversationDown = new Rect(303, 176, 9, 16);
        protected VerticalScrollBar verticalScrollBarConversation = null;

        // green/red arrow buttons for conversation frame
        protected Texture2D arrowConversationUpRed;
        protected Texture2D arrowConversationUpGreen;
        protected Texture2D arrowConversationDownRed;
        protected Texture2D arrowConversationDownGreen;

        // used to guard execution of function SelectTopicFromTopicList - see this function for more detail why this guarding is necessary
        protected bool inListboxTopicContentUpdate = false;

        protected bool suppressTalk = false;
        protected string suppressTalkMessage = string.Empty;

        // Used to store indexes of copied talk fragments so they can be entered into Notebook in chronological order
        protected List<int> copyIndexes;

        protected bool isCloseWindowDeferred = false;

        public ADTalkWindow(IUserInterfaceManager uiManager, DaggerfallBaseWindow previous = null)
            : base(uiManager, previous)
        {
        }

         public override void OnPush()
        {
            base.OnPush();

            respondedCaptions.Clear();
            MagicAndEffects.MagicEffects.RacialOverrideEffect racialOverride = GameManager.Instance.PlayerEffectManager.GetRacialOverrideEffect();
            if (racialOverride != null)
                suppressTalk = racialOverride.GetSuppressTalk(out suppressTalkMessage);

            copyIndexes = new List<int>();
            if (listboxTopic != null)
                listboxTopic.ClearItems();

            SetStartConversation();
            if (verticalScrollBarTopic != null)
                verticalScrollBarTopic.ScrollIndex = 0;
            if (horizontalSliderTopic != null)
                horizontalSliderTopic.ScrollIndex = 0;
            if (verticalScrollBarTopic != null && horizontalSliderTopic != null)
                UpdateScrollBarsTopic();
            if (verticalScrollBarConversation != null)
            {
                verticalScrollBarConversation.ScrollIndex = 0;
                UpdateScrollBarConversation();
            }

            if (textlabelPlayerSays != null)
                textlabelPlayerSays.Text = "";

            if (isSetup)
            {
                SetTalkModeWhereIs();
                talkCategoryLastUsed = TalkCategory.None; // enforce that function SetTalkCategoryLocation does not skip itself and updated its topic list
                SetTalkCategoryLocation();
            }

            selectedTalkOption = TalkOption.WhereIs;
            selectedTalkCategory = TalkCategory.Location;
            talkCategoryLastUsed = TalkCategory.None;
            talkOptionLastUsed = TalkOption.None;
            toneLastUsed = -1;
            currentQuestion = "";
            isFirstAdviceRequest = true;
            isFirstWhoAreYouRequest = true;
            isFirstBuyAleRequest = true;

            LoadLocalizedText();
            InitializeKnownCaptions();
            GetFilterData();
            //ProcessDialogueItem();

            RemoveNumAnswersGivenForNPC();

            string npcName = filterVariables["Name"] as string;
            if (npcName == null)
            {
                Debug.LogError("Name is not set in filterVariables.");
                return;
            }

            // Use tuple to handle both numAnswers and dayOfYear
            if (numAnswersGivenDialogue.TryGetValue(npcName, out (int numAnswers, int dayOfYear) npcData))
            {
                DaggerfallDateTime dateTime = DaggerfallUnity.Instance.WorldTime.Now;
                if (npcData.dayOfYear != dateTime.DayOfYear)
                {
                    npcData.numAnswers = 0; // Reset the count if the day has changed
                }
                currentNumAnswersGivenDialogue = npcData.numAnswers;
            }
            else
            {
                currentNumAnswersGivenDialogue = 0; // No data means no answers have been given yet
            }

            UpdateNameNPC();
            SetTalkModeTellMeAbout();
        }

        public override void OnPop()
        {
            base.OnPop();

            // Send any copied conversation text to notebook
            copyIndexes.Sort();
            List<TextFile.Token> copiedEntries = new List<TextFile.Token>(copyIndexes.Count);
            int prev = -1;
            foreach (int idx in copyIndexes)
            {
                if (idx - prev != 1 && prev > -1)
                    copiedEntries.Add(new TextFile.Token());
                TextFile.Token entry = new TextFile.Token();
                ListBox.ListItem item = listboxConversation.GetItem(idx);
                bool question = (item.textColor == DaggerfallUI.DaggerfallQuestionTextColor || item.textColor == textcolorQuestionBackgroundModernConversationStyle);
                entry.formatting = question ? TextFile.Formatting.TextQuestion : TextFile.Formatting.TextAnswer;
                entry.text = item.textLabel.Text;
                copiedEntries.Add(entry);
                prev = idx;
            }
            GameManager.Instance.PlayerEntity.Notebook.AddNote(copiedEntries);

            // Retrieve the NPC name from filterVariables and cast it explicitly to string
            string npcName = filterVariables["Name"] as string; // Use 'as' for safe casting which returns null if the cast fails
            if (npcName == null)
            {
                Debug.LogError("Name is not set in filterVariables when popping the window.");
                return; // Optionally handle this case if the NPC Name should always be present
            }

            // Save the current number of answers given and the day of the year for this NPC
            DaggerfallDateTime dateTime = DaggerfallUnity.Instance.WorldTime.Now;
            int currentDayOfYear = dateTime.DayOfYear;
            if (numAnswersGivenDialogue.ContainsKey(npcName))
            {
                numAnswersGivenDialogue[npcName] = (currentNumAnswersGivenDialogue, currentDayOfYear);
            }
            else
            {
                numAnswersGivenDialogue.Add(npcName, (currentNumAnswersGivenDialogue, currentDayOfYear));
            }
        }

        public override void Update()
        {
            base.Update();

            // Close window immediately if talk suppressed
            if (suppressTalk)
            {
                CloseWindow();
                if (!string.IsNullOrEmpty(suppressTalkMessage))
                    DaggerfallUI.MessageBox(suppressTalkMessage);
                return;
            }
        }

        public virtual void UpdateListboxTopic()
        {
            if (listboxTopic != null)
            {
                string oldTopic = listboxTopic.SelectedIndex >= 0 ? listboxTopic.SelectedItem : null;
                if (selectedTalkOption == TalkOption.TellMeAbout)
                {
                    SetListboxTopics(ref listboxTopic, TalkManager.Instance.ListTopicTellMeAbout);
                }
                else if (selectedTalkOption == TalkOption.WhereIs)
                {
                    if (selectedTalkCategory == TalkCategory.Location)
                        SetListboxTopics(ref listboxTopic, TalkManager.Instance.ListTopicLocation);
                    else if (selectedTalkCategory == TalkCategory.People)
                        SetListboxTopics(ref listboxTopic, TalkManager.Instance.ListTopicPerson);
                    else if (selectedTalkCategory == TalkCategory.Things)
                        SetListboxTopics(ref listboxTopic, TalkManager.Instance.ListTopicThings);
                }
                if (oldTopic != null)
                    listboxTopic.SelectedIndex = listboxTopic.FindIndex(oldTopic);
                UpdateQuestion(listboxTopic.SelectedIndex);
            }
        }

        public override void SetNPCPortrait(FacePortraitArchive facePortraitArchive, int recordId)
        {
            // Load npc portrait
            string imageName = facePortraitArchive == FacePortraitArchive.CommonFaces ? portraitImgName : facesImgName;
            if (!TextureReplacement.TryImportCifRci(imageName, recordId, 0, false, out texturePortrait))
            {
                CifRciFile rciFile = new CifRciFile(Path.Combine(DaggerfallUnity.Instance.Arena2Path, imageName), FileUsage.UseMemory, false);
                rciFile.LoadPalette(Path.Combine(DaggerfallUnity.Instance.Arena2Path, rciFile.PaletteName));
                DFBitmap bitmap = rciFile.GetDFBitmap(recordId, 0);
                texturePortrait = new Texture2D(bitmap.Width, bitmap.Height, TextureFormat.ARGB32, false);
                texturePortrait.SetPixels32(rciFile.GetColor32(bitmap, 0));
                texturePortrait.Apply(false, false); // make readable
            }

            texturePortrait.filterMode = DaggerfallUI.Instance.GlobalFilterMode;
            if (!texturePortrait)
            {
                Debug.LogError(string.Format("Failed to load portrait image {0} for talk window", texturePortrait));
                CloseWindow();
                return;
            }

            if (panelPortrait != null)
                panelPortrait.BackgroundTexture = texturePortrait;
        }

        public virtual void UpdateNameNPC()
        {
            if (labelNameNPC != null)
            {
                labelNameNPC.Text = TalkManager.Instance.NameNPC;
            }
        }

        protected override void Setup()
        {
            base.Setup();

            ParentPanel.BackgroundColor = ScreenDimColor;

            textureBackground = DaggerfallUI.GetTextureFromImg(talkWindowImgName, TextureFormat.ARGB32, false);
            textureBackground.filterMode = DaggerfallUI.Instance.GlobalFilterMode;
            if (!textureBackground)
            {
                Debug.LogError(string.Format("Failed to load background image {0} for talk window", talkWindowImgName));
                CloseWindow();
                return;
            }

            mainPanel = DaggerfallUI.AddPanel(NativePanel, AutoSizeModes.None);
            mainPanel.BackgroundTexture = textureBackground;
            //mainPanel.Size = new Vector2(textureBackground.width, textureBackground.height);
            mainPanel.Size = new Vector2(320, 200); // reference size is always vanilla df resolution
            mainPanel.HorizontalAlignment = HorizontalAlignment.Center;
            mainPanel.VerticalAlignment = VerticalAlignment.Middle;
            mainPanel.BackgroundColor = Color.black;

            panelPortrait = DaggerfallUI.AddPanel(new Rect(panelPortraitPos, panelPortraitSize), NativePanel);
            panelPortrait.BackgroundTexture = texturePortrait;

            panelNameNPC = DaggerfallUI.AddPanel(mainPanel, AutoSizeModes.None);
            panelNameNPC.Position = new Vector2(117, 52);
            panelNameNPC.Size = new Vector2(197, 10);

            labelNameNPC = new TextLabel();
            labelNameNPC.Position = new Vector2(0, 0);
            labelNameNPC.Size = new Vector2(197, 10);
            labelNameNPC.Name = "label_npcName";
            labelNameNPC.MaxCharacters = -1;
            labelNameNPC.HorizontalAlignment = HorizontalAlignment.Center;
            labelNameNPC.VerticalAlignment = VerticalAlignment.Middle;
            panelNameNPC.Components.Add(labelNameNPC);

            UpdateNameNPC();

            // Load talk options highlight texture
            textureHighlightedOptions = DaggerfallUI.GetTextureFromImg(highlightedOptionsImgName, TextureFormat.ARGB32, false);
            textureHighlightedOptions.filterMode = DaggerfallUI.Instance.GlobalFilterMode;
            if (!textureHighlightedOptions)
            {
                Debug.LogError(string.Format("Failed to load highlighted options image {0} for talk window", highlightedOptionsImgName));
                CloseWindow();
                return;
            }

            // Load talk categories highlight texture

            textureGrayedOutCategories = DaggerfallUI.GetTextureFromImg(talkCategoriesImgName, TextureFormat.ARGB32, false);
            textureGrayedOutCategories.filterMode = DaggerfallUI.Instance.GlobalFilterMode;
            if (!textureGrayedOutCategories)
            {
                Debug.LogError(string.Format("Failed to load grayed-out categories image {0} for talk window", textureGrayedOutCategories));
                CloseWindow();
                return;
            }

            colorsTellMeAboutGrayedOut = textureBackground.GetPixels((int)(4 * (textureBackground.width / 320f)), (int)((200 - 4 - 10) * (textureBackground.height / 200f)), (int)(107 * (textureBackground.width / 320f)), (int)(10 * (textureBackground.height / 200f)));
            colorsWhereIsGrayedOut = textureBackground.GetPixels((int)(4 * (textureBackground.width / 320f)), (int)((200 - 14 - 10) * (textureBackground.height / 200f)), (int)(107 * (textureBackground.width / 320f)), (int)(10 * (textureBackground.height / 200f)));

            colorsTellMeAboutHighlighted = textureHighlightedOptions.GetPixels(0, textureHighlightedOptions.height/2, textureHighlightedOptions.width, textureHighlightedOptions.height/2);
            colorsWhereIsHighlighted = textureHighlightedOptions.GetPixels(0, 0, textureHighlightedOptions.width, textureHighlightedOptions.height/2);

            colorsCategoryLocationGrayedOut = textureGrayedOutCategories.GetPixels(0, textureGrayedOutCategories.height * 3 / 4, textureGrayedOutCategories.width, textureGrayedOutCategories.height / 4);
            colorsCategoryPeopleGrayedOut = textureGrayedOutCategories.GetPixels(0, textureGrayedOutCategories.height * 2 / 4, textureGrayedOutCategories.width, textureGrayedOutCategories.height / 4);
            colorsCategoryThingGrayedOut = textureGrayedOutCategories.GetPixels(0, textureGrayedOutCategories.height / 4, textureGrayedOutCategories.width, textureGrayedOutCategories.height / 4);
            colorsCategoryWorkGrayedOut = textureGrayedOutCategories.GetPixels(0, 0, textureGrayedOutCategories.width, textureGrayedOutCategories.height / 4);

            colorsCategoryLocationHighlighted = textureBackground.GetPixels((int)(4 * (textureBackground.width / 320f)), (int)((200 - 26 - 10) * (textureBackground.height / 200f)), (int)(107 * (textureBackground.width / 320f)), (int)(10 * (textureBackground.height / 200f)));
            colorsCategoryPeopleHighlighted = textureBackground.GetPixels((int)(4 * (textureBackground.width / 320f)), (int)((200 - 36 - 10) * (textureBackground.height / 200f)), (int)(107 * (textureBackground.width / 320f)), (int)(10 * (textureBackground.height / 200f)));
            colorsCategoryThingHighlighted = textureBackground.GetPixels((int)(4 * (textureBackground.width / 320f)), (int)((200 - 46 - 10) * (textureBackground.height / 200f)), (int)(107 * (textureBackground.width / 320f)), (int)(10 * (textureBackground.height / 200f)));
            colorsCategoryWorkHighlighted = textureBackground.GetPixels((int)(4 * (textureBackground.width / 320f)), (int)((200 - 56 - 10) * (textureBackground.height / 200f)), (int)(107 * (textureBackground.width / 320f)), (int)(10 * (textureBackground.height / 200f)));

            textureTellMeAboutGrayedOut = new Texture2D(textureHighlightedOptions.width, textureHighlightedOptions.height / 2, TextureFormat.ARGB32, false);
            textureTellMeAboutGrayedOut.SetPixels(colorsTellMeAboutGrayedOut);
            textureTellMeAboutGrayedOut.filterMode = DaggerfallUI.Instance.GlobalFilterMode;
            textureTellMeAboutGrayedOut.Apply(false, false);
            textureWhereIsGrayedOut = new Texture2D(textureHighlightedOptions.width, textureHighlightedOptions.height / 2, TextureFormat.ARGB32, false);
            textureWhereIsGrayedOut.SetPixels(colorsWhereIsGrayedOut);
            textureWhereIsGrayedOut.filterMode = DaggerfallUI.Instance.GlobalFilterMode;
            textureWhereIsGrayedOut.Apply(false, false);

            textureTellMeAboutHighlighted = new Texture2D(textureHighlightedOptions.width, textureHighlightedOptions.height/2, TextureFormat.ARGB32, false);
            textureTellMeAboutHighlighted.SetPixels(colorsTellMeAboutHighlighted);
            textureTellMeAboutHighlighted.filterMode = DaggerfallUI.Instance.GlobalFilterMode;
            textureTellMeAboutHighlighted.Apply(false, false);
            textureWhereIsHighlighted = new Texture2D(textureHighlightedOptions.width, textureHighlightedOptions.height / 2, TextureFormat.ARGB32, false);
            textureWhereIsHighlighted.SetPixels(colorsWhereIsHighlighted);
            textureWhereIsHighlighted.filterMode = DaggerfallUI.Instance.GlobalFilterMode;
            textureWhereIsHighlighted.Apply(false, false);

            textureCategoryLocationGrayedOut = new Texture2D(textureGrayedOutCategories.width, textureGrayedOutCategories.height / 4, TextureFormat.ARGB32, false);
            textureCategoryLocationGrayedOut.SetPixels(colorsCategoryLocationGrayedOut);
            textureCategoryLocationGrayedOut.filterMode = DaggerfallUI.Instance.GlobalFilterMode;
            textureCategoryLocationGrayedOut.Apply(false, false);
            textureCategoryPersonGrayedOut = new Texture2D(textureGrayedOutCategories.width, textureGrayedOutCategories.height / 4, TextureFormat.ARGB32, false);
            textureCategoryPersonGrayedOut.SetPixels(colorsCategoryPeopleGrayedOut);
            textureCategoryPersonGrayedOut.filterMode = DaggerfallUI.Instance.GlobalFilterMode;
            textureCategoryPersonGrayedOut.Apply(false, false);
            textureCategoryThingGrayedOut = new Texture2D(textureGrayedOutCategories.width, textureGrayedOutCategories.height / 4, TextureFormat.ARGB32, false);
            textureCategoryThingGrayedOut.SetPixels(colorsCategoryThingGrayedOut);
            textureCategoryThingGrayedOut.filterMode = DaggerfallUI.Instance.GlobalFilterMode;
            textureCategoryThingGrayedOut.Apply(false, false);
            textureCategoryWorkGrayedOut = new Texture2D(textureGrayedOutCategories.width, textureGrayedOutCategories.height / 4, TextureFormat.ARGB32, false);
            textureCategoryWorkGrayedOut.SetPixels(colorsCategoryWorkGrayedOut);
            textureCategoryWorkGrayedOut.filterMode = DaggerfallUI.Instance.GlobalFilterMode;
            textureCategoryWorkGrayedOut.Apply(false, false);

            textureCategoryLocationHighlighted = new Texture2D(textureGrayedOutCategories.width, textureGrayedOutCategories.height / 4, TextureFormat.ARGB32, false);
            textureCategoryLocationHighlighted.SetPixels(colorsCategoryLocationHighlighted);
            textureCategoryLocationHighlighted.filterMode = DaggerfallUI.Instance.GlobalFilterMode;
            textureCategoryLocationHighlighted.Apply(false, false);
            textureCategoryPersonHighlighted = new Texture2D(textureGrayedOutCategories.width, textureGrayedOutCategories.height / 4, TextureFormat.ARGB32, false);
            textureCategoryPersonHighlighted.SetPixels(colorsCategoryPeopleHighlighted);
            textureCategoryPersonHighlighted.filterMode = DaggerfallUI.Instance.GlobalFilterMode;
            textureCategoryPersonHighlighted.Apply(false, false);
            textureCategoryThingHighlighted = new Texture2D(textureGrayedOutCategories.width, textureGrayedOutCategories.height / 4, TextureFormat.ARGB32, false);
            textureCategoryThingHighlighted.SetPixels(colorsCategoryThingHighlighted);
            textureCategoryThingHighlighted.filterMode = DaggerfallUI.Instance.GlobalFilterMode;
            textureCategoryThingHighlighted.Apply(false, false);
            textureCategoryWorkHighlighted = new Texture2D(textureGrayedOutCategories.width, textureGrayedOutCategories.height / 4, TextureFormat.ARGB32, false);
            textureCategoryWorkHighlighted.SetPixels(colorsCategoryWorkHighlighted);
            textureCategoryWorkHighlighted.filterMode = DaggerfallUI.Instance.GlobalFilterMode;
            textureCategoryWorkHighlighted.Apply(false, false);

            textlabelPlayerSays = new TextLabel();
            textlabelPlayerSays.Position = new Vector2(123, 8);
            textlabelPlayerSays.Size = new Vector2(124, 38);
            textlabelPlayerSays.Name = "label_player_says";
            textlabelPlayerSays.MaxWidth = (int)textlabelPlayerSays.Size.x;
            textlabelPlayerSays.MaxCharacters = -1;
            textlabelPlayerSays.WrapText = true;
            textlabelPlayerSays.WrapWords = true;
            textlabelPlayerSays.TextColor = textcolorPlayerSays;
            mainPanel.Components.Add(textlabelPlayerSays);

            listboxTopic = new ListBox();
            listboxTopic.OnScroll += ListBoxTopic_OnScroll;
            listboxTopic.Position = new Vector2(6, 71);
            listboxTopic.Size = new Vector2(94, 104);
            //listboxTopic.RowsDisplayed = maxNumTopicsShown;
            listboxTopic.MaxCharacters = -1;
            listboxTopic.RowSpacing = 0;
            listboxTopic.Name = "list_topic";
            listboxTopic.EnabledHorizontalScroll = true;
            listboxTopic.VerticalScrollMode = ListBox.VerticalScrollModes.PixelWise;
            listboxTopic.HorizontalScrollMode = ListBox.HorizontalScrollModes.PixelWise;
            listboxTopic.RectRestrictedRenderArea = new Rect(listboxTopic.Position, listboxTopic.Size);
            listboxTopic.RestrictedRenderAreaCoordinateType = BaseScreenComponent.RestrictedRenderArea_CoordinateType.ParentCoordinates;
            //SetListItems(ref listboxTopic, ref listTopicLocation);
            listboxTopic.OnUseSelectedItem += ListboxTopic_OnUseSelectedItem;
            listboxTopic.OnSelectItem += ListboxTopic_OnSelectItem;
            mainPanel.Components.Add(listboxTopic);

            // Cut out red up/down arrows (topic)
            Texture2D redArrowsTexture = ImageReader.GetTexture(redArrowsTextureName);
            arrowTopicUpRed = ImageReader.GetSubTexture(redArrowsTexture, upArrowRectInSrcImg, arrowsFullSize);
            arrowTopicDownRed = ImageReader.GetSubTexture(redArrowsTexture, downArrowRectInSrcImg, arrowsFullSize);

            // Cut out green up/down arrows (topic)
            Texture2D greenArrowsTexture = ImageReader.GetTexture(greenArrowsTextureName);
            arrowTopicUpGreen = ImageReader.GetSubTexture(greenArrowsTexture, upArrowRectInSrcImg, arrowsFullSize);
            arrowTopicDownGreen = ImageReader.GetSubTexture(greenArrowsTexture, downArrowRectInSrcImg, arrowsFullSize);

            Color32[] colors;
            Color32[] rotated;
            colors = arrowTopicDownRed.GetPixels32();
            rotated = ImageProcessing.RotateColors(ref colors, arrowTopicUpRed.height, arrowTopicUpRed.width);
            arrowTopicLeftRed = new Texture2D(arrowTopicUpRed.height, arrowTopicUpRed.width, TextureFormat.ARGB32, false);
            arrowTopicLeftRed.SetPixels32(ImageProcessing.FlipHorizontallyColors(ref rotated, arrowTopicLeftRed.width, arrowTopicLeftRed.height), 0);
            arrowTopicLeftRed.Apply(false);
            arrowTopicLeftRed.filterMode = DaggerfallUI.Instance.GlobalFilterMode;

            colors = arrowTopicUpRed.GetPixels32();
            rotated = ImageProcessing.RotateColors(ref colors, arrowTopicDownRed.height, arrowTopicDownRed.width);
            arrowTopicRightRed = new Texture2D(arrowTopicUpRed.height, arrowTopicUpRed.width, TextureFormat.ARGB32, false);
            arrowTopicRightRed.SetPixels32(ImageProcessing.FlipHorizontallyColors(ref rotated, arrowTopicRightRed.width, arrowTopicRightRed.height));
            arrowTopicRightRed.Apply(false);
            arrowTopicRightRed.filterMode = DaggerfallUI.Instance.GlobalFilterMode;

            colors = arrowTopicDownGreen.GetPixels32();
            rotated = ImageProcessing.RotateColors(ref colors, arrowTopicUpGreen.height, arrowTopicUpGreen.width);
            arrowTopicLeftGreen = new Texture2D(arrowTopicUpGreen.height, arrowTopicUpGreen.width, TextureFormat.ARGB32, false);
            arrowTopicLeftGreen.SetPixels32(ImageProcessing.FlipHorizontallyColors(ref rotated, arrowTopicLeftGreen.width, arrowTopicLeftGreen.height));
            arrowTopicLeftGreen.Apply(false);
            arrowTopicLeftGreen.filterMode = DaggerfallUI.Instance.GlobalFilterMode;

            colors = arrowTopicUpGreen.GetPixels32();
            rotated = ImageProcessing.RotateColors(ref colors, arrowTopicDownGreen.height, arrowTopicDownGreen.width);
            arrowTopicRightGreen = new Texture2D(arrowTopicDownGreen.height, arrowTopicDownGreen.width, TextureFormat.ARGB32, false);
            arrowTopicRightGreen.SetPixels32(ImageProcessing.FlipHorizontallyColors(ref rotated, arrowTopicRightGreen.width, arrowTopicRightGreen.height));
            arrowTopicRightGreen.Apply(false);
            arrowTopicRightGreen.filterMode = DaggerfallUI.Instance.GlobalFilterMode;

            // Cut out red up/down arrows (conversation)
            arrowConversationUpRed = ImageReader.GetSubTexture(redArrowsTexture, upArrowRectInSrcImg, arrowsFullSize);
            arrowConversationDownRed = ImageReader.GetSubTexture(redArrowsTexture, downArrowRectInSrcImg, arrowsFullSize);

            // Cut out green up/down arrows (conversation)
            arrowConversationUpGreen = ImageReader.GetSubTexture(greenArrowsTexture, upArrowRectInSrcImg, arrowsFullSize);
            arrowConversationDownGreen = ImageReader.GetSubTexture(greenArrowsTexture, downArrowRectInSrcImg, arrowsFullSize);

            listboxConversation = new ListBox();
            listboxConversation.OnScroll += ListBoxConversation_OnScroll;
            listboxConversation.Position = new Vector2(189, 65);
            listboxConversation.Size = new Vector2(114, 126);
            listboxConversation.RowSpacing = 4;
            listboxConversation.MaxCharacters = -1; // text is wrapped, so no max characters defined
            listboxConversation.Name = "list_answers";
            listboxConversation.WrapTextItems = true;
            listboxConversation.WrapWords = true;
            listboxConversation.RectRestrictedRenderArea = new Rect(listboxConversation.Position, listboxConversation.Size);
            listboxConversation.RestrictedRenderAreaCoordinateType = BaseScreenComponent.RestrictedRenderArea_CoordinateType.ParentCoordinates;
            listboxConversation.VerticalScrollMode = ListBox.VerticalScrollModes.PixelWise;
            listboxConversation.SelectedShadowPosition = DaggerfallUI.DaggerfallDefaultShadowPos;
            mainPanel.Components.Add(listboxConversation);

            SetStartConversation();

            SetupButtons();
            SetupCheckboxes();
            SetupScrollBars();
            SetupScrollButtons();

            SetTalkModeWhereIs();
            SetTalkModeTellMeAbout();

            //UpdateButtonState();
            UpdateCheckboxes();
            UpdateScrollBarsTopic();
            UpdateScrollButtonsTopic();
            UpdateScrollBarConversation();
            UpdateScrollButtonsConversation();

            isSetup = true;
        }

        void SetStartConversation()
        {
            if (listboxConversation != null)
            {
                listboxConversation.ClearItems();

                ListBox.ListItem textLabelNPCGreeting;
                listboxConversation.AddItem(TalkManager.Instance.NPCGreetingText, out textLabelNPCGreeting);
                textLabelNPCGreeting.selectedTextColor = textcolorHighlighted;
                textLabelNPCGreeting.textLabel.HorizontalAlignment = HorizontalAlignment.Left;
                textLabelNPCGreeting.textLabel.HorizontalTextAlignment = TextLabel.HorizontalTextAlignmentSetting.Left;
                if (DaggerfallUnity.Settings.EnableModernConversationStyleInTalkWindow)
                {
                    textLabelNPCGreeting.textLabel.TextScale = textScaleModernConversationStyle;
                    textLabelNPCGreeting.textLabel.MaxWidth = (int)(textLabelNPCGreeting.textLabel.MaxWidth * textBlockSizeModernConversationStyle);
                    textLabelNPCGreeting.textLabel.BackgroundColor = textcolorAnswerBackgroundModernConversationStyle;
                }
            }

            TalkManager.Instance.StartNewConversation();
        }

        protected virtual void SetupButtons()
        {
            buttonTellMeAbout = new Button();
            buttonTellMeAbout.Position = new Vector2(4, 4);
            buttonTellMeAbout.Size = new Vector2(107, 10);
            buttonTellMeAbout.Name = "button_tellmeabout";
            buttonTellMeAbout.OnMouseClick += ButtonTellMeAbout_OnMouseClick;
            buttonTellMeAbout.Hotkey = DaggerfallShortcut.GetBinding(DaggerfallShortcut.Buttons.TalkTellMeAbout);
            mainPanel.Components.Add(buttonTellMeAbout);

            buttonWhereIs = new Button();
            buttonWhereIs.Position = new Vector2(4, 14);
            buttonWhereIs.Size = new Vector2(107, 10);
            buttonWhereIs.Name = "button_whereis";
            buttonWhereIs.OnMouseClick += ButtonWhereIs_OnMouseClick;
            buttonWhereIs.Hotkey = DaggerfallShortcut.GetBinding(DaggerfallShortcut.Buttons.TalkWhereIs);
            mainPanel.Components.Add(buttonWhereIs);

            buttonCategoryLocation = new Button();
            buttonCategoryLocation.Position = new Vector2(4, 26);
            buttonCategoryLocation.Size = new Vector2(107, 10);
            buttonCategoryLocation.Name = "button_categoryLocation";
            buttonCategoryLocation.OnMouseClick += ButtonCategoryLocation_OnMouseClick;
            buttonCategoryLocation.Hotkey = DaggerfallShortcut.GetBinding(DaggerfallShortcut.Buttons.TalkCategoryLocation);
            mainPanel.Components.Add(buttonCategoryLocation);

            buttonCategoryPerson = new Button();
            buttonCategoryPerson.Position = new Vector2(4, 36);
            buttonCategoryPerson.Size = new Vector2(107, 10);
            buttonCategoryPerson.Name = "button_categoryPeople";
            buttonCategoryPerson.OnMouseClick += ButtonCategoryPeople_OnMouseClick;
            buttonCategoryPerson.Hotkey = DaggerfallShortcut.GetBinding(DaggerfallShortcut.Buttons.TalkCategoryPeople);
            mainPanel.Components.Add(buttonCategoryPerson);

            buttonCategoryThings = new Button();
            buttonCategoryThings.Position = new Vector2(4, 46);
            buttonCategoryThings.Size = new Vector2(107, 10);
            buttonCategoryThings.Name = "button_categoryThings";
            buttonCategoryThings.OnMouseClick += ButtonCategoryThings_OnMouseClick;
            buttonCategoryThings.Hotkey = DaggerfallShortcut.GetBinding(DaggerfallShortcut.Buttons.TalkCategoryThings);
            mainPanel.Components.Add(buttonCategoryThings);

            buttonCategoryWork = new Button();
            buttonCategoryWork.Position = new Vector2(4, 56);
            buttonCategoryWork.Size = new Vector2(107, 10);
            buttonCategoryWork.Name = "button_categoryWork";
            buttonCategoryWork.OnMouseClick += ButtonCategoryWork_OnMouseClick;
            buttonCategoryWork.Hotkey = DaggerfallShortcut.GetBinding(DaggerfallShortcut.Buttons.TalkCategoryWork);
            mainPanel.Components.Add(buttonCategoryWork);

            buttonOkay = new Button();
            buttonOkay.Position = new Vector2(4, 186);
            buttonOkay.Size = new Vector2(107, 10);
            buttonOkay.Name = "button_okay";
            buttonOkay.OnMouseClick += ButtonOkay_OnMouseClick;
            buttonOkay.Hotkey = DaggerfallShortcut.GetBinding(DaggerfallShortcut.Buttons.TalkAsk);
            mainPanel.Components.Add(buttonOkay);

            buttonGoodbye = new Button();
            buttonGoodbye.Position = new Vector2(118, 183);
            buttonGoodbye.Size = new Vector2(67, 10);
            buttonGoodbye.Name = "button_goodbye";
            buttonGoodbye.OnMouseClick += ButtonGoodbye_OnMouseClick;
            buttonGoodbye.Hotkey = DaggerfallShortcut.GetBinding(DaggerfallShortcut.Buttons.TalkExit);
            buttonGoodbye.OnKeyboardEvent += ButtonGoodbye_OnKeyboardEvent;

            mainPanel.Components.Add(buttonGoodbye);

            buttonLogbook = new Button {
                Position = new Vector2(118, 158),
                Size = new Vector2(67, 18),
                ToolTip = defaultToolTip,
                ToolTipText = TextManager.Instance.GetLocalizedText("copyLogbookInfo"),
            };
            if (defaultToolTip != null)
                buttonLogbook.ToolTip.ToolTipDelay = 1;
            buttonLogbook.OnMouseClick += ButtonLogbook_OnMouseClick;
            buttonLogbook.OnRightMouseClick += ButtonLogbook_OnRightMouseClick;
            // Can only assign one hotkey :(
            buttonLogbook.Hotkey = DaggerfallShortcut.GetBinding(DaggerfallShortcut.Buttons.TalkCopy);
            mainPanel.Components.Add(buttonLogbook);
        }

        protected virtual void SetupCheckboxes()
        {
            buttonCheckboxTonePolite = DaggerfallUI.AddButton(rectButtonTonePolite, NativePanel);
            buttonCheckboxTonePolite.OnMouseClick += ButtonTonePolite_OnClickHandler;
            buttonCheckboxTonePolite.Hotkey = DaggerfallShortcut.GetBinding(DaggerfallShortcut.Buttons.TalkTonePolite);
            buttonCheckboxToneNormal = DaggerfallUI.AddButton(rectButtonToneNormal, NativePanel);
            buttonCheckboxToneNormal.OnMouseClick += ButtonToneNormal_OnClickHandler;
            buttonCheckboxToneNormal.Hotkey = DaggerfallShortcut.GetBinding(DaggerfallShortcut.Buttons.TalkToneNormal);
            buttonCheckboxToneBlunt = DaggerfallUI.AddButton(rectButtonToneBlunt, NativePanel);
            buttonCheckboxToneBlunt.OnMouseClick += ButtonToneBlunt_OnClickHandler;
            buttonCheckboxToneBlunt.Hotkey = DaggerfallShortcut.GetBinding(DaggerfallShortcut.Buttons.TalkToneBlunt);

            panelTone = DaggerfallUI.AddPanel(new Rect(panelTonePolitePos, panelToneSize), NativePanel);
            panelTone.BackgroundColor = toggleColor;
        }

        protected virtual void SetupScrollBars()
        {
            // topic list scroll bar (e.g. items in character inventory)
            verticalScrollBarTopic = new VerticalScrollBar();
            verticalScrollBarTopic.Position = new Vector2(104, 87);
            verticalScrollBarTopic.Size = new Vector2(5, 73);
            verticalScrollBarTopic.OnScroll += VerticalScrollBarTopic_OnScroll;
            NativePanel.Components.Add(verticalScrollBarTopic);

            horizontalSliderTopic = new HorizontalSlider();
            horizontalSliderTopic.Position = new Vector2(22, 178);
            horizontalSliderTopic.Size = new Vector2(62, 5);
            horizontalSliderTopic.OnScroll += HorizontalSliderTopic_OnScroll;
            NativePanel.Components.Add(horizontalSliderTopic);

            // conversion list scroll bar
            verticalScrollBarConversation = new VerticalScrollBar();
            verticalScrollBarConversation.Position = new Vector2(305, 81);
            verticalScrollBarConversation.Size = new Vector2(5, 94);
            verticalScrollBarConversation.OnScroll += VerticalScrollBarConversation_OnScroll;
            NativePanel.Components.Add(verticalScrollBarConversation);
        }

        protected virtual void SetupScrollButtons()
        {
            buttonTopicUp = DaggerfallUI.AddButton(rectButtonTopicUp, NativePanel);
            buttonTopicUp.BackgroundTexture = arrowTopicUpRed;
            buttonTopicUp.OnMouseClick += ButtonTopicUp_OnMouseClick;

            buttonTopicDown = DaggerfallUI.AddButton(rectButtonTopicDown, NativePanel);
            buttonTopicDown.BackgroundTexture = arrowTopicDownRed;
            buttonTopicDown.OnMouseClick += ButtonTopicDown_OnMouseClick;

            buttonTopicLeft = DaggerfallUI.AddButton(rectButtonTopicLeft, NativePanel);
            buttonTopicLeft.BackgroundTexture = arrowTopicLeftRed;
            buttonTopicLeft.OnMouseClick += ButtonTopicLeft_OnMouseClick;

            buttonTopicRight = DaggerfallUI.AddButton(rectButtonTopicRight, NativePanel);
            buttonTopicRight.BackgroundTexture = arrowTopicRightRed;
            buttonTopicRight.OnMouseClick += ButtonTopicRight_OnMouseClick;


            buttonConversationUp = DaggerfallUI.AddButton(rectButtonConversationUp, NativePanel);
            buttonConversationUp.BackgroundTexture = arrowConversationUpRed;
            buttonConversationUp.OnMouseClick += ButtonConversationUp_OnMouseClick;

            buttonConversationDown = DaggerfallUI.AddButton(rectButtonConversationDown, NativePanel);
            buttonConversationDown.BackgroundTexture = arrowConversationDownRed;
            buttonConversationDown.OnMouseClick += ButtonConversationDown_OnMouseClick;
        }

        protected virtual void UpdateScrollBarsTopic()
        {
            verticalScrollBarTopic.DisplayUnits = (int)listboxTopic.Size.y; //Math.Min(maxNumTopicsShown, listboxTopic.Count);
            verticalScrollBarTopic.TotalUnits = listboxTopic.HeightContent();  //listboxTopic.Count;
            verticalScrollBarTopic.ScrollIndex = 0;
            verticalScrollBarTopic.Update();

            horizontalSliderTopic.DisplayUnits = (int)listboxTopic.Size.x; //maxNumCharactersOfTopicShown;
            horizontalSliderTopic.TotalUnits = listboxTopic.WidthContent();  //lengthOfLongestItemInListBox;
            horizontalSliderTopic.ScrollIndex = 0;
            horizontalSliderTopic.Update();
        }

        protected virtual void UpdateScrollBarConversation()
        {
            verticalScrollBarConversation.DisplayUnits = (int)listboxConversation.Size.y; //Math.Max(5, listboxConversation.HeightContent() / 10);
            verticalScrollBarConversation.TotalUnits = listboxConversation.HeightContent();
            verticalScrollBarConversation.ScrollIndex = 0;
            if (listboxConversation.Count > 0)
                verticalScrollBarConversation.ScrollIndex = listboxConversation.HeightContent() - (int)listboxConversation.Size.y; //listboxConversation.GetItem(listboxConversation.Count - 1).textLabel.TextHeight;
            verticalScrollBarConversation.Update();
        }

        protected virtual void UpdateScrollButtonsTopic()
        {
            int verticalScrollIndex = GetSafeScrollIndex(verticalScrollBarTopic);
            // Update scroller buttons
            UpdateListTopicScrollerButtons(verticalScrollBarTopic, verticalScrollIndex, listboxTopic.HeightContent(), buttonTopicUp, buttonTopicDown);
            buttonTopicUp.Update();
            buttonTopicDown.Update();

            int horizontalScrollIndex = GetSafeScrollIndex(horizontalSliderTopic);
            // Update scroller buttons
            UpdateListTopicScrollerButtonsLeftRight(horizontalSliderTopic, horizontalScrollIndex, widthOfLongestItemInListBox, buttonTopicLeft, buttonTopicRight);
            buttonTopicLeft.Update();
            buttonTopicRight.Update();
        }

        protected virtual void UpdateScrollButtonsConversation()
        {
            int scrollIndex = GetSafeScrollIndex(verticalScrollBarConversation);
            // Update scroller buttons
            UpdateListConversationScrollerButtons(verticalScrollBarConversation, scrollIndex, listboxConversation.HeightContent(), buttonConversationUp, buttonConversationDown);
            buttonConversationUp.Update();
            buttonConversationDown.Update();
        }

        protected virtual void SetListboxTopics(ref ListBox listboxTopic, List<TalkManager.ListItem> listTopic)
        {
            listCurrentTopics = listTopic;

            listboxTopic.ClearItems();
            for (int i = 0; i < listTopic.Count; i++)
            {
                TalkManager.ListItem item = listTopic[i];
                ListBox.ListItem listboxItem;
                if (item.caption == null) // this is a check to detect problems arising from old save data - where caption end up as null
                {
                    item.caption = item.key; //  just try to take key as caption then (answers might still be broken)
                    if (item.caption == String.Empty)
                        item.caption = TextManager.Instance.GetLocalizedText("resolvingError");
                }
                else if (item.caption == String.Empty)
                {
                    item.caption = TextManager.Instance.GetLocalizedText("resolvingError");
                }

                // Modify display caption to remove initial "the "
                string displayCaption = RemoveDefiniteArticlePrefix(item.caption);

                listboxTopic.AddItem(displayCaption, out listboxItem);
                if (item.type == TalkManager.ListItemType.NavigationBack)
                {
                    listboxItem.textColor = textcolorCaptionGotoParentList;
                    //listboxItem.selectedTextColor = textcolorCaptionGotoParentListHighlighted;
                }
            }

            // compute length of longest item in listbox from current list items...
            //lengthOfLongestItemInListBox = listboxTopic.LengthOfLongestItem();
            widthOfLongestItemInListBox = listboxTopic.WidthContent();

            // update listboxTopic.MaxHorizontalScrollIndex
            //listboxTopic.MaxHorizontalScrollIndex = Math.Max(0, lengthOfLongestItemInListBox - maxNumCharactersOfTopicShown);
            listboxTopic.MaxHorizontalScrollIndex = Math.Max(0, widthOfLongestItemInListBox - (int)listboxTopic.Size.x);

            listboxTopic.Update();
            UpdateScrollBarsTopic();
            UpdateScrollButtonsTopic();

            if (listTopic.Count <= 0)
                return;

            if (listTopic[0].listParentItems != null) // first entry is "previous" item
            {
                listboxTopic.SelectIndex(1);
            }
            else
            {
                listboxTopic.SelectIndex(0);
                //listboxTopic.SelectNone();
                //UpdateQuestion(-1); // important since it might have selected question from last double-click action when changing level of topic tree
            }
        }

        protected virtual void ClearListboxTopics()
        {
            listboxTopic.ClearItems();
            //lengthOfLongestItemInListBox = 0;
            widthOfLongestItemInListBox = 0;
            listboxTopic.MaxHorizontalScrollIndex = 0;
        }

        protected virtual void UpdateCheckboxes()
        {
            //update tone selection
            switch (selectedTalkTone)
            {
                case TalkTone.Polite:
                default:
                    panelTone.Position = panelTonePolitePos;
                    break;
                case TalkTone.Normal:
                    panelTone.Position = panelToneNormalPos;
                    break;
                case TalkTone.Blunt:
                    panelTone.Position = panelToneBluntPos;
                    break;
            }

        }

        protected virtual void SetTalkModeTellMeAbout()
        {
            selectedTalkOption = TalkOption.TellMeAbout;
            if (selectedTalkOption == talkOptionLastUsed)
                return;
            talkOptionLastUsed = selectedTalkOption;

            talkCategoryLastUsed = TalkCategory.None;  // Important to enable category when switching back

            // Update UI elements. Null checks required because (I think) we inherit from DaggerfallTalkWindow instead of DaggerfallPopupWindow
            if (buttonTellMeAbout == null) {}
            else
                buttonTellMeAbout.BackgroundTexture = textureTellMeAboutHighlighted;

            if (buttonWhereIs == null) {}
            else
                buttonWhereIs.BackgroundTexture = textureWhereIsGrayedOut;

            if (buttonCategoryLocation == null) {}
            else
                buttonCategoryLocation.BackgroundTexture = textureCategoryLocationGrayedOut;

            if (buttonCategoryPerson == null) {}
            else
                buttonCategoryPerson.BackgroundTexture = textureCategoryPersonGrayedOut;

            if (buttonCategoryThings == null) {}
            else
                buttonCategoryThings.BackgroundTexture = textureCategoryThingGrayedOut;

            if (buttonCategoryWork == null) {}
            else
                buttonCategoryWork.BackgroundTexture = textureCategoryWorkGrayedOut;

            // Ensuring that the UpdateTellMeAboutTopics method doesn't operate on null or uninitialized data
            if (listboxTopic == null) {}

            else
                UpdateTellMeAboutTopics();  // Call the refactored method to set topics
        }

        public void InitializeKnownCaptions()
        {
            // Check if LocalizationKeys is available
            if (ADDialogue.LocalizationKeys == null || ADDialogue.LocalizationKeys.Count == 0)
            {
                Debug.LogError("LocalizationKeys not loaded in ADDialogue.");
                return;
            }

            // Specific keys we want to ensure are always present in knownCaptions
            string[] requiredKeys = { "anyAdvice", "whoAreYou", "buyAnAle", "buyABeer" };

            // Add the values of required keys from LocalizationKeys to knownCaptions if not already included
            foreach (var key in requiredKeys)
            {
                if (ADDialogue.LocalizationKeys.TryGetValue(key, out var value))
                {
                    string valueString = value.ToString();

                    // Check if this value is already in knownCaptions
                    if (!knownCaptions.Contains(valueString))
                    {
                        knownCaptions.Add(valueString);
                        Debug.Log($"Added '{valueString}' to knownCaptions from key '{key}' in LocalizationKeys.");
                    }
                }
            }

            Debug.Log("Known captions ensured in knownCaptions list.");
        }

        protected virtual void UpdateTellMeAboutTopics()
        {
            // Create a new list to hold the merged topics
            List<ADDialogue.DialogueListItem> baseTopics = TalkManager.Instance.ListTopicTellMeAbout
                .Select(item => new ADDialogue.DialogueListItem(item)).ToList();

            // Remove all items with QuestionType.OrganizationInfo (ie, Factions questions)
            baseTopics.RemoveAll(dialogueItem => dialogueItem.ListItem.questionType == TalkManager.QuestionType.OrganizationInfo);

            // Filter ADDialogue.instance.dialogueListItems by allowed captions and custom conditions
            var knownDialogueItems = ADDialogue.instance.dialogueListItems
                .Where(item => knownCaptions.Contains(item.ListItem.caption.ToLower()) && EvaluateConditions(item))
                .GroupBy(item => item.ListItem.caption.ToLower())
                .Select(group => group.First()) // Selects only the first item from each group
                .ToList();

            // Add all custom topics from ADDialogue.instance.dialogueListItems to the filtered list
            baseTopics.AddRange(knownDialogueItems);

            baseTopics.Sort((item1, item2) =>
                string.Compare(RemoveDefiniteArticlePrefix(item1.ListItem.caption), RemoveDefiniteArticlePrefix(item2.ListItem.caption), StringComparison.InvariantCultureIgnoreCase));

            // Use the filtered list to set the topics in your list box
            SetListboxTopics(ref listboxTopic, baseTopics.Select(di => di.ListItem).ToList());
            listboxTopic.Update();

            UpdateScrollBarsTopic();
            UpdateScrollButtonsTopic();
        }

        private bool EvaluateConditions(ADDialogue.DialogueListItem item)
        {
            System.Random random = new System.Random(); // Initialize a random number generator
            bool npcNoLoreConditionFound = false; // Flag to check if a specific NPC NoLore condition is set

            // Loop through each condition set (C1, C2, C3)
            for (int i = 1; i <= 3; i++)
            {
                string cVariable = $"C{i}_Variable";
                string cComparison = $"C{i}_Comparison";
                string cValue = $"C{i}_Value";

                if (!item.DialogueData.ContainsKey(cVariable) ||
                    !item.DialogueData.ContainsKey(cComparison) ||
                    !item.DialogueData.ContainsKey(cValue) ||
                    string.IsNullOrWhiteSpace(item.DialogueData[cVariable] as string) ||
                    string.IsNullOrWhiteSpace(item.DialogueData[cComparison] as string) ||
                    string.IsNullOrWhiteSpace(item.DialogueData[cValue] as string))
                {
                    continue;
                }

                string variableName = item.DialogueData[cVariable] as string;
                string comparisonOperator = item.DialogueData[cComparison] as string;
                string valueToCompare = item.DialogueData[cValue] as string;

                if (variableName == "NoLore")
                {
                    npcNoLoreConditionFound = true;
                }

                if (!EvaluateSingleCondition(variableName, comparisonOperator, valueToCompare, random))
                {
                    return false;
                }
            }

            // If no specific NPC NoLore condition has been set, enforce the default condition
            if (!npcNoLoreConditionFound)
            {
                if (!EvaluateSingleCondition("NoLore", "==", "0", random))
                {
                    return false;
                }
            }

            return true; // If all conditions are met
        }

        private bool EvaluateSingleCondition(string variableName, string comparisonOperator, string valueToCompare, System.Random random)
        {
            object variableValue;
            if (variableName == "Random Number")
            {
                variableValue = random.Next(1, 101); // Generate a random number from 1 to 100
            }
            else if (!filterVariables.TryGetValue(variableName, out variableValue) || variableValue == null)
            {
                Debug.LogWarning($"ADTalkWindow: condition skipped because '{variableName}' is missing or null");
                return false;
            }

            string variableValueStr = variableValue.ToString();
            string[] valuesToCompare = valueToCompare.Split('|').Select(v => v.Trim()).ToArray();

            switch (comparisonOperator)
            {
                case "==":
                    // Return true if any value matches exactly
                    return valuesToCompare.Any(v => v.Equals(variableValueStr, StringComparison.InvariantCultureIgnoreCase));
                case "!=":
                    // Return true if none of the values match exactly
                    return valuesToCompare.All(v => !v.Equals(variableValueStr, StringComparison.InvariantCultureIgnoreCase));
                case "~~":
                    // Return true if any value is found within the variable value (partial match)
                    return valuesToCompare.Any(v => variableValueStr.IndexOf(v, StringComparison.InvariantCultureIgnoreCase) >= 0);
                case "!~":
                    // Return true if none of the values are found within the variable value (partial match)
                    return valuesToCompare.All(v => variableValueStr.IndexOf(v, StringComparison.InvariantCultureIgnoreCase) < 0);
                case "<":
                case ">":
                case "<=":
                case ">=":
                    if (int.TryParse(variableValueStr, out int intVariableValue))
                    {
                        return valuesToCompare.All(v =>
                        {
                            if (int.TryParse(v, out int intValue))
                            {
                                return EvaluateNumericComparison(intVariableValue, intValue, comparisonOperator);
                            }
                            Debug.LogError($"Cannot perform numeric comparison because '{v}' is not an integer.");
                            return false;
                        });
                    }
                    else
                    {
                        Debug.LogError($"Cannot perform numeric comparison on non-integer variable '{variableName}' value '{variableValue}'.");
                        return false;
                    }
                default:
                    Debug.LogError($"Unknown comparison operator '{comparisonOperator}'.");
                    return false; // Unknown operator, fail the condition
            }
        }

        private bool EvaluateNumericComparison(int variableValue, int comparisonValue, string comparisonOperator)
        {
            switch (comparisonOperator)
            {
                case "<":
                    return variableValue < comparisonValue;
                case ">":
                    return variableValue > comparisonValue;
                case "<=":
                    return variableValue <= comparisonValue;
                case ">=":
                    return variableValue >= comparisonValue;
                default:
                    throw new ArgumentException($"Invalid comparison operator: {comparisonOperator}");
            }
        }

        private void LoadLocalizedText()
        {
            // Merge localized captions with existing knownCaptions, avoiding duplicates
            var localizedCaptions = (List<string>)ADDialogue.LocalizationKeys["knownCaptions"];
            foreach (var caption in localizedCaptions)
            {
                if (!knownCaptions.Contains(caption.ToLower())) // Avoid duplicates
                {
                    knownCaptions.Add(caption.ToLower());
                }
            }

            repeatedResponses = ((List<string>)ADDialogue.LocalizationKeys["repeatedResponses"]).ToArray();
            exceededMaxResponses = ((List<string>)ADDialogue.LocalizationKeys["exceededMaxResponses"]).ToArray();
        }

        // Helper function to remove definite articles for sorting
        string RemoveDefiniteArticlePrefix(string caption)
        {
            // Check if the key exists in LocalizationKeys
            if (!ADDialogue.LocalizationKeys.TryGetValue("definiteArticles", out var definiteArticlesObj))
            {
                Debug.LogError("Error: 'definiteArticles' key is missing in LocalizationKeys.");
                return caption;
            }

            // Prepare the list of articles to check against
            List<string> definiteArticles;
            if (definiteArticlesObj is List<string> articlesList)
            {
                definiteArticles = articlesList;
            }
            else if (definiteArticlesObj is string singleArticle)
            {
                definiteArticles = new List<string> { singleArticle };
            }
            else
            {
                Debug.LogError("Error: 'definiteArticles' key is not a string or a list.");
                return caption;
            }

            // Find the first article that matches the start of the caption, ignoring case
            string matchedArticle = definiteArticles
                .Select(article => article.Trim() + " ")
                .FirstOrDefault(article => caption.StartsWith(article, StringComparison.InvariantCultureIgnoreCase));

            // If a match was found, remove it and trim; otherwise, return the original caption
            if (matchedArticle != null)
            {
                string result = caption.Substring(matchedArticle.Length).Trim();
                return result;
            }
            else
            {
                return caption;
            }
        }

        // This assumes you have a method to get the current macro context provider
        private IMacroContextProvider GetMacroContextProvider()
        {
            // Assuming TalkManager implements IMacroContextProvider or similar logic
            return TalkManager.Instance;
        }

        public string ProcessAnswerWithMacros(string answer, IMacroContextProvider mcp, int lineNumber)
        {
            try
            {
                // Regex pattern to find macros like @Some Variable@ and preserve spaces
                answer = Regex.Replace(answer, @"@([^@]+)@", match =>
                {
                    string macroKey = match.Groups[1].Value.Trim(); // Get the text inside @ @ and trim spaces

                    // Check if the macro exists in filterVariables
                    if (filterVariables.TryGetValue(macroKey, out object macroValue))
                    {
                        return macroValue.ToString();
                    }
                    else
                    {
                        // Log a warning if the macro is not found in filterVariables
                        //Debug.LogWarning($"Macro '{macroKey}' not found in filterVariables");
                        return $"[{macroKey}]"; // Return the macro name wrapped in [] if not found
                    }
                });

                // Now proceed with the standard macro expansion after custom macros are handled
                TextFile.Token[] tokens = DaggerfallStringTableImporter.ConvertStringToRSCTokens(answer);

                // Expand standard macros within the tokens
                MacroHelper.ExpandMacros(ref tokens, mcp);

                // Convert tokens back to a single string
                StringBuilder expandedAnswer = new StringBuilder();
                foreach (var token in tokens)
                {
                    if (token.formatting == TextFile.Formatting.Text)
                        expandedAnswer.Append(token.text);
                    else if (token.formatting == TextFile.Formatting.NewLine)
                        expandedAnswer.AppendLine();
                    // Handle other formatting as needed
                }

                // Convert StringBuilder to string
                string finalAnswer = expandedAnswer.ToString();

                return finalAnswer;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error processing macros for line {lineNumber} in CSV: {ex.Message}");
                return "Error in macro expansion"; // You may choose to return a default error message
            }
        }

        // Helper method to replace filterVariables macros in the text
        private string ReplaceFilterVariableMacros(string text)
        {
            // Regex pattern to find %VariableName% macros
            var macroPattern = new Regex("%(.*?)%");
            return macroPattern.Replace(text, match =>
            {
                string variableName = match.Groups[1].Value;
                if (filterVariables.ContainsKey(variableName))
                {
                    // Return the value from filterVariables
                    return filterVariables[variableName]?.ToString() ?? "";
                }
                else
                {
                    // If the variable is not found, return the original macro unchanged
                    //Debug.LogWarning($"Macro '{variableName}' not found in filterVariables.");
                    return match.Value;
                }
            });
        }

        protected virtual void SetTalkModeWhereIs()
        {
            selectedTalkOption = TalkOption.WhereIs;
            if (selectedTalkOption == talkOptionLastUsed)
                return;
            talkOptionLastUsed = selectedTalkOption;

            buttonTellMeAbout.BackgroundTexture = textureTellMeAboutGrayedOut;
            buttonWhereIs.BackgroundTexture = textureWhereIsHighlighted;

            SetTalkCategory(selectedTalkCategory);
        }

        protected virtual void SetTalkCategory(TalkCategory talkCategory)
        {
            switch (talkCategory)
            {
                case TalkCategory.Location:
                default:
                    SetTalkCategoryLocation();
                    break;
                case TalkCategory.People:
                    SetTalkCategoryPeople();
                    break;
                case TalkCategory.Things:
                    SetTalkCategoryThings();
                    break;
                case TalkCategory.Work:
                    SetTalkCategoryWork();
                    break;
                case TalkCategory.None:
                    SetTalkCategoryNone();
                    break;
            }
        }

        protected virtual void SetTalkCategoryNone()
        {
            ClearCurrentQuestion();

            selectedTalkCategory = TalkCategory.None;
            talkCategoryLastUsed = TalkCategory.None;

            buttonCategoryLocation.BackgroundTexture = textureCategoryLocationGrayedOut;
            buttonCategoryPerson.BackgroundTexture = textureCategoryPersonGrayedOut;
            buttonCategoryThings.BackgroundTexture = textureCategoryThingGrayedOut;
            buttonCategoryWork.BackgroundTexture = textureCategoryWorkGrayedOut;

            ClearListboxTopics();
            listboxTopic.Update();

            UpdateScrollBarsTopic();
            UpdateScrollButtonsTopic();
        }

        protected virtual void SetTalkCategoryLocation()
        {
            selectedTalkCategory = TalkCategory.Location;
            if (selectedTalkCategory == talkCategoryLastUsed)
                return;
            talkCategoryLastUsed = selectedTalkCategory;

            buttonCategoryLocation.BackgroundTexture = textureCategoryLocationHighlighted;
            buttonCategoryPerson.BackgroundTexture = textureCategoryPersonGrayedOut;
            buttonCategoryThings.BackgroundTexture = textureCategoryThingGrayedOut;
            buttonCategoryWork.BackgroundTexture = textureCategoryWorkGrayedOut;

            SetListboxTopics(ref listboxTopic, TalkManager.Instance.ListTopicLocation);
            listboxTopic.Update();

            UpdateScrollBarsTopic();
            UpdateScrollButtonsTopic();

            UpdateQuestion(listboxTopic.SelectedIndex);
        }

        protected virtual void SetTalkCategoryPeople()
        {
            selectedTalkCategory = TalkCategory.People;
            if (selectedTalkCategory == talkCategoryLastUsed)
                return;
            talkCategoryLastUsed = selectedTalkCategory;

            buttonCategoryLocation.BackgroundTexture = textureCategoryLocationGrayedOut;
            buttonCategoryPerson.BackgroundTexture = textureCategoryPersonHighlighted;
            buttonCategoryThings.BackgroundTexture = textureCategoryThingGrayedOut;
            buttonCategoryWork.BackgroundTexture = textureCategoryWorkGrayedOut;

            SetListboxTopics(ref listboxTopic, TalkManager.Instance.ListTopicPerson);
            listboxTopic.Update();

            UpdateScrollBarsTopic();
            UpdateScrollButtonsTopic();

            UpdateQuestion(listboxTopic.SelectedIndex);
        }

        protected virtual void SetTalkCategoryThings()
        {
            selectedTalkCategory = TalkCategory.Things;
            if (selectedTalkCategory == talkCategoryLastUsed)
                return;
            talkCategoryLastUsed = selectedTalkCategory;

            buttonCategoryLocation.BackgroundTexture = textureCategoryLocationGrayedOut;
            buttonCategoryPerson.BackgroundTexture = textureCategoryPersonGrayedOut;
            buttonCategoryThings.BackgroundTexture = textureCategoryThingHighlighted;
            buttonCategoryWork.BackgroundTexture = textureCategoryWorkGrayedOut;

            SetListboxTopics(ref listboxTopic, TalkManager.Instance.ListTopicThings);
            listboxTopic.Update();

            UpdateScrollBarsTopic();
            UpdateScrollButtonsTopic();

            UpdateQuestion(listboxTopic.SelectedIndex);
        }

        protected virtual void SetTalkCategoryWork()
        {
            selectedTalkCategory = TalkCategory.Work;
            if (selectedTalkCategory == talkCategoryLastUsed)
                return;
            talkCategoryLastUsed = selectedTalkCategory;

            buttonCategoryLocation.BackgroundTexture = textureCategoryLocationGrayedOut;
            buttonCategoryPerson.BackgroundTexture = textureCategoryPersonGrayedOut;
            buttonCategoryThings.BackgroundTexture = textureCategoryThingGrayedOut;
            buttonCategoryWork.BackgroundTexture = textureCategoryWorkHighlighted;

            ClearListboxTopics();
            listboxTopic.Update();

            UpdateScrollBarsTopic();
            UpdateScrollButtonsTopic();

            // create fake list item so that we can call function and set its questionType to QuestionType.Work
            TalkManager.ListItem listItem = new TalkManager.ListItem();
            listItem.questionType = TalkManager.QuestionType.Work;
            currentQuestion = TalkManager.Instance.GetQuestionText(listItem, selectedTalkTone);
            textlabelPlayerSays.Text = currentQuestion;
        }

        /// <summary>
        /// Gets safe scroll index.
        /// Scroller will be adjust to always be inside display range where possible.
        /// </summary>
        protected virtual int GetSafeScrollIndex(VerticalScrollBar scroller)
        {
            // Get current scroller index
            int scrollIndex = scroller.ScrollIndex;
            if (scrollIndex < 0)
                scrollIndex = 0;

            // Ensure scroll index within current range
            if (scrollIndex + scroller.DisplayUnits > scroller.TotalUnits)
            {
                scrollIndex = scroller.TotalUnits - scroller.DisplayUnits;
                if (scrollIndex < 0) scrollIndex = 0;
                scroller.Reset(scroller.DisplayUnits, scroller.TotalUnits, scrollIndex);
            }

            return scrollIndex;
        }

        /// <summary>
        /// Gets safe scroll index.
        /// Scroller will be adjust to always be inside display range where possible.
        /// </summary>
        protected virtual int GetSafeScrollIndex(HorizontalSlider slider)
        {
            // Get current scroller index
            int sliderIndex = slider.ScrollIndex;
            if (sliderIndex < 0)
                sliderIndex = 0;

            // Ensure scroll index within current range
            if (sliderIndex + slider.DisplayUnits > slider.TotalUnits)
            {
                sliderIndex = slider.TotalUnits - slider.DisplayUnits;
                if (sliderIndex < 0) sliderIndex = 0;
                slider.Reset(slider.DisplayUnits, slider.TotalUnits, sliderIndex);
            }

            return sliderIndex;
        }

        // Updates red/green state of scroller buttons
        protected virtual void UpdateListTopicScrollerButtons(VerticalScrollBar verticalScrollBar, int index, int count, Button upButton, Button downButton)
        {
            // Update up button
            if (index > 0)
                upButton.BackgroundTexture = arrowTopicUpGreen;
            else
                upButton.BackgroundTexture = arrowTopicUpRed;

            // Update down button
            if (index < (count - verticalScrollBar.DisplayUnits))
                downButton.BackgroundTexture = arrowTopicDownGreen;
            else
                downButton.BackgroundTexture = arrowTopicDownRed;

            // No items above or below
            if (count <= verticalScrollBar.DisplayUnits)
            {
                upButton.BackgroundTexture = arrowTopicUpRed;
                downButton.BackgroundTexture = arrowTopicDownRed;
            }
        }

        // Updates red/green state of left/right scroller buttons
        protected virtual void UpdateListTopicScrollerButtonsLeftRight(HorizontalSlider horizontalSlider, int index, int count, Button leftButton, Button rightButton)
        {
            // Update up button
            if (index > 0)
                leftButton.BackgroundTexture = arrowTopicLeftGreen;
            else
                leftButton.BackgroundTexture = arrowTopicLeftRed;

            // Update down button
            if (index < (count - horizontalSlider.DisplayUnits))
                rightButton.BackgroundTexture = arrowTopicRightGreen;
            else
                rightButton.BackgroundTexture = arrowTopicRightRed;

            // No items above or below
            if (count <= horizontalSlider.DisplayUnits)
            {
                leftButton.BackgroundTexture = arrowTopicLeftRed;
                rightButton.BackgroundTexture = arrowTopicRightRed;
            }
        }

        // Updates red/green state of scroller buttons
        protected virtual void UpdateListConversationScrollerButtons(VerticalScrollBar verticalScrollBar, int index, int count, Button upButton, Button downButton)
        {
            // Update up button
            if (index > 0)
                upButton.BackgroundTexture = arrowConversationUpGreen;
            else
                upButton.BackgroundTexture = arrowConversationUpRed;

            // Update down button
            if (index < (count - verticalScrollBar.DisplayUnits))
                downButton.BackgroundTexture = arrowConversationDownGreen;
            else
                downButton.BackgroundTexture = arrowConversationDownRed;

            // No items above or below
            if (count <= verticalScrollBar.DisplayUnits)
            {
                upButton.BackgroundTexture = arrowConversationUpRed;
                downButton.BackgroundTexture = arrowConversationDownRed;
            }
        }

        protected void ClearCurrentQuestion()
        {
            currentQuestion = "";
            textlabelPlayerSays.Text = "";
        }

        protected virtual void UpdateQuestion(int index)
        {
            TalkManager.ListItem listItem;
            if (selectedTalkOption == TalkOption.WhereIs && selectedTalkCategory == TalkCategory.Work)
            {
                listItem = new TalkManager.ListItem();
                listItem.questionType = TalkManager.QuestionType.Work;
            }
            else
            {
                if (index < 0 || index >= listboxTopic.Count)
                {
                    textlabelPlayerSays.Text = "";
                    return;
                }
                else
                {
                    listItem = listCurrentTopics[index];
                }
            }
            if (listItem.type == TalkManager.ListItemType.Item)
            {
                string anyAdvice = ADDialogue.LocalizationKeys["anyAdvice"] as string;
                string buyAnAle = ADDialogue.LocalizationKeys["buyAnAle"] as string;
                string buyABeer = ADDialogue.LocalizationKeys["buyABeer"] as string;
                string whoAreYou = ADDialogue.LocalizationKeys["whoAreYou"] as string;

                if (listItem.caption.Equals(anyAdvice, StringComparison.InvariantCultureIgnoreCase))
                {
                    if (isFirstAdviceRequest)
                    {
                        var firstAdviceVariants = ADDialogue.LocalizationKeys["firstAdviceVariants"] as List<string>;
                        currentQuestion = firstAdviceVariants[UnityEngine.Random.Range(0, firstAdviceVariants.Count)];
                    }
                    else
                    {
                        var adviceVariants = ADDialogue.LocalizationKeys["adviceVariants"] as List<string>;
                        currentQuestion = adviceVariants[UnityEngine.Random.Range(0, adviceVariants.Count)];
                    }
                }
                else if (listItem.caption.Equals(buyAnAle, StringComparison.InvariantCultureIgnoreCase))
                {
                    if (isFirstBuyAleRequest)
                    {
                        var firstBuyAleVariants = ADDialogue.LocalizationKeys["firstBuyAleVariants"] as List<string>;
                        currentQuestion = firstBuyAleVariants[UnityEngine.Random.Range(0, firstBuyAleVariants.Count)];
                    }
                    else
                    {
                        var buyAleVariants = ADDialogue.LocalizationKeys["buyAleVariants"] as List<string>;
                        currentQuestion = buyAleVariants[UnityEngine.Random.Range(0, buyAleVariants.Count)];
                    }
                }
                else if (listItem.caption.Equals(buyABeer, StringComparison.InvariantCultureIgnoreCase))
                {
                    if (isFirstBuyBeerRequest)
                    {
                        var firstBuyBeerVariants = ADDialogue.LocalizationKeys["firstBuyBeerVariants"] as List<string>;
                        currentQuestion = firstBuyBeerVariants[UnityEngine.Random.Range(0, firstBuyBeerVariants.Count)];
                    }
                    else
                    {
                        var buyBeerVariants = ADDialogue.LocalizationKeys["buyBeerVariants"] as List<string>;
                        currentQuestion = buyBeerVariants[UnityEngine.Random.Range(0, buyBeerVariants.Count)];
                    }
                }
                else if (listItem.caption.Equals(whoAreYou, StringComparison.InvariantCultureIgnoreCase))
                {
                    if (isFirstWhoAreYouRequest)
                    {
                        var firstWhoAreYouVariants = ADDialogue.LocalizationKeys["firstWhoAreYouVariants"] as List<string>;
                        currentQuestion = firstWhoAreYouVariants[UnityEngine.Random.Range(0, firstWhoAreYouVariants.Count)];
                    }
                    else
                    {
                        var whoAreYouVariants = ADDialogue.LocalizationKeys["whoAreYouVariants"] as List<string>;
                        currentQuestion = whoAreYouVariants[UnityEngine.Random.Range(0, whoAreYouVariants.Count)];
                    }
                }
                else
                {
                    currentQuestion = TalkManager.Instance.GetQuestionText(listItem, selectedTalkTone);
                }
            }
            else
            {
                currentQuestion = "";
            }

            // Set the text label for the player's say panel
            textlabelPlayerSays.Text = currentQuestion;
        }

        protected virtual void SetQuestionAnswerPairInConversationListbox(string question, string answer)
        {
            DaggerfallUI.Instance.PlayOneShot(SoundClips.ButtonClick);
            ListBox.ListItem textLabelQuestion;
            ListBox.ListItem textLabelAnswer;
            listboxConversation.AddItem(question, out textLabelQuestion);
            textLabelQuestion.textColor = DaggerfallUI.DaggerfallQuestionTextColor;
            textLabelQuestion.selectedTextColor = textcolorHighlighted; // textcolorQuestionHighlighted
            textLabelQuestion.textLabel.HorizontalAlignment = HorizontalAlignment.Right;
            textLabelQuestion.textLabel.HorizontalTextAlignment = TextLabel.HorizontalTextAlignmentSetting.Left;
            //textLabelQuestion.textLabel.BackgroundColor = new Color(0.3f, 0.4f, 0.9f);
            if (DaggerfallUnity.Settings.EnableModernConversationStyleInTalkWindow)
            {
                textLabelQuestion.textLabel.TextScale = textScaleModernConversationStyle;
                textLabelQuestion.textLabel.MaxWidth = (int)(textLabelQuestion.textLabel.MaxWidth * textBlockSizeModernConversationStyle);
                textLabelQuestion.textLabel.BackgroundColor = textcolorQuestionBackgroundModernConversationStyle;
            }
            listboxConversation.AddItem(answer, out textLabelAnswer);
            textLabelAnswer.selectedTextColor = textcolorHighlighted;
            textLabelAnswer.textLabel.HorizontalAlignment = HorizontalAlignment.Left;
            textLabelAnswer.textLabel.HorizontalTextAlignment = TextLabel.HorizontalTextAlignmentSetting.Left;
            //textLabelAnswer.textLabel.BackgroundColor = new Color(0.4f, 0.3f, 0.9f);
            if (DaggerfallUnity.Settings.EnableModernConversationStyleInTalkWindow)
            {
                textLabelAnswer.textLabel.TextScale = textScaleModernConversationStyle;
                textLabelAnswer.textLabel.MaxWidth = (int)(textLabelAnswer.textLabel.MaxWidth * textBlockSizeModernConversationStyle);
                textLabelAnswer.textLabel.BackgroundColor = textcolorAnswerBackgroundModernConversationStyle;
            }

            listboxConversation.SelectedIndex = listboxConversation.Count - 1; // always highlight the new answer

            UpdateScrollBarConversation();
            UpdateScrollButtonsConversation();
            isFirstAdviceRequest = false;
            isFirstWhoAreYouRequest = false;
            isFirstBuyAleRequest = false;
        }

        protected virtual void SelectTopicFromTopicList(int index, bool forceExecution = false)
        {
            // guard execution - this is important because I encountered a issue with listbox and double-click:
            // when changing listbox content and updating the listbox in the double click event callback the
            // corresponding item (at the screen position) of the newly created and set content will receive
            // the same double-click event and thus trigger its callback - which is (a) unwanted and (b) can lead -
            // in the case where the click position is a group item in first list and a "previous list" item
            // in linked second list - to an infinite loop (e.g. location list with group item on first position and
            // "previous" item on linked second list)
            // SIDE NOTE: don't use return inside this function (or if you do, don't forget to set
            // inListboxTopicContentUpdate to false again before!)
            if (inListboxTopicContentUpdate && !forceExecution)
                return;
            inListboxTopicContentUpdate = true;

            if (index < 0 || index >= listboxTopic.Count)
            {
                inListboxTopicContentUpdate = false;
                return;
            }

            string currentCaption = listCurrentTopics[index].caption;
            listboxTopic.SelectedIndex = index;
            TalkManager.ListItem listItem = listCurrentTopics[index];
            if (listItem.type == TalkManager.ListItemType.NavigationBack)
            {
                if (listItem.listParentItems != null)
                {
                    selectionIndexLastUsed = -1;
                    SetListboxTopics(ref listboxTopic, listItem.listParentItems);
                    DaggerfallUI.Instance.PlayOneShot(SoundClips.ButtonClick);
                }
            }
            else if (listItem.type == TalkManager.ListItemType.ItemGroup)
            {
                if (listItem.listChildItems != null)
                {
                    selectionIndexLastUsed = -1;
                    SetListboxTopics(ref listboxTopic, listItem.listChildItems);
                    DaggerfallUI.Instance.PlayOneShot(SoundClips.ButtonClick);
                }
            }
            else if (listItem.type == TalkManager.ListItemType.Item)
            {
                PlayerEntity player = GameManager.Instance.PlayerEntity;
                int toneIndex = TalkToneToIndex(selectedTalkTone);

                //Tally ettiquette or streetwise skill as appropriate
                if (toneIndex == 0)
                {
                    if (toneReactionForTalkSession[0] == 0)
                        player.TallySkill(DFCareer.Skills.Etiquette, 1);
                }
                else if (toneIndex == 2)
                {
                    if (toneReactionForTalkSession[2] == 0)
                        player.TallySkill(DFCareer.Skills.Streetwise, 1);
                }

                string answer;
                bool topicsUpdated = false;

                // Get the current day of the year
                DaggerfallDateTime dateTime = DaggerfallUnity.Instance.WorldTime.Now;
                int currentDayOfYear = dateTime.DayOfYear;

                // Get the NPC's name from filterVariables
                string npcName = filterVariables["Name"] as string;

                // Check if the NPC name exists in numAnswersGivenDialogue and get the count of answers given and the day of year
                if (numAnswersGivenDialogue.TryGetValue(npcName, out (int numAnswers, int dayOfYear) npcData))
                {
                    // Your existing logic can remain the same here
                    if (npcData.dayOfYear != currentDayOfYear)
                    {
                        npcData.numAnswers = 0; // Reset the count since it's a new day
                    }
                    currentNumAnswersGivenDialogue = npcData.numAnswers;
                }
                else
                {
                    currentNumAnswersGivenDialogue = 0; // No data means no answers have been given yet
                }

                if (listItem.questionType == TalkManager.QuestionType.News)
                {
                    answer = TalkManager.Instance.GetNewsOrRumors(); // Handle news or rumors
                }
                else
                {
                    string anyAdvice = ADDialogue.LocalizationKeys["anyAdvice"] as string;
                    string whoAreYou = ADDialogue.LocalizationKeys["whoAreYou"] as string;
                    string adviceKey = ADDialogue.LocalizationKeys["adviceKey"] as string;
                    string meKey = ADDialogue.LocalizationKeys["meKey"] as string;

                    // Check if this ListItem has a ADDialogue.DialogueListItem wrapper
                    ADDialogue.DialogueListItem dialogueItem = ADDialogue.instance.dialogueListItems.FirstOrDefault(di => di.ListItem == listItem);
                    if (dialogueItem != null)
                    {
                        string aleKey = ADDialogue.LocalizationKeys["aleKey"] as string;
                        string beerKey = ADDialogue.LocalizationKeys["beerKey"] as string;
                        string buyAnAle = ADDialogue.LocalizationKeys["buyAnAle"] as string;
                        string buyABeer = ADDialogue.LocalizationKeys["buyABeer"] as string;
                        string interlocutorKey = ADDialogue.LocalizationKeys["interlocutorKey"] as string;
                        string notEnoughGold = ADDialogue.LocalizationKeys["notEnoughGold"] as string;

                        string captionLower = dialogueItem.ListItem.caption.ToLower();

                        if (listItem.caption.Equals(buyAnAle, StringComparison.InvariantCultureIgnoreCase) ||
                            listItem.caption.Equals(buyABeer, StringComparison.InvariantCultureIgnoreCase))
                        {
                            // Step 1: Get the NPC Name from filterVariables
                            string aleNpcName = interlocutorKey;  // Default value if NPC name is not found
                            if (filterVariables.ContainsKey("Name"))
                            {
                                aleNpcName = filterVariables["Name"] as string;
                            }

                            // Set the ale price
                            int alePrice = 1; // Vanilla ale price

                            // Step 2: Calculate ale price (this mimics the tavern's ale price)
                            if (ADDialogue.CnCModEnabled)
                            {
                                alePrice = ADDialogue.CalculateAlePrice(); // Call the new method in ADDialogue
                            }

                            PlayerEntity playerEntity = GameManager.Instance.PlayerEntity;

                            // Step 3: Check if the player has enough gold
                            if (playerEntity.GetGoldAmount() < alePrice)
                            {
                                DaggerfallUI.MessageBox(notEnoughGold);
                                return;
                            }

                            // Subtract the ale price from the player's gold
                            playerEntity.DeductGoldAmount(alePrice);

                            // Step 4: Decrease numAnswersGivenDialogue by 3 after buying an ale or beer for the NPC
                            currentNumAnswersGivenDialogue = currentNumAnswersGivenDialogue - 3;
                            numAnswersGivenDialogue[npcName] = (currentNumAnswersGivenDialogue, currentDayOfYear);

                            // Determine the correct article and drink type for the message
                            string drinkType = listItem.caption.Equals(buyABeer, StringComparison.InvariantCultureIgnoreCase) ? beerKey : aleKey;

                            // Step 5: Show a message confirming the purchase with the NPC's name and the price of the drink
                            string boughtDrinkTemplate = ADDialogue.LocalizationKeys["boughtDrink"] as string;

                            // Replace placeholders with actual values
                            string boughtDrinkMessage = boughtDrinkTemplate
                                .Replace("{aleNpcName}", aleNpcName)
                                .Replace("{drinkType}", drinkType)
                                .Replace("{alePrice}", alePrice.ToString());

                            // Display the message
                            DaggerfallUI.MessageBox(boughtDrinkMessage);

                            // Fetch the answer from the DialogueData
                            string answerData = dialogueItem.DialogueData["Answer"] as string;

                            // Split the answer into possible responses if it contains '|'
                            string[] possibleAnswers = answerData.Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
                            int selectedIndex = UnityEngine.Random.Range(0, possibleAnswers.Length);
                            answer = possibleAnswers[selectedIndex].Trim();
                            answer = ProcessAnswerWithMacros(answer, this.GetMacroContextProvider(), -1);
                        }
                        // Check if the caption has been responded to before
                        else if (!ADDialogue.AD_Log && respondedCaptions.Contains(captionLower))
                        {
                            // Select a random response template
                            string responseTemplate = repeatedResponses[UnityEngine.Random.Range(0, repeatedResponses.Length)];

                            // Check if the caption is "Any advice?" or "Who are you?" and replace accordingly
                            string modifiedCaption = captionLower == anyAdvice.ToLower() 
                                ? adviceKey 
                                : captionLower == whoAreYou.ToLower() 
                                ? meKey 
                                : dialogueItem.ListItem.caption;

                            // Replace [caption] with the modified caption
                            responseTemplate = responseTemplate.Replace("[caption]", modifiedCaption);

                            // Process the answer string through macros and use it as the answer
                            answer = ProcessAnswerWithMacros(responseTemplate, this.GetMacroContextProvider(), -1);
                        }
                        else if (!ADDialogue.AD_Log && currentNumAnswersGivenDialogue >= maxNumAnswersNpcGivesDialogue)
                        {
                            // Select a random response template
                            string responseTemplate = exceededMaxResponses[UnityEngine.Random.Range(0, exceededMaxResponses.Length)];

                            // Check if the caption is "Any advice?" or "Who are you?" and replace accordingly
                            string modifiedCaption = captionLower == anyAdvice.ToLower() 
                                ? adviceKey 
                                : captionLower == whoAreYou.ToLower() 
                                ? meKey 
                                : dialogueItem.ListItem.caption;

                            // Replace [caption] with the modified caption
                            responseTemplate = responseTemplate.Replace("[caption]", modifiedCaption);

                            // Process the answer string through macros and use it as the answer
                            answer = ProcessAnswerWithMacros(responseTemplate, this.GetMacroContextProvider(), -1);
                        }
                        else
                        {
                            // Fetch the answer from the DialogueData
                            string answerData = dialogueItem.DialogueData["Answer"] as string;

                            // Split the answer into possible responses if it contains '|'
                            string[] possibleAnswers = answerData.Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
                            int selectedIndex = UnityEngine.Random.Range(0, possibleAnswers.Length);
                            answer = possibleAnswers[selectedIndex].Trim();
                            answer = ProcessAnswerWithMacros(answer, this.GetMacroContextProvider(), -1);

                            // Add to the set of responded captions
                            respondedCaptions.Add(captionLower);
                            currentNumAnswersGivenDialogue++;
                            numAnswersGivenDialogue[npcName] = (currentNumAnswersGivenDialogue, currentDayOfYear);

                            // Process AddCaption if it exists and only if the conditions are met
                            if (dialogueItem.DialogueData.ContainsKey("AddCaption"))
                            {
                                string addCaption = dialogueItem.DialogueData["AddCaption"] as string;
                                if (!string.IsNullOrEmpty(addCaption))
                                {
                                    string[] captionLists = addCaption.Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
                                    if (captionLists.Length > selectedIndex)
                                    {
                                        string selectedCaptionList = captionLists[selectedIndex].Trim();
                                        string[] captionsToAdd = selectedCaptionList.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                                        foreach (var caption in captionsToAdd)
                                        {
                                            string trimmedCaption = caption.Trim().ToLower();

                                            // Check existing dialogue items for this caption and validate conditions
                                            foreach (var item in ADDialogue.instance.dialogueListItems)
                                            {
                                                if (item.ListItem.caption.ToLower() == trimmedCaption && EvaluateConditions(item))
                                                {
                                                    if (!knownCaptions.Contains(trimmedCaption))
                                                    {
                                                        knownCaptions.Add(trimmedCaption);
                                                        topicsUpdated = true;
                                                        break; // Found a valid entry, no need to check further
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        // Check if the question is about the player's current location
                        if (listItem.questionType == TalkManager.QuestionType.WhereAmI)
                        {
                            // Check if the NPC is in a building using filterVariables
                            if (filterVariables.TryGetValue("In Building", out object isInBuilding) && (int)isInBuilding == 1)
                            {
                                // NPC is inside a building, get the building name
                                if (filterVariables.TryGetValue("Building Name", out object buildingName))
                                {
                                    // Convert the building name to lowercase and add it to knownCaptions
                                    string buildingNameToAdd = buildingName.ToString().ToLower();
                                    if (!knownCaptions.Contains(buildingNameToAdd))
                                    {
                                        knownCaptions.Add(buildingNameToAdd);
                                        topicsUpdated = true;
                                    }
                                }
                                else
                                {
                                    Debug.Log("Building Name not found in filterVariables.");
                                }
                            }
                            else
                            {
                                // NPC is not in a building, get the region name
                                if (filterVariables.TryGetValue("Region Name", out object currentRegionName))
                                {
                                    // Convert the region name to lowercase and add it to knownCaptions
                                    string regionNameToAdd = currentRegionName.ToString().ToLower();
                                    if (!knownCaptions.Contains(regionNameToAdd))
                                    {
                                        knownCaptions.Add(regionNameToAdd);
                                        topicsUpdated = true;
                                    }
                                }
                                else
                                {
                                    Debug.Log("Region Name not found in filterVariables.");
                                }
                            }

                            // NPC is not in a building, get the location name (only if non-null string)
                            if (filterVariables.TryGetValue("Location Name", out object currentLocationName)
                                && currentLocationName is string currentLocationNameStr
                                && !string.IsNullOrEmpty(currentLocationNameStr))
                            {
                                string locationNameToAdd = currentLocationNameStr.ToLower();
                                if (!knownCaptions.Contains(locationNameToAdd))
                                {
                                    knownCaptions.Add(locationNameToAdd);
                                    topicsUpdated = true;
                                }
                            }
                            else
                            {
                                Debug.Log("ADTalkWindow: skipping Location Name because it's missing or null.");
                            }
                        }

                        // Fetch the answer using the vanilla method
                        if (selectedTalkOption == TalkOption.TellMeAbout) {
                            SetTalkModeWhereIs(); // Apparently regular TalkWindow runs in the background fighting to control the dialogue, so we have to "reset" the window here
                            SetTalkModeTellMeAbout();
                        }
                        answer = TalkManager.Instance.GetAnswerText(listItem);
                    }
                }

                if (topicsUpdated) // Only update the topics if at least one new caption was added
                {
                    UpdateTellMeAboutTopics();
                }

                // Restore the selection based on ListItem's caption
                int newIndex = listCurrentTopics.FindIndex(x => x.caption == currentCaption);
                if (newIndex != -1)
                {
                    listboxTopic.SelectedIndex = newIndex;
                }
                UpdateQuestion(newIndex);
                SetQuestionAnswerPairInConversationListbox(currentQuestion, answer);
            }
            inListboxTopicContentUpdate = false;
        }

        public void GetFilterData()
        {
            filterVariables.Clear(); // Clear the existing dictionary before populating

            // Get the instance of PlayerGPS from GameManager
            PlayerGPS playerGPS = GameManager.Instance.PlayerGPS;

            //Debug.Log("Checking NPC Type...");
            if (TalkManager.Instance.CurrentNPCType == TalkManager.NPCType.Static)
            {
                //Debug.Log("Talking to a Static NPC");
                StaticNPC staticNpc = TalkManager.Instance.StaticNPC;
                if (staticNpc != null)
                {
                    StaticNPC.NPCData npcData = staticNpc.Data;
                    PersistentFactionData persistentFactionData = GameManager.Instance.PlayerEntity.FactionData;
                    FactionFile.FactionData factionData;

                    // Store static NPC data
                    filterVariables["Type"] = "Static";
                    filterVariables["Hash"] = npcData.hash;
                    filterVariables["Flags"] = npcData.flags;
                    filterVariables["Faction ID"] = npcData.factionID;
                    filterVariables["Name Seed"] = npcData.nameSeed;
                    filterVariables["Gender"] = npcData.gender;
                    filterVariables["Race"] = npcData.race;
                    filterVariables["Context"] = npcData.context;
                    filterVariables["Map ID"] = npcData.mapID;
                    filterVariables["Location ID"] = npcData.locationID;
                    // Check if the NPC is inside a building
                    if (GameManager.Instance.IsPlayerInside && GameManager.Instance.PlayerEnterExit.ExteriorDoors.Length > 0)
                    {
                        filterVariables["In Building"] = 1; // NPC is inside a building

                        // Try to get the discovered building data
                        PlayerGPS.DiscoveredBuilding discoveredBuilding;
                        if (GameManager.Instance.PlayerGPS.GetAnyBuilding(GameManager.Instance.PlayerEnterExit.ExteriorDoors[0].buildingKey, out discoveredBuilding))
                        {
                            filterVariables["Building Name"] = discoveredBuilding.displayName;
                            filterVariables["Building Type"] = discoveredBuilding.buildingType;
                            //filterVariables["Building FactionID"] = discoveredBuilding.factionId;
                        }
                        else
                        {
                            // Fallback to a default building name if no discovered building is found
                            filterVariables["Building Name"] = TextManager.Instance.GetLocalizedLocationName(playerGPS.CurrentMapID, playerGPS.CurrentLocation.Name); // Default value if no building name can be retrieved
                        }
                    }
                    else
                    {
                        filterVariables["In Building"] = 0; // NPC is not inside a building
                        filterVariables["Building Name"] = TextManager.Instance.GetLocalizedLocationName(playerGPS.CurrentMapID, playerGPS.CurrentLocation.Name); // Default value if outside
                    }
                    filterVariables["Building Key"] = npcData.buildingKey;
                    filterVariables["Name Bank"] = npcData.nameBank;
                    filterVariables["Billboard Archive Index"] = npcData.billboardArchiveIndex;
                    filterVariables["Billboard Record Index"] = npcData.billboardRecordIndex;
                    filterVariables["Name"] = staticNpc.DisplayName;
                    filterVariables["Child NPC"] = staticNpc.IsChildNPC;

                    filterVariables["NoLore"] = 0; // Default or other values not specified

                    // Store faction details if available
                    if (persistentFactionData.GetFactionData(npcData.factionID, out factionData))
                    {
                        filterVariables["Faction Name"] = TextManager.Instance.GetLocalizedFactionName(npcData.factionID, factionData.name);
                        filterVariables["Faction Parent"] = factionData.parent;
                        filterVariables["Faction Type"] = factionData.type;
                        filterVariables["Faction Reputation"] = factionData.rep;
                        filterVariables["Faction Region"] = factionData.region;
                        filterVariables["Faction Power"] = factionData.power;
                        filterVariables["Faction Allies"] = new int[] { factionData.ally1, factionData.ally2, factionData.ally3 };
                        filterVariables["Faction Enemies"] = new int[] { factionData.enemy1, factionData.enemy2, factionData.enemy3 };
                        filterVariables["Faction Social Group"] = factionData.sgroup;
                        filterVariables["Faction Guild Group"] = factionData.ggroup;
                        filterVariables["Faction Vampire"] = factionData.vam;
                        filterVariables["Faction Children"] = factionData.children;

                        // Set NPC NoLore based on Faction ID for children
                        if (npcData.factionID == 514)
                        {
                            filterVariables["NoLore"] = 1;
                        }
                        else
                        {
                            // Set NPC NoLore based on Faction Type
                            int factionType = factionData.type;
                            if (new int[] { 2, 3, 5, 6, 8, 9, 10, 13 }.Contains(factionType))
                            {
                                filterVariables["NoLore"] = 2;
                            }
                            else if (new int[] { 0, 1, 4 }.Contains(factionType))
                            {
                                filterVariables["NoLore"] = 3;
                            }
                        }
                    }
                }
            }
            else if (TalkManager.Instance.CurrentNPCType == TalkManager.NPCType.Mobile)
            {
                Debug.Log("Talking to a Mobile NPC");
                MobilePersonNPC mobileNpc = TalkManager.Instance.MobileNPC;
                if (mobileNpc != null)
                {
                    // Store mobile NPC data
                    filterVariables["Type"] = "Mobile";
                    filterVariables["Race"] = mobileNpc.Race;
                    filterVariables["Gender"] = mobileNpc.Gender;
                    filterVariables["Name"] = mobileNpc.NameNPC;
                    filterVariables["Guard"] = mobileNpc.IsGuard;
                    filterVariables["Outfit Variant"] = mobileNpc.PersonOutfitVariant;
                    filterVariables["Faction Social Group"] = 1;
                    filterVariables["NoLore"] = 0;  // Default or other values not specified
                }
            }
            else
            {
                Debug.Log("No NPC or Unknown NPC Type");
            }

            // Get the instance of DaggerfallDateTime
            DaggerfallDateTime dateTime = DaggerfallUnity.Instance.WorldTime.Now;

            // Store date and time information
            filterVariables["Year"] = dateTime.Year;
            filterVariables["Month"] = dateTime.Month;
            filterVariables["MonthName"] = dateTime.MonthName;
            filterVariables["Day"] = dateTime.DayOfMonth;
            filterVariables["DayName"] = dateTime.DayName;
            filterVariables["Season"] = dateTime.SeasonName;
            filterVariables["Daytime"] = dateTime.IsDay;
            filterVariables["Nighttime"] = dateTime.IsNight;
            filterVariables["Massar Lunar Phase"] = dateTime.MassarLunarPhase;
            filterVariables["Secunda Lunar Phase"] = dateTime.SecundaLunarPhase;

            // WeatherManager instance
            WeatherManager weatherManager = GameManager.Instance.WeatherManager;

            // Store weather information
            filterVariables["Raining"] = weatherManager.IsRaining;
            filterVariables["Storming"] = weatherManager.IsStorming;
            filterVariables["Snowing"] = weatherManager.IsSnowing;
            filterVariables["Overcast"] = weatherManager.IsOvercast;

            // Calculate holiday ID and store holiday name
            const int holidaysStartID = 8349;
            uint minutes = DaggerfallUnity.Instance.WorldTime.DaggerfallDateTime.ToClassicDaggerfallTime();
            int holidayId = Formulas.FormulaHelper.GetHolidayId(minutes, playerGPS.CurrentRegionIndex);
            if (holidayId != 0)
            {
                TextFile.Token[] tokens = DaggerfallUnity.Instance.TextProvider.GetRSCTokens(holidaysStartID + holidayId);
                if (tokens != null && tokens.Length > 0)
                {
                    filterVariables["Holiday"] = tokens[0].text;
                }
                else
                {
                    filterVariables["Holiday"] = "Failed to retrieve holiday name.";
                }
            }
            else
            {
                filterVariables["Holiday"] = "No holiday today.";
            }

            // Store Player GPS data
            filterVariables["Map Pixel"] = playerGPS.CurrentMapPixel;
            filterVariables["Climate Index"] = playerGPS.CurrentClimateIndex;
            filterVariables["Politic Index"] = playerGPS.CurrentPoliticIndex;
            filterVariables["Region Index"] = playerGPS.CurrentRegionIndex;
            filterVariables["Location Index"] = playerGPS.CurrentLocationIndex;
            filterVariables["MapID"] = playerGPS.CurrentMapID;
            filterVariables["Has Location"] = playerGPS.HasCurrentLocation;
            filterVariables["Player In Location Rect"] = playerGPS.IsPlayerInLocationRect;
            filterVariables["Region"] = playerGPS.CurrentRegion;
            filterVariables["Region Name"] = TextManager.Instance.GetLocalizedRegionName(playerGPS.CurrentRegionIndex);
            filterVariables["Climate Settings"] = playerGPS.ClimateSettings;
            filterVariables["Location"] = playerGPS.CurrentLocation;
            filterVariables["Location Name"] = TextManager.Instance.GetLocalizedLocationName(playerGPS.CurrentMapID, playerGPS.CurrentLocation.Name);
            filterVariables["Location Type"] = playerGPS.CurrentLocationType;

            // Utility Methods of PlayerGPS
            filterVariables["Name Bank of Region"] = playerGPS.GetNameBankOfCurrentRegion();
            filterVariables["Race of Region"] = playerGPS.GetRaceOfCurrentRegion();
            filterVariables["People of Region (Faction ID)"] = playerGPS.GetPeopleOfCurrentRegion();
            filterVariables["Region Faction (Faction ID)"] = playerGPS.GetCurrentRegionFaction();
            // Court of Region (Faction ID) - requires exception for Mournoth
            try
            {
                int courtFactionId = playerGPS.GetCourtOfCurrentRegion();
                filterVariables["Court of Region (Faction ID)"] = courtFactionId;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"ADTalkWindow: unable to get court of current region – {e.Message}");
                // Optionally set a default or leave it out:
                filterVariables["Court of Region (Faction ID)"] = null;
            }
            filterVariables["Region Vampire Clan (Faction ID)"] = playerGPS.GetCurrentRegionVampireClan();
            filterVariables["Dominant Temple in Region (Faction ID)"] = playerGPS.GetTempleOfCurrentRegion();
            filterVariables["Player In Town"] = playerGPS.IsPlayerInTown();

            // Get the instance of PlayerEntity from GameManager
            PlayerEntity player = GameManager.Instance.PlayerEntity;

            // Store player-specific data
            filterVariables["Health"] = player.CurrentHealth;
            filterVariables["Max Health"] = player.MaxHealth;
            filterVariables["Magicka"] = player.CurrentMagicka;
            filterVariables["Max Magicka"] = player.MaxMagicka;
            filterVariables["Fatigue"] = player.CurrentFatigue;
            filterVariables["Max Fatigue"] = player.MaxFatigue;
            filterVariables["Gold"] = player.GoldPieces;

            // Player Skills
            foreach (DFCareer.Skills skill in Enum.GetValues(typeof(DFCareer.Skills)))
            {
                if (skill != DFCareer.Skills.None)
                {
                    filterVariables[skill.ToString()] = player.Skills.GetPermanentSkillValue(skill);
                }
            }

            // Player Inventory
            ItemCollection inventory = GameManager.Instance.PlayerEntity.Items;

            var itemSummary = new Dictionary<string, int>();
            for (int i = 0; i < inventory.Count; i++)
            {
                DaggerfallUnityItem item = inventory.GetItem(i);
                if (item != null)
                {
                    // Use TextManager to get the localized name, with item.ItemName as fallback if localization is unavailable
                    string itemName = TextManager.Instance.GetLocalizedItemName(item.TemplateIndex, item.ItemName);
                    
                    if (itemSummary.ContainsKey(itemName))
                    {
                        itemSummary[itemName] += item.stackCount;
                    }
                    else
                    {
                        itemSummary[itemName] = item.stackCount;
                    }
                }
            }

            // Store each item count in filterVariables with localized names
            foreach (var item in itemSummary)
            {
                filterVariables["Item: " + item.Key] = item.Value;
            }

            // Additional inventory metrics
            //filterVariables["Inventory Weight"] = inventory.GetWeight(); // total weight of items

            // Query all active quests
            ulong[] activeQuests = QuestMachine.Instance.GetAllActiveQuests();
            foreach (ulong questUID in activeQuests)
            {
                Quest quest = QuestMachine.Instance.GetQuest(questUID);
                if (quest != null)
                {
                    List<int> messageIDs = new List<int>();
                    Quest.LogEntry[] logEntries = quest.GetLogMessages();
                    foreach (Quest.LogEntry entry in logEntries)
                    {
                        // Only add unique message IDs to avoid duplicates
                        if (!messageIDs.Contains(entry.messageID))
                            messageIDs.Add(entry.messageID);
                    }
                    // Add each quest as a separate entry in filterVariables
                    filterVariables["Quest: " + quest.QuestName] = string.Join(", ", messageIDs);
                }
            }

            // Get NPC faction data and reactions
            GetTotalReactionToPlayer();

            // Generate a random number for the NPC
            System.Random random = new System.Random();
            int randomNumber = random.Next(1, 101); // Random number between 1 and 100
            filterVariables["Random Number (NPC)"] = randomNumber;

            // Get active mods and store them in a string variable
            var activeMods = ModManager.Instance.Mods.ToList();

            StringBuilder modList = new StringBuilder();
            foreach (var mod in activeMods)
            {
                modList.AppendLine($"{mod.Title} (Version: {mod.ModInfo.ModVersion})");
            }

            // Trim any trailing space
            filterVariables["Active Mods"] = modList.ToString().TrimEnd();

            // Log all filterVariables, only if ADDialogue.AD_Log is enabled
            if (ADDialogue.AD_Log)
            {
                foreach (var item in filterVariables)
                {
                    Debug.LogFormat("{0}: {1}", item.Key, item.Value);
                }
            }
        }

        public FactionFile.FactionData GetFactionData()
        {
            FactionFile.FactionData factionData = new FactionFile.FactionData();  // Default or new instance
            if (TalkManager.Instance.CurrentNPCType == TalkManager.NPCType.Static)
            {
                StaticNPC staticNpc = TalkManager.Instance.StaticNPC;
                if (staticNpc != null)
                {
                    StaticNPC.NPCData npcData = staticNpc.Data;
                    PersistentFactionData persistentFactionData = GameManager.Instance.PlayerEntity.FactionData;
                    if (persistentFactionData.GetFactionData(npcData.factionID, out FactionFile.FactionData tempFactionData))
                    {
                        factionData = tempFactionData;  // Assign if found
                    }
                }
            }
            return factionData;  // Return either found data or default
        }

        public void GetTotalReactionToPlayer()
        {
            // Get NPC faction data and reactions
            FactionFile.FactionData npcFactionData = GetFactionData(); // Retrieve the NPC's faction data
            int reactionToPlayer = GetReactionToPlayer(npcFactionData); // Calculate reaction based on player's attributes and NPC's faction
            FactionFile.SocialGroups npcSocialGroup = (FactionFile.SocialGroups)npcFactionData.sgroup; // Retrieve the social group of the NPC
            int reactionToPlayer_0_1_2 = GetReactionToPlayer_0_1_2(npcSocialGroup); // Calculate reaction based on social group and tone

            int totalReactionToPlayer = reactionToPlayer + reactionToPlayer_0_1_2; // Sum up all reactions
            filterVariables["Reaction"] = totalReactionToPlayer; // Store the total reaction in the filter variables
            int npcNoLore = Convert.ToInt32(filterVariables["NoLore"]);

            maxNumAnswersNpcGivesDialogue = 1 + totalReactionToPlayer + npcNoLore;

            // Log the reaction, if debugging is enabled
            if (ADDialogue.AD_Log)
            {
                Debug.LogFormat("Total NPC Reaction: {0}", totalReactionToPlayer);
            }
        }

        public int GetReactionToPlayer(FactionFile.FactionData factionData)
        {
            PlayerEntity player = GameManager.Instance.PlayerEntity;

            int reaction = factionData.rep + player.BiographyReactionMod + player.GetReactionMod((FactionFile.SocialGroups)factionData.sgroup);

            if (factionData.sgroup >= 0 && factionData.sgroup < player.SGroupReputations.Length)
                reaction += player.SGroupReputations[factionData.sgroup];

            // Divide reaction by 10 and round down to the next integer
            int adjustedReaction = (int)Math.Floor(reaction / 10.0);

            // Log the reaction, if debugging is enabled
            if (ADDialogue.AD_Log)
            {
                Debug.Log($"GetReactionToPlayer: Original reaction value: {reaction}, Adjusted reaction value: {adjustedReaction}");
            }

            return adjustedReaction;
        }

        int GetReactionToPlayer_0_1_2(FactionFile.SocialGroups npcSocialGroup)
        {
            if (TalkManager.Instance.NPCsAlwaysFriendly())
            {
                return 2;
            }

            int socialGroup = (int)npcSocialGroup;
            if (socialGroup >= 5)
                socialGroup = 1; // Merchants

            int toneModifier = 0;
            int toneIndex = TalkToneToIndex(selectedTalkTone);
            PlayerEntity player = GameManager.Instance.PlayerEntity;
            short skillValue = 0;

            if (toneIndex == 0)
            {
                skillValue = player.Skills.GetLiveSkillValue(DFCareer.Skills.Etiquette);
                toneModifier += etiquetteReactionMods[socialGroup];
            }
            else if (toneIndex == 2)
            {
                skillValue = player.Skills.GetLiveSkillValue(DFCareer.Skills.Streetwise);
                toneModifier += streetwiseReactionMods[socialGroup];
            }

            if (toneIndex != 1)
                toneModifier += Dice100.FailedRoll(skillValue) ? -10 : 5;

            // Convert question type to index to classic data
            //int classicQuestionIndex = GetClassicQuestionIndex(qt);
            int reaction = player.Stats.LivePersonality / 5
            //   + questionTypeReactionMods[classicQuestionIndex]
               + toneModifier;

            // Make roll result be the same every time for a given NPC
            if (currentNPCType == ADTalkWindow.NPCType.Mobile)
                DFRandom.Seed = (uint)lastTargetMobileNPC.GetHashCode();
            else if (currentNPCType == ADTalkWindow.NPCType.Static)
                DFRandom.Seed = (uint)lastTargetStaticNPC.GetHashCode();

            int rollToBeat = DFRandom.random_range_inclusive(0, 20);
            if (toneReactionForTalkSession[toneIndex] != 0)
                reaction = toneReactionForTalkSession[toneIndex];
            else
                toneReactionForTalkSession[toneIndex] = reaction;

            // Store that we've done the reaction check for this tone this talk session
            lastToneIndex = toneIndex;

            int result;
            if (reaction < rollToBeat)
                result = 0;
            else if (reaction < rollToBeat + 20)
                result = 1;
            else
                result = 2;

            if (ADDialogue.AD_Log)
            {
                Debug.Log($"GetReactionToPlayer_0_1_2: Returning reaction category of {result}");
            }
    return result;
        }

        public void RemoveNumAnswersGivenForNPC()
        {
            DaggerfallDateTime dateTime = DaggerfallUnity.Instance.WorldTime.Now;
            int currentDayOfYear = dateTime.DayOfYear;

            var keysToRemove = new List<string>();
            foreach (var entry in numAnswersGivenDialogue)
            {
                if (entry.Value.dayOfYear != currentDayOfYear)
                {
                    keysToRemove.Add(entry.Key);
                }
            }

            foreach (var key in keysToRemove)
            {
                numAnswersGivenDialogue.Remove(key);
            }
        }

        #region event handlers

        protected virtual void VerticalScrollBarTopic_OnScroll()
        {
            // Update scroller
            verticalScrollBarTopic.TotalUnits = listboxTopic.HeightContent(); //listboxTopic.Count;
            int scrollIndex = GetSafeScrollIndex(verticalScrollBarTopic);

            // Update scroller buttons
            //UpdateListScrollerButtons(verticalScrollBarTopic, scrollIndex, listboxTopic.Count, buttonTopicUp, buttonTopicDown);
            UpdateListTopicScrollerButtons(verticalScrollBarTopic, scrollIndex, listboxTopic.HeightContent(), buttonTopicUp, buttonTopicDown);

            listboxTopic.ScrollIndex = scrollIndex;
            listboxTopic.Update();
        }

        protected virtual void HorizontalSliderTopic_OnScroll()
        {
            // Update scroller
            horizontalSliderTopic.TotalUnits = widthOfLongestItemInListBox; //lengthOfLongestItemInListBox;
            int horizontalScrollIndex = GetSafeScrollIndex(horizontalSliderTopic); // horizontalSliderTopicWindow.ScrollIndex;

            // Update scroller buttons
            //UpdateListScrollerButtonsLeftRight(horizontalScrollIndex, lengthOfLongestItemInListBox, buttonTopicLeft, buttonTopicRight);
            UpdateListTopicScrollerButtonsLeftRight(horizontalSliderTopic, horizontalScrollIndex, widthOfLongestItemInListBox, buttonTopicLeft, buttonTopicRight);

            listboxTopic.HorizontalScrollIndex = horizontalScrollIndex;
            listboxTopic.Update();
        }

        protected virtual void VerticalScrollBarConversation_OnScroll()
        {
            // Update scroller
            verticalScrollBarConversation.TotalUnits = listboxConversation.HeightContent();
            int scrollIndex = GetSafeScrollIndex(verticalScrollBarConversation);

            // Update scroller buttons
            UpdateListConversationScrollerButtons(verticalScrollBarConversation, scrollIndex, listboxConversation.HeightContent(), buttonConversationUp, buttonConversationDown);

            listboxConversation.ScrollIndex = scrollIndex;
            listboxConversation.Update();
        }

        protected virtual void ListboxTopic_OnSelectItem()
        {
            int index = listboxTopic.SelectedIndex;
            if (index != selectionIndexLastUsed)
            UpdateQuestion(index);
            selectionIndexLastUsed = index;
        }

        protected virtual void ListboxTopic_OnUseSelectedItem()
        {
            SelectTopicFromTopicList(listboxTopic.SelectedIndex);
        }

        protected virtual void ListBoxTopic_OnScroll()
        {
            int scrollIndex = listboxTopic.ScrollIndex;

            // Update scroller
            verticalScrollBarTopic.SetScrollIndexWithoutRaisingScrollEvent(scrollIndex); // important to use this function here to prevent creating infinite callback loop
            verticalScrollBarTopic.Update();

            // Update scroller buttons
            UpdateListTopicScrollerButtons(verticalScrollBarTopic, scrollIndex, listboxTopic.HeightContent(), buttonTopicUp, buttonTopicDown);
        }

        protected virtual void ListBoxConversation_OnScroll()
        {
            int scrollIndex = listboxConversation.ScrollIndex;

            // Update scroller
            verticalScrollBarConversation.SetScrollIndexWithoutRaisingScrollEvent(scrollIndex); // important to use this function here to prevent creating infinite callback loop
            verticalScrollBarConversation.Update();

            // Update scroller buttons
            UpdateListConversationScrollerButtons(verticalScrollBarConversation, scrollIndex, listboxConversation.HeightContent(), buttonConversationUp, buttonConversationDown);
        }

        protected virtual void ButtonTopicUp_OnMouseClick(BaseScreenComponent sender, Vector2 position)
        {
            DaggerfallUI.Instance.PlayOneShot(SoundClips.ButtonClick);
            verticalScrollBarTopic.ScrollIndex -=5;
        }

        protected virtual void ButtonTopicDown_OnMouseClick(BaseScreenComponent sender, Vector2 position)
        {
            DaggerfallUI.Instance.PlayOneShot(SoundClips.ButtonClick);
            verticalScrollBarTopic.ScrollIndex +=5;
        }

        protected virtual void ButtonTopicLeft_OnMouseClick(BaseScreenComponent sender, Vector2 position)
        {
            DaggerfallUI.Instance.PlayOneShot(SoundClips.ButtonClick);
            horizontalSliderTopic.ScrollIndex--;
        }

        protected virtual void ButtonTopicRight_OnMouseClick(BaseScreenComponent sender, Vector2 position)
        {
            DaggerfallUI.Instance.PlayOneShot(SoundClips.ButtonClick);
            horizontalSliderTopic.ScrollIndex++;
        }

        protected virtual void ButtonConversationUp_OnMouseClick(BaseScreenComponent sender, Vector2 position)
        {
            DaggerfallUI.Instance.PlayOneShot(SoundClips.ButtonClick);
            verticalScrollBarConversation.ScrollIndex -= 5;
        }

        protected virtual void ButtonConversationDown_OnMouseClick(BaseScreenComponent sender, Vector2 position)
        {
            DaggerfallUI.Instance.PlayOneShot(SoundClips.ButtonClick);
            verticalScrollBarConversation.ScrollIndex += 5;
        }

        protected virtual void ButtonTellMeAbout_OnMouseClick(BaseScreenComponent sender, Vector2 position)
        {
            DaggerfallUI.Instance.PlayOneShot(SoundClips.ButtonClick);
            SetTalkModeTellMeAbout();
        }

        protected virtual void ButtonWhereIs_OnMouseClick(BaseScreenComponent sender, Vector2 position)
        {
            SetTalkModeWhereIs();
        }

        protected virtual void ButtonCategoryLocation_OnMouseClick(BaseScreenComponent sender, Vector2 position)
        {
            if (selectedTalkOption == TalkOption.WhereIs)
            {
                DaggerfallUI.Instance.PlayOneShot(SoundClips.ButtonClick);
                SetTalkCategoryLocation();
            }
        }

        protected virtual void ButtonCategoryPeople_OnMouseClick(BaseScreenComponent sender, Vector2 position)
        {
            if (selectedTalkOption == TalkOption.WhereIs)
            {
                DaggerfallUI.Instance.PlayOneShot(SoundClips.ButtonClick);
                SetTalkCategoryPeople();
            }
        }

        protected virtual void ButtonCategoryThings_OnMouseClick(BaseScreenComponent sender, Vector2 position)
        {
            if (selectedTalkOption == TalkOption.WhereIs)
            {
                DaggerfallUI.Instance.PlayOneShot(SoundClips.ButtonClick);
                SetTalkCategoryThings();
            }
        }

        protected virtual void ButtonCategoryWork_OnMouseClick(BaseScreenComponent sender, Vector2 position)
        {
            if (selectedTalkOption == TalkOption.WhereIs)
            {
                DaggerfallUI.Instance.PlayOneShot(SoundClips.ButtonClick);
                SetTalkCategoryWork();
            }
        }

        protected virtual void ButtonTonePolite_OnClickHandler(BaseScreenComponent sender, Vector2 position)
        {
            DaggerfallUI.Instance.PlayOneShot(SoundClips.ButtonClick);
            selectedTalkTone = TalkTone.Polite;
            if (TalkToneToIndex(selectedTalkTone) == toneLastUsed)
                return;
            toneLastUsed = TalkToneToIndex(selectedTalkTone);
            UpdateCheckboxes();
            UpdateQuestion(listboxTopic.SelectedIndex);
            GetTotalReactionToPlayer();
        }

        protected virtual void ButtonToneNormal_OnClickHandler(BaseScreenComponent sender, Vector2 position)
        {
            DaggerfallUI.Instance.PlayOneShot(SoundClips.ButtonClick);
            selectedTalkTone = TalkTone.Normal;
            if (TalkToneToIndex(selectedTalkTone) == toneLastUsed)
                return;
            toneLastUsed = TalkToneToIndex(selectedTalkTone);
            UpdateCheckboxes();
            UpdateQuestion(listboxTopic.SelectedIndex);
            GetTotalReactionToPlayer();
        }

        protected virtual void ButtonToneBlunt_OnClickHandler(BaseScreenComponent sender, Vector2 position)
        {
            DaggerfallUI.Instance.PlayOneShot(SoundClips.ButtonClick);
            selectedTalkTone = TalkTone.Blunt;
            if (TalkToneToIndex(selectedTalkTone) == toneLastUsed)
                return;
            toneLastUsed = TalkToneToIndex(selectedTalkTone);
            UpdateCheckboxes();
            UpdateQuestion(listboxTopic.SelectedIndex);
            GetTotalReactionToPlayer();
        }

        protected virtual void ButtonOkay_OnMouseClick(BaseScreenComponent sender, Vector2 position)
        {
            if (selectedTalkOption == TalkOption.WhereIs && selectedTalkCategory == TalkCategory.Work)
            {
                // create fake list item so that we can call function and set its questionType to QuestionType.Work
                TalkManager.ListItem listItem = new TalkManager.ListItem();
                listItem.questionType = TalkManager.QuestionType.Work;
                string answer = TalkManager.Instance.GetAnswerText(listItem);

                SetQuestionAnswerPairInConversationListbox(currentQuestion, answer);
            }
            else
            {
                SelectTopicFromTopicList(listboxTopic.SelectedIndex);
            }
        }

        protected virtual void ButtonLogbook_OnMouseClick(BaseScreenComponent sender, Vector2 position)
        {
            DaggerfallUI.Instance.PlayOneShot(SoundClips.ButtonClick);
            if (listboxConversation.SelectedIndex < 0)
                return;

            if (!copyIndexes.Contains(listboxConversation.SelectedIndex))
            {
                copyIndexes.Add(listboxConversation.SelectedIndex);
                MarkCopiedListItem(listboxConversation.GetItem(listboxConversation.SelectedIndex));
            }
            else
            {
                copyIndexes.Remove(listboxConversation.SelectedIndex);
                MarkCopiedListItem(listboxConversation.GetItem(listboxConversation.SelectedIndex), true);
            }
        }

        protected virtual void ButtonLogbook_OnRightMouseClick(BaseScreenComponent sender, Vector2 position)
        {
            DaggerfallUI.Instance.PlayOneShot(SoundClips.ButtonClick);
            copyIndexes.Clear();
            for (int idx = 0; idx < listboxConversation.Count; idx++)
            {
                copyIndexes.Add(idx);
                MarkCopiedListItem(listboxConversation.GetItem(idx));
            }
        }

        protected virtual void MarkCopiedListItem(ListBox.ListItem item, bool unmark = false)
        {
            DaggerfallUI.Instance.PlayOneShot(SoundClips.ButtonClick);
            if (unmark)
            {
                item.shadowColor = DaggerfallUI.DaggerfallDefaultShadowColor;
                item.selectedShadowColor = DaggerfallUI.DaggerfallDefaultShadowColor;
            }
            else
            {
                item.shadowColor = Color.blue;
                item.selectedShadowColor = Color.blue;
            }
        }

        protected virtual void ButtonGoodbye_OnMouseClick(BaseScreenComponent sender, Vector2 position)
        {
            DaggerfallUI.Instance.PlayOneShot(SoundClips.ButtonClick);
            CloseWindow();
        }

        protected virtual void ButtonGoodbye_OnKeyboardEvent(BaseScreenComponent sender, Event keyboardEvent)
        {
            if (keyboardEvent.type == EventType.KeyDown)
            {
                DaggerfallUI.Instance.PlayOneShot(SoundClips.ButtonClick);
                isCloseWindowDeferred = true;
            }
            else if (keyboardEvent.type == EventType.KeyUp && isCloseWindowDeferred)
            {
                isCloseWindowDeferred = false;
                CloseWindow();
            }
        }

        #endregion
    }
}
