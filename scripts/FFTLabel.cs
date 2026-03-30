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

    [Export]
    Slider magDrawSlider;

    private const double MagSliderExponent = 3.0;
    private const double MagSliderDeadzone = 0.02;
    private double magMax = 10;

    private double GetMagValue()
    {
        double t = magDrawSlider.Value;
        if (t <= MagSliderDeadzone)
            return 0;
        double normalized = (t - MagSliderDeadzone) / (1.0 - MagSliderDeadzone);
        return Math.Pow(normalized, MagSliderExponent) * magMax;
    }

    [Export]
    Button lockMag;

    [Export]
    Button lockPhase;

    public override void _Ready()
    {
        magDrawSlider.MinValue = 0;
        magDrawSlider.MaxValue = 1;
        magDrawSlider.Step = 0.001;
    }

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
        if (fTScene.FFT.c != null && fTScene.FFT.max > 10)
        {
            magMax = fTScene.FFT.max * 1.3;
        }
        else
        {
            magMax = 10;
        }
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
                    int xC = (int)localPos.X + x;
                    int yC = (int)localPos.Y + y;

                    fTScene.FFT.c.SetPixel(
                        xC,
                        yC,
                        lockMag.ButtonPressed
                            ? fTScene.FFT.c.GetPixel(xC, yC).Magnitude
                            : GetMagValue(),
                        lockPhase.ButtonPressed
                            ? fTScene.FFT.c.GetPixel(xC, yC).Phase
                            : 2 * Math.PI * (hue.Hue) + Math.PI
                    );
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
