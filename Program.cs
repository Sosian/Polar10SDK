using PolarH10;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using IHost host = Host.CreateDefaultBuilder(args).Build();

var bluetoothDeviceFinder = new BluetoothDeviceFinder(host.Services.GetRequiredService<ILogger<BluetoothDeviceFinder>>());
var midiSender = new MidiSender("loopMIDI Port");
var midiConnector = new MidiConnector(midiSender);

var device = bluetoothDeviceFinder.GetDevice();
var polarH10Bluetooth = new PolarH10Bluetooth(host.Services.GetRequiredService<ILogger<PolarH10Bluetooth>>(), new BleDeviceSession(host.Services.GetRequiredService<ILogger<BleDeviceSession>>(), device), midiConnector);
polarH10Bluetooth.Start();

await host.RunAsync();