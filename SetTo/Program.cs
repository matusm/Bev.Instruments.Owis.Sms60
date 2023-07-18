using System;
using System.Globalization;
using Bev.Instruments.Owis.Sms60;

namespace SetTo
{
    class Program
    {
        const double min = 0.1;
        const double max = 50.0;

        static int Main(string[] args)
        {
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

            if (args.Length != 2)
            {
                Console.WriteLine($"You must provide exactly 2 coordinate values (in mm)!");
                return 1;
            }

            double x;
            double y;

            if (!double.TryParse(args[0], out x))
            {
                Console.WriteLine($"'{args[0]}' is not a number!");
                return 2;
            }
            if (!double.TryParse(args[1], out y))
            {
                Console.WriteLine($"'{args[1]}' is not a number!");
                return 3;
            }

            if (x < min || y < min || x > max || y > max)
            {
                Console.WriteLine($"Coordinate values must be in the range [{min} mm, {max} mm]!");
                return 4;
            }

            Console.WriteLine("Connecting instrument ...");
            Sms60 sms = new Sms60("COM1");
            Console.WriteLine("done.");
            Console.WriteLine();

            Console.WriteLine($"Instrument {sms.InstrumentType}, referencing ...");
            sms.MoveToReferenceWait();
            Console.WriteLine("done.");
            Console.WriteLine();

            Console.WriteLine($"Moving to {x} mm / {y} mm ...");
            sms.GoTo(x, y);
            Console.WriteLine("done.");

            return 0; // all fine
        }
    }
}