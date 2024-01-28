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
using XamarinV2.DTO;
//using static Xamarin.Essentials.Platform;

namespace XamarinV2
{
    [Activity(Label = "Discovered Devices", ScreenOrientation = ScreenOrientation.Portrait)]
    public class DiscoveredDevices2 : AppCompatActivity
    {
        private static ProgressDialog progressDialog;
        private ListView listView;
        private BluetoothAdapter mBluetoothAdapter;
        private static ArrayAdapter<string> mArrayAdapter;
        private static BluetoothReceiver mReceiver;
        public static readonly UUID GameUUID = UUID.FromString("9b406827-5fd0-4046-99ad-060521820fb6");
        private BluetoothServerSocket serverSocket;
        private static BluetoothSocket _connectedSocket = null;
        private static Button sendButton;
        private static Button hostButton;
        private static Button scanButton;


        protected override async void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.content_main);
            mBluetoothAdapter = BluetoothAdapter.DefaultAdapter;
            mReceiver = new BluetoothReceiver(this);
            CheckBluetoothPermissions();
            await Permissions.RequestAsync<BLEPermission>();
            PrepareUI();
        }




        private void PrepareUI()
        {
            Button button1 = FindViewById<Button>(Resource.Id.btnScan);
            button1.Click += (sender, e) => {
                StartScanning();
            };
            scanButton = button1;

            Button buttonHost = FindViewById<Button>(Resource.Id.btnHost);
            buttonHost.Click += (sender, e) =>
            {
                StartHosting();
            };
            hostButton = buttonHost;

            listView = FindViewById<ListView>(Resource.Id.deviceListView);

            mArrayAdapter = new ArrayAdapter<string>(this, Android.Resource.Layout.SimpleListItem1, mReceiver.GetDeviceList());
            listView.SetAdapter(mArrayAdapter);

            listView.ItemClick += (sender, e) =>
            {
                // Get the selected device
                string selectedDevice = mArrayAdapter.GetItem(e.Position);

                // Handle the click event for the selected device
                HandleDeviceClick(selectedDevice);

            };
        }



        void HandleDeviceClick(string selectedDevice)
        {
            string[] deviceInfo = selectedDevice.Split('\n');
            string selectedDeviceName = deviceInfo[0];
            string selectedDeviceAddress = deviceInfo[1];
            BluetoothDevice searchingDevice = null;
            List<BluetoothDevice> scannedDevices = mReceiver.GetScannedDeviceList();


            foreach (BluetoothDevice device in scannedDevices)
            {
                if (device.Name == selectedDeviceName && device.Address == selectedDeviceAddress)
                {
                    searchingDevice = device;
                    //Toast.MakeText(this, searchingDevice.Name, ToastLength.Short).Show();
                    break;
                }
            }

            // Check if the selected device was found
            if (searchingDevice != null)
            {
                // Handle the click event for the selected device
                // For example, initiate pairing or connect to the device
                PairDevice(searchingDevice);
            }
            else
            {
                // Device not found, handle the situation accordingly
            }
        }


        private void PairDevice(BluetoothDevice device)
        {
            if (device != null)
            {
                // Ensure Bluetooth is enabled
                if (!mBluetoothAdapter.IsEnabled)
                {
                    // Bluetooth is not enabled, prompt user to enable it
                    Intent enableBtIntent = new Intent(BluetoothAdapter.ActionRequestEnable);
                    StartActivityForResult(enableBtIntent, 2); // Use a different request code
                    return;
                }
            }

            //Check if is bonded??
            /* if (device.BondState == Bond.Bonded)
             {
                 Method m = device.Class.GetMethod("removeBond", null);
                 m.Invoke(device, null);
             } else
             {
                 //device.CreateBond();
             }*/
            ConnectToDevice(this, device);
        }

        private static void HideUIElements(Context context)
        {
            ((Activity)context).RunOnUiThread(() =>
            {
                hostButton.Visibility = ViewStates.Gone;
                scanButton.Visibility = ViewStates.Gone;
            });
        }

        private static void ShowUIElementsAfterDisconnect(Context context)
        {
            ((Activity)context).RunOnUiThread(() =>
            {
                hostButton.Visibility = ViewStates.Visible;
                scanButton.Visibility = ViewStates.Visible;
                sendButton.Visibility = ViewStates.Gone;
            });
        }


        private static async Task ConnectToDevice(Context context, BluetoothDevice device)
        {


            await Task.Run(() =>
            {
                try
                {



                    // Toast.MakeText(context, "Connected?", ToastLength.Short).Show();
                    BluetoothSocket socket = device.CreateRfcommSocketToServiceRecord(GameUUID);
                    ((Activity)context).RunOnUiThread(() =>
                    {
                        // Update UI elements or perform UI-related operations here
                        Toast.MakeText(context, "Connecting...", ToastLength.Short).Show();
                    });
                    socket.Connect();

                    if (socket.IsConnected)
                    {
                        // sendButton.Visibility = ViewStates.Visible;
                        //_connectedSocket = socket;
                        CustomBluetooth.Instance.MakeConnection(socket, device);
                        /* ((Activity)context).RunOnUiThread(() =>
                         {
                             // Update UI elements or perform UI-related operations here
                             *//* Toast.MakeText(context, "Connected!", ToastLength.Short).Show();
                           //   sendButton.Visibility = ViewStates.Visible;
                              mReceiver.ClearDeviceList();
                              mArrayAdapter.NotifyDataSetChanged();*//*
                             var intent = new Intent(context, typeof(GameActivity));
                             intent.PutExtra("Connected-Device", "discovered");
                             context.StartActivity(intent);
                         });*/
                        //HideUIElements(context);
                        LeadToNewActivity(context, "Client");


                        //HandleSocketInput(context, socket);
                    }
                    // Continue with socket operations or UI updates
                }
                catch (Java.IO.IOException e)
                {

                    ((Activity)context).RunOnUiThread(() =>
                    {
                        // Update UI elements or perform UI-related operations here
                        Toast.MakeText(context, "Device is not listining", ToastLength.Short).Show();
                    });
                    // Handle the exception appropriately
                }
            });
        }

        private static void LeadToNewActivity(Context context, string type)
        {
            var intent = new Intent(context, typeof(GameActivity));
            intent.PutExtra("Connected-Device", type);
            context.StartActivity(intent);
        }

        private static void HandleSocketInput(Context context, BluetoothSocket socket)
        {
            bool isConnection = true;
            try
            {
                // Get the InputStream from the socket for reading data
                Stream inputStream = socket.InputStream;
                byte[] buffer = new byte[1024]; // Adjust the buffer size as needed

                while (isConnection)
                {
                    int bytesRead = inputStream.Read(buffer, 0, buffer.Length);
                    // Handle the read data as needed
                    if (bytesRead > 0)
                    {
                        string receivedData = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                        var receivedObject = JsonConvert.DeserializeObject<GameData>(receivedData);
                        // Process the received data or update UI as needed

                        ((Activity)context).RunOnUiThread(() =>
                        {
                            Toast.MakeText(context, $"Msg->P_State: {receivedObject.PlayerState} ->G_sState: {receivedObject.State}", ToastLength.Short).Show();
                        });
                    }
                }
            }
            catch (Java.IO.IOException e)
            {
                //Log.Error("SocketError", "Error handling socket input: " + e.Message);

                ((Activity)context).RunOnUiThread(() =>
                {
                    Toast.MakeText(context, "Socket disconnected", ToastLength.Short).Show();

                });
                ShowUIElementsAfterDisconnect(context);
                isConnection = false;
                // Handle the exception appropriately
            }
            finally // new->cleanup?
            {
                // Cleanup actions (e.g., close the socket or release resources)
                if (socket != null && socket.IsConnected)
                {
                    try
                    {
                        socket.Close();
                    }
                    catch (Java.IO.IOException e)
                    {
                        Log.Error("SocketError", "Error closing socket: " + e.Message);
                        // Handle the exception appropriately
                    }
                }
            }
        }


        //  Toast.MakeText(this, "PariDevice:" + device.Name, ToastLength.Short).Show();
        private void StartHosting()
        {
            if (mBluetoothAdapter == null)
            {
                // Device does not support Bluetooth
                return;
            }
            if (!mBluetoothAdapter.IsEnabled)
            {
                // Bluetooth is not enabled, prompt user to enable it
                Intent enableBtIntent = new Intent(BluetoothAdapter.ActionRequestEnable);
                StartActivityForResult(enableBtIntent, 1);
                return;
            }


            progressDialog = new ProgressDialog(this);
            progressDialog.SetMessage("Waiting for oponnet...");
            progressDialog.SetCancelable(true);
            progressDialog.SetButton("Cancel", (sender, args) =>
            {
                // User pressed cancel, close the server socket
                if (serverSocket != null)
                {
                    try
                    {
                        serverSocket.Close();
                    }
                    catch (Java.IO.IOException e)
                    {
                        //   e.PrintStackTrace();
                    }
                }
                progressDialog.Dismiss();
                Toast.MakeText(this, "Cancel", ToastLength.Short).Show();
            });

            progressDialog.CancelEvent += (sender, e) =>
            {
                // User clicked outside the dialog, close the server socket
                if (serverSocket != null)
                {
                    try
                    {
                        serverSocket.Close();
                    }
                    catch (Java.IO.IOException ex)
                    {
                        //  e.PrintStackTrace();
                    }
                }
                Toast.MakeText(this, "Userclicked Outisde", ToastLength.Short).Show();
            };



            progressDialog.Show();




            Task.Run(() =>
            {
                try
                {


                    serverSocket = mBluetoothAdapter.ListenUsingRfcommWithServiceRecord("BluetoothGame", GameUUID);

                    while (true)
                    {
                        // Check for incoming connection requests
                        BluetoothSocket socket = serverSocket.Accept();

                        if (socket != null)
                        {
                            CustomBluetooth.Instance.MakeConnection(socket, null);
                            if (serverSocket != null)
                            {
                                serverSocket.Close();
                            }
                            // _connectedSocket = socket;
                            LeadToNewActivity(this, "Host");
                            /* RunOnUiThread(() =>
                             {
                                 Toast.MakeText(this, "Device connected", ToastLength.Short).Show();
                                 // Handle the accepted socket on the UI thread as needed
                                 sendButton.Visibility = ViewStates.Visible;
                                 progressDialog.Dismiss();
                                 mReceiver.ClearDeviceList();
                                 mArrayAdapter.NotifyDataSetChanged();
                             });
                             HideUIElements(this);
                             HandleSocketInput(this, socket);*/
                        }
                    }
                }
                catch (Java.IO.IOException e)
                {
                    Log.Error("432", "Error-1: " + e.Message);
                    progressDialog.Dismiss();
                }
                finally
                {
                    // Close the server socket when done
                    if (serverSocket != null)
                    {
                        try
                        {
                            serverSocket.Close();
                        }
                        catch (Java.IO.IOException e)
                        {
                            Log.Error("4213", "Error-2: " + e.Message);
                            // Handle socket close exception if needed
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
                // Bluetooth is not enabled, prompt user to enable it
                Intent enableBtIntent = new Intent(BluetoothAdapter.ActionRequestEnable);
                StartActivityForResult(enableBtIntent, 1);
                return;
            }

            // Clear existing device list

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
                progressDialog.SetMessage("Scanning for devices...");
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
                    // Permission is not granted, request it
                    Toast.MakeText(this, "NotGranted?" + permission, ToastLength.Short).Show();
                    ActivityCompat.RequestPermissions(this, permissions, 1);
                }
                else
                {
                    //   Toast.MakeText(this, "granted?" + permission, ToastLength.Short).Show();
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
