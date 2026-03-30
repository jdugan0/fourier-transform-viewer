using System;
using System.Runtime.InteropServices.Marshalling;
using Godot;

public partial class FFTLabel : TextureRect
{
    [Export]
    private Label text;

    [Export]
    private FTScene fTScene;

    private bool mouseOver;
    private Vector2? lastPaintPos;

    [Export]
    private HueRing hue;

    [Export]
    Slider radiusSlider;

    [Export]
    Slider magDrawSlider;

    private const double MagSliderExponent = 6;
    private const double MagSliderDeadzone = 0.001;
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

    [Export]
    Label l;

    public override void _Ready()
    {
        magDrawSlider.MinValue = 0;
        magDrawSlider.MaxValue = 1;
        magDrawSlider.Step = 0.001;
        magDrawSlider.ValueChanged += UpdateLabel;
        UpdateLabel(magDrawSlider.Value);
    }

    public void UpdateLabel(double v)
    {
        l.Text = String.Format("{0:F2}", GetMagValue());
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

    private void PaintAt(Vector2 center)
    {
        var tex = (ImageTexture)fTScene.imageFT.Texture;
        var image = tex.GetImage();
        int radius = (int)radiusSlider.Value;

        for (int x = -radius; x < radius; x++)
        for (
            int y = -(int)Math.Sqrt(radius * radius - x * x);
            y < (int)Math.Sqrt(radius * radius - x * x);
            y++
        )
        {
            int xC = (int)center.X + x;
            int yC = (int)center.Y + y;

            if (xC >= image.GetWidth() || xC < 0 || yC >= image.GetHeight() || yC < 0)
                continue;

            double mag = lockMag.ButtonPressed
                ? fTScene.FFT.c.GetPixel(xC, yC).Magnitude
                : GetMagValue();
            double phase = lockPhase.ButtonPressed
                ? fTScene.FFT.c.GetPixel(xC, yC).Phase
                : 2 * Math.PI * (hue.Hue) + Math.PI;

            fTScene.FFT.c.SetPixel(xC, yC, mag, phase);
            int mirrorX = (image.GetWidth() - xC) % image.GetWidth();
            int mirrorY = (image.GetHeight() - yC) % image.GetHeight();
            fTScene.FFT.c.SetPixel(mirrorX, mirrorY, mag, -phase);
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
                if (lastPaintPos == null)
                    lastPaintPos = localPos;

                float dist = lastPaintPos.Value.DistanceTo(localPos);
                int steps = Math.Max(1, (int)Math.Ceiling(dist));

                for (int s = 0; s <= steps; s++)
                {
                    float t = steps == 0 ? 0 : (float)s / steps;
                    Vector2 pos = lastPaintPos.Value.Lerp(localPos, t);
                    PaintAt(pos);
                }

                lastPaintPos = localPos;

                fTScene.imageFT.Texture = ImageTexture.CreateFromImage(
                    fTScene.FFT.c.ToArgPlot(fTScene.magScaleSlider.Value).ToGodotImage()
                );
                fTScene.Inverse();
            }
            else
            {
                lastPaintPos = null;
            }
        }
        else
        {
            text.Text = "";
        }
    }
}
