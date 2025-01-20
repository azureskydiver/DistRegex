using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

// Transliteration of distrx.cpp from C++ to C#

namespace DistRegex.Literal
{
    record struct RGB(byte R, byte G, byte B)
    {
        public readonly Color ARGB => Color.FromArgb(R, G, B);
    }

    class Mat
    {
        readonly int _size;
        readonly int[] _data;
        readonly byte _depth;

        public Mat(int n)
        {
            _depth = (byte) n;
            _size = (int) Math.Pow(2, n);
            _data = Enumerable.Repeat(0, _size * _size).ToArray();

            Populate(_data, _size, _size);
        }

        static void Populate(Span<int> mat, int size, int rawSize)
        {
            if (size <= 1)
                return;

            for (int i = 0; i < size; ++i)
            {
                for (int j = 0; j < size; ++j)
                {
                    mat[j + i * rawSize] *= 10;
                    mat[j + i * rawSize] += (i < size / 2) ? ((j < size / 2) ? 1 : 2)
                                                           : ((j < size / 2) ? 3 : 4);
                }
            }

            Populate(mat[0..],                          size / 2, rawSize);
            Populate(mat[(size / 2)..],                 size / 2, rawSize);
            Populate(mat[(size / 2 * rawSize)..],       size / 2, rawSize);
            Populate(mat[(size / 2 * (rawSize + 1))..], size / 2, rawSize);
        }

        static RGB D_To_Col(RGB[] grad, double norm_d)
        {
            return new RGB(Comp(norm_d, grad[0].R, grad[1].R),
                           Comp(norm_d, grad[0].G, grad[1].G),
                           Comp(norm_d, grad[0].B, grad[1].B));

            static byte Comp(double norm, byte snd, byte fst)
            {
                if (fst > snd)
                    norm = 1 - norm;
                return (byte)(Math.Min(snd, fst) + norm * Math.Abs(snd - fst));
            }
        }

        static byte N_Edits(int fst, int snd)
        {
            byte dist = 0;

            while (fst != 0)
            {
                if (fst % 10 != snd % 10)
                    dist++;
                fst /= 10;
                snd /= 10;
            }
            return dist;
        }

        static byte Min_Distance(int px, IEnumerable<int> matches, byte depth)
        {
            byte min = depth;
            foreach(var it in matches)
            {
                var distance = N_Edits(it, px);
                if (distance == 0)
                    return 0;
                else if (distance < min)
                    min = distance;
            }
            return min;
        }

        public Img Apply(string restr, RGB [] grad)
        {
            var re = new Regex(restr, RegexOptions.Compiled);
            string format = $"D{_depth}";
            var d0 = _data.AsParallel()
                          .WithDegreeOfParallelism(4)
                          .Where(val => re.IsMatch(val.ToString(format)))
                          .ToList();
            var res = _data.AsParallel()
                           .AsOrdered()
                           .WithDegreeOfParallelism(4)
                           .Select(it => D_To_Col(grad, Min_Distance(it, d0, _depth) / (double)_depth));
            return new Img(res);
        }
    }

    class Img(IEnumerable<RGB> pxs)
    {
        readonly RGB[] _pxs = pxs.ToArray();

        public void Save(string @out)
        {
            var size = (int) Math.Sqrt(_pxs.Length);
            using var image = new Bitmap(size, size);
            for(int i = 0; i < size; ++i)
            {
                for (int j = 0; j < size; ++j)
                    image.SetPixel(i, j, _pxs[i * size + j].ARGB);
            }
            image.Save(@out, ImageFormat.Png);
        }
    }

    class DistRx
    {
        public static void Main(string[] args)
        {
            if (args == null || args.Length <= 0)
            {
                PrintHelp();
                return;
            }

            var fname = args[0];
            var depth = Convert.ToInt32(args[1]);
            var rx = args[2];

            var grad = new RGB[2]
            {
                new(0x00, 0x00, 0x00),
                new(0xFF, 0xFF, 0xFF)
            };

            if (args.Length == 0)
            {
                grad[0] = new RGB(Convert.ToByte(args[3]), Convert.ToByte(args[4]), Convert.ToByte(args[5]));
                grad[1] = new RGB(Convert.ToByte(args[6]), Convert.ToByte(args[7]), Convert.ToByte(args[8]));
            }

            new Mat(depth).Apply(rx, grad).Save(fname);

            static void PrintHelp()
            {
                Console.Error.WriteLine($$"""
USAGE   {{Environment.ProcessPath}} file depth regx [grad_params]
    depth<unsigned>: recursion depth of the canvas generator, determines the image size (size: 2^depth, 2^10 is 1024)
    regex<string>: self explanatory"
SCOPE: https://ssodelta.wordpress.com/2015/01/26/gradient-images-from-regular-expression
""");
            }
        }
    }
}
