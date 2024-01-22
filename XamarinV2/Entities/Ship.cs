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
    public class Ship
    {
        public int Size { get; }
        public List<Positions> positions { get; set; }
        public bool IsDestroyed => positions.All(position => position.isHit);

        public Ship(int size)
        {
            Size = size;
            positions = new List<Positions>();
        }
    }

    public class Positions
    {
        public int x;
        public int y;
        public bool isHit;
    }
}