using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;

namespace NgrokLauncher
{
    public partial class MainForm : Form
    {
        private Ngrok ngrok = new Ngrok();

        public MainForm()
        {
            InitializeComponent();
        }

        private async void MainForm_Shown(object sender, EventArgs e)
        {
            if (!File.Exists(Ngrok.FileNgrokExecutable))
            {
                var dialog = MessageBox.Show("This application requires ngrok.exe\nDownload now?", Text, MessageBoxButtons.YesNo, MessageBoxIcon.Error);
                if (dialog == DialogResult.Yes)
                    Process.Start("https://ngrok.com/download");

                Application.Exit();
            }

            await ngrok.Stop();

            button1.Enabled = false;
            button2.Enabled = false;
            groupBox4.Enabled = false;

            var config = ngrok.Load();
            textBox1.Text = config.authtoken;
            textBox2.Text = config.tunnels.website.addr.ToString();
            textBox3.Text = config.tunnels.ssh.addr.ToString();
            checkBox1.Checked = config.run_website;
            checkBox2.Checked = config.run_ssh;
        }

        private void LockAll(bool value)
        {
            button1.Enabled = !value;
            button2.Enabled = value;
            groupBox1.Enabled = !value;
            groupBox2.Enabled = !value;
            groupBox4.Enabled = value;

            // inside grupbox4
            textBox4.Text = string.Empty;
            textBox5.Text = string.Empty;
            textBox6.Text = string.Empty;
            button3.Enabled = false;
            button4.Enabled = false;
            button5.Enabled = false;
            button6.Enabled = false;
            button7.Enabled = false;
            button8.Enabled = false;
        }

        private void SaveConfigs()
        {
            var token = textBox1.Text;

            var http = 80;
            int.TryParse(textBox2.Text, out http);
            textBox2.Text = http.ToString();

            var tcp = 22;
            int.TryParse(textBox3.Text, out tcp);
            textBox3.Text = tcp.ToString();

            ngrok.Save(token, http, tcp, checkBox1.Checked, checkBox2.Checked);
        }

        private async void button1_Click(object sender, EventArgs e)
        {
            LockAll(true);
            SaveConfigs();

            int code = 0;
            if (checkBox1.Checked && !checkBox2.Checked) code = 1;
            else if (!checkBox1.Checked && checkBox2.Checked) code = 2;
            else code = 0;

            timer1.Enabled = true;
            await ngrok.Start(code);

            LockAll(false);
        }

        private async void button2_Click(object sender, EventArgs e)
        {
            button2.Enabled = false;

            await ngrok.Stop();
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            var json = ngrok.GetResponse();
            if (json == null) return;

            foreach (var tunnel in json.tunnels)
            {
                if (tunnel.proto == "http")
                {
                    textBox4.Text = tunnel.public_url;
                    button3.Enabled = true;
                    button6.Enabled = true;
                }
                if (tunnel.proto == "https")
                {
                    textBox5.Text = tunnel.public_url;
                    button4.Enabled = true;
                    button7.Enabled = true;
                }
                if (tunnel.proto == "tcp")
                {
                    textBox6.Text = tunnel.public_url;
                    button5.Enabled = true;
                    button8.Enabled = true;
                }
            }

            if (checkBox1.Checked && checkBox2.Checked)
            {
                if (button3.Enabled && button4.Enabled && button5.Enabled)
                    timer1.Enabled = false;
            }
            else if (checkBox1.Checked)
            {
                if (button3.Enabled && button4.Enabled)
                    timer1.Enabled = false;
            }
            else if (checkBox2.Checked)
            {
                if (button5.Enabled)
                    timer1.Enabled = false;
            }
        }

        private void button_url(object sender, EventArgs e)
        {
            var button = sender as Button;
            switch (button.Tag)
            {
                case "1c":
                    Clipboard.SetText(textBox4.Text);
                    break;

                case "2c":
                    Clipboard.SetText(textBox5.Text);
                    break;

                case "3c":
                    Clipboard.SetText(textBox6.Text);
                    break;

                case "1o":
                    Process.Start(textBox4.Text);
                    break;

                case "2o":
                    Process.Start(textBox5.Text);
                    break;

                case "3o":
                    Process.Start(textBox6.Text);
                    break;
            }
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            button1.Enabled = !string.IsNullOrWhiteSpace(textBox1.Text);
        }

        private void textDigit_KeyPress(object sender, KeyPressEventArgs e)
        {
            var k = e.KeyChar;
            var IsDigitBackDelete = char.IsDigit(k) || (k == (char)Keys.Back) || (k == (char)Keys.Delete);
            e.Handled = !IsDigitBackDelete || (k == '.');
        }

        private void checkBox_CheckedChanged(object sender, EventArgs e)
        {
            if (!checkBox1.Checked && !checkBox2.Checked)
            {
                if (sender == checkBox1) checkBox2.Checked = true;
                else checkBox1.Checked = true;
            }
        }

        private void MainForm_Resize(object sender, EventArgs e)
        {
            if ((FormWindowState.Minimized == this.WindowState) && !button1.Enabled)
            {
                notifyIcon1.Visible = true;
                this.Hide();
            }
        }

        private void notifyIcon1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                this.Show();
                notifyIcon1.Visible = false;
                this.WindowState = FormWindowState.Normal;
            }
        }
    }
}