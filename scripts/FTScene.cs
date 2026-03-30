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
    public ComplexChannel FFT = null;

    public override void _Ready()
    {
        fileDialog = new FileDialog();
        fileDialog.FileMode = FileDialog.FileModeEnum.OpenFile;
        fileDialog.Access = FileDialog.AccessEnum.Filesystem;
        fileDialog.Filters = new[] { "*.png, *.jpg, *.jpeg, *.bmp, *.webp ; Images" };
        fileDialog.FileSelected += OnFileSelected;
        AddChild(fileDialog);
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
        FFT = ComplexChannel.FromChannel(helper, Channel.L).FFT().data.FFTShift();
        imageFT.Texture = ImageTexture.CreateFromImage(FFT.ToArgPlot().ToGodotImage());
    }

    public void Inverse()
    {
        imageNormal.Texture = ImageTexture.CreateFromImage(
            FFT.FFTShift().InverseFFT().ToDualPlot().Item1.ToGodotImage()
        );
    }
}
