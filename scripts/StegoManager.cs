using System;
using System.IO;
using FTHelper;
using Godot;

public partial class StegoManager : Node
{
    private FileDialog filePrivate;
    private FileDialog filePublic;
    ImageHelper publicTexture;
    ImageHelper privateTexture;

    FFTImage fftPublic;

    [Export]
    TextureRect encodedImage;

    [Export]
    TextureRect decodedImage;

    [Export]
    TextureRect privateImage;

    [Export]
    TextureRect publicImage;

    [Export]
    int startingCol = 20;

    [Export]
    double magScale = 0.333;

    [Export]
    HSlider scaleSlider;

    [Export]
    HSlider colSlider;

    public override void _Ready()
    {
        filePrivate = new FileDialog();
        filePublic = new FileDialog();
        CreateFile(filePrivate, false);
        CreateFile(filePublic, true);
        scaleSlider.MinValue = 0.001;
        scaleSlider.MaxValue = 0.3;
        scaleSlider.Step = 0.01;
        scaleSlider.Value = magScale;
        colSlider.Step = 1;
        colSlider.MinValue = 0;
        colSlider.MaxValue = 120;
        colSlider.Value = startingCol;

        colSlider.ValueChanged += (double v) =>
        {
            startingCol = (int)v;
            Stego();
        };
        scaleSlider.ValueChanged += (double v) =>
        {
            magScale = v;
            Stego();
        };
    }

    public void CreateFile(FileDialog fileDialog, bool p)
    {
        fileDialog.FileMode = FileDialog.FileModeEnum.OpenFile;
        fileDialog.Access = FileDialog.AccessEnum.Filesystem;
        fileDialog.Filters = new[] { "*.png, *.jpg, *.jpeg, *.bmp, *.webp ; Images" };
        fileDialog.FileSelected += (string path) => LoadImage(path, p);
        AddChild(fileDialog);
    }

    public void Load(bool p)
    {
        (p ? ref filePublic : ref filePrivate).PopupCentered(new Vector2I(800, 600));
    }

    public void LoadImage(string path, bool p)
    {
        var helper = ImageHelper.LoadAndPrepare(path);
        (p ? ref publicTexture : ref privateTexture) = helper.ToGreyscale();
        (p ? ref publicImage : ref privateImage).Texture = ImageTexture.CreateFromImage(
            (p ? ref publicTexture : ref privateTexture).ToGodotImage()
        );
        if (p)
        {
            fftPublic = FFTImage.FromImageNoShift(helper, Channel.L);
        }
        if (publicTexture != null && privateTexture != null)
        {
            Stego();
        }
    }

    public void Stego()
    {
        FFTImage encodedFFT = fftPublic.Clone();
        int w = encodedFFT.Width;
        int h = encodedFFT.Height;
        int halfW = w / 2;
        double[,] source = privateTexture.Sample(halfW, h).GetChannel(Channel.L);
        int firstCol = Math.Max(startingCol, 1);
        double maxMagReal = -1;
        double maxMagOG = -1;
        for (int x = firstCol; x < halfW; x++)
        {
            int mx = w - x;
            for (int y = 0; y < h; y++)
            {
                int my = (h - y) % h;
                double phase = encodedFFT.Complex.GetPixel(x, y).Phase;
                double ogMag = encodedFFT.Complex.GetPixel(x, y).Magnitude;
                if (ogMag > maxMagOG)
                {
                    maxMagOG = ogMag;
                }
                double mag = source[x, y] * magScale / 100;
                if (mag > maxMagReal)
                {
                    maxMagReal = mag;
                }
                encodedFFT.Complex.SetPixel(x, y, mag, phase);
                encodedFFT.Complex.SetPixel(mx, my, mag, -phase);
            }
        }
        GD.Print(maxMagOG);
        GD.Print(maxMagReal);
        ImageHelper e = encodedFFT.Complex.InverseFFT().ToDualPlot().Item1;
        encodedImage.Texture = ImageTexture.CreateFromImage(e.ToGodotImage());

        ImageHelper decoded = FFTImage
            .FromImageNoShift(e, Channel.L)
            .Complex.Scale(magScale)
            .ToDualPlot()
            .Item1;
        int decodedW = halfW - firstCol;
        decodedImage.Size = new Vector2(512 - startingCol * 2, 512);
        decodedImage.Texture = ImageTexture.CreateFromImage(
            decoded.Crop(decodedW, h, new Vector2((firstCol + halfW) / 2f, h / 2f)).ToGodotImage()
        );
    }
}
