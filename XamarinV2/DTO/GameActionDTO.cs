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
using XamarinV2.Enums;
using GameState = XamarinV2.Enums.GameState;

namespace XamarinV2.DTO
{
    public class GameActionDTO
    {
        public int row = 0;
        public int column = 0;
        public GameState gameState;
        public GameAction gameAction;
        public bool isShootedCallback = false;
    }
}