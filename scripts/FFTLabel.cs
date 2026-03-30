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
            }
            if (Input.IsActionPressed("CLICK"))
            {
                var tex = (ImageTexture)fTScene.imageFT.Texture;
                var image = tex.GetImage();
                // image.SetPixel((int)localPos.X, (int)localPos.Y, Colors.Black);
                for (int x = -25; x < 25; x++)
                for (int y = -25; y < 25; y++)
                {
                    if (
                        (int)localPos.X + x >= image.GetWidth()
                        || (int)localPos.X + x < 0
                        || (int)localPos.Y + y >= image.GetHeight()
                        || (int)localPos.Y + x < 0
                    )
                    {
                        continue;
                    }
                    image.SetPixel((int)localPos.X + x, (int)localPos.Y + y, Colors.Black);
                    fTScene.FFT.SetPixel((int)localPos.X + x, (int)localPos.Y + y);
                }
                tex.Update(image);
                fTScene.Inverse();
            }
        }
        else
        {
            text.Text = "";
        }
    }
}
