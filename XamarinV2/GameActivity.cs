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
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using XamarinV2.DTO;
using XamarinV2.Enums;
using GameState = XamarinV2.Enums.GameState;


namespace XamarinV2
{
    [Activity(Label = "BattleShips")]
    public class GameActivity : AppCompatActivity
    {
       
        private float dX, dY;
        static TextView[,] textViews = new TextView[5, 5];
        static TextView[,] myTextViews = new TextView[5, 5]; 
        private ImageView ship1;
        private ImageView ship2;
        private ImageView ship3;
        private static TextView _turnText;
        private static BluetoothSocket _connectedSocket;
        private static GameActivity _activity;
        private static int[,] ShipLocation;
        // turn?
        private static PlayerState playerState;
        private static bool isActionPerformed;
        private static readonly Random random = new Random();


        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.content_game);

                _activity = this;

                TableLayout opponentTableLayout = FindViewById<TableLayout>(Resource.Id.OpponentTableLayout);
                TableLayout myTableLayout = FindViewById<TableLayout>(Resource.Id.OwnTableLayout);
            
            
            
            ShipLocation = new int[5, 5];
            RandomShipPlacement();
            isActionPerformed = false;

            for (int i=0; i < 5; i++)
            {
                TableRow tableRow = new TableRow(this);
                for (int j = 0; j < 5; j++)
                {
                    TextView textView = new TextView(this);
                    /*textView.Text = $"Cell {i * 6 + j + 1}";*/
                    textView.Gravity = GravityFlags.Center;
                    if (ShipLocation[i, j] == 1)
                    {
                        textView.SetBackgroundResource(Resource.Drawable.cell_border_ownship);
                    } else
                    {
                        textView.SetBackgroundResource(Resource.Drawable.cell_border);
                    }

                    // Set padding to add spacing between cells
                    int padding = 5; // Set your desired padding
                    textView.SetPadding(padding, padding, padding, padding);

                    TableRow.LayoutParams layoutParams = new TableRow.LayoutParams();
                    layoutParams.Width = 100; // Set your desired width
                    layoutParams.Height = 100; // Set your desired height
                    layoutParams.SetMargins(padding, padding, padding, padding);
                    textView.LayoutParameters = layoutParams;

                    myTextViews[i, j] = textView;
                    tableRow.AddView(textView);
                }

                myTableLayout.AddView(tableRow);
            }




            for (int i = 0; i < 5; i++)
            {
                TableRow tableRow = new TableRow(this);

                for (int j = 0; j < 5; j++)
                {
                    TextView textView = new TextView(this);
                   // textView.Text = $"Cell {i * 6 + j + 1}";
                    textView.Gravity = GravityFlags.Center;

                    // Set the background drawable for the border
                    textView.SetBackgroundResource(Resource.Drawable.cell_border);

                    // Set padding to add spacing between cells
                    int padding = 5; // Set your desired padding
                    textView.SetPadding(padding, padding, padding, padding);

                    TableRow.LayoutParams layoutParams = new TableRow.LayoutParams();
                    layoutParams.Width = 150; // Set your desired width
                    layoutParams.Height = 150; // Set your desired height
                    layoutParams.SetMargins(padding, padding, padding, padding);
                    textView.LayoutParameters = layoutParams;

                    textViews[i, j] = textView;
                    int finalI = i;
                    int finalJ = j;

                    textView.Click += (sender, e) =>
                    {
                        CheckClick(finalI, finalJ);
                    };

                    tableRow.AddView(textView);
                }

                opponentTableLayout.AddView(tableRow);
            }

/*          ship1 = FindViewById<ImageView>(Resource.Id.ship1);
            ship2 = FindViewById<ImageView>(Resource.Id.ship2);
            ship3 = FindViewById<ImageView>(Resource.Id.ship3);*/
            _turnText = FindViewById<TextView>(Resource.Id.turnText);

            string connectedDevice = Intent.GetStringExtra("Connected-Device");

            if (CustomBluetooth.Instance.GetConnectedSocket() != null && connectedDevice!=null)
            {
                if(connectedDevice == "Host")
                {
                    playerState = PlayerState.Turn;
                    _turnText.Text = $"Your turn";
                } else
                {
                    playerState = PlayerState.Waiting;
                    _turnText.Text = $"Opponent turn";
                }
                _connectedSocket = CustomBluetooth.Instance.GetConnectedSocket();

                Task.Run(() => { HandleSocketInput(this, _connectedSocket); });

                Toast.MakeText(this, "Udalo sie polaczyc z socketem w nowej aktywnosci", ToastLength.Short).Show();
            }
        }


       

        private void RandomShipPlacement()
        {
            int pointsToAllocate = 7;
            int maxAttempts = 50; // Set a reasonable maximum number of attempts

            for (int i = 0; i < pointsToAllocate; i++)
            {
                int attempts = 0;

                while (true)
                {
                    // Generate random row and column indices
                    int randomRow = random.Next(0, 5);
                    int randomColumn = random.Next(0, 5);

                    // Check if the selected cell is already allocated
                    if (ShipLocation[randomRow, randomColumn] == 0)
                    {
                        // Allocate the point
                        ShipLocation[randomRow, randomColumn] = 1;
                        break; // Break out of the loop if a suitable index is found
                    }

                    // Increment attempts
                    attempts++;

                    // Check if the maximum number of attempts is reached
                    if (attempts >= maxAttempts)
                    {
                        // Handle the situation when a suitable index is not found within the limit
                        // You can throw an exception, log a message, or take appropriate action
                        break;
                        // throw new InvalidOperationException("Unable to find suitable indices within the maximum number of attempts.");
                    }
                }
            }
        }


        private void CheckClick(int i, int j) {
            if(playerState == PlayerState.Waiting && isActionPerformed)
            {
                RunOnUiThread(() =>
                {
                    Toast.MakeText(this, "Wait for your turn", ToastLength.Short).Show();
                });
                return;
            }
            if (textViews[i,j].Text == "X")
            {
                RunOnUiThread(() =>
                {
                    Toast.MakeText(this, "Ten cel został już trafiony", ToastLength.Short).Show();
                });
                return;
            } else
            {
                if (playerState == PlayerState.Turn && !isActionPerformed)
                {
                    GameActionDTO gameActionDTO = new GameActionDTO
                    {
                        row = i,
                        column = j,
                        gameState = GameState.PlayingPhase,
                        gameAction = GameAction.Shot,
                       // isShootedCallback = true
                    };

                    isActionPerformed = true;
                    SendGameData(gameActionDTO);
                }
            }
        }

        private bool CheckField(Context context, int row, int column)
        {
            if (ShipLocation[row,column] != 1)
            {
                ((Activity)context).RunOnUiThread(() =>
                {
                    if (myTextViews[row, column].Text != "X")
                    {
                        myTextViews[row, column].Text = "X";
                    }
                });
              return false;
            }

                ((Activity)context).RunOnUiThread(() =>
                {
                    if (myTextViews[row, column].Text != "X")
                    {
                        myTextViews[row, column].SetBackgroundResource(Resource.Drawable.cellShooted);
                        myTextViews[row, column].Text = "X";
                    }
                });
            return true;
        }



        private void UpdateFieldCallback(Context context, int row, int column, bool isshooted)
        {
            ((Activity)context).RunOnUiThread(() =>
            {
                if (textViews[row, column].Text != "X")
                {
                    if (isshooted)
                    {
                        textViews[row, column].SetBackgroundResource(Resource.Drawable.cell_clicked);
                        textViews[row, column].Text = "X";
                        _turnText.Text = "Opponent turn";
                        return;
                    }
                    textViews[row, column].Text = "X";
                    _turnText.Text = "Opponent turn";
                    playerState = PlayerState.Waiting;
                }
            });      
        }

        public override void OnBackPressed()
        {
            // Show a confirmation dialog
            ShowConfirmationDialog();
        }

        private void ShowConfirmationDialog()
        {
            AndroidX.AppCompat.App.AlertDialog.Builder alertDialogBuilder = new AndroidX.AppCompat.App.AlertDialog.Builder(this);
            alertDialogBuilder.SetMessage("Are you sure you want to leave?");
            alertDialogBuilder.SetCancelable(false);

            alertDialogBuilder.SetPositiveButton("Yes", (senderAlert, args) =>
            {
                // Close the Bluetooth socket or handle other cleanup actions
                //CloseBluetoothSocket(); // Adjust the method name based on your requirements

                // Finish the activity
                CloseBluetoothSocket();

                Intent intent = new Intent(this, typeof(DiscoveredDevicesActivity));

                // Set the flags to clear the activity stack
                intent.SetFlags(ActivityFlags.ClearTop);
                // Start PreviousActivity
                StartActivity(intent);

                Finish();
            });

            alertDialogBuilder.SetNegativeButton("No", (senderAlert, args) =>
            {
                // If the user cancels, do nothing (or handle as needed)
            });

            // Create and show the dialog
            AndroidX.AppCompat.App.AlertDialog alertDialog = alertDialogBuilder.Create();
            alertDialog.Show();
        }


        private static void CloseBluetoothSocket()
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
    
        private void SendGameData(GameActionDTO gameActionDTO)
        {
            if (_connectedSocket != null && playerState == PlayerState.Turn)
            {
                try
                {
                    string jsonString = JsonConvert.SerializeObject(gameActionDTO);

                    Stream streamOutStream = _connectedSocket.OutputStream;
                    byte[] bytes = System.Text.Encoding.UTF8.GetBytes(jsonString);
                    // Write the bytes to the OutputStream
                    streamOutStream.Write(bytes, 0, bytes.Length);
                    streamOutStream.Flush(); // Ensure data is sent immediately
                }
                catch (Java.IO.IOException ex)
                {
                    Log.Error("SocketError", "Error sending data: " + ex.Message);
                    // Handle the exception appropriately
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

                            if(receivedObject != null)
                            {
                                if(receivedObject.gameAction == GameAction.Shot)
                                {
                                    playerState = PlayerState.Turn;
                                    isActionPerformed = false;

                                    ((Activity)context).RunOnUiThread(() =>
                                    {
                                        _turnText.Text = "Your turn";
                                       
                                    });

                                    bool Checked = CheckField(context, receivedObject.row, receivedObject.column);
                                    GameActionDTO gameActionCallback = new GameActionDTO
                                    {
                                        row = receivedObject.row,
                                        column = receivedObject.column,
                                        gameState = GameState.PlayingPhase,
                                        gameAction = GameAction.Callback,
                                    };

                                    if (Checked)
                                    {
                                        gameActionCallback.isShootedCallback = true;
                                    } else
                                    {
                                        gameActionCallback.isShootedCallback = false;
                                    }

                                    SendGameData(gameActionCallback);
                                } else if(receivedObject.gameAction == GameAction.Callback)
                                {
                                    UpdateFieldCallback(context, receivedObject.row, receivedObject.column, receivedObject.isShootedCallback);
                                }
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
                    //Log.Error("SocketError", "Error handling socket input: " + e.Message);
                    CloseBluetoothSocket();
                    isConnection = false;
                    ((Activity)context).RunOnUiThread(() =>
                    {
                        Toast.MakeText(context, "Socket disconnected", ToastLength.Short).Show();

                        Intent intent = new Intent(context, typeof(DiscoveredDevicesActivity));

                        // Set the flags to clear the activity stack
                        intent.SetFlags(ActivityFlags.ClearTop);
                        // Start PreviousActivity
                        context.StartActivity(intent);
                        _activity.Finish();
                    });
                    //  ShowUIElementsAfterDisconnect(context);
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
    }
}