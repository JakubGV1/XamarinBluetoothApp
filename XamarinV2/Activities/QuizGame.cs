using Android.App;
using Android.Bluetooth;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Views.InputMethods;
using Android.Widget;
using AndroidX.Activity;
using AndroidX.Annotations;
using AndroidX.AppCompat.App;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XamarinV2.DTO;
using XamarinV2.Entities;
using XamarinV2.Enums;
using GameState = XamarinV2.Enums.GameState;
using System.Diagnostics;
using Java.IO;
using Java.Net;

namespace XamarinV2
{
    [Activity(Label = "Quizy")]
    public class QuizGame: AppCompatActivity
    {

        private BluetoothSocket _connectedSocket;
        bool isConnection;
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.quiz_game);

          
            string connectedDevice = Intent.GetStringExtra("Connected-Device");
            if (CustomBluetooth.Instance.GetConnectedSocket() != null && connectedDevice != null)
            {
               // playerConnectInfo = connectedDevice;
                if (connectedDevice == "Host")
                {
                 /* //  playerState = PlayerState.Turn;
                    RunOnUiThread(() =>
                    {

                        ShowCategoryMenu();
                    });*/
                }
                else
                {
/*                    playerState = PlayerState.Waiting;
                    RunOnUiThread(() =>
                    {

                        WaitingForCategory();
                    });*/
                }
                _connectedSocket = CustomBluetooth.Instance.GetConnectedSocket();
                if (_connectedSocket != null && _connectedSocket.IsConnected)
                {
                    isConnection = true;
                    Task.Run(() => HandleSocketInput(this, _connectedSocket));
                    //Task.Run(() => HandleSocketInput2(this, _connectedSocket));
                   // Toast.MakeText(this, "Nawiązano połączenie", ToastLength.Short).Show();
                }
            }
        }


        private void HandleSocketInput(Context context, BluetoothSocket socket)
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
                        var receivedObject = JsonConvert.DeserializeObject<GameActionDTO>(receivedData);
                        // Process the received data or update UI as needed
                        Array.Clear(buffer, 0, buffer.Length);
                        if (receivedObject != null)
                        {

                        }

                        /*
                                                    ((Activity)context).RunOnUiThread(() =>
                                                    {
                                                        Toast.MakeText(context, $"Msg->P_State: {receivedObject.PlayerState} ->G_sState: {receivedObject.State}", ToastLength.Short).Show();
                                                    });*/
                    }
                }
            }
            catch (Java.IO.IOException e)
            {
                // Log.Error("SocketError", "Error handling socket input: " + e.Message);
                CloseBluetoothSocket();
                isConnection = false;
                ((Activity)context).RunOnUiThread(() =>
                {
                    Toast.MakeText(context, "Nastąpiło rozłączenie", ToastLength.Short).Show();
                    Intent intent = new Intent(context, typeof(DiscoveredDevicesActivity));

                    // Set the flags to clear the activity stack
                    intent.SetFlags(ActivityFlags.ClearTop);
                    // Start PreviousActivity
                    intent.PutExtra("Game", "Statki");
                    context.StartActivity(intent);
                    this.Finish();
                });
                //  ShowUIElementsAfterDisconnect(context);
            }
            /*finally // new->cleanup?
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
            }*/
        }

        private void CloseBluetoothSocket()
        {
            try
            {
                if (_connectedSocket != null)
                {
                    // Close the Bluetooth socket
                    _connectedSocket.Close();
                    // _connectedSocket.Dispose();
                    _connectedSocket = null;
                    CustomBluetooth.Instance.CloseConnection();
                }
            }
            catch (Exception ex)
            {
                // Handle exceptions, e.g., log or display an error message
                Log.Error("Socket Error", "Error closing socket: " + ex.Message);
            }
        }

        public override void OnBackPressed()
        {
            // Show a confirmation dialog
            ShowConfirmationDialog();
        }


        private void ShowConfirmationDialog()
        {
            AndroidX.AppCompat.App.AlertDialog.Builder alertDialogBuilder = new AndroidX.AppCompat.App.AlertDialog.Builder(this);
            alertDialogBuilder.SetMessage("Czy chcesz wyjść?");
            alertDialogBuilder.SetCancelable(false);

            alertDialogBuilder.SetPositiveButton("Tak", (senderAlert, args) =>
            {
                // Close the Bluetooth socket or handle other cleanup actions
                //CloseBluetoothSocket(); // Adjust the method name based on your requirements

                // Finish the activity
                CloseBluetoothSocket();

                Intent intent = new Intent(this, typeof(DiscoveredDevicesActivity));

                // Set the flags to clear the activity stack
                intent.SetFlags(ActivityFlags.ClearTop);
                intent.PutExtra("Game", "Quiz");
                // Start PreviousActivity
                StartActivity(intent);

                Finish();
            });

            alertDialogBuilder.SetNegativeButton("Nie", (senderAlert, args) =>
            {
                // If the user cancels, do nothing (or handle as needed)
            });

            // Create and show the dialog
            AndroidX.AppCompat.App.AlertDialog alertDialog = alertDialogBuilder.Create();
            alertDialog.Show();
        }

    }
}