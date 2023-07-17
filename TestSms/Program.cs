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
            Console.WriteLine();

            PrintPosition();

            sms.MoveAbsoluteWait(20000, 20000);
            PrintPosition();

            sms.MoveAbsoluteWait(20000, 50000);
            PrintPosition();

            sms.MoveAbsoluteWait(20000, 20000);
            PrintPosition();

            sms.MoveToReferenceWait();
            PrintPosition();

            void PrintPosition()
            {
                Console.WriteLine($"X-axis: {sms.GetCounter(Axes.X)} steps  =>  {sms.GetPosition(Axes.X):F4} mm");
                Console.WriteLine($"Y-axis: {sms.GetCounter(Axes.Y)} steps  =>  {sms.GetPosition(Axes.Y):F4} mm");
                Console.WriteLine("---");
            }

        }
    }
}
