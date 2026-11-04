using Svg;
using System.Drawing;
using System.Drawing.Imaging;

var svgDir = Path.Combine("..", "Assets", "Icons");
var svgSrc = Path.Combine("..", "uicons-regular-rounded", "svg");
var logoSrc = Path.Combine("..", "Assets", "logo.png");

// ── 1. UI icons at 32x32 white tint ─────────────────────────────────────────
const int UI = 32;
foreach (var svg in Directory.GetFiles(svgDir, "*.svg"))
{
    try
    {
        var doc = SvgDocument.Open(svg);
        using var bmp = doc.Draw(UI, UI);
        using var w = ToWhite(bmp, UI, UI);
        w.Save(Path.ChangeExtension(svg, ".png"), ImageFormat.Png);
        Console.WriteLine("UI  " + Path.GetFileName(svg));
    }
    catch (Exception ex) { Console.WriteLine("ERR " + Path.GetFileName(svg) + ": " + ex.Message); }
}

// ── 1b. App logo + ICO source (escudo, alta resolución) ───────────────────────
var shieldSvg = Path.Combine(svgDir, "fi-rr-shield.svg");
if (File.Exists(shieldSvg))
{
    Console.WriteLine("\nBuilding logo.png from fi-rr-shield.svg...");
    var doc = SvgDocument.Open(shieldSvg);
    const int logoSize = 256;
    using var bmp = doc.Draw(logoSize, logoSize);
    using var w = ToWhite(bmp, logoSize, logoSize);
    w.Save(logoSrc, ImageFormat.Png);
    Console.WriteLine("Saved: " + logoSrc);
}

// ── 2. Build ICO from logo.png at multiple sizes ─────────────────────────────
Console.WriteLine("\nBuilding ICO from logo.png...");
if (!File.Exists(logoSrc))
{
    Console.WriteLine("ERROR: logo.png missing. Add fi-rr-shield.svg to Assets/Icons and re-run.");
    return;
}
using var logoOriginal = (Bitmap)Image.FromFile(logoSrc);

var frames = new List<Bitmap>();
foreach (var sz in new[] { 16, 32, 48, 64, 128, 256 })
{
    var frame = new Bitmap(sz, sz, PixelFormat.Format32bppArgb);
    using var g = Graphics.FromImage(frame);
    g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
    g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
    g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
    g.Clear(Color.Transparent);
    g.DrawImage(logoOriginal, 0, 0, sz, sz);
    frames.Add(frame);
    Console.WriteLine($"ICO {sz}x{sz}");
}
WriteIco(Path.Combine("..", "Assets", "app.ico"), frames);
frames.ForEach(f => f.Dispose());
Console.WriteLine("Saved: app.ico");
Console.WriteLine("Done.");

// ── Helpers ──────────────────────────────────────────────────────────────────
static Bitmap ToWhite(Bitmap src, int w, int h)
{
    var dst = new Bitmap(w, h, PixelFormat.Format32bppArgb);
    for (int x = 0; x < w; x++)
        for (int y = 0; y < h; y++)
        {
            var px = src.GetPixel(x, y);
            if (px.A > 10) dst.SetPixel(x, y, Color.FromArgb(px.A, 255, 255, 255));
        }
    return dst;
}

static void WriteIco(string path, List<Bitmap> bitmaps)
{
    using var fs = new FileStream(path, FileMode.Create);
    using var bw = new BinaryWriter(fs);
    int n = bitmaps.Count;
    bw.Write((short)0); bw.Write((short)1); bw.Write((short)n);
    var pngs = bitmaps.Select(b => { using var ms = new MemoryStream(); b.Save(ms, ImageFormat.Png); return ms.ToArray(); }).ToList();
    int off = 6 + n * 16;
    for (int i = 0; i < n; i++)
    {
        int s = bitmaps[i].Width;
        bw.Write((byte)(s >= 256 ? 0 : s)); bw.Write((byte)(s >= 256 ? 0 : s));
        bw.Write((byte)0); bw.Write((byte)0); bw.Write((short)1); bw.Write((short)32);
        bw.Write(pngs[i].Length); bw.Write(off);
        off += pngs[i].Length;
    }
    foreach (var p in pngs) bw.Write(p);
}
