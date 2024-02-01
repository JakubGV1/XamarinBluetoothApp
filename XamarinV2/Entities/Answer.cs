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

namespace XamarinV2.Entities
{
    public class Answer
    {
        public string Text { get; set; }
        public bool IsCorrect { get; set; }
    }
}