using Eto.Forms;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Form = System.Windows.Forms.Form;
using MessageBox = System.Windows.Forms.MessageBox;
using TextBox = System.Windows.Forms.TextBox;

namespace UrbanX.Traffic
{
    public class Interface
    {

    }

    public class UOutputForm : Form
    {
        public TextBox OutputTextBox { get; private set; } = new TextBox();
        public Process AssociatedProcess { get; set; }

        public UOutputForm(int Width=500, int Height=300)
        {
        }

        public UOutputForm(Process process, int Width = 500, int Height = 300)
        {
            InitializeForm(Width, Height);
            this.AssociatedProcess = process;
            this.FormClosing += OutputForm_FormClosing;
        }

        private void InitializeForm(int Width, int Height)
        {
            this.Width = Width;
            this.Height = Height;
            this.Text = "UrbanXTools Traffic";

            OutputTextBox.Dock = DockStyle.Fill;
            OutputTextBox.Multiline = true;
            OutputTextBox.ScrollBars = ScrollBars.Vertical;
            OutputTextBox.ReadOnly = true;
            this.Controls.Add(OutputTextBox);
        }


        public void AppendOutput(string output)
        {
            if (!this.IsDisposed)
            {
                OutputTextBox.AppendText(output + Environment.NewLine);
            }
        }

        private void OutputForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (AssociatedProcess != null && !AssociatedProcess.HasExited)
            {
                AssociatedProcess.Kill();
            }
        }

    }
}
