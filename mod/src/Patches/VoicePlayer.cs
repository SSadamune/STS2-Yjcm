using Godot;

#nullable enable

namespace Yjcm;

/// <summary>
/// 语音播放器：每次创建新 AudioStreamPlayer，支持同时播放多个音效
/// </summary>
public static class VoicePlayer
{
    public static void PlayWav(string path)
    {
        if (!Godot.FileAccess.FileExists(path))
        {
            MainFile.Logger.Warn($"Voice file not found: {path}");
            return;
        }

        using var file = Godot.FileAccess.Open(path, Godot.FileAccess.ModeFlags.Read);
        var riff = file.GetBuffer(4);
        if (riff[0] != 'R' || riff[1] != 'I' || riff[2] != 'F' || riff[3] != 'F')
        {
            MainFile.Logger.Warn($"Not a valid WAV file: {path}");
            return;
        }

        file.GetBuffer(4);
        var wave = file.GetBuffer(4);
        if (wave[0] != 'W' || wave[1] != 'A' || wave[2] != 'V' || wave[3] != 'E')
        {
            MainFile.Logger.Warn($"Not a valid WAV file: {path}");
            return;
        }

        short channels = 1;
        int sampleRate = 44100;
        AudioStreamWav.FormatEnum format = AudioStreamWav.FormatEnum.Format16Bits;

        while (file.GetPosition() < file.GetLength() - 8)
        {
            var chunkId = file.GetBuffer(4);
            var chunkSize = BitConverter.ToInt32(file.GetBuffer(4));

            if (chunkId[0] == 'f' && chunkId[1] == 'm' && chunkId[2] == 't' && chunkId[3] == ' ')
            {
                var audioFormat = BitConverter.ToInt16(file.GetBuffer(2));
                channels = BitConverter.ToInt16(file.GetBuffer(2));
                sampleRate = BitConverter.ToInt32(file.GetBuffer(4));
                file.GetBuffer(6);
                var bitsPerSample = BitConverter.ToInt16(file.GetBuffer(2));
                format = bitsPerSample == 8 ? AudioStreamWav.FormatEnum.Format8Bits :
                    AudioStreamWav.FormatEnum.Format16Bits;
                if (chunkSize > 16) file.GetBuffer(chunkSize - 16);
            }
            else if (chunkId[0] == 'd' && chunkId[1] == 'a' && chunkId[2] == 't' && chunkId[3] == 'a')
            {
                var pcmData = file.GetBuffer(chunkSize);
                var stream = new AudioStreamWav();
                stream.Format = format;
                stream.MixRate = sampleRate;
                stream.Stereo = channels >= 2;
                stream.Data = pcmData;

                PlayStream(stream);
                return;
            }
            else
            {
                file.GetBuffer(chunkSize);
            }
        }

        MainFile.Logger.Warn($"No data chunk found in WAV: {path}");
    }

    private static void PlayStream(AudioStreamWav stream)
    {
        var player = new AudioStreamPlayer();
        player.Bus = "Master";
        player.Stream = stream;

        var sceneTree = Engine.GetMainLoop() as SceneTree;
        sceneTree?.Root.AddChild(player);
        player.Finished += () => player.QueueFree();
        player.Play();
    }
}
