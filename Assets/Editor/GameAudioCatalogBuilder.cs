using System.Collections.Generic;
using Otowa.Audio;
using UnityEditor;
using UnityEngine;

public static class GameAudioCatalogBuilder
{
    private const string CATALOG_PATH = "Assets/Resources/Audio/GameAudioCatalog.asset";

    [MenuItem("Tools/Otowa/Audio/Rebuild Game Audio Catalog")]
    public static void RebuildCatalog()
    {
        EnsureFolder("Assets", "Resources");
        EnsureFolder("Assets/Resources", "Audio");

        var catalog = AssetDatabase.LoadAssetAtPath<AudioCatalog>(CATALOG_PATH);
        if (catalog == null)
        {
            catalog = ScriptableObject.CreateInstance<AudioCatalog>();
            AssetDatabase.CreateAsset(catalog, CATALOG_PATH);
        }

        catalog.SetEntries(new List<AudioCatalog.Entry>
        {
            Entry(AudioId.NightWalk, "Assets/Audio/Music/night-walk.mp3", 1f),
            Entry(AudioId.OtowaBlues, "Assets/Audio/Music/otowa blues.mp3", 0.47f),
            Entry(AudioId.Ending, "Assets/Audio/Music/ending.mp3", 0.4f),
            Entry(AudioId.DayWalk, "Assets/Audio/Music/day-walk.mp3", 1f),
            Entry(AudioId.Crisis, "Assets/Audio/Music/crisis.mp3", 0.476f),
            Entry(AudioId.HotSpring, "Assets/Audio/Music/hot-spring.mp3", 1f),
            Entry(AudioId.Gameplay, "Assets/Audio/Music/gameplay.mp3", 0.264f),
            Entry(AudioId.Ryotei, "Assets/Audio/Music/ryotei.mp3", 0.28f),
            Entry(AudioId.Decision, "Assets/Audio/Music/decision.mp3", 0.861f),
            Entry(AudioId.ForestAtmosphere, "Assets/Audio/SoundEffects/forest-atmosphere.mp3", 0.45f),
            Entry(AudioId.LeatherFootsteps, "Assets/Audio/SoundEffects/leather footsteps.mp3", 0.8f),
            Entry(AudioId.LivelierBirdsong, "Assets/Audio/SoundEffects/livelier birdsong.mp3", 0.52f),
            Entry(AudioId.InspirationUnlocked, "Assets/Audio/SoundEffects/inspiration unlocked.mp3", 0.9f),
            Entry(AudioId.Failure, "Assets/Audio/SoundEffects/failure.mp3", 0.9f),
            Entry(AudioId.Jingle, "Assets/Audio/SoundEffects/jingle.mp3", 0.9f),
            Entry(AudioId.DoorOpen, "Assets/Audio/SoundEffects/door open.mp3", 0.85f),
            Entry(AudioId.KnockingDoor, "Assets/Audio/SoundEffects/knocking door.mp3", 0.9f),
            Entry(AudioId.DrinkPour, "Assets/Audio/SoundEffects/drink pour.mp3", 0.85f),
            Entry(AudioId.GlassesToast, "Assets/Audio/SoundEffects/glasses toast.mp3", 0.9f),
            Entry(AudioId.SwitchClick, "Assets/Audio/SoundEffects/swtich click.mp3", 0.9f),
            Entry(AudioId.OnTheTrain, "Assets/Audio/SoundEffects/on the train.mp3", 0.5f),
            Entry(AudioId.Snoring, "Assets/Audio/SoundEffects/snoring.mp3", 0.45f),
            Entry(AudioId.Wind, "Assets/Audio/SoundEffects/wind.mp3", 0.45f),
            Entry(AudioId.WhistleFar, "Assets/Audio/SoundEffects/whistle-far.mp3", 0.9f),
            Entry(AudioId.WhistleClose, "Assets/Audio/SoundEffects/whistle-close.mp3", 0.9f),
            Entry(AudioId.WhistleIn, "Assets/Audio/SoundEffects/whistle-in.mp3", 0.9f),
            Entry(AudioId.PageTurn, "Assets/Audio/SoundEffects/page turn.mp3", 0.8f),
            Entry(AudioId.Run, "Assets/Audio/SoundEffects/run.mp3", 0.85f),
            Entry(AudioId.Fireworks, "Assets/Audio/SoundEffects/fireworks.mp3", 0.65f),
            Entry(AudioId.TrainRunning, "Assets/Audio/SoundEffects/Train running.mp3", 0.65f),
            Entry(AudioId.WhistleMid, "Assets/Audio/SoundEffects/whistle-mid.mp3", 0.9f),
            Entry(AudioId.Chopping, "Assets/Audio/SoundEffects/chopping.mp3", 0.58f),
            Entry(AudioId.RunningWater, "Assets/Audio/SoundEffects/running water.mp3", 0.7f),
            Entry(AudioId.BluesBeat, "Assets/Audio/SoundEffects/Blues beat.mp3", 0.62f),
            Entry(AudioId.FaintInsectChirp, "Assets/Audio/SoundEffects/faint insect chirp.mp3", 0.45f),
            Entry(AudioId.Construction, "Assets/Audio/SoundEffects/construction.mp3", 0.5f)
        });

        EditorUtility.SetDirty(catalog);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"Rebuilt audio catalog at {CATALOG_PATH}.");
    }

    [MenuItem("Tools/Otowa/Audio/Validate Game Audio Catalog")]
    public static void ValidateCatalog()
    {
        var catalog = AssetDatabase.LoadAssetAtPath<AudioCatalog>(CATALOG_PATH);
        if (catalog == null)
        {
            Debug.LogError($"Missing audio catalog at {CATALOG_PATH}.");
            return;
        }

        foreach (AudioId id in System.Enum.GetValues(typeof(AudioId)))
        {
            if (id != AudioId.None && !catalog.TryGet(id, out _, out _))
            {
                Debug.LogWarning($"Audio catalog has no clip for {id}.");
            }
        }

        Debug.Log("Audio catalog validation finished.");
    }

    private static AudioCatalog.Entry Entry(AudioId id, string path, float defaultVolume)
    {
        AudioClip clip = null;
        if (!string.IsNullOrEmpty(path))
        {
            clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            if (clip == null)
            {
                Debug.LogWarning($"Audio clip not found: {path}");
            }
        }

        return new AudioCatalog.Entry
        {
            Id = id,
            Clip = clip,
            DefaultVolume = defaultVolume
        };
    }

    private static void EnsureFolder(string parentFolder, string folderName)
    {
        var path = $"{parentFolder}/{folderName}";
        if (!AssetDatabase.IsValidFolder(path))
        {
            AssetDatabase.CreateFolder(parentFolder, folderName);
        }
    }
}
