using Godot;
using System.IO.Compression;
using System.Text;

#nullable enable

namespace Yjcm;

/// <summary>
/// Plays a compact mesh animation extracted from a LayaAnimation/DragonBone .sk file.
/// Every frame shares one atlas, one UV array, and one triangle index array, so the
/// character renders with a single canvas draw call instead of a PNG sequence.
/// </summary>
internal sealed partial class WuZhugeliangMeshAnimation : Node2D
{
    private const string DefaultAnimation = "play";
    private static readonly string AssetRoot = Path.Combine(
        Path.GetDirectoryName(typeof(WuZhugeliangMeshAnimation).Assembly.Location)!,
        "MeshAnimation", "WuZhugeliang");

    private ImageTexture? _texture;
    private MeshAnimationData? _data;
    private AnimationClip? _clip;
    private double _elapsed;
    private int _frameIndex = -1;

    public override void _Ready()
    {
        string meshPath = Path.Combine(AssetRoot, "wumiao_zhugeliang.lmesh.gz");
        string texturePath = Path.Combine(AssetRoot, "wumiao_zhugeliang.png");
        _data = MeshAnimationData.Load(meshPath);
        Image image = Image.LoadFromFile(texturePath);
        if (image.IsEmpty())
            throw new InvalidDataException($"无法读取武诸葛网格动画贴图：{texturePath}");
        _texture = ImageTexture.CreateFromImage(image);
        TextureFilter = TextureFilterEnum.Linear;
        Play(DefaultAnimation);
    }

    public override void _Process(double delta)
    {
        if (_clip == null || _data == null || _clip.Frames.Length == 0)
            return;
        _elapsed += delta;
        int nextFrame = (int)(_elapsed * _data.FramesPerSecond) % _clip.Frames.Length;
        if (nextFrame == _frameIndex)
            return;
        _frameIndex = nextFrame;
        QueueRedraw();
    }

    public override void _Draw()
    {
        if (_texture == null || _data == null || _clip == null || _frameIndex < 0)
            return;
        RenderingServer.CanvasItemAddTriangleArray(GetCanvasItem(), _data.Indices,
            _clip.Frames[_frameIndex], Array.Empty<Color>(), _data.Uvs, Array.Empty<int>(),
            Array.Empty<float>(), _texture.GetRid(), -1);
    }

    public void Play(string animationName)
    {
        if (_data == null || !_data.Animations.TryGetValue(animationName, out AnimationClip? clip))
            throw new InvalidDataException($"武诸葛网格动画不存在：{animationName}");
        _clip = clip;
        _elapsed = 0;
        _frameIndex = 0;
        QueueRedraw();
    }

    private sealed class MeshAnimationData
    {
        public required int FramesPerSecond { get; init; }
        public required int[] Indices { get; init; }
        public required Vector2[] Uvs { get; init; }
        public required Dictionary<string, AnimationClip> Animations { get; init; }

        public static MeshAnimationData Load(string path)
        {
            if (!File.Exists(path)) throw new FileNotFoundException("缺少武诸葛网格动画数据。", path);
            using FileStream file = File.OpenRead(path);
            using var gzip = new GZipStream(file, CompressionMode.Decompress);
            using var reader = new BinaryReader(gzip, Encoding.UTF8, leaveOpen: false);
            string magic = Encoding.ASCII.GetString(reader.ReadBytes(4));
            ushort version = reader.ReadUInt16();
            if (magic != "LYM1" || version != 1)
                throw new InvalidDataException($"不支持的武诸葛网格动画格式：{magic} v{version}");
            int fps = reader.ReadUInt16();
            int vertexCount = checked((int)reader.ReadUInt32());
            int indexCount = checked((int)reader.ReadUInt32());
            int animationCount = reader.ReadUInt16();
            ushort flags = reader.ReadUInt16();
            _ = reader.ReadUInt32();
            _ = reader.ReadUInt32();
            if (vertexCount <= 0 || indexCount <= 0 || animationCount <= 0 || (flags & 1) == 0)
                throw new InvalidDataException("武诸葛网格动画头信息无效。");
            var indices = new int[indexCount];
            for (int index = 0; index < indexCount; index++) indices[index] = checked((int)reader.ReadUInt32());
            var uvs = new Vector2[vertexCount];
            for (int vertex = 0; vertex < vertexCount; vertex++) uvs[vertex] = new Vector2(reader.ReadSingle(), reader.ReadSingle());
            var animations = new Dictionary<string, AnimationClip>(StringComparer.Ordinal);
            for (int animationIndex = 0; animationIndex < animationCount; animationIndex++)
            {
                int nameLength = reader.ReadUInt16();
                string name = Encoding.UTF8.GetString(reader.ReadBytes(nameLength));
                int frameCount = reader.ReadUInt16();
                _ = reader.ReadUInt16();
                var frames = new Vector2[frameCount][];
                for (int frame = 0; frame < frameCount; frame++)
                {
                    var positions = new Vector2[vertexCount];
                    for (int vertex = 0; vertex < vertexCount; vertex++) positions[vertex] = new Vector2(reader.ReadSingle(), reader.ReadSingle());
                    frames[frame] = positions;
                    if (reader.ReadBytes(vertexCount).Length != vertexCount)
                        throw new EndOfStreamException("武诸葛网格动画透明度数据不完整。");
                }
                animations.Add(name, new AnimationClip(name, frames));
            }
            return new MeshAnimationData { FramesPerSecond = fps, Indices = indices, Uvs = uvs, Animations = animations };
        }
    }

    private sealed record AnimationClip(string Name, Vector2[][] Frames);
}
