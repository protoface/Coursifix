using System;
using Eto.Forms;

namespace Courser.UI.Mac
{
    class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            EntryPoint.Enter(new Application(Eto.Platforms.Mac64));
        }
    }
}
