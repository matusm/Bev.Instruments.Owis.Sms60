using System;
using System.Globalization;
using System.IO;
using System.Threading;
using Bev.Instruments.Owis.Sms60;
using Bev.Instruments.Conrad.Relais;
using Bev.UI;

namespace SixD
{
    class Program
    {
        const string PORT_SMS60 = "COM1";
        const string PORT_REL = "COM3";
        const int SIGNAL_DURATION = 1_000;  // in ms
        const int SIGNAL_PULSE = 100;       // in ms
        const int CHANNEL = 1;              // relay number
        const int SPEED = 1000;             // speed of stepper motors
        const bool RELATIVE = true;         // coordinates relative to first point

        static Sms60 sms60;
        static ConradRelais relay;
        static PointCloud targets;

        static void Main(string[] args)
        {
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

            ConsoleUI.Welcome();

            #region Input section

            DateTime startDate = DateTime.UtcNow;

            string targetFilename = "SixDtarget.csv";
            string resultFilename = $"SixD_{startDate.ToString("yyyyMMddHHmm")}.csv";

            if (args.Length == 1)
            {
                targetFilename = args[0];
            }

            if (args.Length == 2)
            {
                targetFilename = args[0];
                resultFilename = args[1];
            }

            LoadTargetsFromCsv(targetFilename);

            if (targets.NumberOfPoints == 0)
            {
                ConsoleUI.ErrorExit($"No targets in {targetFilename}!", 1);
            }

            ConsoleUI.WriteLine($"Number of targets: {targets.NumberOfPoints}");
            #endregion

            ConsoleUI.StartOperation("Initializing hardware");
                relay = new ConradRelais(PORT_REL);
                sms60 = new Sms60(PORT_SMS60);
                sms60.MoveToReferenceWait();
                sms60.SetSpeed(Axes.X, SPEED);
                sms60.SetSpeed(Axes.Y, SPEED);
                StreamWriter hOutfile = File.CreateText(resultFilename);
            ConsoleUI.Done();

            Point origin = new Point(0, 0);
            for (int i = 0; i < targets.NumberOfPoints; i++)
            {
                Point target = targets.Points[i];
                ConsoleUI.StartOperation($"Moving to point ({target.X} , {target.Y})");
                    sms60.GoTo(target.X, target.Y);
                ConsoleUI.Done();
                Point position = GetPosition();
                if (i == 0 && RELATIVE) origin = new Point(position);
                Signal();
                string csvLine = $"{i},{DateTime.UtcNow.ToString("o")},{position.X - origin.X:F5},{position.Y - origin.Y:F5}";
                hOutfile.WriteLine(csvLine);
                hOutfile.Flush();
            }
            
            hOutfile.Close();

            ConsoleUI.StartOperation("Perform referencing");
                sms60.GoTo(1, 1); // to speed things up
                sms60.MoveToReferenceWait();
            ConsoleUI.Done();
            ConsoleUI.WriteLine($"Data written to file {resultFilename}");
        }

        static void LoadTargetsFromCsv(string filename)
        {
            targets = new PointCloud();
            StreamReader reader = new StreamReader(File.OpenRead(filename));
            ConsoleUI.ReadingFile(filename);
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
            ConsoleUI.Done();
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
            ConsoleUI.StartOperation("Send signal to remote sensor");
            relay.On(CHANNEL);
            Thread.Sleep(SIGNAL_PULSE);
            relay.Off(CHANNEL);
            Thread.Sleep(SIGNAL_DURATION);
            ConsoleUI.Done();
        }

    }
}
