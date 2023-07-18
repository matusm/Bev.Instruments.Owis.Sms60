using System;
using System.Globalization;
using System.IO;
using System.Threading;
using Bev.Instruments.Owis.Sms60;

namespace SixD
{
    class Program
    {
        const string PORT_SMS60 = "COM1";
        const string PORT_REL = "COM2";
        const int HOLD_TIME = 1_000; // in ms

        static Sms60 sms60;
        static PointCloud targets;

        static void Main(string[] args)
        {
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

            string targetFilename = "SixDtarget.csv";
            string resultFilename = "SixDresult.csv";

            LoadTargetsFromCsv(targetFilename);
            Console.WriteLine($"Number of targets: {targets.NumberOfPoints}");

            sms60 = new Sms60(PORT_SMS60);
            sms60.MoveToReferenceWait();

            Point origin = new Point(0,0);
            for (int i = 0; i < targets.NumberOfPoints; i++)
            {
                Point target = targets.Points[i];
                sms60.GoTo(target.X, target.Y);
                Point position = GetPosition();
                if (i == 0) 
                    origin = new Point(position);
                Signal();
                string csvLine = $"{i},{position.X-origin.X:F5},{position.Y - origin.Y:F5}";
                Console.WriteLine(csvLine);
            }

            sms60.MoveToReferenceWait();

        }

        static void LoadTargetsFromCsv(string filename)
        {
            targets = new PointCloud();
            StreamReader reader = new StreamReader(File.OpenRead(filename));
            while (!reader.EndOfStream)
            {
                var line = reader.ReadLine();
                var tokens = line.Split(',', ';', ' ', '\t');
                if (tokens.Length == 2)
                {
                    double x = MyParse(tokens[0]);
                    double y = MyParse(tokens[1]);
                    if (!double.IsNaN(x) && !double.IsNaN(y))
                    {
                        targets.Add(x, y);
                    }
                }
            }
            reader.Close();
        }

        static double MyParse(string token)
        {
            if (double.TryParse(token, out double value))
                return value;
            else
                return double.NaN;
        }

        static Point GetPosition()
        {
            double x = sms60.GetPosition(Axes.X);
            double y = sms60.GetPosition(Axes.Y);
            return new Point(x, y);
        }

        static void Signal()
        {
            // close contact
            Thread.Sleep(HOLD_TIME);
            // open contact
        }

    }
}
