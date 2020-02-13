using System;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.Pwm;
using CommonHelpers.Common;
using Microsoft.IoT.Lightning.Providers;

namespace Flusher.Uwp.Services.Servo
{
    /// <summary>
    /// Controls a service directly from the PWN pins.
    /// IMPORTANT: The Windows IoT Device must have Direct Memory Mapped Driver enabled or I get a null reference to the PwmPin object.
    /// Open the Windows Device Portal in a web browser, Select 'Devices', change 'Inbox Driver' to 'Direct Memory Mapped Driver'
    /// </summary>
    public class ServoService : BindableBase, IDisposable
    {
        private PwmPin pin;
        private PwmController controller;
        private Timer timer;

        public readonly int PinNumber;
        public readonly int Frequency;
        public readonly int SignalDuration;
        public readonly double MinPulseWidth;
        public readonly double MiddlePulseWidth;
        public readonly double MaxPulseWidth;
        public readonly int MaxAngle;

        private bool autoFollow = true;
        private int desiredAngle;
        private double desiredPulseWidth;
        private bool isInitialized;

        public event EventHandler<ServoChangedEventArgs> ServoChanged;

        public ServoService(int pinNumber, int frequency = 50, double minPulseWidth = 0.6, double maxPulseWidth = 2.4, int maxAngle = 180, int signalDuration = 40)
        {
            this.PinNumber = pinNumber;
            this.Frequency = frequency;
            this.MinPulseWidth = minPulseWidth;
            this.MaxPulseWidth = maxPulseWidth;
            this.MaxAngle = maxAngle;
            this.SignalDuration = signalDuration;
            this.MiddlePulseWidth = (maxPulseWidth - minPulseWidth) / 2 + minPulseWidth;
        }

        public bool IsInitialized
        {
            get => isInitialized;
            private set => SetProperty(ref isInitialized, value);
        }

        /// <summary>
        /// Enables property changed notifications for DesiredAngle and DesiredPulseWidth properties.
        /// </summary>
        public bool AutoFollow
        {
            get => autoFollow;
            set => SetProperty(ref autoFollow, value);
        }

        public int DesiredAngle
        {
            get => desiredAngle;
            set
            {
                if (value < 0 || value > MaxAngle)
                {
                    throw new ArgumentException("The angle of the servo must be between 0 and MaxAngle");
                }

                if (value == 0)
                {
                    desiredPulseWidth = MinPulseWidth;
                }
                else
                {
                    desiredPulseWidth = MinPulseWidth + (MaxPulseWidth - MinPulseWidth) / ((double) MaxAngle / value);
                }

                OnPropertyChanged(nameof(DesiredPulseWidth));

                SetProperty(ref desiredAngle, value);

                if (AutoFollow)
                {
                    MoveServo();
                }
            }
        }

        public double DesiredPulseWidth
        {
            get => desiredPulseWidth;
            set
            {
                if (value < MinPulseWidth || value > MaxPulseWidth)
                {
                    throw new ArgumentException("Pulse width is out of range. It must be between the MinPulseWidth and MaxPulseWidth values");
                }

                if (SetProperty(ref desiredPulseWidth, value))
                {
                    desiredAngle = (int)((value - MinPulseWidth) / (MaxPulseWidth - MinPulseWidth) * MaxAngle);

                    OnPropertyChanged(nameof(DesiredAngle));

                    if (AutoFollow)
                    {
                        MoveServo();
                    }
                }
            }
        }

        public async Task InitializeAsync()
        {
            try
            {
                if (!LightningProvider.IsLightningEnabled)
                {
                    throw new Exception("Servo can only be used with Lightning provider");
                }

                controller = (await PwmController.GetControllersAsync(LightningPwmProvider.GetPwmProvider()))[1];

                pin = controller.OpenPin(PinNumber);
                controller.SetDesiredFrequency(Frequency);

                pin.Start();

                DesiredPulseWidth = MiddlePulseWidth;

                MoveServo();

                IsInitialized = true;
            }
            catch (Exception ex)
            {
                IsInitialized = false;
                throw;
            }
        }

        /// <summary>
        /// Moves the servo using the current values of DesiredPulseWidth (or DesiredAngle).
        /// </summary>
        public void MoveServo()
        {
            var percentage = desiredPulseWidth / (1000.0 / Frequency);
            pin.SetActiveDutyCyclePercentage(percentage);

            ServoChanged?.Invoke(this, new ServoChangedEventArgs
            {
                Angle = this.DesiredAngle,
                PulseWidth = this.DesiredPulseWidth
            });
        }

        /// <summary>
        /// Moves the servo after setting the angle.
        /// </summary>
        /// <param name="angle">Sets the DesiredAngle</param>
        public void MoveServo(int angle)
        {
            this.DesiredAngle = angle;
            MoveServo();
        }

        /// <summary>
        /// Moves the servo after setting the pulse width.
        /// </summary>
        /// <param name="pulseWidth">Sets the DesiredPulseWidth (PWM).</param>
        public void MoveServo(double pulseWidth)
        {
            this.DesiredPulseWidth = pulseWidth;
            MoveServo();
        }

        public void Dispose()
        {
            pin?.Stop();
            pin?.Dispose();
            pin = null;

            timer?.Dispose();
            timer = null;
        }
    }
}
