using System;
using Godot;

namespace FTHelper
{
    public static class ColorConvert
    {
        public static (double L, double A, double B) RGBToLAB(int r, int g, int b)
        {
            double rf = r / 255.0;
            double gf = g / 255.0;
            double bf = b / 255.0;

            rf = rf > 0.04045 ? Math.Pow((rf + 0.055) / 1.055, 2.4) : rf / 12.92;
            gf = gf > 0.04045 ? Math.Pow((gf + 0.055) / 1.055, 2.4) : gf / 12.92;
            bf = bf > 0.04045 ? Math.Pow((bf + 0.055) / 1.055, 2.4) : bf / 12.92;

            double x = (rf * 0.4124564 + gf * 0.3575761 + bf * 0.1804375) / 0.95047;
            double y = (rf * 0.2126729 + gf * 0.7151522 + bf * 0.0721750);
            double z = (rf * 0.0193339 + gf * 0.1191920 + bf * 0.9503041) / 1.08883;

            x = x > 0.008856 ? Math.Pow(x, 1.0 / 3.0) : (903.3 * x + 16.0) / 116.0;
            y = y > 0.008856 ? Math.Pow(y, 1.0 / 3.0) : (903.3 * y + 16.0) / 116.0;
            z = z > 0.008856 ? Math.Pow(z, 1.0 / 3.0) : (903.3 * z + 16.0) / 116.0;

            double L = 116.0 * y - 16.0;
            double A = 500.0 * (x - y);
            double B = 200.0 * (y - z);

            return (L, A, B);
        }

        public static (int R, int G, int B) LABToRGB(double L, double A, double B)
        {
            double y = (L + 16.0) / 116.0;
            double x = A / 500.0 + y;
            double z = y - B / 200.0;

            double x3 = x * x * x;
            double y3 = y * y * y;
            double z3 = z * z * z;

            x = x3 > 0.008856 ? x3 : (116.0 * x - 16.0) / 903.3;
            y = y3 > 0.008856 ? y3 : (116.0 * y - 16.0) / 903.3;
            z = z3 > 0.008856 ? z3 : (116.0 * z - 16.0) / 903.3;

            x *= 0.95047;
            z *= 1.08883;

            double rf = x * 3.2404542 + y * -1.5371385 + z * -0.4985314;
            double gf = x * -0.9692660 + y * 1.8760108 + z * 0.0415560;
            double bf = x * 0.0556434 + y * -0.2040259 + z * 1.0572252;

            rf = rf > 0.0031308 ? 1.055 * Math.Pow(rf, 1.0 / 2.4) - 0.055 : 12.92 * rf;
            gf = gf > 0.0031308 ? 1.055 * Math.Pow(gf, 1.0 / 2.4) - 0.055 : 12.92 * gf;
            bf = bf > 0.0031308 ? 1.055 * Math.Pow(bf, 1.0 / 2.4) - 0.055 : 12.92 * bf;

            int R = (int)Math.Clamp(Math.Round(rf * 255.0), 0, 255);
            int G = (int)Math.Clamp(Math.Round(gf * 255.0), 0, 255);
            int Bl = (int)Math.Clamp(Math.Round(bf * 255.0), 0, 255);

            return (R, G, Bl);
        }
    }

    public class Pixel
    {
        (int, int, int) rgb;

        public Pixel((int, int, int) rgb)
        {
            this.rgb = rgb;
        }

        public (int, int, int) GetRGB()
        {
            return rgb;
        }

        public (double, double, double) GetLAB()
        {
            return ColorConvert.RGBToLAB(rgb.Item1, rgb.Item2, rgb.Item3);
        }

        public static Pixel FromLAB(double L, double A, double B)
        {
            return new Pixel(ColorConvert.LABToRGB(L, A, B));
        }
    }

    public class ImageHelper
    {
        private Pixel[,] pixelArray;

        public ImageHelper(Image image)
        {
            int w = image.GetWidth();
            int h = image.GetHeight();
            pixelArray = new Pixel[w, h];
            for (int i = 0; i < w; i++)
            {
                for (int j = 0; j < h; j++)
                {
                    Color c = image.GetPixel(i, j);
                    int r = (int)Math.Round(c.R * 255);
                    int g = (int)Math.Round(c.G * 255);
                    int b = (int)Math.Round(c.B * 255);
                    pixelArray[i, j] = new Pixel((r, g, b));
                }
            }
        }

        public ImageHelper(ImageHelper image)
        {
            pixelArray = image.pixelArray;
        }

        public ImageHelper(ImageHelper image, int x, int y)
        {
            int oldW = image.pixelArray.GetLength(0);
            int oldH = image.pixelArray.GetLength(1);
            pixelArray = new Pixel[x, y];
            for (int i = 0; i < x; i++)
            {
                for (int j = 0; j < y; j++)
                {
                    int srcX = Math.Min((int)((double)i / x * oldW), oldW - 1);
                    int srcY = Math.Min((int)((double)j / y * oldH), oldH - 1);
                    pixelArray[i, j] = image.pixelArray[srcX, srcY];
                }
            }
        }

        public ImageHelper(int x, int y)
        {
            pixelArray = new Pixel[x, y];
        }

        public Pixel GetPixel(int x, int y)
        {
            return pixelArray[x, y];
        }

        public Pixel SetPixel(Pixel p, int x, int y)
        {
            Pixel temp = pixelArray[x, y];
            pixelArray[x, y] = p;
            return temp;
        }

        public Image ToGodotImage()
        {
            int w = pixelArray.GetLength(0);
            int h = pixelArray.GetLength(1);
            var img = Image.CreateEmpty(w, h, false, Image.Format.Rgb8);
            for (int i = 0; i < w; i++)
            {
                for (int j = 0; j < h; j++)
                {
                    if (pixelArray[i, j] == null)
                    {
                        img.SetPixel(i, j, new Color(0, 0, 0));
                    }
                    else
                    {
                        var (r, g, b) = pixelArray[i, j].GetRGB();
                        img.SetPixel(i, j, new Color(r / 255f, g / 255f, b / 255f));
                    }
                }
            }
            return img;
        }

        public ImageHelper Crop(int x, int y, Vector2 center)
        {
            var cropped = new ImageHelper(x, y);
            int startX = (int)center.X - x / 2;
            int startY = (int)center.Y - y / 2;
            for (int i = 0; i < x; i++)
            {
                for (int j = 0; j < y; j++)
                {
                    int srcX = startX + i;
                    int srcY = startY + j;
                    if (
                        srcX >= 0
                        && srcX < pixelArray.GetLength(0)
                        && srcY >= 0
                        && srcY < pixelArray.GetLength(1)
                    )
                    {
                        cropped.pixelArray[i, j] = pixelArray[srcX, srcY];
                    }
                }
            }
            return cropped;
        }

        // upsamples or downsamples image to dimensions x, y
        public ImageHelper Sample(int x, int y)
        {
            return new ImageHelper(this, x, y);
        }
    }
}
