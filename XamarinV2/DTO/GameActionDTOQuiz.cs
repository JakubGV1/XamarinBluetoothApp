using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using XamarinV2.Entities;
using XamarinV2.Enums;
using GameState = XamarinV2.Enums.GameState;

namespace XamarinV2.DTO
{
    public class GameActionDTOQuiz
    {
        public GameAction gameAction;
        public Category ChosenCategory { get; set; }
        public int result { get; set; }
    }
}