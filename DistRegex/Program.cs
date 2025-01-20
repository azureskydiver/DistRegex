using System.Diagnostics;

namespace DistRegex
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            // DistRegex.Literal.DistRx.Main(["test.png", "8", "2*3?(4+1)+"]);
            DistRegex.Literal.DistRx.Main(["test.png", "8", ".?2*1.3", "8", "143", "143", "236", "255", "220"]);
            stopwatch.Stop();
            Console.WriteLine($"Elapsed time: {stopwatch.Elapsed}");
        }
    }
}
