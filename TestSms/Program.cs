using System;
using Bev.Instruments.Owis.Sms60;

namespace TestSms
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("initializing instrument");
            Sms60 sms = new Sms60("COM1");
            Console.WriteLine($"Type:         {sms.InstrumentType}");
            Console.WriteLine($"Manufacturer: {sms.InstrumentManufacturer}");
            Console.WriteLine($"Firmware:     {sms.InstrumentFirmwareVersion}");
            Console.WriteLine($"#Axes:        {sms.NumberOfAxis}");
            Console.WriteLine($"X-axis speed: {sms.GetSpeed(Axes.X)}");
            Console.WriteLine($"Y-axis speed: {sms.GetSpeed(Axes.Y)}");
            Console.WriteLine();

            PrintPosition();

            sms.SetSpeed(Axes.X, 2000);
            sms.SetSpeed(Axes.Y, 2000);

            PointCloud pointCloud = new PointCloud();

            pointCloud.Add(25, 25);
            pointCloud.Add(49.5, 49.5);
            for (int i = 0; i < 3; i++)
            {
                double x = 49 - i * 4.1;
                double y = i * 4.1 + 0.5;
                pointCloud.Add(x, y);
            }

            for (int i = 0; i < 3; i++)
            {
                pointCloud.Add(49, 1);
                pointCloud.Add(1, 1);
                pointCloud.Add(1, 49);
                pointCloud.Add(49, 49);
                pointCloud.Add(25, 25);
            }

            pointCloud.Add(0.5, 0.5);




            foreach (var p in pointCloud.Points)
            {
                GoTo(p.X, p.Y);
            }


            sms.MoveToReferenceWait();
            PrintPosition();

            Console.WriteLine("End");


            void GoTo(double x, double y)
            {
                Console.WriteLine($"Goto {x:F3} mm / {y:F3} mm");
                sms.GoTo(x, y);
                PrintPosition();
            }

            void PrintPosition()
            {
                Console.WriteLine($"X-axis: {sms.GetCounter(Axes.X)} steps  =>  {sms.GetPosition(Axes.X):F5} mm");
                Console.WriteLine($"Y-axis: {sms.GetCounter(Axes.Y)} steps  =>  {sms.GetPosition(Axes.Y):F5} mm");
                Console.WriteLine("---");
            }

        }
    }
}
