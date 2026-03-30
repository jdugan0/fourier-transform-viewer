using System;
using Godot;

public partial class FFTLabel : TextureRect
{
    [Export]
    private Label text;

    [Export]
    private FTScene fTScene;

    private bool mouseOver;

    [Export]
    private HueRing hue;

    [Export]
    Slider radiusSlider;

    public void Hover()
    {
        mouseOver = true;
    }

    public void UnHover()
    {
        mouseOver = false;
    }

    public override void _Draw()
    {
        if (mouseOver && Texture != null)
        {
            Vector2 localPos = GetLocalMousePosition();
            int radius = (int)radiusSlider.Value;
            DrawArc(localPos, radius, 0, Mathf.Tau, 64, Colors.White, 1.0f);
        }
    }

    public override void _Process(double delta)
    {
        QueueRedraw();
        if (mouseOver)
        {
            Vector2 localPos = GetLocalMousePosition();
            if (fTScene.FFT.c != null)
            {
                text.Text = String.Format(
                    "{0:F2}",
                    fTScene.FFT.c.data[(int)localPos.X, (int)localPos.Y].Magnitude
                );
            }
            if (Input.IsActionPressed("CLICK"))
            {
                var tex = (ImageTexture)fTScene.imageFT.Texture;
                var image = tex.GetImage();
                int radius = (int)radiusSlider.Value;
                // image.SetPixel((int)localPos.X, (int)localPos.Y, Colors.Black);
                for (int x = -radius; x < radius; x++)
                for (
                    int y = -(int)Math.Sqrt(radius * radius - x * x);
                    y < (int)Math.Sqrt(radius * radius - x * x);
                    y++
                )
                {
                    if (
                        (int)localPos.X + x >= image.GetWidth()
                        || (int)localPos.X + x < 0
                        || (int)localPos.Y + y >= image.GetHeight()
                        || (int)localPos.Y + y < 0
                    )
                    {
                        continue;
                    }
                    fTScene.FFT.c.SetPixel((int)localPos.X + x, (int)localPos.Y + y, 0, hue.Hue);
                }
                fTScene.imageFT.Texture = ImageTexture.CreateFromImage(
                    fTScene.FFT.c.ToArgPlot(fTScene.magScaleSlider.Value).ToGodotImage()
                );
                fTScene.Inverse();
            }
        }
        else
        {
            text.Text = "";
        }
    }
}
