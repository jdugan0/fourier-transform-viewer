using System;
using System.Numerics;
using Godot;
using MathNet.Numerics;
using MathNet.Numerics.IntegralTransforms;
using Vector2 = Godot.Vector2;

namespace FTHelper
{
    public class ComplexChannel
    {
        public Complex[,] data;

        public ComplexChannel(Complex[,] data)
        {
            this.data = data;
        }

        public void SetPixel(int x, int y, double mag, double phase)
        {
            data[x, y] = Complex.FromPolarCoordinates(mag, phase);
        }

        public Complex GetPixel(int x, int y)
        {
            return data[x, y];
        }

        private static double ChannelScale(Channel ch)
        {
            switch (ch)
            {
                case Channel.R:
                case Channel.G:
                case Channel.B:
                    return 255.0;
                case Channel.L:
                    return 100.0;
                case Channel.A:
                case Channel.Lab_B:
                    return 128.0;
                case Channel.H:
                    return 360.0;
                case Channel.S:
                case Channel.V:
                    return 1.0;
                default:
                    return 1.0;
            }
        }

        public static ComplexChannel FromChannel(ImageHelper image, Channel ch)
        {
            double[,] raw = image.GetChannel(ch);
            int w = raw.GetLength(0),
                h = raw.GetLength(1);
            double scale = ChannelScale(ch);
            Complex[,] data = new Complex[w, h];
            for (int i = 0; i < w; i++)
            for (int j = 0; j < h; j++)
                data[i, j] = new Complex(raw[i, j] / scale, 0);
            return new ComplexChannel(data);
        }

        public (ComplexChannel data, double maxValue) FFT()
        {
            int w = data.GetLength(0),
                h = data.GetLength(1);
            Complex[,] result = (Complex[,])data.Clone();

            // Transform rows
            for (int j = 0; j < h; j++)
            {
                Complex[] row = new Complex[w];
                for (int i = 0; i < w; i++)
                    row[i] = result[i, j];
                Fourier.Forward(row, FourierOptions.Default);
                for (int i = 0; i < w; i++)
                    result[i, j] = row[i];
            }

            // Transform columns
            double maxMag = double.MinValue;
            for (int i = 0; i < w; i++)
            {
                Complex[] col = new Complex[h];
                for (int j = 0; j < h; j++)
                    col[j] = result[i, j];
                Fourier.Forward(col, FourierOptions.Default);
                for (int j = 0; j < h; j++)
                {
                    result[i, j] = col[j];
                    double mag = col[j].Magnitude;
                    if (mag > maxMag)
                        maxMag = mag;
                }
            }

            return (new ComplexChannel(result), maxMag);
        }

        public ComplexChannel FFTShift()
        {
            int w = data.GetLength(0);
            int h = data.GetLength(1);
            Complex[,] result = new Complex[w, h];
            int hw = w / 2,
                hh = h / 2;
            for (int i = 0; i < w; i++)
            for (int j = 0; j < h; j++)
                result[(i + hw) % w, (j + hh) % h] = data[i, j];
            return new ComplexChannel(result);
        }

        public ComplexChannel InverseFFT()
        {
            int w = data.GetLength(0),
                h = data.GetLength(1);
            Complex[,] result = (Complex[,])data.Clone();

            // Transform rows
            for (int j = 0; j < h; j++)
            {
                Complex[] row = new Complex[w];
                for (int i = 0; i < w; i++)
                    row[i] = result[i, j];
                Fourier.Inverse(row, FourierOptions.Default);
                for (int i = 0; i < w; i++)
                    result[i, j] = row[i];
            }

            // Transform columns
            double maxMag = double.MinValue;
            for (int i = 0; i < w; i++)
            {
                Complex[] col = new Complex[h];
                for (int j = 0; j < h; j++)
                    col[j] = result[i, j];
                Fourier.Inverse(col, FourierOptions.Default);
                for (int j = 0; j < h; j++)
                {
                    result[i, j] = col[j];
                    double mag = col[j].Magnitude;
                    if (mag > maxMag)
                        maxMag = mag;
                }
            }

            return new ComplexChannel(result);
        }

        public ImageHelper ToArgPlot(double magScale)
        {
            int w = data.GetLength(0);
            int h = data.GetLength(1);

            double[,] rr = new double[w, h];
            double[,] gg = new double[w, h];
            double[,] bb = new double[w, h];

            for (int i = 0; i < w; i++)
            for (int j = 0; j < h; j++)
            {
                double hue = (data[i, j].Phase / Math.PI + 1.0) * 180.0;
                double val = Math.Log(1 + data[i, j].Magnitude) * magScale;
                var (R, G, B) = ColorConvert.HSVToRGB(hue, 1.0, val);
                rr[i, j] = R;
                gg[i, j] = G;
                bb[i, j] = B;
            }
            return new ImageHelper(rr, gg, bb);
        }

        public (ImageHelper, ImageHelper) ToDualPlot()
        {
            int w = data.GetLength(0);
            int h = data.GetLength(1);

            double[,] magR = new double[w, h],
                magG = new double[w, h],
                magB = new double[w, h];
            double[,] argR = new double[w, h],
                argG = new double[w, h],
                argB = new double[w, h];

            for (int i = 0; i < w; i++)
            for (int j = 0; j < h; j++)
            {
                double mag = data[i, j].Magnitude * 255.0;
                magR[i, j] = mag;
                magG[i, j] = mag;
                magB[i, j] = mag;

                double arg = (data[i, j].Phase / Math.PI + 1.0) * 0.5 * 255.0;
                argR[i, j] = arg;
                argG[i, j] = arg;
                argB[i, j] = arg;
            }

            return (new ImageHelper(magR, magG, magB), new ImageHelper(argR, argG, argB));
        }
    }

    public enum Channel
    {
        R,
        G,
        B,
        L,
        A,
        Lab_B,
        H,
        S,
        V,
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

        public static (double H, double S, double V) RGBToHSV(double r, double g, double b)
        {
            double rf = r / 255.0;
            double gf = g / 255.0;
            double bf = b / 255.0;

            double max = Math.Max(rf, Math.Max(gf, bf));
            double min = Math.Min(rf, Math.Min(gf, bf));
            double delta = max - min;

            double H = 0;
            if (delta > 0)
            {
                if (max == rf)
                    H = 60.0 * (((gf - bf) / delta) % 6.0);
                else if (max == gf)
                    H = 60.0 * (((bf - rf) / delta) + 2.0);
                else
                    H = 60.0 * (((rf - gf) / delta) + 4.0);
            }
            if (H < 0)
                H += 360.0;

            double S = max == 0 ? 0 : delta / max;
            double V = max;

            return (H, S, V);
        }

        public static (double R, double G, double B) HSVToRGB(double H, double S, double V)
        {
            double C = V * S;
            double X = C * (1.0 - Math.Abs((H / 60.0) % 2.0 - 1.0));
            double m = V - C;

            double rf,
                gf,
                bf;
            if (H < 60)
            {
                rf = C;
                gf = X;
                bf = 0;
            }
            else if (H < 120)
            {
                rf = X;
                gf = C;
                bf = 0;
            }
            else if (H < 180)
            {
                rf = 0;
                gf = C;
                bf = X;
            }
            else if (H < 240)
            {
                rf = 0;
                gf = X;
                bf = C;
            }
            else if (H < 300)
            {
                rf = X;
                gf = 0;
                bf = C;
            }
            else
            {
                rf = C;
                gf = 0;
                bf = X;
            }

            double R = Math.Clamp(Math.Round((rf + m) * 255.0), 0, 255);
            double G = Math.Clamp(Math.Round((gf + m) * 255.0), 0, 255);
            double B = Math.Clamp(Math.Round((bf + m) * 255.0), 0, 255);

            return (R, G, B);
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
        private double[,] hsvH,
            hsvS,
            hsvV;
        private bool labCached = false;
        private bool hsvCached = false;

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

        public ImageHelper(double[,] r, double[,] g, double[,] b)
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

        private void EnsureHSV()
        {
            if (hsvCached)
                return;
            int w = Width,
                h = Height;
            hsvH = new double[w, h];
            hsvS = new double[w, h];
            hsvV = new double[w, h];
            for (int i = 0; i < w; i++)
            {
                for (int j = 0; j < h; j++)
                {
                    var (H, S, V) = ColorConvert.RGBToHSV(r[i, j], g[i, j], b[i, j]);
                    hsvH[i, j] = H;
                    hsvS[i, j] = S;
                    hsvV[i, j] = V;
                }
            }
            hsvCached = true;
        }

        private void InvalidateCaches()
        {
            labCached = false;
            hsvCached = false;
        }

        public void SetPixelRGB(int x, int y, double red, double green, double blue)
        {
            r[x, y] = red;
            g[x, y] = green;
            b[x, y] = blue;
            InvalidateCaches();
        }

        public void SetPixelLAB(int x, int y, double L, double A, double B)
        {
            var (R, G, Bl) = ColorConvert.LABToRGB(L, A, B);
            r[x, y] = R;
            g[x, y] = G;
            b[x, y] = Bl;
            InvalidateCaches();
        }

        public void SetPixelHSV(int x, int y, double H, double S, double V)
        {
            var (R, G, Bl) = ColorConvert.HSVToRGB(H, S, V);
            r[x, y] = R;
            g[x, y] = G;
            b[x, y] = Bl;
            InvalidateCaches();
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
                case Channel.H:
                    EnsureHSV();
                    return hsvH;
                case Channel.S:
                    EnsureHSV();
                    return hsvS;
                case Channel.V:
                    EnsureHSV();
                    return hsvV;
                default:
                    throw new ArgumentException("Unknown channel");
            }
        }

        // Returns a new ImageHelper with one channel replaced.
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
                case Channel.H:
                case Channel.S:
                case Channel.V:
                    EnsureHSV();
                    double[,] newH = ch == Channel.H ? data : hsvH;
                    double[,] newS = ch == Channel.S ? data : hsvS;
                    double[,] newV = ch == Channel.V ? data : hsvV;
                    return FromHSV(newH, newS, newV);
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

        public static ImageHelper FromHSV(double[,] H, double[,] S, double[,] V)
        {
            int w = H.GetLength(0),
                h = H.GetLength(1);
            double[,] r = new double[w, h];
            double[,] g = new double[w, h];
            double[,] b = new double[w, h];
            for (int i = 0; i < w; i++)
            {
                for (int j = 0; j < h; j++)
                {
                    var (R, G, Bl) = ColorConvert.HSVToRGB(H[i, j], S[i, j], V[i, j]);
                    r[i, j] = R;
                    g[i, j] = G;
                    b[i, j] = Bl;
                }
            }
            var result = new ImageHelper(r, g, b);
            result.hsvH = H;
            result.hsvS = S;
            result.hsvV = V;
            result.hsvCached = true;
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
