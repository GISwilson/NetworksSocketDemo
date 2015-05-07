using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ClientApp
{
    public partial class SumForm : Form
    {
        private MyServer myServer;

        public SumForm(MyServer server)
        {
            InitializeComponent();

            this.myServer = server;
            this.button_Sum.Click += button_Sum_Click;
            this.FormClosed += SumForm_FormClosed;
        }

        private void button_Sum_Click(object sender, EventArgs e)
        {
            var x = 0;
            int y = 0;
            int z = 0;
            backgroundWorker1.RunWorkerAsync();
            int.TryParse(this.textBox1.Text, out x);
            int.TryParse(this.textBox2.Text, out y);
            int.TryParse(this.textBox3.Text, out z);
            Task<int> sum = null;
            for (int i = 0; i < 10000; i++)
            {
                sum = this.myServer.GetSun(x, y, z);
                //Thread.Sleep(1);
            }
            MessageBox.Show("服务器返回：" + sum.Result.ToString());
        }

        private void SumForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            myServer.Dispose();
            Environment.Exit(0);
        }

        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            var x = 0;
            int y = 0;
            int z = 0;

            int.TryParse(this.textBox1.Text, out x);
            int.TryParse(this.textBox2.Text, out y);
            int.TryParse(this.textBox3.Text, out z);
            for (int i = 0; i < 1000; i++)
            {
                var sum = this.myServer.GetSun(x, y, z);
                Thread.Sleep(1);
            }
        }

        private void backgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {

        }
    }
}
