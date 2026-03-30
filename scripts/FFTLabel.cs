using System;
using Godot;

public partial class FFTLabel : TextureRect
{
    [Export]
    private Label text;

    [Export]
    private FTScene fTScene;

    private bool mouseOver;

    public void Hover()
    {
        mouseOver = true;
    }

    public void UnHover()
    {
        mouseOver = false;
    }

    public override void _Process(double delta)
    {
        if (mouseOver)
        {
            Vector2 localPos = GetLocalMousePosition();
            if (fTScene.FFT != null)
            {
                text.Text = String.Format(
                    "{0:F2}",
                    fTScene.FFT.data[(int)localPos.X, (int)localPos.Y].Magnitude
                );
                return;
            }
        }
        text.Text = "";
    }
}
