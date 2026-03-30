using System;
using FTHelper;
using Godot;

public partial class FTScene : Control
{
    [Export]
    private TextureRect imageNormal;

    [Export]
    public TextureRect imageFT;

    private FileDialog fileDialog;
    public (ComplexChannel c, double max) FFT = (null, 0);

    [Export]
    public Slider magScaleSlider;

    public override void _Ready()
    {
        fileDialog = new FileDialog();
        fileDialog.FileMode = FileDialog.FileModeEnum.OpenFile;
        fileDialog.Access = FileDialog.AccessEnum.Filesystem;
        fileDialog.Filters = new[] { "*.png, *.jpg, *.jpeg, *.bmp, *.webp ; Images" };
        fileDialog.FileSelected += OnFileSelected;
        AddChild(fileDialog);
        magScaleSlider.ValueChanged += MagChanged;
    }

    public void MagChanged(double v)
    {
        if (FFT.c != null)
        {
            imageFT.Texture = ImageTexture.CreateFromImage(
                FFT.c.ToArgPlot(magScaleSlider.Value).ToGodotImage()
            );
        }
    }

    public override void _Process(double delta)
    {
        if (FFT.max != 0)
        {
            magScaleSlider.MinValue = 1 / FFT.max;
        }
    }

    public void LoadImage()
    {
        fileDialog.PopupCentered(new Vector2I(800, 600));
    }

    private void OnFileSelected(string path)
    {
        var image = new Image();
        image.Load(path);

        var helper = new ImageHelper(image);
        int size = Math.Min(helper.Width, helper.Height);
        var center = new Godot.Vector2(helper.Width / 2f, helper.Height / 2f);
        helper = helper.Crop(size, size, center).Sample(512, 512);
        ImageHelper greyScale = ImageHelper.FromLAB(
            helper.GetChannel(Channel.L),
            new double[512, 512],
            new double[512, 512]
        );
        imageNormal.Texture = ImageTexture.CreateFromImage(greyScale.ToGodotImage());
        var f = ComplexChannel.FromChannel(helper, Channel.L).FFT();
        FFT = (f.data.FFTShift(), f.maxValue);
        imageFT.Texture = ImageTexture.CreateFromImage(
            FFT.c.ToArgPlot(magScaleSlider.Value).ToGodotImage()
        );
    }

    public void LoadBlank()
    {
        var image = Image.CreateEmpty(512, 512, false, Image.Format.Rgba8);
        image.Fill(Colors.Black);

        var helper = new ImageHelper(image);
        ImageHelper greyScale = ImageHelper.FromLAB(
            helper.GetChannel(Channel.L),
            new double[512, 512],
            new double[512, 512]
        );
        imageNormal.Texture = ImageTexture.CreateFromImage(greyScale.ToGodotImage());
        var f = ComplexChannel.FromChannel(helper, Channel.L).FFT();
        FFT = (f.data.FFTShift(), f.maxValue);
        imageFT.Texture = ImageTexture.CreateFromImage(
            FFT.c.ToArgPlot(magScaleSlider.Value).ToGodotImage()
        );
    }

    public void Inverse()
    {
        imageNormal.Texture = ImageTexture.CreateFromImage(
            FFT.c.FFTShift().InverseFFT().ToDualPlot().Item1.ToGodotImage()
        );
    }
}
