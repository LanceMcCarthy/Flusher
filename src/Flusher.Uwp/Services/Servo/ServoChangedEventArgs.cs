using System;

namespace Flusher.Uwp.Services.Servo
{
    public class ServoChangedEventArgs : EventArgs
    {
        public int Angle { get; set; }
        public double PulseWidth { get; set; }
    }
}
