using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace SocketTest
{
    public partial class FormTest : Form
    {
        public FormTest()
        {
            InitializeComponent();
        }

        private MyServer myServer;
        private DataTable dt;

        

        /// <summary>
        /// 测试短时间高频发送数据
        /// </summary>
        void TestTime()
        {
            //myServer.ResetServerNum();
            //System.Timers.Timer timer = new System.Timers.Timer();
            //int count = 1000;
            //timer.Interval = 1;
            //int index = 1;
            //string data = new string('a', 1000);
            //timer.Elapsed += (sender, e) =>
            //{
            //    if (index > count)
            //    {
            //        timer.Stop();
            //    }
            //    else
            //    {
            //        Console.WriteLine(index.ToString());
            //        myServer.SendStrData(index.ToString());
            //        lock (data)
            //        {
            //            index++;
            //            data += new string('a', index);
            //        }
            //    }
            //};
            //timer.Start();
        }

        /// <summary>
        /// 测试长时间连接的可靠性
        /// </summary>
        void TestLongConnect()
        {
            System.Timers.Timer timer = new System.Timers.Timer();
            timer.Interval = 1000000;
            timer.Elapsed += (sender, e) =>
            {
                myServer.SendStrData(DateTime.Now.ToLongTimeString());
            };
            timer.Start();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            myServer = new MyServer();
            var splitIps=textBox1.Text.Split('.');

            System.Net.IPAddress ip = System.Net.IPAddress.Loopback;
            System.Net.IPAddress.TryParse(textBox1.Text, out ip);
            int port = 4502;
            int.TryParse(textBox2.Text, out port);
            var endPoint = new System.Net.IPEndPoint(ip, port);
            var state = myServer.Connect(endPoint);
            while (!state.IsCompleted)
            {
            }
            MessageBox.Show(string.Format("连接{0}！", state.Result ? "成功" : "失败"));
        }

        private void button2_Click(object sender, EventArgs e)
        {
            TestBigData(Convert.ToInt32(numericUpDown2.Value),Convert.ToInt32(numericUpDown1.Value));
        }

        /// <summary>
        /// 测试数据量大的情形
        /// </summary>
        void TestBigData(int startValue, int times)
        {
            List<long> values = new List<long>();
            for (int i = 0; i < times; i++)
            {
                Thread.Sleep(10);
                string msg = new string('a', startValue);
                //List<int> data = new List<int>(count);
                //for (int j = 0; j < count; j++)
                //{
                //    data.Add(j);
                //}
                Console.WriteLine(i.ToString());
                var watch = Stopwatch.StartNew();
                watch.Start();
                //var aa = myServer.GetItself(data);
                var aa = myServer.GetItself(msg);
                while (!aa.IsCompleted)
                {
                }
                watch.Stop();
                DataRow row = dt.NewRow();
                row[0] = startValue;
                row[1] = i;
                row[2] = watch.ElapsedMilliseconds;
                dt.Rows.Add(row);
                values.Add(watch.ElapsedMilliseconds);
                //var value=myServer.GetCount(data);
                //Console.WriteLine("计算结果为：");
                //Console.WriteLine(value.Result.ToString());
            }
            DataRow rowSum = dt.NewRow();
            rowSum[0] = startValue;
            rowSum[1] = "合计";
            rowSum[2] = string.Format("均值{0}，方差{1}", values.GetAverage().ToString(), values.GetBiaozhunCha().ToString());
            dt.Rows.Add(rowSum);

            dataGridView1.DataSource = dt;
            //for (int count = startValue; count < int.MaxValue; count *= times)
            //{
            //    for (int i = 0; i < 100; i++)
            //    {
            //        Thread.Sleep(10);
            //        string msg = new string('a', count);
            //        //List<int> data = new List<int>(count);
            //        //for (int j = 0; j < count; j++)
            //        //{
            //        //    data.Add(j);
            //        //}
            //        Console.WriteLine(i.ToString());
            //        var watch = Stopwatch.StartNew();
            //        watch.Start();
            //        //var aa = myServer.GetItself(data);
            //        var aa = myServer.GetItself(msg);
            //        while (!aa.IsCompleted)
            //        {
            //            //Thread.Sleep(10);
            //        }
            //        watch.Stop();
            //        Console.WriteLine(string.Format("结果为{0}，耗时：{1}ms", aa.Result.Length.ToString(), watch.ElapsedMilliseconds.ToString()));
            //        times.Add(watch.ElapsedMilliseconds);
            //        //var value=myServer.GetCount(data);
            //        //Console.WriteLine("计算结果为：");
            //        //Console.WriteLine(value.Result.ToString());
            //    }
            //    Console.WriteLine(string.Format("以上测试平均时间为{0}ms，方差为{1}ms", times.GetAverage(), times.GetBiaozhunCha()));
            //}
        }

        private void FormTest_Load(object sender, EventArgs e)
        {
            dt = new DataTable();
            dt.Columns.Add("字符串长度");
            dt.Columns.Add("序号");
            dt.Columns.Add("时间(毫秒)");
        }

        private void button3_Click(object sender, EventArgs e)
        {
            SaveFileDialog dialog = new SaveFileDialog();
            dialog.Filter = "Excel文件|*.xls";
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                ExcelRender.ExcelRender.RenderToExcel(dt, dialog.FileName);
            }
        }
    }

    
}
