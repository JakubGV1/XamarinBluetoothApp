using System;
using Android.App;
using Android.OS;
using Android.Widget;
using Android.Bluetooth;
using System.Collections.Generic;
using Android.Content;
using AndroidX.AppCompat.App;
using Android.Util;
using Xamarin.Essentials;
using static XamarinV2.MainActivity;
using System.Threading.Tasks;
using AndroidX.Core.App;
using AndroidX.Core.Content;
using Android;
using Android.Content.PM;
using Java.Util;
using System.IO;
using Plugin.BLE.Android;
using static Android.Bluetooth.BluetoothClass;
using System.Text;
using Java.IO;
using System.Net.Sockets;
using Android.Views;
using System.Linq;
using Android.Companion;
using Android.Bluetooth.LE;

using Java.Lang.Reflect;
using Newtonsoft.Json;
using Android.App.Admin;


namespace XamarinV2 {
    public class CustomBluetooth
    {
        public static readonly UUID GameUUID = UUID.FromString("9b406827-5fd0-4046-99ad-060521820fb6");
        private BluetoothDevice _device;
        private BluetoothSocket _connectedSocket = null;
        private static CustomBluetooth instance;
        private readonly object lockObject = new object();



        public static CustomBluetooth Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new CustomBluetooth();
                }
                return instance;
            }
        }



        public void CloseConnection()
        {
            lock (lockObject)
            {
                try
                {
                    if (_connectedSocket != null)
                    {
                        _connectedSocket.Close();
                        _connectedSocket = null;
                    }
                }
                catch (Java.IO.IOException ex)
                {
                    Log.Error("Socket-closing", ex.Message);
                }
            }
        }

        public void MakeConnection(BluetoothSocket socket, BluetoothDevice device)
        {
            lock (lockObject)
            {
                _connectedSocket = socket;
                _device = device;
            }
        }

        public BluetoothSocket GetConnectedSocket()
        {
            lock (lockObject)
            {
                return _connectedSocket;
            }
        }

        public void Dispose()
        {
            CloseConnection();
        }


    }
}