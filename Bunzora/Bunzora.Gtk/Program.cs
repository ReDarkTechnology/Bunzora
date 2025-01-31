using System;
using Eto.Forms;

namespace Bunzora.Gtk
{
    internal class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            Stylus.CreateStylus = () => new LinuxStylus();

            MainScene.Create(Eto.Platforms.Gtk, 1280, 720, "Bunzora");
            MainScene.Instance.RunAndWait();
        }
    }
}
