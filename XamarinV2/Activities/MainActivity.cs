using System;
using Android.App;
using Android.OS;
using Android.Runtime;
using Android.Views;
using AndroidX.AppCompat.Widget;
using AndroidX.AppCompat.App;
using Google.Android.Material.FloatingActionButton;
using Google.Android.Material.Snackbar;
using System.Collections.Generic;
using Xamarin.Essentials;
using Android.Bluetooth;
using Android.Content;
using Android.Widget;


namespace XamarinV2
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme.NoActionBar", MainLauncher = true)]
    public class MainActivity : AppCompatActivity
    {

        private BluetoothAdapter _bluetoothAdapter;

        protected override async void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            SetContentView(Resource.Layout.activity_main);

            await Permissions.RequestAsync<BLEPermission>();
            Button button1 = FindViewById<Button>(Resource.Id.btnDiscoverDevices);
            button1.Click += (sender, e) => {
                OnDiscoverDevicesClick();
            };
           // Button button2 = FindViewById<Button>(Resource.Id.)
        }


        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults)
        {
            Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }


        private void SetupBluetooth()
        {
            // Get the Bluetooth adapter
            // _bluetoothAdapter = BluetoothAdapter.DefaultAdapter;
            _bluetoothAdapter = BluetoothAdapter.DefaultAdapter;
            // Check if the device supports Bluetooth
            if (_bluetoothAdapter == null)
            {
                // Device does not support Bluetooth
                // Handle accordingly (show a message, disable Bluetooth features, etc.)
                return;
            }

            // Check if Bluetooth is enabled
            if (!_bluetoothAdapter.IsEnabled)
            {
                return;
            }
        }

        public void OnDiscoverDevicesClick()
        {
           if (_bluetoothAdapter == null)
            {
                SetupBluetooth();
            }
            var intent = new Intent(this, typeof(GameSelectionActivity));
            
            StartActivity(intent);

        }

        public class BLEPermission : Xamarin.Essentials.Permissions.BasePlatformPermission
        {
            public override (string androidPermission, bool isRuntime)[] RequiredPermissions => new List<(string androidPermission, bool isRuntime)>
{
                (Android.Manifest.Permission.BluetoothScan, true),
                (Android.Manifest.Permission.BluetoothConnect, true),
                (Android.Manifest.Permission.AccessFineLocation, true),
            }.ToArray();
        }

    }
}
