using Godot;
using HarmonyLib;

using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Audio;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;

#nullable enable

namespace Yjcm;

/// <summary>负责谋司马懿首领战专属主题曲的播放、循环与清理。</summary>
public static class MouSimaYiBgmPlayer
{
    private const string BgmPath =
        "res://Yjcm/Audio/Monsters/MouSimaYi/mou_sima_yi_theme.wav";

    private static AudioStream? _stream;
    private static AudioStreamPlayer? _player;
    private static bool _stopping;
    private static bool _battleActive;

    public static bool IsPlaying =>
        _player != null && GodotObject.IsInstanceValid(_player) && _player.Playing;

    public static bool IsBattleActive => _battleActive;

    /// <summary>停止游戏原有战斗音乐，并从头循环播放谋司马懿主题曲。</summary>
    public static void Play()
    {
        _battleActive = true;
        StopNativeMusicOnly();
        ReconcileMusicPriority();
    }

    /// <summary>根据玩家设置保证司马懿曲与尽瘁曲只保留一首。</summary>
    public static void ReconcileMusicPriority()
    {
        if (!_battleActive)
        {
            JinCuiPower.ApplyBgmVolumeSetting();
            return;
        }

        bool preferJinCui =
            WuZhugeliangSettings.MouSimaYiBgmPriority == WuZhugeliangSettings.PreferJinCuiBgm
            && JinCuiPower.IsBgmRequested;

        if (preferJinCui)
        {
            StopPlayerOnly();
            JinCuiPower.StartRequestedBgm();
        }
        else
        {
            JinCuiPower.StopBgmPlayerOnly();
            StartPlayer();
        }
    }

    public static void ApplyVolumeSetting()
    {
        if (_player != null && GodotObject.IsInstanceValid(_player))
            _player.VolumeDb = GetVolumeDb();
    }

    private static void StartPlayer()
    {
        _stopping = false;
        if (_player != null && GodotObject.IsInstanceValid(_player))
        {
            ApplyVolumeSetting();
            return;
        }

        _stream ??= GD.Load<AudioStream>(BgmPath);
        if (_stream == null)
        {
            MainFile.Logger.Warn($"谋司马懿主题曲加载失败: {BgmPath}");
            return;
        }

        StopNativeMusicOnly();

        _player = new AudioStreamPlayer
        {
            Name = "MouSimaYiBossBgm",
            Stream = _stream,
            VolumeDb = GetVolumeDb(),
            Autoplay = true
        };

        _player.Finished += () =>
        {
            if (!_stopping && _player != null && GodotObject.IsInstanceValid(_player))
                _player.Play(0f);
        };

        if (Engine.GetMainLoop() is SceneTree tree)
            tree.Root.AddChild(_player);
    }

    private static float GetVolumeDb()
    {
        float linear = SaveManager.Instance.SettingsSave.VolumeBgm
            * SaveManager.Instance.SettingsSave.VolumeMaster
            * WuZhugeliangSettings.MouSimaYiBgmVolume / 100f;
        return linear <= 0f ? -80f : Mathf.LinearToDb(linear);
    }

    /// <summary>停止专属主题曲，并恢复本幕原有音乐。</summary>
    public static void Stop(bool restoreRunMusic = true)
    {
        _battleActive = false;
        StopPlayerOnly();

        if (restoreRunMusic)
            NRunMusicController.Instance?.StopCustomMusic();
    }

    private static void StopPlayerOnly()
    {
        if (_player == null || !GodotObject.IsInstanceValid(_player))
        {
            _player = null;
            return;
        }
        _stopping = true;
        _player.Stop();
        _player.QueueFree();
        _player = null;
    }

    /// <summary>仅停止 FMOD 音乐，保留环境音与当前章节曲目记录，便于战后恢复。</summary>
    private static void StopNativeMusicOnly()
    {
        NRunMusicController? controller = NRunMusicController.Instance;
        if (controller == null)
            return;

        var proxy = AccessTools.Field(typeof(NRunMusicController), "_proxy")?.GetValue(controller) as Node;
        if (proxy == null)
        {
            MainFile.Logger.Warn("未找到游戏音乐代理，谋司马懿主题曲可能与原音乐同时播放");
            return;
        }

        proxy.Call("stop_music");
    }
}

/// <summary>战斗正常结束时确保停止谋司马懿主题曲。</summary>
[HarmonyPatch(typeof(CombatRoom), "OnCombatEnded")]
public static class MouSimaYiBgmCombatEndPatch
{
    public static void Postfix() => MouSimaYiBgmPlayer.Stop();
}

/// <summary>放弃当前游戏时清理主题曲播放器。</summary>
[HarmonyPatch(typeof(RunManager), "Abandon")]
public static class MouSimaYiBgmAbandonPatch
{
    public static void Postfix() => MouSimaYiBgmPlayer.Stop(false);
}

/// <summary>离开战斗场景时兜底清理主题曲播放器。</summary>
[HarmonyPatch(typeof(NRun), "SetCurrentRoom")]
public static class MouSimaYiBgmRoomChangePatch
{
    public static void Postfix(Control? node)
    {
        if (node == null || !node.GetType().Name.Contains("Combat"))
            MouSimaYiBgmPlayer.Stop();
    }
}
