#region Licence and Terms
// Accord.NET Extensions Framework
// https://github.com/dajuric/accord-net-extensions
//
// Copyright © Darko Jurić, 2014-2015 
// darko.juric2@gmail.com
//
//   This program is free software: you can redistribute it and/or modify
//   it under the terms of the GNU Lesser General Public License as published by
//   the Free Software Foundation, either version 3 of the License, or
//   (at your option) any later version.
//
//   This program is distributed in the hope that it will be useful,
//   but WITHOUT ANY WARRANTY; without even the implied warranty of
//   MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//   GNU Lesser General Public License for more details.
// 
//   You should have received a copy of the GNU Lesser General Public License
//   along with this program.  If not, see <https://www.gnu.org/licenses/lgpl.txt>.
//
#endregion

using System;
using System.IO;
using Accord.Extensions.Imaging;
using Accord.Extensions.Math;
using Accord.Math;
using AForge.Imaging;

namespace GenericImage
{
    public partial class Test
    {
#if PORTABLE
        private static string resourceFolder = "Resources";
#else
        static string resourceFolder = Path.Combine(new DirectoryInfo(Environment.CurrentDirectory).Parent.FullName, "Resources");
#endif
        static string colorImgName = Path.Combine(resourceFolder, "colorBig.jpg");
        static string FFT_sampleImgName = Path.Combine(resourceFolder, "FFT-sample.bmp");

        public static void TestConvolve(int kernelSize)
        {
#if PORTABLE
            var bmp = FromFile(colorImgName, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
#else
            var bmp = (System.Drawing.Bitmap)System.Drawing.Bitmap.FromFile(colorImgName);
#endif
            var smallIm = bmp.ToImage<Bgr, byte>();/*.Resize(new Size(320, 240), InterpolationMode.NearestNeighbor);*/ //AForge convolution takes too long

            var image = smallIm.Convert<Bgr, float>(); //do not measure converting byte->float and float->byte
            UnmanagedImage uIm = smallIm.ToAForgeImage(copyAlways: true);

            var floatKernel = Matrix.Random(kernelSize, kernelSize, 0, 255).ToSingle();
            int[,] intKernel = floatKernel.ToImage().Convert<Gray, int>().ToArray();

            measure(() =>
            {
                image.Convolve(floatKernel);
            },
            () =>
            {

                AForge.Imaging.Filters.Convolution c = new AForge.Imaging.Filters.Convolution(intKernel);
                c.Apply(uIm); 
            },
            2,
            "Image<,> Convolve",
            "AForge Convolve");     
        }

        public static void TestColorConversion()
        {
#if PORTABLE
            var bmp = FromFile(colorImgName, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
#else
            var bmp = (System.Drawing.Bitmap)System.Drawing.Bitmap.FromFile(colorImgName);
#endif
            var image = bmp.ToImage<Bgr, byte>();
            UnmanagedImage uIm = UnmanagedImage.FromManagedImage(bmp);

            measure(() =>
            {
                image.Convert<Gray, byte>();
            },
            () =>
            {
                AForge.Imaging.Filters.Grayscale.CommonAlgorithms.BT709.Apply(uIm);
            },
            1000,
            "Image<,> Bgr->Gray Conversion",
            "AForge Bgr->Gray Conversion");     
        }

        public static void TestFFT()
        {
#if PORTABLE
            var bmp = FromFile(FFT_sampleImgName, System.Drawing.Imaging.PixelFormat.Format8bppIndexed);
#else
            var bmp = (System.Drawing.Bitmap)System.Drawing.Bitmap.FromFile(FFT_sampleImgName);
#endif
            var image = bmp.ToImage<Gray, byte>().Convert<Complex, float>();
            ComplexImage cuIm = ComplexImage.FromBitmap(bmp);

            measure(() =>
            {
                image.FFT(FourierTransform.Direction.Forward);
                image.FFT(FourierTransform.Direction.Backward);
            },
            () =>
            {

                cuIm.ForwardFourierTransform();
                cuIm.BackwardFourierTransform();
            },
            100,
            "Image<,> FFT",
            "AForge FFT");     
        }

        public static void TestColorFiltering()
        {
#if PORTABLE
            var bmp = FromFile(colorImgName, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
#else
            var bmp = (System.Drawing.Bitmap)System.Drawing.Bitmap.FromFile(colorImgName);
#endif
            var image = bmp.ToImage<Bgr, byte>();
            UnmanagedImage uIm = UnmanagedImage.FromManagedImage(bmp);

            measure(() =>
            {
                var hsvIm = image.Convert<Hsv, byte>();
                var mask = hsvIm.InRange(new Hsv(335 / 2, 0, 0), new Hsv(180, 0, 0), 0 /*just first channel*/);

                var maskedIm = image.CopyBlank();
                image.CopyTo(maskedIm , mask);
            },
            () =>
            {

                AForge.Imaging.Filters.HSLFiltering f = new AForge.Imaging.Filters.HSLFiltering();
                f.Hue = new AForge.IntRange(335, 0);
                f.Apply(uIm);
            },
            100,
            "Image<,> HSV filtering",
            "AForge HSL filtering");
        }

        public static void TestChannelModifier()
        {
#if PORTABLE
            var bmp = FromFile(colorImgName, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
#else
            var bmp = (System.Drawing.Bitmap)System.Drawing.Bitmap.FromFile(colorImgName);
#endif
            var image = bmp.ToImage<Bgr, byte>();
            UnmanagedImage uIm = UnmanagedImage.FromManagedImage(bmp);

            measure(() =>
            {
                var hsvIm = image.Convert<Hsv, byte>();

                var hue = new Image<Gray, byte>(hsvIm.Size);
                hue.SetValue(180 / 2);

                hsvIm[Hsv.IdxH] = hue;
                var modifiedIm = hsvIm.Convert<Bgr, byte>();
            },
            () =>
            {
                AForge.Imaging.Filters.HueModifier hm = new AForge.Imaging.Filters.HueModifier(180);
                hm.Apply(uIm);
            },
            100,
            "Image<,> Hue modifier",
            "AForge Hue modifier");
        }

        public static void TestColorCasting()
        {
#if PORTABLE
            var bmp = FromFile(colorImgName, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
#else
            var bmp = (System.Drawing.Bitmap)System.Drawing.Bitmap.FromFile(colorImgName);
#endif
            var image = bmp.ToImage<Bgr, byte>();

            long elapsed = measure(() =>
            {
                image.Convert<Color3, byte>();
            },          
            1000, 
            true);
        }

        private static long measure(Action action, int numberOfTimes, bool writeMessage = false)
        {
            long s = DateTime.Now.Ticks;

            for (int i = 0; i < numberOfTimes; i++)
            {
                action();
            }

            long e = DateTime.Now.Ticks;
            long elapsed = (e - s) / TimeSpan.TicksPerMillisecond;
            
            if(writeMessage)
                Console.WriteLine("Per call ~{0} ms", (float)elapsed / numberOfTimes);

            return elapsed;
        }

        private static void measure(Action action1, Action action2, int numberOfTimes, string action1Name, string action2Name)
        {
            Console.WriteLine("Measuring {0}", action1Name);
            var elapsed1 = measure(action1, numberOfTimes);

            Console.WriteLine("Measuring {0}", action2Name);
            var elapsed2 = measure(action2, numberOfTimes);

            if (elapsed1 < elapsed2)
                Console.WriteLine("{0} is faster than {1} ~{2} times. Per call: ~{3} ms", action1Name, action2Name, (float)elapsed2 / elapsed1, (float)elapsed1 / numberOfTimes);
            else
                Console.WriteLine("{0} is slower than {1} ~{2} times. Per call: ~{3} ms", action1Name, action2Name, (float)elapsed1 / elapsed2, (float)elapsed1 / numberOfTimes);
        }

#if PORTABLE
        private static System.Drawing.Bitmap FromFile(string fileName, System.Drawing.Imaging.PixelFormat pixelFormat)
        {
            var img = new System.Windows.Media.Imaging.BitmapImage(new Uri(fileName, UriKind.Relative));
            return ((System.Drawing.Bitmap)img).Clone(pixelFormat);
        }
#endif
    }
}
