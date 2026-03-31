using System;
using FTHelper;
using Godot;

public partial class FTScene : Control, IFFTDisplay
{
    [Export]
    private TextureRect imageNormal;

    [Export]
    public TextureRect imageFT;

    private FileDialog fileDialog;
    public FFTImage FFT { get; private set; }

    [Export]
    public Slider magScaleSlider;

    public double MagScale => magScaleSlider.Value;

    public override void _Ready()
    {
        fileDialog = new FileDialog();
        fileDialog.FileMode = FileDialog.FileModeEnum.OpenFile;
        fileDialog.Access = FileDialog.AccessEnum.Filesystem;
        fileDialog.Filters = new[] { "*.png, *.jpg, *.jpeg, *.bmp, *.webp ; Images" };
        fileDialog.FileSelected += OnFileSelected;
        AddChild(fileDialog);
        if (magScaleSlider != null)
        {
            magScaleSlider.ValueChanged += MagChanged;
        }
    }

    public void MagChanged(double v)
    {
        if (FFT != null)
        {
            imageFT.Texture = ImageTexture.CreateFromImage(FFT.ToArgPlot(MagScale).ToGodotImage());
        }
    }

    public override void _Process(double delta)
    {
        if (FFT != null && FFT.Max != 0 && magScaleSlider != null)
        {
            magScaleSlider.MinValue = 1 / FFT.Max;
        }
    }

    public void LoadImage()
    {
        fileDialog.PopupCentered(new Vector2I(800, 600));
    }

    private void OnFileSelected(string path)
    {
        var helper = ImageHelper.LoadAndPrepare(path);
        imageNormal.Texture = ImageTexture.CreateFromImage(helper.ToGreyscale().ToGodotImage());
        FFT = FFTImage.FromImage(helper, Channel.L);
        imageFT.Texture = ImageTexture.CreateFromImage(FFT.ToArgPlot(MagScale).ToGodotImage());
    }

    public void LoadBlank()
    {
        var helper = ImageHelper.BlankGrey();
        imageNormal.Texture = ImageTexture.CreateFromImage(helper.ToGreyscale().ToGodotImage());
        FFT = FFTImage.FromImage(helper, Channel.L);
        imageFT.Texture = ImageTexture.CreateFromImage(FFT.ToArgPlot(MagScale).ToGodotImage());
    }

    public void OnFFTModified()
    {
        imageFT.Texture = ImageTexture.CreateFromImage(FFT.ToArgPlot(MagScale).ToGodotImage());
        imageNormal.Texture = ImageTexture.CreateFromImage(FFT.ToSpatial().ToGodotImage());
    }
}
