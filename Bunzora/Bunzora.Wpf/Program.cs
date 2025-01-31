using System;
using System.Threading.Tasks;
using System.Timers;
using Eto.Forms;

namespace Bunzora.Wpf
{
    internal class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            Stylus.CreateStylus = () => new WindowsStylus();

            Task.Run(() =>
            {
                MainScene.Create(Eto.Platforms.Wpf, 1280, 720, "Bunzora");
                MainScene.Instance.OnDisposed += Program_OnDisposed;
                MainScene.Instance.RunAndWait();
            });

            new Application(Eto.Platforms.Wpf).Run();
        }

        private static void Program_OnDisposed()
        {
            Application.Instance.Invoke(() => Application.Instance.Quit());
        }
    }
}
