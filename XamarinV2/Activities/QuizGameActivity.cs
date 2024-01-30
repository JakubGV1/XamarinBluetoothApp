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
        private int maxQuestions = 3;

        private int correctAnswersCount;
        private int rounds;
        private int currentRound;

        private readonly Random random = new Random();

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.quiz_game);

            _activity = this;

            Initialize();
            ShowCategoryMenu();


            string connectedDevice = Intent.GetStringExtra("Connected-Device");
            if (CustomBluetooth.Instance.GetConnectedSocket() != null && connectedDevice != null)
            {
                _connectedSocket = CustomBluetooth.Instance.GetConnectedSocket();
                if (_connectedSocket != null && _connectedSocket.IsConnected)
                {
                    isConnection = true;
                    Task.Run(() => HandleSocketInput(this, _connectedSocket));
                    Toast.MakeText(this, "Nawiązano połączenie", ToastLength.Short).Show();

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
            isCategoryChosen = false;
            currentCategory = null;
            previousCategories = new List<Category>();
            currentCategoriesToChoose = new List<Category>();
            rounds = 3;
            currentRound = 1;
            currentQuestion = 0;
            LoadQuestions();
        }

        private void LoadQuestions()
        {
            using (StreamReader streamReader = new StreamReader(Android.App.Application.Context.Assets.Open("quizdata.json")))
            {
                string jsonContent = streamReader.ReadToEnd();
                questionsDatabase = JsonConvert.DeserializeObject<List<Category>>(jsonContent);
            }
            if (questionsDatabase != null)
            {
                foreach(var category in questionsDatabase)
                {
                   System.Diagnostics.Debug.WriteLine("KATEGORIA !@:" + category.CategoryName);
                }
            }
        }

        private void ShowCategoryMenu()
        {
            questionText.Text = "";
            statusText.Text = "Wybierz kategorię";
            thirdButton.Visibility = ViewStates.Invisible;
            fourthButton.Visibility = ViewStates.Invisible;
            currentCategoriesToChoose.Clear();
            currentCategoriesToChoose = drawRandomCategories();
            firstButton.Text = currentCategoriesToChoose[0].CategoryName;
            secondButton.Text = currentCategoriesToChoose[1].CategoryName;
        }

        private void WaitingForCategory()
        {
            statusText.Text = "Oczekiwanie na wybór kategorii...";
            questionText.Text = "";
            buttonsGroup.Visibility = ViewStates.Invisible;
        }

        private void ButtonClick(int buttonIndex)
        {
            if(currentCategory== null && playerState == PlayerState.Turn)
            {
                System.Diagnostics.Debug.WriteLine($"Wybrano: {currentCategoriesToChoose[buttonIndex].CategoryName}");
                currentCategory = currentCategoriesToChoose[buttonIndex];
                previousCategories.Add(currentCategory);
                ShowQuestions(currentCategory);
            } else
            {
                if(currentQuestion <= maxQuestions)
                {
                    ShowQuestions(currentCategory);
                } else
                {
             
                }
            }
        }

        private void ShowQuestions(Category category)
        {
            if (currentQuestion <= maxQuestions)
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

                    currentQuestion++;
                });
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

            return selectedCategories;
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
                        var receivedObject = JsonConvert.DeserializeObject<GameActionDTO>(receivedData);
                        
                        Array.Clear(buffer, 0, buffer.Length);
                        if (receivedObject != null)
                        {

                            
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


        private void SendGameData(GameActionDTO gameActionDTO)
        {
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