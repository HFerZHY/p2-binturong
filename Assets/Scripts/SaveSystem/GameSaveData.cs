using System;
using System.Collections.Generic;

namespace Otowa.SaveSystem
{
    [Serializable]
    public class GameSaveData
    {
        public int version = 1;
        public string savedAtUtc;
        public string sceneName;
        public Day1InquirySaveData day1 = new();
        public Day2InquirySaveData day2 = new();
        public InspirationSaveData journal = new();
        public MapPositionSaveData day1WorldPosition = new();
        public MapPositionSaveData day2WorldPosition = new();
        public MapSpawnSaveData day1WorldSpawn = new();
        public MapSpawnSaveData day2WorldSpawn = new();
        public ExhibitionSaveData exhibitionDay2 = new();
        public ExhibitionSaveData exhibitionDay3 = new();
        public AudioSaveData audio = new();
    }

    [Serializable]
    public class AudioSaveData
    {
        public bool hasSnapshot;
        public bool hasBgm;
        public string bgmId;
        public float bgmTime;
        public float bgmVolume = 1f;
        public List<AudioLoopSaveData> loops = new();
    }

    [Serializable]
    public class AudioLoopSaveData
    {
        public string id;
        public float time;
        public float volume = 1f;
    }

    [Serializable]
    public class MapPositionSaveData
    {
        public bool hasPosition;
        public float x;
        public float y;
        public float z;
    }

    [Serializable]
    public class MapSpawnSaveData
    {
        public bool hasSpawn;
        public string spawnObjectName;
        public float offsetX;
        public float offsetY;
        public float offsetZ;
    }

    [Serializable]
    public class Day1InquirySaveData
    {
        public bool initialized;
        public bool amuletReceived;
        public bool mizukiCityTopicComplete;
        public bool mizukiFestivalTopicComplete;
        public bool objectivePromptShown;
        public bool allInquiryThoughtShown;
        public List<int> askedItemIds = new();
        public List<string> introducedNpcs = new();
    }

    [Serializable]
    public class Day2InquirySaveData
    {
        public bool day2AfternoonInitialized;
        public bool freeExplorationUnlocked;
        public bool yujiFestivalTopicComplete;
        public bool junkoLastTrainTopicComplete;
        public bool jiroStationTopicComplete;
        public bool jiroFestivalTopicComplete;
        public bool mizukiFestivalTopicComplete;
        public bool dangoAskedByJiro;
        public bool dangoAskedByMizuki;
        public bool paintingInquiryStarted;
        public bool paintingReceived;
        public bool allInquiryThoughtShown;
        public List<int> askedItemIds = new();
        public List<string> introducedNpcs = new();
    }

    [Serializable]
    public class InspirationSaveData
    {
        public bool introduced;
        public List<int> unlockedInspirationIds = new();
        public List<int> collectedItemSortOrders = new();
        public List<string> completedThemeTitles = new();
    }

    [Serializable]
    public class ExhibitionSaveData
    {
        public string sceneName;
        public string currentThemeTitle;
        public string activeExhibitionTitle;
        public string state;
        public int satisfaction;
        public int visitorIndex;
        public bool isRunning;
        public List<ExhibitionSlotSaveData> currentSlots = new();
        public List<ExhibitionThemeCurationSaveData> themeStates = new();
        public List<int> knownInspirationIds = new();
        public List<string> completedThemeTitles = new();
        public List<ExhibitionItemStateSaveData> itemStates = new();
        public List<ExhibitionInspirationStateSaveData> inspirationStates = new();
        public TutorialPopupSaveData tutorialPopup = new();
    }

    [Serializable]
    public class TutorialPopupSaveData
    {
        public bool selectThemeHintShown;
        public bool selectThemeDismissed;
        public bool arrangementHintShown;
        public bool arrangementHintDismissed;
        public bool inspirationHintShown;
        public bool inspirationHintDismissed;
        public bool inspirationFirstHintShown;
        public bool startHintShown;
        public bool startHintDismissed;
        public bool tryAnotherThemeHintShown;
        public bool tryAnotherThemeHintDismissed;
        public bool reuseItemsHintShown;
        public bool verifiedLabelHintShown;
    }

    [Serializable]
    public class ExhibitionThemeCurationSaveData
    {
        public string themeTitle;
        public List<ExhibitionSlotSaveData> slots = new();
    }

    [Serializable]
    public class ExhibitionSlotSaveData
    {
        public int itemSortOrder;
        public int inspirationId;
        public bool hasValidation;
        public bool itemCorrect;
        public bool hasInspirationCorrect;
        public bool inspirationCorrect;
    }

    [Serializable]
    public class ExhibitionItemStateSaveData
    {
        public int sortOrder;
        public bool isUnlocked;
        public List<string> usedInExhibitions = new();
    }

    [Serializable]
    public class ExhibitionInspirationStateSaveData
    {
        public int id;
        public bool isUnlocked;
    }
}
