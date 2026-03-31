using System;
using Godot;

public partial class StegoManager : Node
{
    [Export]
    public Slider magScaleSlider;
    private FileDialog fileDialog;

    [Export]
    private TextureRect imageOrigin;

    [Export]
    public TextureRect originFT;

    [Export]
    TextureRect hidden;

    [Export]
    TextureRect modifiedFT;

    [Export]
    TextureRect encodedImage;

    [Export]
    TextureRect decodedImage;

    public override void _Ready()
    {
        fileDialog = new FileDialog();
        fileDialog.FileMode = FileDialog.FileModeEnum.OpenFile;
        fileDialog.Access = FileDialog.AccessEnum.Filesystem;
        fileDialog.Filters = new[] { "*.png, *.jpg, *.jpeg, *.bmp, *.webp ; Images" };
        fileDialog.FileSelected += LoadOrigin;
        AddChild(fileDialog);
        if (magScaleSlider != null)
        {
            magScaleSlider.ValueChanged += MagChanged;
        }
    }

    public void MagChanged(double v) { }

    public void LoadOrigin(string path) { }
}
