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
    [Activity(Label = "Statki")]
    public class GameActivity : AppCompatActivity
    {
       
        TextView[,] textViews = new TextView[5, 5];
        TextView[,] myTextViews = new TextView[5, 5];
        TextView ownTable;
        private ImageView ship1;
        private ImageView ship2;
        private ImageView ship3;

        private TextView ship1Text;
        private TextView ship2Text;
        private TextView ship3Text;

        private LinearLayout ShipsView;

        private TextView _turnText;
        private BluetoothSocket _connectedSocket;
        private GameActivity _activity;
        private int[,] ShipLocation;
        // turn?
        private PlayerState playerState;
        private GameState _gamestate;
        private bool isActionPerformed;
        private readonly Random random = new Random();
        private Dictionary<int, int> shipsToPlace;
        private int ShipSelected;
        private List<Ship> PlayerShips;
        private int _currentShipSize;
        private int gridSize = 5;
        private Ship _currentShip;
        private bool isOtherPlayerReady;
        private bool isPlayerReady;
        private string playerConnectInfo;

        private TableLayout myTableLayout;
        private TableLayout opponentTableLayout;
        bool isConnection;

        private bool isOtherPlayerWantPlayAgain;
        private bool wantPlayAgain;

        private ProgressDialog progressDialog;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.content_game);

            _activity = this;

            opponentTableLayout = FindViewById<TableLayout>(Resource.Id.OpponentTableLayout);
            myTableLayout = FindViewById<TableLayout>(Resource.Id.OwnTableLayout);
            ownTable = FindViewById<TextView>(Resource.Id.ownTable);    
            isActionPerformed = false;

            RunOnUiThread(() =>
            {
                InitializeConfig();
                _turnText = FindViewById<TextView>(Resource.Id.turnText);
                CreatePlanningTable(opponentTableLayout);
            });


            string connectedDevice = Intent.GetStringExtra("Connected-Device");
            if (CustomBluetooth.Instance.GetConnectedSocket() != null && connectedDevice != null)
            {
                playerConnectInfo = connectedDevice;
                if (connectedDevice == "Host")
                {
                    playerState = PlayerState.Turn;
                }
                else
                {
                    playerState = PlayerState.Waiting;
                }
                _gamestate = GameState.PlanningPhase;
                _connectedSocket = CustomBluetooth.Instance.GetConnectedSocket();
                if (_connectedSocket != null && _connectedSocket.IsConnected)
                {
                    isConnection = true;
                    Task.Run(() =>  HandleSocketInput(this, _connectedSocket));
                    //Task.Run(() => HandleSocketInput2(this, _connectedSocket));
                    Toast.MakeText(this, "Nawiązano połączenie", ToastLength.Short).Show();
                }



            }
          //  _gamestate = GameState.PlanningPhase;
        }



        private void InitializeConfig()
        {
            wantPlayAgain = false;
            isOtherPlayerWantPlayAgain = false;
            isActionPerformed = false;
            isPlayerReady = false;
            isOtherPlayerReady = false;
            _currentShipSize = 0;
            ShipLocation = new int[5, 5];
            shipsToPlace = new Dictionary<int, int>();
            PlayerShips = new List<Ship>();
            shipsToPlace.Add(1, 2);
            shipsToPlace.Add(2, 1);
            shipsToPlace.Add(3, 1);
            ownTable.Text = "Wybierz statek";
            ship1 = FindViewById<ImageView>(Resource.Id.ship1View);
            ship2 = FindViewById<ImageView>(Resource.Id.ship2View);
            ship3 = FindViewById<ImageView>(Resource.Id.ship3View);

            ship1.Click += (sender, e) =>
            {
                if (ShipSelected != 0 && _currentShip != null && shipsToPlace[1] == 0) return;
                SelectShip(1, new Ship(1));
            };

            ship2.Click += (sender, e) =>
            {
                if (ShipSelected != 0 && _currentShip != null && shipsToPlace[2] == 0) return;
                SelectShip(2, new Ship(2));
            };

            ship3.Click += (sender, e) =>
            {
                if (ShipSelected != 0 && _currentShip != null && shipsToPlace[3] == 0) return;
                SelectShip(3, new Ship(3));
            };

            ship1Text = FindViewById<TextView>(Resource.Id.textView);
            ship2Text = FindViewById<TextView>(Resource.Id.textView2);
            ship3Text = FindViewById<TextView>(Resource.Id.textView3);

            ship1Text.Text = $"Posiadasz {shipsToPlace[1]}";
            ship2Text.Text = $"Posiadasz {shipsToPlace[2]}";
            ship3Text.Text = $"Posiadasz {shipsToPlace[3]}";
            ShipsView = FindViewById<LinearLayout>(Resource.Id.groupShip);
            //ReadyButton
        }

        private void SelectShip(int index, Ship ship)
        {
            _currentShip = ship;
            ShipSelected = index;
            _currentShipSize = 0;
        }

        private bool ArePositionsAvailable(int finalI, int finalJ, int size)
        {
            bool horizontalValid = true;
            bool verticalValid = true;

            for (int i = 0; i < size; i++)
            {
                int currentX = finalI + i;
                int currentY = finalJ;

                if (currentX < 0 || currentX >= gridSize || currentY < 0 || currentY >= gridSize)
                {
                    // Horizontal position is out of bounds
                    horizontalValid = false;
                }

                currentX = finalI;
                currentY = finalJ + i;

                if (currentX < 0 || currentX >= gridSize || currentY < 0 || currentY >= gridSize)
                {
                    // Vertical position is out of bounds
                    verticalValid = false;
                }

                // Check if the position is occupied by another ship
                if (horizontalValid && PlayerShips.Any(ship => ship.positions.Any(pos => pos.x == currentX && pos.y == currentY)))
                {
                    return false;
                }

                // Reset validity for the next iteration

                if (verticalValid && PlayerShips.Any(ship => ship.positions.Any(pos => pos.x == currentX && pos.y == currentY)))
                {
                    return false;
                }

                // Reset validity for the next iteration
            }

            if (_currentShip.positions.Count == 0)
            {
                if (horizontalValid || verticalValid)
                {
                    return true;
                }
            }

             bool isHorizontal = _currentShip.positions.All(pos => pos.x == _currentShip.positions.First().x);
             bool isVertical = _currentShip.positions.All(pos => pos.y == _currentShip.positions.First().y);

            if (_currentShip.positions.Count > 0)
            {
                Positions lastSegment = _currentShip.positions.Last();
                Positions firstFragment = _currentShip.positions.First();

                if (isHorizontal && verticalValid)
                {
                    if(_currentShip.positions.Count > 1)
                    {
                        if ((lastSegment.x == finalI && Math.Abs(lastSegment.y - finalJ) == 1) && firstFragment.x == finalI)
                        {
                            return true;
                        }
                    } else
                    {
                        if (lastSegment.x == finalI && Math.Abs(lastSegment.y - finalJ) == 1)
                        {
                            return true;
                        }
                    }
                }
                if (isVertical && horizontalValid)
                {
                    if (_currentShip.positions.Count > 1)
                    {
                        if ((lastSegment.y == finalJ && Math.Abs(lastSegment.x - finalI) == 1) && firstFragment.y == finalJ)
                        {
                            return true;
                        }
                    } else
                    {
                        if(lastSegment.y == finalJ && Math.Abs(lastSegment.x - finalI)==1)
                        {
                            return true;
                        }
                    }
                }

                return false;
            }

            return true;
        }

        private void HideShipGroupAndGetReady()
        {
            bool allShipPlaced = shipsToPlace.Values.All(value => value == 0);
            if (allShipPlaced)
            {
                isPlayerReady = true;
                GameActionDTO gameAction = new GameActionDTO
                {
                    row = 0,
                    column = 0,
                    gameState = GameState.ReadyToPlay,
                    gameAction = GameAction.PlayerAction,
                };
                SendGameData(gameAction);
                ShipsView.Visibility = ViewStates.Gone;
                if (!isOtherPlayerReady) _turnText.Text = $"Faza planowania (oczekiwanie na drugiego gracza)";
                else ShowGameView();
            }
        }

        



        private void ShowGameView()
        {
            try
            {
                RunOnUiThread(() =>
                {
                    ownTable.Text = "Twoja tabela";
                    myTableLayout.RemoveAllViews();
                    opponentTableLayout.RemoveAllViews();
                    CreateSmallerTable();
                    CreateOpponentTable();
                    _turnText.Text = $"Twoja tura";
                    _gamestate = GameState.PlayingPhase;
                    if (playerState == PlayerState.Turn)
                    {
                        _turnText.Text = $"Twoja tura";
                    }
                    else
                    {
                        _turnText.Text = $"Tura przeciwnika";
                    }
                });
            }
            catch (Exception ex)
            {
                Log.Error("ShowGameView", "Exception: " + ex.Message);
            }
        }

        private void CreateSmallerTable()
        {
            
            for (int i = 0; i < 5; i++)
            {
                TableRow tableRow = new TableRow(this);
                for (int j = 0; j < 5; j++)
                {
                    TextView textView = new TextView(this);
                    textView.Gravity = GravityFlags.Center;
                
                    if (PlayerShips.Any(ship => ship.positions.Any(pos => pos.x == i && pos.y == j)))
                    {
                        textView.SetBackgroundResource(Resource.Drawable.cellShooted);
                    }
                    else
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
        }

        private void SwitchText(int index)
        {
            switch (index)
            {
                case 1:
                    ship1Text.Text = $"Posiadasz {shipsToPlace[index]}";
                    break;
                case 2:
                    ship2Text.Text = $"Posiadasz {shipsToPlace[index]}";
                    break;
                case 3:
                    ship3Text.Text = $"Posiadasz {shipsToPlace[index]}";
                    break;
            }
        }

        private void CreatePlanningTable(TableLayout opponentTableLayout)
        {
            _turnText.Text = $"Faza planowania";
            for (int i = 0; i < 5; i++)
            {
                TableRow tableRow = new TableRow(this);

                for (int j = 0; j < 5; j++)
                {
                    TextView textView = new TextView(this);
                    textView.Gravity = GravityFlags.Center;
                    textView.SetBackgroundResource(Resource.Drawable.cell_border);
                    int padding = 5;
                    textView.SetPadding(padding, padding, padding, padding);
                    TableRow.LayoutParams layoutParams = new TableRow.LayoutParams();
                    layoutParams.Width = 150;
                    layoutParams.Height = 150;
                    layoutParams.SetMargins(padding, padding, padding, padding);
                    textView.LayoutParameters = layoutParams;

                    myTextViews[i, j] = textView;
                    int finalI = i;
                    int finalJ = j;

                    textView.Click += (sender, e) =>
                    {
                        PlacementShip(finalI, finalJ);
                    };

                    tableRow.AddView(textView);
                }

                opponentTableLayout.AddView(tableRow);
            }
        }


        private void PlacementShip(int finalI, int finalJ) 
        {
            if (_gamestate == GameState.PlanningPhase)
            {
                if (ShipSelected == 0) return;

                if(_currentShipSize == 0)
                {
                    if (shipsToPlace.ContainsKey(ShipSelected) && shipsToPlace[ShipSelected] > 0)
                    {

                        if (ArePositionsAvailable(finalI, finalJ, _currentShip.Size)){
                            _currentShipSize = _currentShip.Size-1;
                            Positions newPosition = new Positions { x = finalI, y = finalJ };
                            Ship newShip = new Ship(_currentShip.Size);
                            newShip.positions.Add(newPosition);
                            _currentShip = newShip;
                            PlayerShips.Add(newShip);
                            myTextViews[finalI, finalJ].SetBackgroundResource(Resource.Drawable.cellShooted);

                            if (_currentShipSize == 0)
                            {
                                
                                shipsToPlace[ShipSelected]--;
                                SwitchText(ShipSelected);
                                ShipSelected = 0;
                                _currentShip = null;
                                HideShipGroupAndGetReady();
                                
                            }
                        } else
                        {
                            RunOnUiThread(() =>
                            {
                                Toast.MakeText(this, "Nie można tutaj postawić statku", ToastLength.Short).Show();
                            });
                        }
                    }
                } else
                {
                    Ship existingShip = PlayerShips.Find(ship => ship == _currentShip);
                    if (existingShip != null && shipsToPlace.ContainsKey(ShipSelected) && shipsToPlace[ShipSelected] > 0) {
                    if (ArePositionsAvailable(finalI, finalJ, _currentShipSize))
                    {
                        Positions newPosition = new Positions { x = finalI, y = finalJ };
                        _currentShip.positions.Add(newPosition);
                        _currentShipSize--;
                        existingShip.positions.Add(newPosition);
                        myTextViews[finalI, finalJ].SetBackgroundResource(Resource.Drawable.cellShooted);

  
                            if (_currentShipSize == 0)
                            {
                                
                                shipsToPlace[ShipSelected]--;
                                SwitchText(ShipSelected);
                                ShipSelected = 0;
                                _currentShip = null;
                                HideShipGroupAndGetReady();
                            }
                    }
                    else
                    {
                        RunOnUiThread(() =>
                        {
                            Toast.MakeText(this, "Nie można tutaj postawić kolejnego segmentu statku", ToastLength.Short).Show();
                        });
                    }
                    }
                }
            }
        }


        private void CreateOpponentTable()
        {
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
        }


        private void CheckClick(int i, int j) {
            if(playerState == PlayerState.Waiting && isActionPerformed)
            {
                RunOnUiThread(() =>
                {
                    Toast.MakeText(this, "Poczekaj na swoją ture", ToastLength.Short).Show();
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
                    };

                    isActionPerformed = true;
                    SendGameData(gameActionDTO);
                }
            }
        }

        private bool CheckField(Context context, int row, int column, out bool isDestroyed)
        {
            //ShipLocation[row,column]
            
            if (!PlayerShips.Any(ship => ship.positions.Any(pos => pos.x == row && pos.y == column)))
            {
                ((Activity)context).RunOnUiThread(() =>
                {
                    if (myTextViews[row, column].Text != "X")
                    {
                        myTextViews[row, column].Text = "X";
                    }
                });
                isDestroyed = false;
                return false;
            }
            isDestroyed = false;
            if (myTextViews[row, column].Text != "X")
            {
                foreach (Ship ship in PlayerShips)
                {
                    Positions hitPosition = ship.positions.FirstOrDefault(pos => pos.x == row && pos.y == column);
                    if (hitPosition != null)
                    {
                        hitPosition.isHit = true;
                        if (ship.IsDestroyed)
                        {
                            isDestroyed = true;
                        }

                        bool allDestroyed = PlayerShips.All(ship => ship.IsDestroyed);
                        if (allDestroyed)
                        {
                            GameActionDTO gameActionCallback = new GameActionDTO
                            {
                                row = 0,
                                column = 0,
                                gameState = GameState.Finished,
                                gameAction = GameAction.End,
                            };

                            GameFinished(context, "Przegrałeś");
                            SendGameData(gameActionCallback);
                            _gamestate = GameState.Finished;
                        }
                    }
                }
                RunOnUiThread(() =>{
                    myTextViews[row, column].SetBackgroundResource(Resource.Drawable.cellShooted);
                    myTextViews[row, column].Text = "X";
                });

            }
            return true;
        }



        private void UpdateFieldCallback(Context context, int row, int column, bool isshooted, bool isDestroyed)
        {
            ((Activity)context).RunOnUiThread(() =>
            {
                if (textViews[row, column].Text != "X")
                {
                    if (isshooted)
                    {
                        textViews[row, column].SetBackgroundResource(Resource.Drawable.cell_clicked);
                        textViews[row, column].Text = "X";
                        _turnText.Text = "Tura przeciwnika";
                        if (isDestroyed == true)
                        {
                            Toast.MakeText(context, "Zniszczono statek", ToastLength.Short).Show();
                        } else
                        {
                            Toast.MakeText(context, "Trafiono statek", ToastLength.Short).Show();
                        }

                        return;
                    }
                    textViews[row, column].Text = "X";
                    _turnText.Text = "Tura przeciwnika";
                    Toast.MakeText(context, "Pudło", ToastLength.Short).Show();
                    playerState = PlayerState.Waiting;
                }
            });      
        }


        private void OtherPlayerReady(Context context)
        {
            isOtherPlayerReady = true;
            if (isPlayerReady)
            {
                ShowGameView();
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

        private void ShowProgressWindow()
        {
            RunOnUiThread(() =>
            {
                progressDialog = new ProgressDialog(this);
                progressDialog.SetMessage("Oczekiwanie na decyzję gracza...");
                progressDialog.SetCancelable(false);
                progressDialog.SetButton("Anuluj", (sender, args) =>
                {
                    CloseBluetoothSocket();
                    Intent intent = new Intent(this, typeof(DiscoveredDevicesActivity));

                    intent.SetFlags(ActivityFlags.ClearTop);
                    progressDialog.Dismiss();
                    StartActivity(intent);
                    Finish();
                });
                progressDialog.Show();
            });
        }

        private void FinishedDialogShow(string status)
        {
            AndroidX.AppCompat.App.AlertDialog.Builder alertDialogBuilder = new AndroidX.AppCompat.App.AlertDialog.Builder(this);
            alertDialogBuilder.SetMessage($"{status}! \n Czy chcesz zagrać ponownie?");
            alertDialogBuilder.SetCancelable(false);

            alertDialogBuilder.SetPositiveButton("Tak", (senderAlert, args) =>
            {
                wantPlayAgain = true;
                // System.Diagnostics.Debug.WriteLine("Kliknieto tak");
                GameActionDTO gameActionCallback = new GameActionDTO
                {
                    row = 0,
                    column = 0,
                    gameState = GameState.Finished,
                    gameAction = GameAction.PlayAgain,
                };
                SendGameData(gameActionCallback);

                if (isOtherPlayerWantPlayAgain)
                {
                    RestartGame();
                } else
                {
                    ShowProgressWindow();
                }
             
            });

            alertDialogBuilder.SetNegativeButton("Nie", (senderAlert, args) =>
            {
                CloseBluetoothSocket();
                Intent intent = new Intent(this, typeof(DiscoveredDevicesActivity));

                // Set the flags to clear the activity stack
                intent.SetFlags(ActivityFlags.ClearTop);
                // Start PreviousActivity
                StartActivity(intent);

                Finish();
            });

            // Create and show the dialog
            AndroidX.AppCompat.App.AlertDialog alertDialog = alertDialogBuilder.Create();
            alertDialog.Show();
        }

        private void RestartGame()
        {
            if (playerConnectInfo == "Host")
            {
                playerState = PlayerState.Waiting;
            }
            else
            {
                playerState = PlayerState.Turn;
            }
            _gamestate = GameState.PlanningPhase;

            RunOnUiThread(() =>
            {
                myTableLayout.RemoveAllViews();
                opponentTableLayout.RemoveAllViews();
                ShipsView.Visibility = ViewStates.Visible;
                InitializeConfig();
            //    _turnText = FindViewById<TextView>(Resource.Id.turnText);
                CreatePlanningTable(opponentTableLayout);
            });

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
    
        private void SendGameData(GameActionDTO gameActionDTO)
        {
            if (_connectedSocket != null && (playerState == PlayerState.Turn || _gamestate == GameState.PlanningPhase || _gamestate == GameState.Finished))
            {
                System.Diagnostics.Debug.WriteLine("SendData" + _gamestate);
                try
                {
                    string jsonString = JsonConvert.SerializeObject(gameActionDTO);

                    Stream streamOutStream = _connectedSocket.OutputStream;
                    byte[] bytes = System.Text.Encoding.UTF8.GetBytes(jsonString);
                    // Write the bytes to the OutputStream
                   
                    streamOutStream.Write(bytes, 0, bytes.Length);
                    streamOutStream.Flush();
                    
                    // Ensure data is sent immediately
                }
                catch (Java.IO.IOException ex)
                {
                    Log.Error("SocketError", "Error sending data: " + ex.Message);
                    // Handle the exception appropriately
                }
            }
        }

        private void GamePlayAgain()
        {
            isOtherPlayerWantPlayAgain = true;
            if (wantPlayAgain)
            {
                if (progressDialog.IsShowing)
                {
                    progressDialog.Dismiss();
                }
                RestartGame();
            }
        }


        private void GameFinished(Context context, string status)
        {
            RunOnUiThread(() =>
            {
                _gamestate = GameState.Finished;
                FinishedDialogShow(status);
            });
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
                           
                            if (receivedObject.gameAction == GameAction.Shot)
                                {
                                    playerState = PlayerState.Turn;
                                    isActionPerformed = false;

                                    ((Activity)context).RunOnUiThread(() =>
                                    {
                                        _turnText.Text = "Twoja tura";
                                       
                                    });

                                    bool shipDestroyedInformation;
                                    bool Checked = CheckField(context, receivedObject.row, receivedObject.column, out shipDestroyedInformation);
                                System.Diagnostics.Debug.WriteLine($"Czy zniszczono - {shipDestroyedInformation}");    

                                if (_gamestate != GameState.Finished)
                                {
                                    GameActionDTO gameActionCallback = new GameActionDTO
                                    {
                                        row = receivedObject.row,
                                        column = receivedObject.column,
                                        gameState = GameState.PlayingPhase,
                                        gameAction = GameAction.Callback,
                                        isShipDestroyed = shipDestroyedInformation,
                                    };

                                    if (Checked)
                                    {
                                        gameActionCallback.isShootedCallback = true;
                                    }
                                    else
                                    {
                                        gameActionCallback.isShootedCallback = false;
                                    }
                                    SendGameData(gameActionCallback);
                                }

                                // SendGameData(gameActionCallback);
                            } else if(receivedObject.gameAction == GameAction.Callback)
                                {
                                    UpdateFieldCallback(context, receivedObject.row, receivedObject.column, receivedObject.isShootedCallback, receivedObject.isShipDestroyed);
                                } else if(receivedObject.gameAction == GameAction.PlayerAction)
                                {
                                    OtherPlayerReady(context);
                                } else if(receivedObject.gameAction == GameAction.End)
                                {
                                    GameFinished(context, "Wygrałeś");
                                } else if(receivedObject.gameAction == GameAction.PlayAgain)
                                {
                                    GamePlayAgain();
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
                        context.StartActivity(intent);
                        _activity.Finish();
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


        private async void HandleSocketInput2(Context context, BluetoothSocket socket)
        {
            isConnection = true;
            try
            {
                // Get the InputStream from the socket for reading data
                // Adjust the buffer size as needed
                Stream inputStream = socket.InputStream;
                byte[] buffer = new byte[1024];
                while (isConnection)
                {
                    if (inputStream.IsDataAvailable()) {
                        int bytesRead = inputStream.Read(buffer, 0, buffer.Length);
                        if (bytesRead > 0)
                        {
                            string receivedData = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                            var receivedObject = JsonConvert.DeserializeObject<GameActionDTO>(receivedData);
                            // Process the received data or update UI as needed
                            System.Diagnostics.Debug.WriteLine("received LOOPER?");
                            if (receivedObject != null)
                            {
                                if (receivedObject.gameAction == GameAction.Shot)
                                {
                                    playerState = PlayerState.Turn;
                                    isActionPerformed = false;

                                    ((Activity)context).RunOnUiThread(() =>
                                    {
                                        _turnText.Text = "Your turn";

                                    });
                                    bool shipDestroyed = false;
                                    bool Checked = CheckField(context, receivedObject.row, receivedObject.column, out shipDestroyed);
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
                                    }
                                    else
                                    {
                                        gameActionCallback.isShootedCallback = false;
                                    }

                                    SendGameData(gameActionCallback);
                                }
                                else if (receivedObject.gameAction == GameAction.Callback)
                                {
                                    //UpdateFieldCallback(context, receivedObject.row, receivedObject.column, receivedObject.isShootedCallback);
                                }
                                else if (receivedObject.gameAction == GameAction.PlayerAction)
                                {
                                    OtherPlayerReady(context);
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
            }
            catch (Java.IO.IOException e)
            {
                if (e is Java.IO.EOFException)
                {
                    // Handle end of stream (possible disconnection)
                    System.Diagnostics.Debug.WriteLine("EOF JAKIS JEBANY");
                }
                Log.Error("SocketError", "Error handling socket input: " + e.Message);
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