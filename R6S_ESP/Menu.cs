using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;
using System.ServiceProcess;

namespace R6S_ESP
{
    public partial class Menu : Form
    {
        Overlay overlay;
        bool AlreadyRunning = false;
        bool BEbypass;

        public Menu(bool BE)
        {
            InitializeComponent();
            BEbypass = BE;
        }

        private void startbtn_Click(object sender, EventArgs e)
        {
            overlay = new Overlay(BEbypass);
            overlay.Show();
            start_btn.Enabled = false;
            stop_btn.Enabled = true;
        }

        private void stopbtn_Click(object sender, EventArgs e)
        {
            overlay.Close();
            start_btn.Enabled = true;
            stop_btn.Enabled = false;
        }

        private void timer_Tick(object sender, EventArgs e)
        {
            if(Process.GetProcessesByName("RainbowSix").Length < 1)
            {
                start_btn.Enabled = false;
                stop_btn.Enabled = false;
                label1.Text = "Not Running";
                label1.ForeColor = Color.Red;
                label3.Text = "Not Running";
                label3.ForeColor = Color.Red;
                AlreadyRunning = false;
            }
            else
            {
                if (!AlreadyRunning)
                {
                    start_btn.Enabled = true;
                    stop_btn.Enabled = false;
                    label1.Text = "Running";
                    label1.ForeColor = Color.Green;
                    if (BEbypass)
                    {
                        label3.Text = "Bypassed";
                        label3.ForeColor = Color.Green;
                    }
                    else
                    {
                        label3.Text = "Stopped";
                        label3.ForeColor = Color.Green;
                    }
                    AlreadyRunning = true;
                }
            }
        }
    }
}
