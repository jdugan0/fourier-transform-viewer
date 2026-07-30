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
    double scale = 1;

    public override void _Ready()
    {
        filePrivate = new FileDialog();
        filePublic = new FileDialog();
        CreateFile(filePrivate, false);
        CreateFile(filePublic, true);
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
        FFTImage encodedFFT = fftPublic;
        double maxMagReal = -1;
        double maxMagOG = -1;
        for (int x = startingCol; x < 512; x++)
        {
            for (int y = 0; y < 512; y++)
            {
                double phase = encodedFFT.Complex.GetPixel(x, y).Phase;
                double ogMag = encodedFFT.Complex.GetPixel(x, y).Magnitude;
                if (ogMag > maxMagOG)
                {
                    maxMagOG = ogMag;
                }
                double mag = privateTexture.GetChannel(Channel.L)[x, y] * scale;
                if (mag > maxMagReal)
                {
                    maxMagReal = mag;
                }
                encodedFFT.Complex.SetPixel(x, y, mag, phase);
            }
        }
        GD.Print(maxMagOG);
        GD.Print(maxMagReal);
        ImageHelper e = encodedFFT.Complex.InverseFFT().ToDualPlot().Item1;
        encodedImage.Texture = ImageTexture.CreateFromImage(e.ToGodotImage());

        decodedImage.Texture = ImageTexture.CreateFromImage(
            FFTImage
                .FromImageNoShift(e, Channel.L)
                .Complex.Scale(scale)
                .ToDualPlot()
                .Item1.ToGodotImage()
        );
    }
}
