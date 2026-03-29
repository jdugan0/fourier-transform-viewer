using System;
using Godot;

namespace FTHelper
{
    public enum Channel
    {
        R,
        G,
        B,
        L,
        A,
        Lab_B,
    }

    public static class ColorConvert
    {
        public static (double L, double A, double B) RGBToLAB(double r, double g, double b)
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

        public static (double R, double G, double B) LABToRGB(double L, double A, double B)
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

            double R = Math.Clamp(Math.Round(rf * 255.0), 0, 255);
            double G = Math.Clamp(Math.Round(gf * 255.0), 0, 255);
            double Bl = Math.Clamp(Math.Round(bf * 255.0), 0, 255);

            return (R, G, Bl);
        }
    }

    public class ImageHelper
    {
        private readonly double[,] r,
            g,
            b;
        private double[,] labL,
            labA,
            labB;
        private bool labCached = false;

        public int Width => r.GetLength(0);
        public int Height => r.GetLength(1);

        public ImageHelper(Image image)
        {
            int w = image.GetWidth();
            int h = image.GetHeight();
            r = new double[w, h];
            g = new double[w, h];
            b = new double[w, h];
            for (int i = 0; i < w; i++)
            {
                for (int j = 0; j < h; j++)
                {
                    Color c = image.GetPixel(i, j);
                    r[i, j] = Math.Round(c.R * 255);
                    g[i, j] = Math.Round(c.G * 255);
                    b[i, j] = Math.Round(c.B * 255);
                }
            }
        }

        private ImageHelper(double[,] r, double[,] g, double[,] b)
        {
            this.r = r;
            this.g = g;
            this.b = b;
        }

        private void EnsureLAB()
        {
            if (labCached)
                return;
            int w = Width,
                h = Height;
            labL = new double[w, h];
            labA = new double[w, h];
            labB = new double[w, h];
            for (int i = 0; i < w; i++)
            {
                for (int j = 0; j < h; j++)
                {
                    var (L, A, B) = ColorConvert.RGBToLAB(r[i, j], g[i, j], b[i, j]);
                    labL[i, j] = L;
                    labA[i, j] = A;
                    labB[i, j] = B;
                }
            }
            labCached = true;
        }

        public double[,] GetChannel(Channel ch)
        {
            switch (ch)
            {
                case Channel.R:
                    return r;
                case Channel.G:
                    return g;
                case Channel.B:
                    return b;
                case Channel.L:
                    EnsureLAB();
                    return labL;
                case Channel.A:
                    EnsureLAB();
                    return labA;
                case Channel.Lab_B:
                    EnsureLAB();
                    return labB;
                default:
                    throw new ArgumentException("Unknown channel");
            }
        }

        // Returns a new ImageHelper with one RGB channel replaced.
        public ImageHelper WithChannel(Channel ch, double[,] data)
        {
            switch (ch)
            {
                case Channel.R:
                    return new ImageHelper(data, g, b);
                case Channel.G:
                    return new ImageHelper(r, data, b);
                case Channel.B:
                    return new ImageHelper(r, g, data);
                case Channel.L:
                case Channel.A:
                case Channel.Lab_B:
                    EnsureLAB();
                    double[,] newL = ch == Channel.L ? data : labL;
                    double[,] newA = ch == Channel.A ? data : labA;
                    double[,] newB = ch == Channel.Lab_B ? data : labB;
                    return FromLAB(newL, newA, newB);
                default:
                    throw new ArgumentException("Unknown channel");
            }
        }

        public static ImageHelper FromLAB(double[,] L, double[,] A, double[,] B)
        {
            int w = L.GetLength(0),
                h = L.GetLength(1);
            double[,] r = new double[w, h];
            double[,] g = new double[w, h];
            double[,] b = new double[w, h];
            for (int i = 0; i < w; i++)
            {
                for (int j = 0; j < h; j++)
                {
                    var (R, G, Bl) = ColorConvert.LABToRGB(L[i, j], A[i, j], B[i, j]);
                    r[i, j] = R;
                    g[i, j] = G;
                    b[i, j] = Bl;
                }
            }
            var result = new ImageHelper(r, g, b);
            result.labL = L;
            result.labA = A;
            result.labB = B;
            result.labCached = true;
            return result;
        }

        public Image ToGodotImage()
        {
            int w = Width,
                h = Height;
            var img = Image.CreateEmpty(w, h, false, Image.Format.Rgb8);
            for (int i = 0; i < w; i++)
            {
                for (int j = 0; j < h; j++)
                {
                    img.SetPixel(
                        i,
                        j,
                        new Color(
                            (float)(r[i, j] / 255.0),
                            (float)(g[i, j] / 255.0),
                            (float)(b[i, j] / 255.0)
                        )
                    );
                }
            }
            return img;
        }

        public ImageHelper Crop(int w, int h, Vector2 center)
        {
            int startX = (int)center.X - w / 2;
            int startY = (int)center.Y - h / 2;
            double[,] cr = new double[w, h];
            double[,] cg = new double[w, h];
            double[,] cb = new double[w, h];
            for (int i = 0; i < w; i++)
            {
                for (int j = 0; j < h; j++)
                {
                    int srcX = startX + i;
                    int srcY = startY + j;
                    if (srcX >= 0 && srcX < Width && srcY >= 0 && srcY < Height)
                    {
                        cr[i, j] = r[srcX, srcY];
                        cg[i, j] = g[srcX, srcY];
                        cb[i, j] = b[srcX, srcY];
                    }
                }
            }
            return new ImageHelper(cr, cg, cb);
        }

        public ImageHelper Sample(int newW, int newH)
        {
            int oldW = Width,
                oldH = Height;
            double[,] sr = new double[newW, newH];
            double[,] sg = new double[newW, newH];
            double[,] sb = new double[newW, newH];
            for (int i = 0; i < newW; i++)
            {
                for (int j = 0; j < newH; j++)
                {
                    int srcX = Math.Min((int)((double)i / newW * oldW), oldW - 1);
                    int srcY = Math.Min((int)((double)j / newH * oldH), oldH - 1);
                    sr[i, j] = r[srcX, srcY];
                    sg[i, j] = g[srcX, srcY];
                    sb[i, j] = b[srcX, srcY];
                }
            }
            return new ImageHelper(sr, sg, sb);
        }
    }
}
