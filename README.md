### Advanced Dialogue

This mod adds filter-based NPC dialogue for Daggerfall Unity, similar to that in Morrowind.

**Main Features**
The mod currently includes:
- Over 25,000 words of dialogue.
- Extensive regional dialogue for ﻿Daggerfall, Sentinel, and Wayrest.
- Additional dialogue for Abibon-Gora, Alcaire, Alik'r Desert, Anticlere, Antiphyllos, Bergama, Betony, Bhoriane, Daenia, Dak'fron, Dragontail Mountains, Dwynnen, Glenpoint, Isle of Balfiera, Lainlyn, Kairou, Myrkwasa, Totambu, and Wrothgarian Mountains.
- Dialogue for all joinable factions and witch covens.
- Unique NPC dialogue (for the first release, only King Gothryd of Daggerfall is scripted).
- A system for other modders to add dialogue from their own mods.
- Ability to completely localize (translate) the mod.

**How to Get Started**
Head to one of the covered regions (see above) and start talking to NPCs. Ask them for advice and "Where Am I?" NPCs are busy, so watch your tone and your local reputation if you'd like them to answer more of your endless questions. Make sure to ask different people about the same topic, as different NPCs have different bits of information, and sometimes different opinions.

**Future Features**
Future releases willl add additional unique NPC dialogue, regional dialogue, faction dialogue, and dialogue that changes as the player progresses through the main quest.

If you're interested in writing dialogue, either as part of this project or for a separate mod, please contact me on Lysandus' Tomb.

**Compatibility Issues**
Advanced Dialogue is not compatible with UncannyUI. However, it is compatible with Jagget's fork of UncannyUI if you disable the Uncanny dialogue window in the mod settings and delete mod_uncannyui.txt.

Advanced Dialogue and Hidden Map Locations are mostly compatible if you put AD after HML in the load order. But there's one HML feature that currently doesn't work with AD (adding discovered locations from the "Where Is > Regional" menu). I'll be working on a fix for this shortly as I love HML.

Advanced Dialogue should be safe to install without starting a new game.

**Recommended Mods**
I strongly recommend Main Quest Enhanced by Sappho20 as a companion mod to this one. MQE makes the main quest much richer and more coherent.

**Lore Notes**
This mod treats only Daggerfall-era lore as canonical. Thus, Redguards and Bretons worship the same gods, the Direnni are Bretons, and the Direnni Tower is just a tall old building. Later lore may sometimes be adopted as inspiration on a case-by-case basis.

**Contributors**
carademono (coding and most dialogue)
Jo'Zakar (regional and faction dialogue)
Deepfighter (faction dialogue)
Jehuty (some regional dialogue)

---

### Modding Tutorial: Adding Custom Dialogue with Advanced Dialogue

If you\'d like to add your own dialogue lines to the *Advanced Dialogue*
mod in **Daggerfall Unity**, you can do this by creating your own custom
*\*\_Dialogue.csv* files. These files can have any prefix, as long as
they end in *\_Dialogue.csv* (i.e. *WritersGuild_Dialogue.csv*), and
they should follow a specific format. Here's how to set it up:

#### Step 1: Create Your Custom *\_Dialogue.csv* File

1.  **Format Your CSV File**: Note that your csv should actually be a
    *tab-*separated file. Each *\*\_Dialogue.csv* file should contain
    the following columns:

    `caption answer add_caption c1_variable c1_comp c1_condition
    c2_variable c2_comp c2_condition c3_variable c3_comp c3_condition`

    -   **caption**: The main prompt or question the NPC will say.
    -   **answer**: The NPC\'s response if this dialogue line is
        selected.
    -   **add_caption**: Adds new captions (dialogue options) to the
        player\'s known list when this dialogue is triggered.
    -   **c1_variable, c1_comp, c1_condition**: The first condition for
        this dialogue line to appear.
    -   **c2_variable, c2_comp, c2_condition**: (Optional) A second
        condition that must also be met.
    -   **c3_variable, c3_comp, c3_condition**: (Optional) A third
        condition that must also be met.

    - **Note**: The conditions must all be met for the dialogue to
    appear. See the glossary for details on these columns.

2.  **Example Entry**: Here's what a line in your CSV might look like:

    `Who are you? I am \@Name@, a member of the Writers Guild. We are
    dedicated to preserving knowledge and literature throughout the
    Iliac Bay. Writers Guild NoLore == 2 Faction ID == 1001`

    This example will show the greeting if the NPC has NoLore set to 2
    (NoLore designates NPCs like faction members that shouldn't receive
    generic dialogue, see below) and has a Faction ID of 1001. The
    caption \"Writers Guild\" will be added to the player\'s known
    captions if the conditions are met.

#### Step 2: Place Your CSV File in the Correct Directory

1.  **Create a Directory Structure**: In your mod's folder, create a
    subdirectory named *Dialogue*. Place your custom *\*\_Dialogue.csv*
    file in this *Dialogue* folder.

    -   Example path:
        *Assets/Game/Mods/YourMod/Dialogue/Custom_Dialogue.csv*

    -**Note**: You can name your file anything as long as it ends with
    *\_Dialogue.csv* (e.g., *Custom_Dialogue.csv*,
    *WritersGuild_Dialogue.csv*, etc.)

#### Step 3: Pack Your Mod with Daggerfall Tools for Unity

1.  **Open the Mod Builder**:

    -   In Unity, go to **Daggerfall Tools \> Mod Builder**.

2.  **Add Your Mod's Files**:

    -   Under **Mod Builder** settings, ensure that your
        *\*\_Dialogue.csv* file and other assets are included in the
        build. You do **not** need to include
        Advanced Dialogue's scripts.

3.  **Set Your Mod's Dependencies:**

    -   In the **Mod Builder**, click on Open Dependencies Editor.
        Add *****advanced dialogue.dfmod***** (version 0.3.0) as a
        dependency, and make sure to select "Must be loaded before."

4.  **Build the Mod**:

    -   Set up your mod's details (name, description, version, etc.) in
        the Mod Builder.
    -   Click **Build Mod** to compile your mod into a *.dfmod* file,
        which will include your custom dialogue CSV.

---

### Columns in the Dialogue CSV

1.  **caption**

    -   **Description**: This is the main dialogue line or question that
        will appear for the player to select.
    -   **Usage**: Displayed as the prompt or question for the NPC
        interaction.

2.  **answer**

    -   **Description**: The NPC's response or dialogue line in reply to
        the player's selection.
    -   **Usage**: If multiple answers are provided, separated by *\|*,
        one will be selected at random each time the dialogue condition
        is met. This creates variety in NPC responses.
    -   **Note**: If you include apostrophes or quotation marks in your
        answers, make sure to use the correct Unicode for left
        apostrophes (') or left quotations (") where appropriate!

3.  **add_caption**

    -   **Description**: This column allows the addition of one or more
        captions to the player\'s list of known captions.
    -   **Usage**: When the dialogue item is used, the captions listed
        here (separated by *\|* if multiple) will be added to the
        player's known captions list, making them accessible in future
        interactions.

### Main Condition Columns

The following columns are used to control whether a specific dialogue
line appears to the player. A dialogue item will only appear if **all
conditions** are met.

4.  **c1_variable**

    -   **Description**: This specifies the first variable or attribute
        that must be checked for the dialogue line to appear.
    -   **Usage**: This could be a game-specific attribute like
        \"Faction ID,\" \"Race,\" \"Gold,\" or even player-related data
        such as \"Health\" or \"Location.\" The variable name must match
        an attribute recognized by the mod to be compared.

5.  **c1_comp**

    -   **Description**: The comparison operator used to compare
        *c1_variable* with *c1_condition*.

    -   **Usage**:

        -   *****==*****: Checks if the variable value exactly matches
            the condition.
        -   *****!=*****: Checks if the variable value does not match
            the condition.
        -   *****\<***, ***\>***, ***\<=***, ***\>=*****: Checks if the
            variable value is less than, greater than, less than or
            equal to, or greater than or equal to the condition. Used
            mainly for numeric values.
        -   *****\~\~*****: Checks if the condition is found within the
            variable's value (partial match).
        -   *****!\~*****: Checks if the condition is **not** found
            within the variable's value (ensures no partial match).

6.  **c1_condition**

    -   **Description**: The specific value or state that *c1_variable*
        must meet for the dialogue line to be eligible.
    -   **Usage**: The value can be a specific number, string, or list
        of values separated by *\|*. If a list is provided, the
        condition is met if any item in the list matches the variable
        (for operators that support this).

### Additional Conditions (Optional)

-   **c2_variable, c2_comp, c2_condition, c3_variable, c3_comp,
    c3_condition**: These columns work just like *c1\_\** columns,
    allowing you to add a second or third condition that must also be
    met for the dialogue item to appear.

### Important Notes

-   **All conditions (***c1***, ***c2***, and ***c3***) must be met**
    for a dialogue item to appear, making it possible to create highly
    specific dialogue responses based on multiple factors.
-   **Lists separated by ***\|***** within conditions allow for multiple
    values to match, giving more flexibility. If a list is specified for
    *answer*, the system will randomly choose one of the answers when
    all conditions are met.
-   **add_caption**: Adds the listed captions to the player\'s known
    captions, separated by *\|* if there are multiple captions to add.
    This helps dynamically build the player's dialogue options as they
    explore and interact with NPCs.

---

### Dialogue Filtering Variables (for use in c1_variable, c2_variable, and c3_variable)

-   **Type**: The type of NPC, either \"Static\" or \"Mobile.\" Static
    NPCs are typically found in fixed locations like towns, while Mobile
    NPCs roam around.
-   **Hash**: A unique identifier for the NPC, which can be used to
    specify dialogue for specific NPCs.
-   **Flags**: Additional data flags associated with the NPC, indicating
    various properties or states.
-   **Faction ID**: The unique ID of the NPC\'s faction, used to filter
    dialogue based on the NPC's affiliation.
-   **Name Seed**: A value used to generate the NPC\'s name, which could
    be used for distinguishing specific NPCs.
-   **Gender**: The NPC's gender (0 for male, 1 for female).
-   **Race**: The NPC's race, such as Breton, Redguard, etc.
-   **Context**: The NPC\'s purpose or role within the game, which may
    help filter dialogue based on the NPC's functionality.
-   **Map ID**: The ID of the map where the NPC is located.
-   **Location ID**: The specific location ID within the game world,
    which can be used to filter dialogue for NPCs in certain locations.
-   **In Building**: 1 if the NPC is inside a building, 0 if outside.
    Useful for adjusting dialogue based on indoor or outdoor settings.
-   **Building Name**: The name of the building where the NPC is
    located, or \"Outside\" if they are outside.
-   **Building Type**: The type of building (e.g., tavern, temple) where
    the NPC is located.
-   **Building Key**: A unique identifier for the building.
-   **Name Bank**: The source of names used for the NPC, which can
    influence the names assigned to them.
-   **Billboard Archive Index**: An index used to determine the NPC's
    appearance.
-   **Billboard Record Index**: Another index related to the NPC\'s
    visual appearance.
-   **Name**: The display name of the NPC.
-   **Child NPC**: Indicates whether the NPC is a child, useful for
    child-specific dialogue.
-   **NoLore**: Indicates whether the dialogue will be spoken by generic
    NPCs or special NPCs. NoLore = 0 (default) means dialogue will be
    spoken by generic NPCs (like mobiles or bar patrons). NoLore =1
    means dialogue will be spoken by children. NoLore = 2 means dialogue
    will be spoken by faction members. NoLore = 3 means dialogue will be
    spoken by a unique NPC.

### Faction Variables (see FactionsFile.cs for more detail)

-   **Faction Name**: The name of the faction the NPC belongs to.
-   **Faction Parent**: The ID of the parent faction, useful for
    hierarchy-based dialogue.
-   **Faction Type**: A number representing the type of faction.
-   **Faction Reputation**: The reputation of the faction, which can
    influence interactions with the NPC.
-   **Faction Region**: The region associated with the faction.
-   **Faction Power**: A value representing the power of the faction,
    which might affect the influence of NPCs.
-   **Faction Allies**: List of faction IDs allied with the NPC's
    faction.
-   **Faction Enemies**: List of faction IDs that are enemies of the
    NPC's faction.
-   **Faction Social Group**: The social group of the faction, used to
    categorize factions (e.g., temple, guild).
-   **Faction Guild Group**: The guild group the faction belongs to.
-   **Faction Vampire**: Indicates if the faction is associated with
    vampires.
-   **Faction Children**: IDs of any child factions, useful for dialogue
    specific to familial or factional ties.

### Time and Date Variables

-   **Year**: The current in-game year.
-   **Month**: The current month (numeric).
-   **MonthName**: The name of the current month.
-   **Day**: The day of the month.
-   **DayName**: The name of the day.
-   **Season**: The current season (e.g., Winter, Summer).
-   **Daytime**: True if it's daytime, False if nighttime.
-   **Nighttime**: True if it's nighttime, False if daytime.
-   **Massar Lunar Phase**: The phase of the Massar moon.
-   **Secunda Lunar Phase**: The phase of the Secunda moon.

### Weather Variables

-   **Raining**: True if it's currently raining.
-   **Storming**: True if there is a storm.
-   **Snowing**: True if it's snowing.
-   **Overcast**: True if the sky is overcast.

### Holiday Variables

-   **Holiday**: The name of the current holiday, if any. If no holiday
    is occurring, this will indicate \"No holiday today.\"

### Location Variables

-   **Map Pixel**: The current map pixel (x, y) location, useful for
    specific geographic filtering.
-   **Climate Index**: The climate type of the region (e.g., desert,
    swamp).
-   **Politic Index**: The political affiliation of the region.
-   **Region Index**: The index of the region where the player or NPC is
    located.
-   **Location Index**: The index of the specific location within the
    region.
-   **MapID**: The unique map identifier for the location.
-   **Has Location**: True if the player is in a defined location, False
    otherwise.
-   **Player In Location Rect**: True if the player is within the
    rectangular bounds of a location.
-   **Region**: The name of the region.
-   **Region Name**: The display name of the region.
-   **Climate Settings**: The climate settings of the region.
-   **Location**: The current location object.
-   **Location Name**: The name of the location where the player or NPC
    is.
-   **Location Type**: The type of location (e.g., town, dungeon).

### Region-Specific Variables

-   **Name Bank of Region**: The name bank used for generating names in
    the region.
-   **Race of Region**: The predominant race of the region.
-   **People of Region (Faction ID)**: The faction ID representing the
    people of the region.
-   **Region Faction (Faction ID)**: The faction associated with the
    region.
-   **Court of Region (Faction ID)**: The faction representing the court
    of the region.
-   **Region Vampire Clan (Faction ID)**: The vampire clan associated
    with the region, if any.
-   **Dominant Temple in Region (Faction ID)**: The dominant temple
    faction within the region.
-   **Player In Town**: True if the player is currently in a town.

### Player Variables

-   **Health**: The current health of the player.
-   **Max Health**: The maximum health of the player.
-   **Magicka**: The current magicka (magic points) of the player.
-   **Max Magicka**: The maximum magicka of the player.
-   **Fatigue**: The current fatigue of the player.
-   **Max Fatigue**: The maximum fatigue of the player.
-   **Gold**: The amount of gold the player is carrying.

### Skills and Inventory Variables

-   **Skill Names**: Each skill (e.g., Archery, Lockpicking) and its
    corresponding skill level.
-   **Item: \[Item Name\]**: The count of specific items in the player's
    inventory (e.g., \"Item: Gold Nugget\" might show the number of gold
    nuggets in the inventory).

### Quest Variables

-   **Quest: \[Quest Name\]**: A list of message IDs associated with
    active quests, used for filtering dialogue based on quest progress.

### Miscellaneous Variables

-   **Random Number:** A random number between 1 and 100 for each NPC,
    useful for adding variability to dialogue. **This number is
    randomized for every topic.**
-   **Random Number (NPC)**: A random number between 1 and 100 for each
    NPC, useful for adding variability to dialogue. This stays the same
    for the duration of a conversation.
-   **Active Mods**: A list of currently active mods, potentially useful
    for conditionally enabling dialogue based on specific mods.

### Important Notes

-   ****Pay attention to NoLore! ****Dialogue will not appear if you
    don't set the correct NoLore level for your intended NPCs.****
-   ****You can put any dialogue filter between @ symbols to have its
    value appear in **answer** dialogue itself. For example: I love
    living in \@Region Name@ and working for \@Faction Name@!****

---

### Console Commands:

**Command**: *AD_Log*

**Description**: This command toggles the logging functionality for the
*Advanced Dialogue* mod. When enabled, the mod will output debug
information to the console about dialogue filter variables for each NPC
the player interacts with. This is extremely useful when used in the
Unity Editor for debugging dialogue conditions and ensuring that
dialogue options appear as intended.

**How to Use**:

-   Simply type *AD_Log* in the console.

**Effect**:

-   If logging is currently **disabled**, this command will **enable**
    it.
-   If logging is currently **enabled**, this command will **disable**
    it.

**Command**: *AD_Reload*

**Description**: This command reloads all dialogue and localization data
for the *Advanced Dialogue* mod. It's useful in the Unity Editor if
you've added or changed dialogue files (like
*WritersGuild\_Dialogue.csv* or *AD_Keys.csv*) and want to see the
effect of your changes without restarting the game.

**How to Use**:

-   Type *AD_Reload* in the console.

**Effect**:

-   Clears all currently loaded dialogue items.
-   Reloads dialogue topics from all *\*\_Dialogue.csv* files and
    localization keys from *AD_Keys.csv*.

---

### Localization for *Advanced Dialogue*

This final tutorial will guide you through the steps to localize the
*Advanced Dialogue* mod by creating translated versions of
*AD_Dialogue.csv* and *AD_Keys.csv*. These files allow you to provide
translated dialogue, making the mod accessible to players in different
languages.

### Step 1: Prepare the Translated Files

For localization, you need to create translated versions of the
following files:

1.  **AD_Dialogue.csv** -- This file contains the main dialogue options
    and their associated conditions.
2.  **AD_Keys.csv** -- This file holds text used by the mod's internal
    logic.

> **Important**: These files must be named **exactly** *AD_Dialogue.csv*
> and *AD_Keys.csv* for them to replace the default text. These files
> should be placed in a **Dialogue** subdirectory within your mod
> project folder.

### Step 2: Translate the Files

#### Translating *AD_Dialogue.csv*

-   Open the original *AD_Dialogue.csv* file as a reference.
-   For each row, translate the *caption*, *answer*, and other
    text-based columns into your target language.

#### Translating *AD_Keys.csv*

-   *AD_Keys.csv* contains various dialogue prompts and special text
    used by the mod.
-   Translate each entry in the *text* column to the corresponding text
    in your target language.

**Example for Special Captions**:

-   The entry with the key *definiteArticles* is important for languages
    with multiple definite articles (like Spanish or German). This field
    accepts a *\|*-separated list. This list allows the mod to remove
    definite articles when sorting captions alphabetically.
-   In **Spanish**, for instance, you would use: *el\|la\|los\|las*.
-   In **German**, you would use: *der\|die\|das*.

### Step 3: Place the Files in Your Mod's Folder

1.  **Create a Folder Structure**: In your mod's directory, create a
    folder named **Dialogue**.
    -   Your file path should look like this:
        *Assets/Game/Mods/YourMod/Dialogue/AD_Dialogue.csv* and
        *Assets/Game/Mods/YourMod/Dialogue/AD_Keys.csv*.

2.  **Save the Translations**: Place the translated *AD_Dialogue.csv*
    and *AD_Keys.csv* in this **Dialogue** folder.

### Step 4: Build Your Mod in Daggerfall Unity

1.  Open **Daggerfall Tools for Unity**.

2.  Use the **Mod Builder** to create your mod package (*.dfmod* file).

    -   Make sure the *AD_Dialogue.csv* and *AD_Keys.csv* files are
        included in the build. You do **not** need to include
        Advanced Dialogue's scripts.

3.  **Set Your Mod's Dependencies:**

    -   In the **Mod Builder**, click on Open Dependencies Editor.
        Add *****advanced dialogue.dfmod***** (version 0.3.0) as a
        dependency, and make sure to select "Must be loaded before."

4.  Build and export the mod.
