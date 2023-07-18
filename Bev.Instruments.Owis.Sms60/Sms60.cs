//****************************************************************************************************************
// 
// Library for the communication of OWIS SMS60 stepper motor driver 
// 
// Usage:
// 1.) create instance of the Sms60 class with the COM port name as the only parameter;
// 2.) you can consume properties like serial number, type designation, etc.; 
// 
// 
// Author: Michael Matus, 2023
// 
//****************************************************************************************************************
//
// RS232 cable connection to a PC (recommended):
//
//   9-pin (DC500)	          9-pin (PC)  
//        2		                   3		    
//        3		                   2		    
//        5		                   5		    
//     7 <-> 8                  7 <-> 8
//  1 <-> 4 <-> 6            1 <-> 4 <-> 6
//
// The pins marked "<->" are short-circuits of the handshake signals
// on the same plug and may not be connected to the second plug.
//
//****************************************************************************************************************

using System;
using System.IO.Ports;
using System.Threading;
using System.Text;

namespace Bev.Instruments.Owis.Sms60
{
    public class Sms60
    {
        private readonly SerialPort comPort;
        private const int DELAY_DISPATCH = 500; // in ms, delay after dispatching a command
        private const int DELAY_RESET = 5000;   // in ms, delay after reseting the controller
        private const int MAX_ITERATION = 10000;
        private const int MAX_SPEED = 8191;
        private const int BACKLASH = 1000;
        private const double SCALE_FACTOR = 0.00008; // mm/step

        public Sms60(string portName)
        {
            DevicePort = portName.Trim();
            comPort = new SerialPort(DevicePort, 9600, Parity.None, 8, StopBits.One);
            comPort.Handshake = Handshake.None;
            OpenPort();
            comPort.ReadTimeout = 500;
            comPort.WriteTimeout = 500;
            Initialize();
        }

        public string DevicePort { get; }
        public string InstrumentManufacturer => "OWIS GmbH Staufen";
        public string InstrumentType => "SMS60";
        public string InstrumentFirmwareVersion => GetFirmwareVersion();
        public int NumberOfAxis => Enum.GetValues(typeof(Axes)).Length - 2; // None and All are included, too
        public double XAxisScaleFactor => SCALE_FACTOR;
        public double YAxisScaleFactor => SCALE_FACTOR;

        // Sends a command to the instrument. The command string is build 
        // out of the actual command, an optional axis number and an optional parameter
        // Returns the response of the instrument as a string
        public string SendAndRead(string command, Axes axis, string parameter)
        {
            DispatchCommand(command, (int)axis, parameter);
            return ReadAnswer();
        }
        public string SendAndRead(string command) => SendAndRead(command, Axes.None, "");
        public string SendAndRead(string command, Axes axis) => SendAndRead(command, axis, "");
        public string SendAndRead(string command, string parameter) => SendAndRead(command, Axes.None, parameter);

        public void GoTo(double x, double y)
        {
            int xSteps = (int)(x / XAxisScaleFactor);
            int ySteps = (int)(y / YAxisScaleFactor);
            MoveAbsoluteWait(xSteps, ySteps);
        }

        // make a reference move and resets internal counters to 0
        public void MoveToReferenceWait(Axes axis)
        {
            SendAndRead("REF", axis, "2");
            ReturnOnHalt();
        }

        public void MoveToReferenceWait()
        {
            for (int i = 1; i <= NumberOfAxis; i++)
            {
                MoveToReferenceWait((Axes)i);
            }
        }

        public void MoveRelative(Axes axis, int steps)
        {
            if (steps < 0)
            {
                // TODO check if possible !
                MoveRelativeRaw(axis, steps - BACKLASH);
                ReturnOnHalt();
                MoveRelativeRaw(axis, BACKLASH);
                return;
            }
            MoveRelativeRaw(axis, steps);
        }

        public void MoveRelativeWait(Axes axis, int steps)
        {
            MoveRelative(axis, steps);
            ReturnOnHalt();
        }

        public void MoveAbsolute(Axes axis, int steps)
        {
            if (GetCounter(axis) > steps)
            {
                MoveAbsoluteRaw(axis, steps - BACKLASH);
                ReturnOnHalt();
            }
            MoveAbsoluteRaw(axis, steps);
        }

        public void MoveAbsoluteWait(Axes axis, int steps)
        {
            MoveAbsolute(axis, steps);
            ReturnOnHalt();
        }

        public void MoveAbsolute(int xSteps, int ySteps)
        {
            MoveAbsolute(Axes.X, xSteps);
            MoveAbsolute(Axes.Y, ySteps);
        }

        public void MoveAbsoluteWait(int xSteps, int ySteps)
        {
            MoveAbsolute(xSteps, ySteps);
            ReturnOnHalt();
        }

        public int GetCounter(Axes axis) => GetIntegerParameter("?CNT", axis);

        public double GetPosition(Axes axis)
        {
            switch (axis)
            {
                case Axes.X:
                    return GetCounter(axis) * XAxisScaleFactor;
                case Axes.Y:
                    return GetCounter(axis) * YAxisScaleFactor;
                default:
                    return double.NaN;
            }
        }

        public void SetSpeed(Axes axis, int speed)
        {
            if (speed <= 0) speed = 1;
            if (speed > MAX_SPEED) speed = MAX_SPEED;
            SendAndRead("VEL", axis, speed.ToString());
        }

        public int GetSpeed(Axes axis) => GetIntegerParameter("?VEL", axis);

        public void ReturnOnHalt()
        {
            int i = 0;
            while (AxisIsMoving())
            {
                i++;
                if (i > MAX_ITERATION)
                    return;
            }
        }

        // returns true if any axis is moving
        // returns false on complete standstill
        private bool AxisIsMoving()
        {
            string str = SendAndRead("?ST");
            if (str.Contains("MOTION=0") && str.Contains("REF=0")) return false;
            return true;
        }

        private string ReadAnswer()
        {
            try
            {
                byte[] buffer = new byte[comPort.ReadBufferSize];
                int bytesread = comPort.Read(buffer, 0, buffer.Length);
                return Encoding.ASCII.GetString(buffer, 0, bytesread);
            }
            catch
            {
                return string.Empty;
            }
        }

        private void DispatchCommand(string command, int axis, string parameter)
        {
            if (axis < 0) return;
            if (axis > NumberOfAxis) return;
            string sParameter = "";
            if (parameter != "") sParameter = $"={parameter}";
            comPort.WriteLine($"{command}{axis}{sParameter}{(char)13}");
            Thread.Sleep(DELAY_DISPATCH);
        }
        private void DispatchCommand(string command) => DispatchCommand(command, 0, "");
        private void DispatchCommand(string command, int axis) => DispatchCommand(command, axis, "");

        private void MoveAbsoluteRaw(Axes axis, int steps)
        {
            DispatchCommand("MOD", (int)axis, "1");
            DispatchCommand("SET", (int)axis, steps.ToString());
            DispatchCommand("GO", (int)axis);
        }

        private void MoveRelativeRaw(Axes axis, int steps)
        {
            DispatchCommand("MOD", (int)axis, "0");
            DispatchCommand("SET", (int)axis, steps.ToString());
            DispatchCommand("GO", (int)axis);
        }

        private int GetIntegerParameter(string command, Axes axis)
        {
            int nResult;
            string str;
            str = SendAndRead(command, axis);
            if (!int.TryParse(str, out nResult)) // repeat once
            {
                str = SendAndRead(command, axis);
                if (!int.TryParse(str, out nResult)) Console.WriteLine("parse error in GetIntegerParameter for command {0} -> {1}", command, str);
            }
            return nResult;
        }

        private void OpenPort()
        {
            try
            {
                if (!comPort.IsOpen)
                    comPort.Open();
            }
            catch (Exception)
            { }
        }

        private string GetFirmwareVersion()
        {
            string str = SendAndRead("?VD");
            str = str.Remove(0, Math.Min(1, str.Length));
            if (str.Length > 1)
                return str;
            return string.Empty;
        }

        private void Initialize()
        {
            DispatchCommand("RST");
            Thread.Sleep(DELAY_RESET);
            DispatchCommand("TERM=1");  // set terminal mode
        }

    }
}
