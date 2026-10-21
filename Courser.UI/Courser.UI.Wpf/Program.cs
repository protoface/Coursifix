using System;


namespace Courser.UI.Wpf
{
    class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            EntryPoint.Enter(new Eto.Forms.Application(Eto.Platforms.Wpf));
        }
    }
}
