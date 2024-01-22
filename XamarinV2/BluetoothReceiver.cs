using Android.App;
using Android.Bluetooth;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace XamarinV2
{
    public class BluetoothReceiver : BroadcastReceiver
    {
        private Android.Runtime.JavaList<string> _mDeviceList = new Android.Runtime.JavaList<string>();
        private List<BluetoothDevice> _scannedDevices = new List<BluetoothDevice>();
        private DiscoveredDevicesActivity _activity;

        public BluetoothReceiver(DiscoveredDevicesActivity activity)
        {
            _activity = activity;
        }

        public Android.Runtime.JavaList<string> GetDeviceList()
        {
            return _mDeviceList;
        }

        public List<BluetoothDevice> GetScannedDeviceList()
        {
            return _scannedDevices;
        }

        public void ClearDeviceList()
        {
            _mDeviceList.Clear();
        }

        public override void OnReceive(Context context, Intent intent)
        {
            string action = intent.Action;
            CustomBluetooth customBluetooth = CustomBluetooth.Instance;
            if (BluetoothDevice.ActionFound.Equals(action))
            {

                // Get the BluetoothDevice object from the Intent
                BluetoothDevice device = intent.GetParcelableExtra(BluetoothDevice.ExtraDevice) as BluetoothDevice;


                // Add the device name and address to the list
                string deviceInfo = $"{device.Name}\n{device.Address}";
                //Check if device is in list & check if got an UUID  && CheckForGameService(device)

                if (!_scannedDevices.Contains(device))
                {
                    _scannedDevices.Add(device);
                }
            }

            else if (BluetoothAdapter.ActionDiscoveryFinished.Equals(action))
            {

                foreach (BluetoothDevice device in _scannedDevices)
                {

                    string deviceInfo = $"{device.Name}\n{device.Address}";
                    if (!_mDeviceList.Contains(deviceInfo))
                    {
                        _mDeviceList.Add(deviceInfo);

                        ((Activity)context).RunOnUiThread(() =>
                        {
                            /*mArrayAdapter.NotifyDataSetChanged();*/
                            _activity.UpdateArrayAdapater();
                        });

                    }
                }
                _activity.DiscoveryFinish(context);
            }
            else if (BluetoothDevice.ActionUuid.Equals(action))
            {
                System.Diagnostics.Debug.WriteLine($"Found");
            }
        }
    }
}