using System.IO;
using System.Text;
using System.Windows.Media;
using System.Windows.Media.Imaging;
namespace EnesTV.Smoke;
public static class TestMedia
{
    public static void Create(string path)
    {
        const int width = 320, height = 180, count = 900;
        var frames = new List<byte[]>();
        for (var frame = 0; frame < 30; frame++)
        {
            var pixels = new byte[width * height * 3];
            for (var y = 0; y < height; y++) for (var x = 0; x < width; x++)
            {
                var p = (y * width + x) * 3; pixels[p] = (byte)(30 + x / 3); pixels[p + 1] = (byte)(50 + y / 2); pixels[p + 2] = (byte)(50 + frame * 5);
                if (x > frame * 8 && x < frame * 8 + 55 && y > 50 && y < 130) { pixels[p] = 107; pixels[p + 1] = 243; pixels[p + 2] = 179; }
            }
            var bitmap = BitmapSource.Create(width, height, 96, 96, PixelFormats.Bgr24, null, pixels, width * 3); bitmap.Freeze();
            if (frame == 0) { var logo = new PngBitmapEncoder(); logo.Frames.Add(BitmapFrame.Create(bitmap)); using var logoFile = File.Create(path + ".png"); logo.Save(logoFile); }
            var encoder = new JpegBitmapEncoder { QualityLevel = 80 }; encoder.Frames.Add(BitmapFrame.Create(bitmap)); using var memory = new MemoryStream(); encoder.Save(memory); frames.Add(memory.ToArray());
        }
        using var file = File.Create(path); using var writer = new BinaryWriter(file, Encoding.ASCII);
        void Four(string s) => writer.Write(Encoding.ASCII.GetBytes(s));
        long Begin(string four) { Four(four); var pos = file.Position; writer.Write(0); return pos; }
        void End(long pos) { var end = file.Position; file.Position = pos; writer.Write((int)(end - pos - 4)); file.Position = end; if ((end & 1) == 1) writer.Write((byte)0); }
        var riff = Begin("RIFF"); Four("AVI "); var hdrl = Begin("LIST"); Four("hdrl"); var avih = Begin("avih");
        foreach (uint v in new uint[] { 100000, 200000, 0, 0x10, count, 0, 1, 65536, width, height, 0, 0, 0, 0 }) writer.Write(v); End(avih);
        var strl = Begin("LIST"); Four("strl"); var strh = Begin("strh"); Four("vids"); Four("MJPG"); writer.Write(0u); writer.Write((ushort)0); writer.Write((ushort)0);
        foreach (uint v in new uint[] { 0, 1, 10, 0, count, 65536, uint.MaxValue, 0 }) writer.Write(v);
        foreach (short v in new short[] { 0, 0, width, height }) writer.Write(v); End(strh);
        var strf = Begin("strf"); writer.Write(40); writer.Write(width); writer.Write(height); writer.Write((ushort)1); writer.Write((ushort)24); Four("MJPG");
        foreach (var v in new[] { width * height * 3, 0, 0, 0, 0 }) writer.Write(v); End(strf); End(strl); End(hdrl);
        var movi = Begin("LIST"); var moviStart = file.Position; Four("movi"); var index = new List<(int Offset, int Length)>();
        for (var i = 0; i < count; i++) { var bytes = frames[i % frames.Count]; index.Add(((int)(file.Position - moviStart), bytes.Length)); var chunk = Begin("00dc"); writer.Write(bytes); End(chunk); }
        End(movi); var idx = Begin("idx1"); foreach (var item in index) { Four("00dc"); writer.Write(0x10); writer.Write(item.Offset); writer.Write(item.Length); } End(idx); End(riff);
    }
    public static void CreateWave(string path)
    {
        const int samples = 16000 * 30; using var stream = File.Create(path); using var writer = new BinaryWriter(stream);
        writer.Write(Encoding.ASCII.GetBytes("RIFF")); writer.Write(36 + samples * 2); writer.Write(Encoding.ASCII.GetBytes("WAVEfmt ")); writer.Write(16); writer.Write((short)1); writer.Write((short)1); writer.Write(16000); writer.Write(32000); writer.Write((short)2); writer.Write((short)16); writer.Write(Encoding.ASCII.GetBytes("data")); writer.Write(samples * 2); writer.Write(new byte[samples * 2]);
    }
}
