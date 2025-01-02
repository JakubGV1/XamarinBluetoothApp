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
using Android.Text.Style;
using Android.Text;
using Android.Graphics;

namespace XamarinV2
{
    [Activity(Label = "Sekwencje")]
    public class SequenceGameActivity : AppCompatActivity
    {
        private string playerConnectInfo;
        private BluetoothSocket _connectedSocket;
        private bool isConnection;
        private SequenceGameActivity _activity;
        private PlayerState playerState;
        private GameState playerGameState;

        //Texts

        private TextView statusText;
        private TextView playerPoints;
        private TextView comboCounter;
        private TextView roundCounter;
        private TextView playerChance;
      //  private TextView sequenceCounter;

        private TextView playerActionText;

        /// Buttons and Visuals


        private Button upButton;
        private Button downButton;
        private Button rightButton;
        private Button leftButton;
        private Button middleButton;

        private LinearLayout buttonsGroup;
        private ProgressDialog progressDialog;

        ///Variables

        private int roundMax = 3;
        private int playerScore;
        private int round;
        private int playerCombo;
        private int chances;

       // private int planningCurrentSequenceCounter;
        private int currentSequenceCounter;

        private int currentRoundSequenceCounter;


        private int otherPlayerScore;
        private string playerSequenceToSend;
        private string receivedSequence;

        private bool isOtherPlayerWantPlayAgain;
        private bool wantPlayAgain;

        private bool isPlayerFinished;
        private bool isOtherPlayerFinished;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.sequence_game);

            _activity = this;

            RunOnUiThread(() =>
            {
                Initialize();

            });

            string connectedDevice = Intent.GetStringExtra("Connected-Device");
            if (CustomBluetooth.Instance.GetConnectedSocket() != null && connectedDevice != null)
            {
                playerConnectInfo = connectedDevice;
                if (connectedDevice == "Host")
                {
                    playerState = PlayerState.Turn;
                    RunOnUiThread(() =>
                    {
                        ShowSequencePlanning();
                    });
                }
                else
                {
                    playerState = PlayerState.Waiting;
                    RunOnUiThread(() =>
                    {
                        WaitingForSequence();
                    });
                }
                _connectedSocket = CustomBluetooth.Instance.GetConnectedSocket();
                if (_connectedSocket != null && _connectedSocket.IsConnected)
                {
                    isConnection = true;
                    Task.Run(() => HandleSocketInput(this, _connectedSocket));
                }
            }
        }

        private void Initialize()
        {
            upButton = FindViewById<Button>(Resource.Id.upArrow);
            upButton.Click += (sender, e) =>
            {
                ButtonClick("↑");
            };

            downButton = FindViewById<Button>(Resource.Id.downArrow);

            downButton.Click += (sender, e) =>
            {
                ButtonClick("↓");
            };

            leftButton = FindViewById<Button>(Resource.Id.leftArrow);

            leftButton.Click += (sender, e) =>
            {
                ButtonClick("←");
            };
            rightButton = FindViewById<Button>(Resource.Id.rightArrow);

            rightButton.Click += (sender, e) =>
            {
                ButtonClick("→");
            };

            middleButton = FindViewById<Button>(Resource.Id.middleArrow);

            middleButton.Click += (sender, e) =>
            {
                ButtonClick("🔁");
            };

            statusText = FindViewById<TextView>(Resource.Id.sequenceStatus);
            buttonsGroup = FindViewById<LinearLayout>(Resource.Id.sequenceButtonGroup);
            playerActionText = FindViewById<TextView>(Resource.Id.sequenceText);
            playerPoints = FindViewById<TextView>(Resource.Id.pointsCounter);
            comboCounter = FindViewById<TextView>(Resource.Id.comboCounter);
            roundCounter = FindViewById<TextView>(Resource.Id.roundCounter);
            playerChance = FindViewById<TextView>(Resource.Id.chanceCounter);
            DefaultValues();
        }

        private void WaitingForSequence()
        {
            buttonsGroup.Visibility = ViewStates.Invisible;
            statusText.Text = "Oczekiwanie na sekwencję";
            playerActionText.Text = string.Empty;
            playerGameState = GameState.None;
        }

        private void ShowSequencePlanning()
        {
            buttonsGroup.Visibility = ViewStates.Visible;
            statusText.Text = "Utwórz sekwencję dla przeciwnika";
            playerGameState = GameState.PlanningPhase;
        }

        public override void OnBackPressed()
        {
            
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
                intent.PutExtra("Game", "Sekwencje");
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


        private void ButtonClick(string buttonArrow)
        {
            int diffrenceValue = 0;
            if (playerConnectInfo == "Host") diffrenceValue = 4;
            else diffrenceValue = 3;
            if (playerState == PlayerState.Turn && currentSequenceCounter < round+diffrenceValue)
            {
                if(playerGameState == GameState.PlayingPhase && currentSequenceCounter < receivedSequence.Length)
                {
                    if (buttonArrow.Equals(receivedSequence[currentSequenceCounter].ToString()))
                    {
                        AppendColoredArrow(buttonArrow, Color.Green);
                        playerCombo += 1;
                        playerScore += (10 * playerCombo);
                        currentRoundSequenceCounter+=1;
                    } else
                    {
                        if(chances > 0)
                        {
                            AppendColoredArrow(buttonArrow, Color.Orange);
                            chances -= 1;
                            currentRoundSequenceCounter = 0;
                        } else
                        {
                            AppendColoredArrow(buttonArrow, Color.Red);
                            playerCombo = 0;
                            currentRoundSequenceCounter = 0;
                        }
                    }
                    currentSequenceCounter+=1;
                    RunOnUiThread(() =>
                    {
                        comboCounter.Text = $"Combo: {playerCombo}";
                        playerPoints.Text = $"Punkty: {playerScore}";
                        playerChance.Text = $"Szansa: {chances}";
                    });

                } else if(playerGameState == GameState.PlanningPhase)
                {
                    AppendColoredArrow(buttonArrow, Color.White);
                    playerSequenceToSend += buttonArrow;
                    currentSequenceCounter += 1;
                    
                    if (currentSequenceCounter == round + diffrenceValue)
                    {
                            GameActionDTOSequence gameActionDTO = new GameActionDTOSequence
                            {
                                gameAction = GameAction.SequenceSend,
                                stringSequence = playerSequenceToSend,
                            };

                            SendGameData(gameActionDTO);
                    }



                }
            }
        }


        void AppendColoredArrow(string arrow, Color color)
        {
            SpannableString spannable = new SpannableString(arrow);
            spannable.SetSpan(new ForegroundColorSpan(color), 0, arrow.Length, SpanTypes.ExclusiveExclusive);

            playerActionText.Append(spannable);
        }

        private async void ShowSequenceDialog(string sequence)
        {
            RunOnUiThread(() =>
            {
                progressDialog?.Dismiss();
                progressDialog = new ProgressDialog(this);
                progressDialog.SetTitle("Sekwencja do zapamiętania");
                progressDialog.SetMessage(sequence);
                progressDialog.SetCancelable(false);
                progressDialog.Show();
            });

            // Wait for 5 seconds (5000 milliseconds)
            await Task.Delay(5000);

            // Ensure the dismissal and starting sequence is done on the UI thread
            RunOnUiThread(() =>
            {
                if (progressDialog.IsShowing)
                {
                    progressDialog.Dismiss();
                    StartSequence();
                }
            });
        }


        private async void StartSequence() // StartGame
        {
            int remainingTime = 5;
            buttonsGroup.Visibility = ViewStates.Visible;
            playerActionText.Text = "";
            currentSequenceCounter = 0;
            currentRoundSequenceCounter = 0;

            // Create a timer to update the status text every second
            while (remainingTime > 0)
            {
                // Update the status text on the UI thread
                RunOnUiThread(() => statusText.Text = $"Pozostały czas: {remainingTime} sekund");

                remainingTime--;

                // Wait for 1 second
                await Task.Delay(1000);
            }
            if (currentRoundSequenceCounter == receivedSequence.Length)
            {
                chances += 1;
                RunOnUiThread(() =>
                {
                    playerChance.Text = $"Szanse: {chances}";
                });
            }
            // Switch phase after the timer completes
            if (round == roundMax && isOtherPlayerFinished)
            {
                GameActionDTOSequence gameActionDTO = new GameActionDTOSequence
                {
                    gameAction = GameAction.Result,
                    result = playerScore,
                };
                isPlayerFinished = true;
                SendGameData(gameActionDTO);
                RunOnUiThread(() => {
                    ShowEndResults();
                });

            } else
            {
                SwitchPhase();
            }
        }

        private void SwitchPhase()
        {
            RunOnUiThread(() => {
                playerGameState = GameState.PlanningPhase;
                playerActionText.Text = "";
                statusText.Text = "Faza tworzenia sekwencji";
                currentSequenceCounter = 0;
                playerSequenceToSend = string.Empty;
            });
        }


        private void DefaultValues()
        {
            chances = 0;
            playerGameState = GameState.None;
            round = 0;
            playerActionText.Text = "";
            statusText.Text = "";
            playerCombo = 0;
            playerScore = 0;
            playerPoints.Text = $"Punkty: {0}";
            comboCounter.Text = $"Combo: {0}";
            playerChance.Text = $"Szanse: {0}";
            roundCounter.Text = $"Runda: {1}";
            currentSequenceCounter = 0;
            playerSequenceToSend = string.Empty;
            isPlayerFinished = false;
        }

        private void SendGameData(GameActionDTOSequence gameActionDTO)
        {
            System.Diagnostics.Debug.WriteLine("Wyslano ->:" + gameActionDTO.gameAction);
            if (_connectedSocket != null)
            {
                try
                {
                    string jsonString = JsonConvert.SerializeObject(gameActionDTO);

                    Stream streamOutStream = _connectedSocket.OutputStream;
                    byte[] bytes = System.Text.Encoding.UTF8.GetBytes(jsonString);

                    streamOutStream.Write(bytes, 0, bytes.Length);
                    streamOutStream.Flush();

                }
                catch (Java.IO.IOException ex)
                {
                    Log.Error("SocketError", "Error sending data: " + ex.Message);
                }
            }
        }

        private void OtherPlayerFinished()
        {
            isOtherPlayerFinished = true;
            if (isPlayerFinished)
            {
                ShowEndResults();
            }
        }

        private void ShowEndResults()
        {
            RunOnUiThread(() =>
            {
                buttonsGroup.Visibility = ViewStates.Gone;
                playerActionText.Text = string.Empty;
                if (!isOtherPlayerFinished)
                {
                    statusText.Text = "Oczekiwanie aż przeciwnik skończy sekwencję...";
                }
                else
                {
                    statusText.Text = "Koniec gry...";
                    FinishedDialogShow();
                }
            });
        }

        private void FinishedDialogShow()
        {
            AndroidX.AppCompat.App.AlertDialog.Builder alertDialogBuilder = new AndroidX.AppCompat.App.AlertDialog.Builder(this);

            string status = "";

            if (otherPlayerScore > playerScore)
            {
                status = "Przegrałeś";
            }
            else if (playerScore > otherPlayerScore)
            {
                status = "Wygrałeś";
            }
            else
            {
                status = "Remis";
            }

            alertDialogBuilder.SetMessage($"{status}! Uzyskałeś {playerScore} pkt, natomiast przeciwnik uzyskał {otherPlayerScore} pkt! \n Czy chcesz zagrać ponownie?");
            alertDialogBuilder.SetCancelable(false);

            alertDialogBuilder.SetPositiveButton("Tak", (senderAlert, args) =>
            {
                wantPlayAgain = true;
                GameActionDTOSequence gameActionCallback = new GameActionDTOSequence
                {
                    gameAction = GameAction.PlayAgain,
                };
                SendGameData(gameActionCallback);

                if (isOtherPlayerWantPlayAgain)
                {
                    RestartGame();
                }
                else
                {
                    ShowProgressWindow();
                }
            });

            alertDialogBuilder.SetNegativeButton("Nie", (senderAlert, args) =>
            {
                CloseBluetoothSocket();
                Intent intent = new Intent(this, typeof(DiscoveredDevicesActivity));

                intent.SetFlags(ActivityFlags.ClearTop);
                intent.PutExtra("Game", "Sekwencje");
                StartActivity(intent);

                Finish();
            });

            // Create and show the dialog
            AndroidX.AppCompat.App.AlertDialog alertDialog = alertDialogBuilder.Create();
            alertDialog.Show();
        }

        private void RestartGame()
        {
            RunOnUiThread(() =>
            {
                DefaultValues();
                if (playerConnectInfo == "Host")
                {
                    playerConnectInfo = "Client";
                    playerState = PlayerState.Waiting;
                    WaitingForSequence();
                }
                else
                {
                    playerConnectInfo = "Host";
                    playerState = PlayerState.Turn;
                    ShowSequencePlanning();
                }
            });
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
                    intent.PutExtra("Game", "Sekwencje");
                    StartActivity(intent);
                    Finish();
                });
                progressDialog.Show();
            });
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

                    if (bytesRead > 0)
                    {
                        string receivedData = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                        var receivedObject = JsonConvert.DeserializeObject<GameActionDTOSequence>(receivedData);

                        Array.Clear(buffer, 0, buffer.Length);
                        if (receivedObject != null)
                        {

                            if (receivedObject.gameAction == GameAction.SequenceSend)
                            {
                                round += 1;
                                if (round <= roundMax)
                                {
                                    playerState = PlayerState.Turn;
                                    playerGameState = GameState.PlayingPhase;
                                    receivedSequence = receivedObject.stringSequence;
                                    RunOnUiThread(() =>
                                    {
                                        roundCounter.Text = $"Runda: {round}";
                                        ShowSequenceDialog(receivedObject.stringSequence);
                                    });
                                } else
                                {
                                    GameActionDTOSequence gameActionDTO2 = new GameActionDTOSequence
                                    {
                                        gameAction = GameAction.Result,
                                        result = playerScore,
                                    };
                                    SendGameData(gameActionDTO2);
                                }

                                GameActionDTOSequence gameActionDTO = new GameActionDTOSequence
                                {
                                    gameAction = GameAction.Callback,
                                };
                                SendGameData(gameActionDTO);
                            }
                            else if (receivedObject.gameAction == GameAction.Result)
                            {
                                otherPlayerScore = receivedObject.result;
                                OtherPlayerFinished();
                            }
                            else if (receivedObject.gameAction == GameAction.PlayAgain)
                            {
                                GamePlayAgain();
                            }
                            else if (receivedObject.gameAction == GameAction.Callback)
                            {
                                playerState = PlayerState.Waiting;
                                if (round == roundMax)
                                {
                                    GameActionDTOSequence gameActionDTO = new GameActionDTOSequence
                                    {
                                        gameAction = GameAction.Result,
                                        result = playerScore,
                                    };
                                    isPlayerFinished = true;
                                    SendGameData(gameActionDTO);
                                    RunOnUiThread(() => {
                                        ShowEndResults();
                                    });
                                } else
                                {
                                    RunOnUiThread(() =>
                                    {
                                        WaitingForSequence();
                                    });
                                }
                            }
                        }
                    }
                }
            }
            catch (Java.IO.IOException e)
            {
                CloseBluetoothSocket();
                isConnection = false;
                ((Activity)context).RunOnUiThread(() =>
                {
                    Toast.MakeText(context, "Nastąpiło rozłączenie", ToastLength.Short).Show();
                    Intent intent = new Intent(context, typeof(DiscoveredDevicesActivity));

                    intent.SetFlags(ActivityFlags.ClearTop);
                    intent.PutExtra("Game", "Sekwencje");
                    context.StartActivity(intent);
                    _activity.Finish();
                });
            }
        }


        private void CloseBluetoothSocket()
        {
            try
            {
                if (_connectedSocket != null)
                {
                    _connectedSocket.Close();
                    _connectedSocket = null;
                    CustomBluetooth.Instance.CloseConnection();
                }
            }
            catch (Exception ex)
            {
                Log.Error("Socket Error", "Error closing socket: " + ex.Message);
            }
        }



    }

}