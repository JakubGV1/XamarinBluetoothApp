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
using XamarinV2.Enums;
using GameState = XamarinV2.Enums.GameState;

//using static Xamarin.Essentials.Platform;

namespace XamarinV2
{
    [Activity(Label = "Lobby", ScreenOrientation = ScreenOrientation.Portrait)]
    public class DiscoveredDevicesActivity : AppCompatActivity
    {
        private ProgressDialog progressDialog;
        private ListView listView;
        private BluetoothAdapter mBluetoothAdapter;
        private ArrayAdapter<string> mArrayAdapter;
        private BluetoothReceiver mReceiver;
        public static readonly UUID GameUUID = UUID.FromString("9b406827-5fd0-4046-99ad-060521820fb6");
        private BluetoothServerSocket serverSocket;
        private bool isClickable;
        private readonly object lockObject = new object();
        private bool isServerRunning;

        public readonly Dictionary<string, UUID> GamesUUID = new Dictionary<string, UUID>()
        {
                { "Statki", UUID.FromString("9b406827-5fd0-4046-99ad-060521820fb6") },
                { "Quiz", UUID.FromString("d89ccc65-d79b-4cc9-9bcd-d8d516448f60") },
                { "Sekwencje", UUID.FromString("a12bc34d-567e-8f90-abcd-1234567890ef") },
        };

        public string _chosenGame;


        protected override async void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            isServerRunning = false;
            isClickable = true;
            SetContentView(Resource.Layout.content_main);


            string chosenGame = Intent.GetStringExtra("Game");
            if (!string.IsNullOrEmpty(chosenGame))
            {
                _chosenGame = chosenGame;
                
            }
            else
            {
                _chosenGame = "Statki";
            }


            // BluetoothManager bluetoothManager = (BluetoothManager)this.GetSystemService(Context.BluetoothService);
            // mBluetoothAdapter = bluetoothManager.Adapter;
            mBluetoothAdapter = BluetoothAdapter.DefaultAdapter;
            mReceiver = new BluetoothReceiver(this);
            await Permissions.RequestAsync<BLEPermission>();
            PrepareUI();
        }

        private void PrepareUI()
        {
            TextView gameName = FindViewById<TextView>(Resource.Id.gameName);
            gameName.Text = _chosenGame;


            Button button1 = FindViewById<Button>(Resource.Id.btnScan);
            button1.Click += (sender, e) =>
            {
                StartScanning();
            };

            Button buttonHost = FindViewById<Button>(Resource.Id.btnHost);
            buttonHost.Click += (sender, e) =>
            {
                StartHosting();
            };

            Button backButton = FindViewById<Button>(Resource.Id.btnBack);
            backButton.Click += (sender, e) =>
            {
                BackButton();
            };

            listView = FindViewById<ListView>(Resource.Id.deviceListView);

            
             mArrayAdapter = new ArrayAdapter<string>(this, Android.Resource.Layout.SimpleListItem1, mReceiver.GetDeviceList());
            // listView.SetAdapter(mArrayAdapter);
            listView.Adapter = mArrayAdapter;

            listView.ItemClick += async (sender, e) =>
            {
                if (isClickable)
                {
                    string selectedDevice = mArrayAdapter.GetItem(e.Position);
                    isClickable = false;
                    try
                    {
                      
                        HandleDeviceClick(selectedDevice);
                    }
                    finally
                    {
                      
                        await Task.Delay(500); 
                        isClickable = true;
                    }
                }
            };
        }

        private void BackButton()
        {
            Intent intent = new Intent(this, typeof(GameSelectionActivity));
            intent.SetFlags(ActivityFlags.ClearTop);
            StartActivity(intent);
            Finish();
        }

        public UUID GetSelectedGameUUID()
        {
            if (GamesUUID.TryGetValue(_chosenGame, out var selectedUUID))
            {
                return selectedUUID;
            }
            else
            {
                // Handle the case when the _chosenGame is not found
                throw new ArgumentException("Invalid game selection.");
                // or return a default UUID or handle it accordingly based on your requirements
            }
        }

        private void HandleDeviceClick(string selectedDevice)
        {
            string[] deviceInfo = selectedDevice.Split('\n');
            string selectedDeviceName = deviceInfo[0];
            string selectedDeviceAddress = deviceInfo[1];
            BluetoothDevice searchingDevice = null;
            List<BluetoothDevice> scannedDevices = mReceiver.GetScannedDeviceList();

            if (!mBluetoothAdapter.IsEnabled)
            {
                // Bluetooth is not enabled, prompt user to enable it
                Intent enableBtIntent = new Intent(BluetoothAdapter.ActionRequestEnable);
                StartActivityForResult(enableBtIntent, 2); // Use a different request code
                return;
            }

            foreach (BluetoothDevice device in scannedDevices)
            {
                if (device.Name == selectedDeviceName && device.Address == selectedDeviceAddress)
                {
                    searchingDevice = device;
                    
                    break;
                }
            }

            if (searchingDevice != null)
            {
                Task.Run(() => ConnectToDevice(this, searchingDevice));
            }
        }

        private async Task ConnectToDevice(Context context, BluetoothDevice device)
        {
                try
                {
                // BluetoothSocket socket = device.CreateRfcommSocketToServiceRecord(GameUUID);
                BluetoothSocket socket = device.CreateRfcommSocketToServiceRecord(GetSelectedGameUUID());
                ((Activity)context).RunOnUiThread(() =>
                    {
                        Toast.MakeText(context, "Łączenie...", ToastLength.Short).Show();
                    });
                    isClickable = false;
                    await socket.ConnectAsync();

                    if (socket.IsConnected)
                    {
                        CustomBluetooth.Instance.MakeConnection(socket, device);
                        LeadToNewActivity(context, "Client");
                    }
                }
                catch (Java.IO.IOException e)
                {

                    ((Activity)context).RunOnUiThread(() =>
                    {
                        Toast.MakeText(context, "Urządzenie nie nasłuchuje", ToastLength.Short).Show();
                    });
                    isClickable = true;
                }
        }

        private void LeadToNewActivity(Context context, string type)
        {
            if(_chosenGame == "Quiz")
            {
                System.Diagnostics.Debug.WriteLine("TUTAJ");
                var intent = new Intent(context, typeof(QuizGameActivity));
                intent.PutExtra("Connected-Device", type);
                // intent.SetFlags(ActivityFlags.ClearTop | ActivityFlags.NewTask | ActivityFlags.ClearTask);
                context.StartActivity(intent);
            }
            else if(_chosenGame == "Statki")
            {
                var intent = new Intent(context, typeof(GameActivity));
                intent.PutExtra("Connected-Device", type);
                // intent.SetFlags(ActivityFlags.ClearTop | ActivityFlags.NewTask | ActivityFlags.ClearTask);
                context.StartActivity(intent);
            } else if(_chosenGame == "Sekwencje")
            {
                var intent = new Intent(context, typeof(SequenceGameActivity));
                intent.PutExtra("Connected-Device", type);
                // intent.SetFlags(ActivityFlags.ClearTop | ActivityFlags.NewTask | ActivityFlags.ClearTask);
                context.StartActivity(intent);
            }
        }

        private void StartHosting()
        {
            if (isServerRunning)
            {
                // Server is already running
                return;
            }
            isServerRunning = true;
            if (mBluetoothAdapter == null)
            {
                return;
            }
            if (!mBluetoothAdapter.IsEnabled)
            {
                Intent enableBtIntent = new Intent(BluetoothAdapter.ActionRequestEnable);
                StartActivityForResult(enableBtIntent, 1);
                return;
            }


            progressDialog = new ProgressDialog(this);
            progressDialog.SetMessage("Oczekiwanie na gracza...");
            progressDialog.SetCancelable(true);
            progressDialog.SetButton("Anuluj", (sender, args) =>
            {
                isServerRunning = false;

                if (serverSocket != null)
                {
                    try
                    {
                        serverSocket.Close();
                    }
                    catch (Java.IO.IOException e)
                    {
                        
                    }
                }
                progressDialog.Dismiss();
            });

            progressDialog.CancelEvent += (sender, e) =>
            {

                isServerRunning = false;
               
                if (serverSocket != null)
                {
                    try
                    {
                        serverSocket.Close();
                    }
                    catch (Java.IO.IOException f)
                    {
                        
                    }
                }

              //  Toast.MakeText(this, "Przerwano nasłuchiwanie", ToastLength.Short).Show();
            };



            progressDialog.Show();


            Task.Run(() =>
            {
                lock (lockObject)
                {
                    try
                    {
                        // serverSocket = mBluetoothAdapter.ListenUsingRfcommWithServiceRecord("BluetoothGame", GameUUID);
                        serverSocket = mBluetoothAdapter.ListenUsingRfcommWithServiceRecord("BluetoothGame", GetSelectedGameUUID());
                        while (isServerRunning)
                        {
                            BluetoothSocket socket = serverSocket.Accept();

                            if (socket != null)
                            {
                                CustomBluetooth.Instance.MakeConnection(socket, null);
                                if (serverSocket != null)
                                {
                                    serverSocket.Close();
                                }
                                LeadToNewActivity(this, "Host");

                            }
                        }
                    }
                    catch (Java.IO.IOException e)
                    {
                        progressDialog.Dismiss();
                    }
                    finally
                    {
                        if (serverSocket != null)
                        {
                            try
                            {
                                serverSocket.Close();
                            }
                            catch (Java.IO.IOException e)
                            {

                            }
                        }
                        lock (lockObject)
                        {
                            isServerRunning = false;
                        }
                    }
                }
            });
        }

        private void StartScanning()
        {
            if (mBluetoothAdapter == null)
            {
                // Device does not support Bluetooth
                return;
            }

            if (!mBluetoothAdapter.IsEnabled)
            {
                Intent enableBtIntent = new Intent(BluetoothAdapter.ActionRequestEnable);
                StartActivityForResult(enableBtIntent, 1);
                return;
            }

            mArrayAdapter.NotifyDataSetChanged();


            /*Toast.MakeText(this, mBluetoothAdapter.StartDiscovery().ToString(), ToastLength.Short).Show();*/
            if (mBluetoothAdapter.StartDiscovery())
            {
                IntentFilter discoveryFilter = new IntentFilter();
                discoveryFilter.AddAction(BluetoothAdapter.ActionDiscoveryStarted);
                discoveryFilter.AddAction(BluetoothAdapter.ActionDiscoveryFinished);
                discoveryFilter.AddAction(BluetoothDevice.ActionPairingRequest);
                discoveryFilter.AddAction(BluetoothDevice.ActionBondStateChanged);

                progressDialog = new ProgressDialog(this);
                progressDialog.SetMessage("Skanowanie w poszukiwaniu urządzeń...");
                progressDialog.SetCancelable(false);
                progressDialog.Show();
                discoveryFilter.AddAction(BluetoothDevice.ActionFound);
                RegisterReceiver(mReceiver, discoveryFilter);
            }

        }

        public void DiscoveryFinish(Context context)
        {
            if (progressDialog != null && progressDialog.IsShowing)
            {
                progressDialog.Dismiss();
                //   Toast.MakeText(context, mDeviceList.Count.ToString(), ToastLength.Short).Show();
            }
        }
        public void UpdateArrayAdapater()
        {
            mArrayAdapter.NotifyDataSetChanged();
        }

        private void CheckBluetoothPermissions()
        {
            string[] permissions = { Manifest.Permission.Bluetooth, Manifest.Permission.BluetoothAdmin, Manifest.Permission.BluetoothScan, Manifest.Permission.BluetoothConnect, Manifest.Permission.AccessFineLocation };

            foreach (string permission in permissions)
            {
                if (ContextCompat.CheckSelfPermission(this, permission) != Permission.Granted)
                {
                    ActivityCompat.RequestPermissions(this, permissions, 1);
                }
            }
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, Permission[] grantResults)
        {
            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            // Check if the permission is granted
            if (grantResults.Length > 0 && grantResults[0] == Permission.Granted)
            {

            }
            else
            {

            }
        }
    }

}
