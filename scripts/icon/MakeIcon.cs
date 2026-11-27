#:package SkiaSharp@3.119.4
#:property ManagePackageVersionsCentrally=false

// Renders src/RestDraft.UI/Assets/app-icon.svg into app-icon.ico and app-icon.png beside it. Run it after a change to
// the SVG and commit both files; the build does not run it:
//   dotnet run scripts/icon/MakeIcon.cs
// It reads only what the icon uses: circle and path elements with fill, stroke, stroke-width and round or default line
// caps and joins. Anything else is reported as an error instead of being left out of the picture.

using System.Globalization;
using System.Xml;
using System.Xml.Linq;
using SkiaSharp;

const int LargestSize = 256;
int[] sizes = [16, 20, 24, 32, 40, 48, 64, 128, LargestSize];

if (AppContext.GetData("EntryPointFileDirectoryPath") is not string scriptFolder)
{
    Console.Error.WriteLine("Run the script with: dotnet run scripts/icon/MakeIcon.cs");
    return 1;
}

var assets = Path.GetFullPath(Path.Combine(scriptFolder, "..", "..", "src", "RestDraft.UI", "Assets"));
try
{
    var drawing = XDocument.Load(Path.Combine(assets, "app-icon.svg")).Root!;
    using var picture = Record(drawing, out var error);
    if (picture is null)
    {
        Console.Error.WriteLine(error);
        return 1;
    }

    var images = sizes.Select(size => Encode(picture, size)).ToArray();
    WriteIcon(Path.Combine(assets, "app-icon.ico"), sizes, images);
    File.WriteAllBytes(Path.Combine(assets, "app-icon.png"), images[^1]);
}
catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or XmlException)
{
    Console.Error.WriteLine(exception);
    return 1;
}

Console.WriteLine($"Wrote app-icon.ico ({string.Join(", ", sizes)} px) and app-icon.png to {assets}");
return 0;

// The drawing as a vector picture on a LargestSize square (the viewBox of the SVG), so every size is rendered sharp.
static SKPicture? Record(XElement drawing, out string error)
{
    using var recorder = new SKPictureRecorder();
    var canvas = recorder.BeginRecording(SKRect.Create(LargestSize, LargestSize));
    error = string.Empty;
    if (drawing.Attribute("viewBox")?.Value != $"0 0 {LargestSize} {LargestSize}")
    {
        error = $"The viewBox of the SVG must be 0 0 {LargestSize} {LargestSize}.";
        return null;
    }

    foreach (var element in drawing.Elements())
    {
        using var path = ReadShape(element);
        if (path is null)
        {
            error = $"Cannot draw this element: {element}";
            return null;
        }

        Paint(canvas, path, element);
    }

    return recorder.EndRecording();
}

static SKPath? ReadShape(XElement element)
{
    if (element.Name.LocalName == "path")
    {
        return SKPath.ParseSvgPathData(element.Attribute("d")?.Value ?? string.Empty);
    }

    if (element.Name.LocalName != "circle")
    {
        return null;
    }

    var circle = new SKPath();
    circle.AddCircle(Number(element, "cx"), Number(element, "cy"), Number(element, "r"));
    return circle;
}

static void Paint(SKCanvas canvas, SKPath path, XElement element)
{
    // The SVG defaults: a black fill and no stroke.
    if (SKColor.TryParse(element.Attribute("fill")?.Value ?? "#000000", out var fill))
    {
        using var paint = new SKPaint { Style = SKPaintStyle.Fill, Color = fill, IsAntialias = true };
        canvas.DrawPath(path, paint);
    }

    if (SKColor.TryParse(element.Attribute("stroke")?.Value ?? "none", out var stroke))
    {
        using var paint = new SKPaint
        {
            Style = SKPaintStyle.Stroke,
            Color = stroke,
            IsAntialias = true,
            StrokeWidth = Number(element, "stroke-width"),
            StrokeCap = element.Attribute("stroke-linecap")?.Value == "round" ? SKStrokeCap.Round : SKStrokeCap.Butt,
            StrokeJoin = element.Attribute("stroke-linejoin")?.Value == "round" ? SKStrokeJoin.Round : SKStrokeJoin.Miter,
        };
        canvas.DrawPath(path, paint);
    }
}

static float Number(XElement element, string name)
{
    var text = element.Attribute(name)?.Value;
    return float.TryParse(text, CultureInfo.InvariantCulture, out var value) ? value : 0;
}

// The largest image is stored as PNG and the others as bitmaps, the layout that every reader of .ico files accepts.
static byte[] Encode(SKPicture picture, int size)
{
    using var bitmap = new SKBitmap(new SKImageInfo(size, size, SKColorType.Bgra8888, SKAlphaType.Premul));
    using (var canvas = new SKCanvas(bitmap))
    {
        canvas.Clear(SKColors.Transparent);
        canvas.Scale((float)size / LargestSize);
        canvas.DrawPicture(picture);
    }

    if (size < LargestSize)
    {
        return EncodeBitmap(bitmap);
    }

    using var png = bitmap.Encode(SKEncodedImageFormat.Png, 100);
    return png.ToArray();
}

// An icon bitmap: a BITMAPINFOHEADER with twice the height, the BGRA rows from the bottom up with straight alpha, and a
// one bit mask that stays empty because the alpha channel already says what is transparent.
static byte[] EncodeBitmap(SKBitmap bitmap)
{
    var size = bitmap.Width;
    using var straight = new SKBitmap(bitmap.Info.WithAlphaType(SKAlphaType.Unpremul));
    using var source = bitmap.PeekPixels();
    using var target = straight.PeekPixels();
    source.ReadPixels(target);
    var pixels = straight.Bytes;
    var maskRowBytes = (size + 31) / 32 * 4;

    using var stream = new MemoryStream();
    using var writer = new BinaryWriter(stream);
    writer.Write(40);
    writer.Write(size);
    writer.Write(size * 2);
    writer.Write((short)1);
    writer.Write((short)32);
    writer.Write(new byte[24]);
    for (var row = size - 1; row >= 0; row--)
    {
        writer.Write(pixels, row * straight.RowBytes, size * 4);
    }

    writer.Write(new byte[maskRowBytes * size]);
    writer.Flush();
    return stream.ToArray();
}

static void WriteIcon(string path, int[] sizes, byte[][] images)
{
    const int HeaderBytes = 6;
    const int EntryBytes = 16;
    using var writer = new BinaryWriter(File.Create(path));
    writer.Write((short)0);
    writer.Write((short)1);
    writer.Write((short)sizes.Length);
    var offset = HeaderBytes + EntryBytes * sizes.Length;
    for (var index = 0; index < sizes.Length; index++)
    {
        // A width or height of 256 is written as 0.
        var side = (byte)(sizes[index] % 256);
        writer.Write(side);
        writer.Write(side);
        writer.Write((short)0);
        writer.Write((short)1);
        writer.Write((short)32);
        writer.Write(images[index].Length);
        writer.Write(offset);
        offset += images[index].Length;
    }

    foreach (var image in images)
    {
        writer.Write(image);
    }
}
