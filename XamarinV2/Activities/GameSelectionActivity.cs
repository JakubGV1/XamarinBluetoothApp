using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using AndroidX.AppCompat.App;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace XamarinV2
{
    [Activity(Label = "Wybór gry")]
    public class GameSelectionActivity : AppCompatActivity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
             SetContentView(Resource.Layout.selection_menu);

            Button btnShips = FindViewById<Button>(Resource.Id.btnBattleships);
                btnShips.Click += (sender, e) => {
                  ChangeActivity("Statki");
              };


            Button btnQuiz = FindViewById<Button>(Resource.Id.btnQuiz);
            btnQuiz.Click += (sender, e) => {
                 ChangeActivity("Quiz");
        //        Intent intent = new Intent(this, typeof(QuizGameActivity));
          //      intent.SetFlags(ActivityFlags.ClearTop);
            //    StartActivity(intent);
              //  Finish();
            };
            Button btnThird = FindViewById<Button>(Resource.Id.btnSequence);
            btnThird.Click += (sender, e) => {
                ChangeActivity("Sekwencje");
            };

            Button btnBackSelect = FindViewById<Button>(Resource.Id.btnBackSelect);
            btnBackSelect.Click += (sender, e) => {
                BackButton();
            };
        }

        private void BackButton()
        {
            Intent intent = new Intent(this, typeof(MainActivity));
            intent.SetFlags(ActivityFlags.ClearTop);
            StartActivity(intent);
            Finish();
        }

        private void ChangeActivity(string extras)
        {
            var intent = new Intent(this, typeof(DiscoveredDevicesActivity));
            intent.PutExtra("Game", extras);
            StartActivity(intent);
        }

    

    
    }

}