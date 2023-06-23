using PolarH10;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;


namespace PolarH10
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            HubConnection connection= new HubConnectionBuilder()
                .WithUrl(new Uri("http://127.0.0.1:5000"))
                .WithAutomaticReconnect()
                .Build();
            //TODO More Connection handling stuff
            
            using IHost host = Host.CreateDefaultBuilder(args).Build();
            var mode = args[0];
            var bluetoothDeviceRequired = (mode == "Record" || mode == "Live");

            var midiSender = new MidiSender("loopMIDI Port");
            var midiConnector = new MidiConnector(midiSender);
            var recordConnecter = new RecordConnector(@"C:\Users\flori\Documents\DanceSensors\PolarH10\Records");

            if (bluetoothDeviceRequired)
            {
                var bluetoothDeviceFinder = new BluetoothDeviceFinder(host.Services.GetRequiredService<ILogger<BluetoothDeviceFinder>>());
                var bluetoothDevice = bluetoothDeviceFinder.GetDevice();
                var bluetoothDeviceSession = new BleDeviceSession(host.Services.GetRequiredService<ILogger<BleDeviceSession>>(), bluetoothDevice);

                PolarH10Bluetooth polarH10Bluetooth;

                if (mode == "Record")
                    polarH10Bluetooth = new PolarH10Bluetooth(host.Services.GetRequiredService<ILogger<PolarH10Bluetooth>>(), bluetoothDeviceSession, recordConnecter);
                else if (mode == "Live")
                    polarH10Bluetooth = new PolarH10Bluetooth(host.Services.GetRequiredService<ILogger<PolarH10Bluetooth>>(), bluetoothDeviceSession, midiConnector);
                else
                    throw new Exception($"Argument '{args[0]}' not supported.");

                polarH10Bluetooth.Start();
            }
            else if (mode == "Replay")
            {
                var replayLogfile = new ReplayRecordfile(host.Services.GetRequiredService<ILogger<ReplayRecordfile>>(), midiConnector);
                replayLogfile.Play(@"C:\Users\flori\Documents\DanceSensors\PolarH10\Records\20230611T1138");
            }
            else
            {
                throw new Exception("Please specify mode 'Record', 'Live' or 'Replay'");
            }

            await host.RunAsync();
        }
    }
}