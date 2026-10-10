using Godot;

using MegaCrit.Sts2.Core.Saves;

#nullable enable

namespace Yjcm;

/// <summary>按谋司马懿当前阴阳形态随机播放权谋与平辽音效。</summary>
public static class MouSimaYiSfxPlayer
{
    private const string RootPath =
        "res://Yjcm/Audio/Monsters/MouSimaYi/Sfx";

    private static readonly Dictionary<string, AudioStream> StreamCache = new();

    /// <summary>进入目标形态时，从该形态的权谋语音中随机播放一条。</summary>
    public static void PlayQuanMou(bool isYang) =>
        PlayRandom(isYang, "quan_mou");

    /// <summary>执行攻击行动时，从当前形态的平辽语音中随机播放一条。</summary>
    public static void PlayPingLiao(bool isYang) =>
        PlayRandom(isYang, "ping_liao");

    private static void PlayRandom(bool isYang, string series)
    {
        string formFolder = isYang ? "Yang" : "Yin";
        int index = Random.Shared.Next(1, 5);
        string path = $"{RootPath}/{formFolder}/{series}_{index}.wav";

        if (!StreamCache.TryGetValue(path, out AudioStream? stream))
        {
            stream = GD.Load<AudioStream>(path);
            if (stream == null)
            {
                MainFile.Logger.Warn($"谋司马懿音效加载失败: {path}");
                return;
            }

            StreamCache[path] = stream;
        }

        float volume = SaveManager.Instance.SettingsSave.VolumeSfx;
        var player = new AudioStreamPlayer
        {
            Name = "MouSimaYiSfx",
            Stream = stream,
            VolumeDb = Mathf.LinearToDb(volume)
        };

        player.Finished += player.QueueFree;
        if (Engine.GetMainLoop() is not SceneTree tree)
        {
            player.QueueFree();
            return;
        }

        tree.Root.AddChild(player);
        player.Play();
    }
}
