using Android.App;
using Android.Bluetooth;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Java.Interop;
using Java.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace XamarinV2.Entities
{
    [Serializable]
    public class Player
    {
        public bool isHost;
        public BluetoothSocket _connectedSocket;
        public Player(bool isHost, BluetoothSocket connectedSocket)
        {
            this.isHost = isHost;
            _connectedSocket = connectedSocket;
        }

    }
}