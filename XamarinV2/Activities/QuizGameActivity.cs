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
    [Activity(Label = "Quiz")]
    public class QuizGameActivity : AppCompatActivity
    {

        private string playerConnectInfo;
        private BluetoothSocket _connectedSocket;
        private bool isConnection;
        private QuizGameActivity _activity;
        private PlayerState playerState;

        private Button firstButton;
        private Button secondButton;
        private Button thirdButton;
        private Button fourthButton;

        private TextView statusText;
        private TextView questionText;

        private LinearLayout buttonsGroup;

        private Category currentCategory;
        private List<Category> questionsDatabase;

        private List<Category> previousCategories;
        private List<Category> currentCategoriesToChoose;

        private bool isCategoryChosen;

        private int currentQuestion;
        private int maxQuestions = 2;

        private int correctAnswersThisRound;
        private int totalCorrectAnswersCount;

        private int otherPlayerCorrectAnswers;

        private int rounds = 2;
        private int currentRound;

        private readonly Random random = new Random();

        private ProgressDialog progressDialog;
        private bool isOtherPlayerWantPlayAgain;
        private bool wantPlayAgain;

        private bool isPlayerFinished;
        private bool isOtherPlayerFinished;

        private bool isOtherPlayerFinishedAnswers;
        private bool isPlayerAnswered;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.quiz_game);

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
                        ShowCategoryMenu();
                    });
                }
                else
                {
                    playerState = PlayerState.Waiting;
                    RunOnUiThread(() =>
                    {

                        WaitingForCategory();
                    });
                }
                _connectedSocket = CustomBluetooth.Instance.GetConnectedSocket();
                if (_connectedSocket != null && _connectedSocket.IsConnected)
                {
                    isConnection = true;
                    Task.Run(() => HandleSocketInput(this, _connectedSocket));
                    //Task.Run(() => HandleSocketInput2(this, _connectedSocket));
                    //Toast.MakeText(this, "Nawiązano połączenie", ToastLength.Short).Show();
                }
            }
        }

        private void Initialize()
        {
            firstButton = FindViewById<Button>(Resource.Id.btnFirstQ);
            firstButton.Click += (sender, e) =>
            {
                ButtonClick(0);
            };

            secondButton = FindViewById<Button>(Resource.Id.btnSecondQ);

            secondButton.Click += (sender, e) =>
            {
                ButtonClick(1);
            };

            thirdButton = FindViewById<Button>(Resource.Id.btnThirdQ);

            thirdButton.Click += (sender, e) =>
            {
                ButtonClick(2);
            };
            fourthButton = FindViewById<Button>(Resource.Id.btnFourthQ);

            fourthButton.Click += (sender, e) =>
            {
                ButtonClick(3);
            };

            statusText = FindViewById<TextView>(Resource.Id.statusText);
            questionText = FindViewById<TextView>(Resource.Id.QuestionText);
            buttonsGroup = FindViewById<LinearLayout>(Resource.Id.groupButtons);
            DefaultValues();
            LoadQuestions();
        }

        private void DefaultValues()
        {
            isCategoryChosen = false;
            currentCategory = null;
            previousCategories = new List<Category>();
            currentCategoriesToChoose = new List<Category>();
            correctAnswersThisRound = 0;
            totalCorrectAnswersCount = 0;
            currentRound = 1;
            currentQuestion = 0;
            otherPlayerCorrectAnswers = 0;
            questionText.Text = "";
            statusText.Text = "";
            isOtherPlayerFinishedAnswers = false;
            isPlayerAnswered = false;
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


        private void LoadQuestions()
        {
            using (StreamReader streamReader = new StreamReader(Android.App.Application.Context.Assets.Open("quizdata.json")))
            {
                string jsonContent = streamReader.ReadToEnd();
                questionsDatabase = JsonConvert.DeserializeObject<List<Category>>(jsonContent);
            }
        }

        private void ShowCategoryMenu()
        {
            if (currentRound <= rounds)
            {
                buttonsGroup.Visibility = ViewStates.Visible;
                isOtherPlayerFinishedAnswers = false;
                isPlayerAnswered = false;
                questionText.Text = "Wybierz kategorię";
                //statusText.Text = "Wybierz kategorię";
                thirdButton.Visibility = ViewStates.Invisible;
                fourthButton.Visibility = ViewStates.Invisible;
                currentCategoriesToChoose.Clear();
                currentCategoriesToChoose = drawRandomCategories();
                firstButton.Text = currentCategoriesToChoose[0].CategoryName;
                secondButton.Text = currentCategoriesToChoose[1].CategoryName;
                correctAnswersThisRound = 0;
                currentQuestion = 0;
            } else
            {
                isPlayerFinished = true;
                GameActionDTOQuiz gacdtoQuiz = new GameActionDTOQuiz
                {
                    gameAction = GameAction.Result,
                    result = totalCorrectAnswersCount
                };
                SendGameData(gacdtoQuiz);

                ShowEndResults();
            }
        }

        private void WaitingForOtherPlayer()
        {

                if (isOtherPlayerFinishedAnswers && isPlayerAnswered)
                {
                    RunOnUiThread(() =>
                    {
                        ShowCategoryMenu();
                    });
                }
                else
                {
                    if (isPlayerAnswered)
                    {
                        RunOnUiThread(() =>
                        {
                            buttonsGroup.Visibility = ViewStates.Gone;
                            questionText.Text = "Oczekiwanie aż przeciwnik odpowie na pytania...";
                        });

                    }
                }
            
        }


        private void ShowEndResults()
        {
            RunOnUiThread(() =>
            {
                buttonsGroup.Visibility = ViewStates.Gone;
                if (!isOtherPlayerFinished)
                {
                    statusText.Text = string.Empty;
                    questionText.Text = "Oczekiwanie aż przeciwnik skończy quiz...";
                }
                else
                {
                    statusText.Text = string.Empty;
                    questionText.Text = "Koniec quizu...";
                    FinishedDialogShow();
                }
            });
        }

        private void WaitingForCategory()
        {
            if (currentRound <= rounds)
            {
                RunOnUiThread(() => { 
                if (currentCategory != null)
                    {
                        statusText.Text = $"W kategorii {currentCategory.CategoryName} uzyskałeś {correctAnswersThisRound} pkt!";
                    }
                    questionText.Text = "Oczekiwanie na wybór nowej kategorii...";
                    buttonsGroup.Visibility = ViewStates.Invisible;
                    correctAnswersThisRound = 0;
                    currentQuestion = 0;
                });
            } else
            {
                System.Diagnostics.Debug.WriteLine($"Rundy: {currentRound} / {rounds} ");
                GameActionDTOQuiz gacdtoQuiz = new GameActionDTOQuiz
                {
                    gameAction = GameAction.Result,
                    result = totalCorrectAnswersCount
                };
                SendGameData(gacdtoQuiz);
                isPlayerFinished = true;
                RunOnUiThread(() =>
                {
                    ShowEndResults();
                });
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

        private void ButtonClick(int buttonIndex)
        {
            if(currentCategory== null && playerState == PlayerState.Turn)
            {
                currentCategory = currentCategoriesToChoose[buttonIndex];
                previousCategories.Add(currentCategory);

                GameActionDTOQuiz GameActionQuiz = new GameActionDTOQuiz
                {
                    ChosenCategory = currentCategory,
                    gameAction = GameAction.ChoseCategory
                };

                SendGameData(GameActionQuiz);
                playerState = PlayerState.Waiting;
                ShowQuestions(currentCategory);
            } else
            {
                if(currentQuestion < maxQuestions)
                {

/*                    System.Diagnostics.Debug.WriteLine($"Pytanie? : {currentCategory.Questions[currentQuestion].Answers[buttonIndex].Text} - {currentCategory.Questions[currentQuestion].Answers[buttonIndex].IsCorrect})");*/
                    if (currentCategory.Questions[currentQuestion].Answers[buttonIndex].IsCorrect)
                    {
                        RunOnUiThread(() => {
                            Toast.MakeText(this, "Poprawna odpowiedź", ToastLength.Short).Show();
                        });
                        correctAnswersThisRound++;
                    } else
                    {
                        RunOnUiThread(() => {
                            Toast.MakeText(this, "Błędna odpowiedź", ToastLength.Short).Show();
                        });
                    }
                    currentQuestion++;
                    ShowQuestions(currentCategory);
                }
            }
        }

        private void ShowQuestions(Category category)
        {
            RunOnUiThread(() => {
                buttonsGroup.Visibility = ViewStates.Visible;
            });
            if (currentQuestion < maxQuestions)
            {

                Question currentQuestionObject = category.Questions[currentQuestion];


                // Get the number of answers for the current question
                int numberOfAnswers = currentQuestionObject.Answers.Count;
                RunOnUiThread(() =>
                {
                    // Show/hide buttons based on the number of answers
                    firstButton.Visibility = numberOfAnswers > 0 ? ViewStates.Visible : ViewStates.Gone;
                    secondButton.Visibility = numberOfAnswers > 1 ? ViewStates.Visible : ViewStates.Gone;
                    thirdButton.Visibility = numberOfAnswers > 2 ? ViewStates.Visible : ViewStates.Gone;
                    fourthButton.Visibility = numberOfAnswers > 3 ? ViewStates.Visible : ViewStates.Gone;

                    questionText.Text = currentQuestionObject.QuestionText;
                    statusText.Text = string.Empty;
                    // Set button texts based on the number of answers
                    firstButton.Text = numberOfAnswers > 0 ? currentQuestionObject.Answers[0].Text : string.Empty;
                    secondButton.Text = numberOfAnswers > 1 ? currentQuestionObject.Answers[1].Text : string.Empty;
                    thirdButton.Text = numberOfAnswers > 2 ? currentQuestionObject.Answers[2].Text : string.Empty;
                    fourthButton.Text = numberOfAnswers > 3 ? currentQuestionObject.Answers[3].Text : string.Empty;
                });
            } else
            {
                    statusText.Text = $"W kategorii {currentCategory.CategoryName} uzyskałeś {correctAnswersThisRound} pkt!";
                    currentCategory = null;
                    totalCorrectAnswersCount += correctAnswersThisRound;
                    currentRound++;
                    if(playerState == PlayerState.Turn)
                    {
                        isPlayerAnswered = true;
                        if(currentRound > rounds)
                        {
                        System.Diagnostics.Debug.WriteLine($"WAITNING: {currentRound} / {rounds}");
                        
                        isPlayerFinished = true;
                        GameActionDTOQuiz gacdtoQuiz = new GameActionDTOQuiz
                        {
                            gameAction = GameAction.Result,
                            result = totalCorrectAnswersCount
                        };
                        SendGameData(gacdtoQuiz);
                        RunOnUiThread(() =>
                        {
                            ShowEndResults();
                        });
                    } else
                        {
                        WaitingForOtherPlayer();
                        }
                    } else
                    {

                    
                        if(currentRound > rounds)
                        {
                        isPlayerFinished = true;
                        GameActionDTOQuiz gacdtoQuiz = new GameActionDTOQuiz
                        {
                            gameAction = GameAction.Result,
                            result = totalCorrectAnswersCount
                        };
                        SendGameData(gacdtoQuiz);
                        RunOnUiThread(() =>
                        {
                            ShowEndResults();
                        });
                    } else
                        {
                        GameActionDTOQuiz gdtoquiz = new GameActionDTOQuiz
                        {
                            gameAction = GameAction.FinishedAnswers
                        };
                        SendGameData(gdtoquiz);
                        //isPlayerAnswered = true;
                        RunOnUiThread(() =>
                        {
                            WaitingForCategory();
                        });
                    }
                    }
            }
        }


        private List<Category> drawRandomCategories()
        {
            List<Category> shuffledCategories = questionsDatabase.OrderBy(x => random.Next()).ToList();
            var selectedCategories = shuffledCategories.Take(2).ToList();


            while (selectedCategories.Any(category => previousCategories?.Any(prevCategory => prevCategory.Id == category.Id) ?? false))
            {
                // If any category is found in previousCategories, redraw the categories
                shuffledCategories = questionsDatabase.OrderBy(x => random.Next()).ToList();
                selectedCategories = shuffledCategories.Take(2).ToList();
            }

            foreach (var category in selectedCategories)
            {
                var shuffledQuestions = category.Questions.OrderBy(x => random.Next()).ToList();
                var selectedQuestions = shuffledQuestions.Take(2).ToList();

                foreach (var question in selectedQuestions)
                {
                    question.Answers = question.Answers.OrderBy(x => random.Next()).ToList();
                }

                category.Questions = selectedQuestions;
            }


            return selectedCategories;
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
                    intent.PutExtra("Game", "Quiz");
                    StartActivity(intent);
                    Finish();
                });
                progressDialog.Show();
            });
        }


        private void FinishedDialogShow()
        {
            AndroidX.AppCompat.App.AlertDialog.Builder alertDialogBuilder = new AndroidX.AppCompat.App.AlertDialog.Builder(this);

            string status = "";

            if(otherPlayerCorrectAnswers > totalCorrectAnswersCount)
            {
                status = "Przegrałeś";
            } else if(totalCorrectAnswersCount > otherPlayerCorrectAnswers)
            {
                status = "Wygrałeś";
            } else
            {
                status = "Remis";
            }

            alertDialogBuilder.SetMessage($"{status}! Uzyskałeś {totalCorrectAnswersCount} pkt w całym Quizie, natomiast przeciwnik uzyskał {otherPlayerCorrectAnswers} pkt! \n Czy chcesz zagrać ponownie?");
            alertDialogBuilder.SetCancelable(false);

            alertDialogBuilder.SetPositiveButton("Tak", (senderAlert, args) =>
            {
                wantPlayAgain = true;
                GameActionDTOQuiz gameActionCallback = new GameActionDTOQuiz
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
                intent.PutExtra("Game", "Quiz");
                StartActivity(intent);

                Finish();
            });

            // Create and show the dialog
            AndroidX.AppCompat.App.AlertDialog alertDialog = alertDialogBuilder.Create();
            alertDialog.Show();
        }

        private void RestartGame()
        {
            DefaultValues();
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
                        var receivedObject = JsonConvert.DeserializeObject<GameActionDTOQuiz>(receivedData);
                        
                        Array.Clear(buffer, 0, buffer.Length);
                        if (receivedObject != null)
                        {
                          /*  System.Diagnostics.Debug.WriteLine("Otrzymano obiket: " + receivedObject.gameAction);*/
                            if(receivedObject.gameAction == GameAction.ChoseCategory)
                            {
                                playerState = PlayerState.Turn;
                                previousCategories.Add(receivedObject.ChosenCategory);
                                currentCategory = receivedObject.ChosenCategory;
                                RunOnUiThread(() =>
                                {
                                    ShowQuestions(currentCategory);
                                });
                            } else if(receivedObject.gameAction == GameAction.Result)
                            {
                                otherPlayerCorrectAnswers = receivedObject.result;
                               
                                OtherPlayerFinished();
                            } else if(receivedObject.gameAction == GameAction.PlayAgain)
                            {
                                GamePlayAgain();
                            } else if(receivedObject.gameAction == GameAction.FinishedAnswers)
                            {
                                isOtherPlayerFinishedAnswers = true;
                                WaitingForOtherPlayer();
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
                    intent.PutExtra("Game", "Quiz");
                    context.StartActivity(intent);
                    _activity.Finish();
                });
            }
        }


        private void SendGameData(GameActionDTOQuiz gameActionDTO)
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