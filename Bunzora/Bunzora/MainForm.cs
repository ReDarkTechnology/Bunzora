using System;
using System.Threading.Tasks;
using Eto.Drawing;
using Eto.Forms;

namespace Bunzora
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
            Task.Run(() => new MainScene());
        }
    }
}
