

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace RemoteTemperature
{
    using System;

    using Windows.UI.Xaml;
    using Windows.UI.Xaml.Controls;

    using Microsoft.Maker.Serial;
    using Microsoft.Maker.RemoteWiring;
    using System.Collections.ObjectModel;

    using Windows.UI.Core;

    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        readonly RemoteDevice arduino;


        private ObservableCollection<string> data = new ObservableCollection<string>();

        private readonly TemperaturStorage tempStorage;

        public MainPage()
        {
            this.InitializeComponent();
            var usbConnection = new UsbSerial("VID_2341", "PID_0043");
            usbConnection.ConnectionLost +=
                async message => await this.Dispatcher.RunAsync(
                    CoreDispatcherPriority.Normal,
                    () =>
                        {
                            this.tbConnected.Text = "Connection Lost";
                        });
            usbConnection.ConnectionFailed += async m => await this.Dispatcher.RunAsync(
                CoreDispatcherPriority.Normal,
                () =>
                    {
                        this.tbConnected.Text = $"Connection FAILED. Message: {m}";
                    });
            usbConnection.ConnectionEstablished += async () => await this.Dispatcher.RunAsync(
                CoreDispatcherPriority.Normal,
                () =>
                {
                    this.tbConnected.Text = $"Connection ESTABLISHED.";
                });

            this.tempStorage = new TemperaturStorage();
            this.tempStorage.TempCalculated += async (sender, temperature) =>
            {
                // Need to run via dispatcher since we're updateing the GUI
                await this.Dispatcher.RunAsync(
                    CoreDispatcherPriority.Normal,
                    () =>
                    {
                        this.tbTemperatur.Text = string.Format("{0:N2} °C", temperature);
                    });
            };

            this.arduino = new RemoteDevice(usbConnection);
            this.arduino.DeviceReady += this.DeviceReady;
            usbConnection.begin(57600, SerialConfig.SERIAL_8N1);

           
        } 

        private async void DeviceReady()
        {
            await this.Dispatcher.RunAsync(
                CoreDispatcherPriority.Normal, () =>
                    {
                        this.btOn.IsEnabled = true;
                        this.btOff.IsEnabled = true;

                        this.arduino.pinMode("A0", PinMode.ANALOG);
                        this.arduino.AnalogPinUpdated += (pin, value) =>
                            {
                                var voltage = value * 0.004882814;
                                var degreesC = (voltage - 0.5) * 100.0;
                                this.tempStorage.Temperature = degreesC;
                            };
                    });
        }

        private void BtOn_OnClick(object sender, RoutedEventArgs e)
        {
            this.arduino.digitalWrite(8, PinState.HIGH);
        }

        private void BtOff_OnClick(object sender, RoutedEventArgs e)
        {
            this.arduino.digitalWrite(8, PinState.LOW);
        }
    }


    public class TemperaturStorage 
    {

        private double temp;

        public int cnt;

        public const int NumberOfEntries = 500;
        public double Temperature {
            private get
            {
                return this.temp;
            }
            set
            {
                if (this.cnt >= NumberOfEntries)
                {
                    this.cnt = 0;

                    // Middle
                    this.temp = this.temp / NumberOfEntries;

                    this.TempCalculated?.Invoke(this, this.temp);

                    this.temp = 0;
                }
                
                this.temp += value;
                this.cnt++;
            }
        }

      

        public event EventHandler<double> TempCalculated;
    }
}
