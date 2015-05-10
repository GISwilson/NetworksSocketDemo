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

        private MyServer myServer;//客户端的通信对象
        private const string TYPE = "NetworkSocket开源组件";
        private bool isConeneted = false;//是否已连接服务器
        private static object _sync = new object();//同步锁

        private DataTable dtBigDataTotalTest;//所有大数据量测试的数据
        private DataTable dtBigDataShow;//用于显示大数据量测试结果的数据

        private DataTable dtFrequencyTotalTest;//所有高频通信测试数据
        private DataTable dtFrequencyShow;//用于显示高频通信测试结果的数据

        private void FormTest_Load(object sender, EventArgs e)
        {
            dtBigDataTotalTest = new DataTable();
            dtBigDataTotalTest.Columns.Add("序号");
            dtBigDataTotalTest.Columns.Add("数据大小（kb）");
            dtBigDataTotalTest.Columns.Add("传输时间（毫秒）");

            dtBigDataShow = new DataTable();
            dtBigDataShow.Columns.Add("执行次数");
            dtBigDataShow.Columns.Add("数据大小（kb）");
            dtBigDataShow.Columns.Add("技术类型");
            dtBigDataShow.Columns.Add("平均传输时间(毫秒)");
            dtBigDataShow.Columns.Add("标准差（毫秒）");
            dtBigDataShow.Columns.Add("平均传输速度（Mb/s)");

            dtFrequencyTotalTest = new DataTable();
            dtFrequencyTotalTest.Columns.Add("序号");
            dtFrequencyTotalTest.Columns.Add("时间间隔(毫秒)");
            dtFrequencyTotalTest.Columns.Add("发送的数据大小（字节）");
            dtFrequencyTotalTest.Columns.Add("接收的数据大小（字节）");
            //dtFrequencyTotalTest.Columns.Add("传输时间(毫秒)");

            dtFrequencyShow = new DataTable();
            dtFrequencyShow.Columns.Add("执行次数");
            dtFrequencyShow.Columns.Add("时间间隔（ms）");
            dtFrequencyShow.Columns.Add("丢包数");
            dtFrequencyShow.Columns.Add("丢包率");
        }

        #region 服务器配置
        private void button1_Click(object sender, EventArgs e)
        {
            myServer = new MyServer();
            var splitIps = textBox1.Text.Split('.');

            System.Net.IPAddress ip = System.Net.IPAddress.Loopback;
            System.Net.IPAddress.TryParse(textBox1.Text, out ip);
            int port = 4502;
            int.TryParse(textBox2.Text, out port);
            var endPoint = new System.Net.IPEndPoint(ip, port);
            var state = myServer.Connect(endPoint);
            while (!state.IsCompleted)
            {
            }
            isConeneted = state.Result;
            MessageBox.Show(string.Format("连接{0}！", isConeneted ? "成功" : "失败"));
        }


        #endregion

        #region 大数据量通信测试

        private void button2_Click(object sender, EventArgs e)
        {
            if (!isConeneted)
            {
                MessageBox.Show("请先连接服务器");
                return;
            }
            Action<int, int> a = (strNum, times) => { TestBigData(strNum, times); };
            IAsyncResult result = a.BeginInvoke(Convert.ToInt32(numericUpDown2.Value) * 1024, Convert.ToInt32(numericUpDown1.Value),
                new AsyncCallback((asyncResult) =>
                {
                    if (asyncResult.IsCompleted)
                    {
                        MessageBox.Show("测试完毕！");
                    }
                }),
                null);
            // a.EndInvoke(result);
            //while (!result.IsCompleted)
            //{
            //}

            //TestBigData(Convert.ToInt32(numericUpDown2.Value),Convert.ToInt32(numericUpDown1.Value));
        }

        /// <summary>
        /// 测试数据量大的情形
        /// </summary>
        void TestBigData(int strNum, int times)
        {
            int statisticNode = 1;//统计节点
            List<long> values = new List<long>();
            for (int i = 0; i < times; i++)
            {
                Thread.Sleep(100);
                string msg = new string('a', strNum);
                Console.WriteLine(i.ToString());
                var watch = Stopwatch.StartNew();
                watch.Start();
                var aa = myServer.GetItself(msg);
                aa.Wait();
                //while (!aa.IsCompleted)
                //{
                //}
                watch.Stop();
                //统计数据记录
                values.Add(watch.ElapsedMilliseconds/2);
                //整体数据记录
                DataRow row = dtBigDataTotalTest.NewRow();
                row[0] = i;
                row[1] = strNum / 1024;
                row[2] = watch.ElapsedMilliseconds /2;

                dtBigDataTotalTest.Rows.Add(row);
                //更新显示数据
                if (i == statisticNode - 1)
                {
                    DataRow newRow = dtBigDataShow.NewRow();
                    newRow[0] = statisticNode;
                    newRow[1] = strNum / 1024;
                    newRow[2] = TYPE;
                    newRow[3] = values.GetAverage().ToString("n");
                    newRow[4] = values.GetBiaozhunCha().ToString("n");
                    //newRow[5] = strNum / 1024 * 1.0 / values.GetAverage();
                    newRow[5] = (strNum / 1024 / values.GetAverage() * 1000 / 1024).ToString("n");

                    dtBigDataShow.Rows.Add(newRow);
                    //实时显示数据
                    this.Invoke(new Action(() =>
                    {
                        if (!(this.dataGridView1.Disposing || this.dataGridView1.IsDisposed))
                        {
                            this.dataGridView1.DataSource = null;
                            this.dataGridView1.DataSource = dtBigDataShow;
                            //this.dataGridView1.AutoResizeColumns();
                            this.dataGridView1.Invalidate();
                        }
                    }));
                    //更新统计节点
                    statisticNode *= 10;
                }
                //this.dataGridView1.DataSource = dt;

                Thread.Sleep(10);
                //updateDgv.Invoke(dt);
                //dataGridView1.Update();

            }
            //DataRow rowSum = dtBigDataTotalTest.NewRow();
            //rowSum[0] = strNum;
            //rowSum[1] = "均值";
            //rowSum[2] = string.Format("{0}±{1}", values.GetAverage().ToString(), values.GetBiaozhunCha().ToString());
            //dtBigDataTotalTest.Rows.Add(rowSum);
            //this.Invoke(new Action(delegate() { this.dataGridView1.DataSource = dtBigDataTotalTest; this.dataGridView1.Invalidate(); }));
        }

        private void button3_Click(object sender, EventArgs e)
        {
            SaveFileDialog dialog = new SaveFileDialog();
            dialog.Filter = "Excel文件|*.xls";
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                ExcelRender.ExcelRender.RenderToExcel(dtBigDataShow, dialog.FileName);
            }
        }

        private void button6_Click(object sender, EventArgs e)
        {
            dtBigDataTotalTest.Rows.Clear();
            dtBigDataShow.Rows.Clear();
            dataGridView1.DataSource = dtBigDataShow;
            dataGridView1.Invalidate();
        }

        //详表
        private void button8_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < dtBigDataTotalTest.Rows.Count; i++)
            {
                dtBigDataTotalTest.Rows[i][0] = i + 1;
            }
            FormDetail frm = new FormDetail(dtBigDataTotalTest);
            frm.Show();
        }
        #endregion

        #region 高频通信测试
        private void button5_Click(object sender, EventArgs e)
        {
            if (!isConeneted)
            {
                MessageBox.Show("请先连接服务器");
                return;
            }
            Action<int, int, int> a = (strNum, executionTime, interval) => { TestTime(strNum, executionTime, interval); };
            a.BeginInvoke(Convert.ToInt32(numericUpDown3.Value), Convert.ToInt32(numericUpDown5.Value), Convert.ToInt32(numericUpDown4.Value), null, null);
        }

        /// <summary>
        /// 测试短时间高频发送数据
        /// </summary>
        void TestTime(int strNum, int executionTime, int interval)
        {
            int statisticNode = 10;//统计时间节点
            List<long> values = new List<long>();
            //List<string> strs = new List<string>();
            //for (int i = 0; i < executionNum; i++)
            //{
            //    strs.Add(new string('a', strNum + i));
            //}

            myServer.ResetServerNum();
            System.Timers.Timer timer = new System.Timers.Timer();
            timer.Interval = interval;
            int index = 0;
            //var watch = Stopwatch.StartNew();
            string data = new string('a', strNum);
            var totalWatch = Stopwatch.StartNew();

            timer.Elapsed += (sender, e) =>
            {
                //long runMiliseconds = totalWatch.ElapsedMilliseconds;
                //if (runMiliseconds >= statisticNode)
                //{
                //    //更新统计时间节点
                //    statisticNode += 1000;
                //    int totalNum = dtFrequencyTotalTest.Rows.Count;
                //    long wrongNum = values.Sum();
                //    DataRow newRow = dtFrequencyShow.NewRow();
                //    newRow[0] = statisticNode / 1000;
                //    newRow[1] = interval;
                //    newRow[2] = wrongNum;
                //    newRow[3] = (wrongNum * 1.0 / totalNum).ToString("p");

                //    dtFrequencyShow.Rows.Add(newRow);

                //    this.Invoke(new Action(delegate()
                //    {
                //        if (!(this.dataGridView2.Disposing || this.dataGridView2.IsDisposed))
                //        {
                //            this.dataGridView2.DataSource = null;
                //            this.dataGridView2.DataSource = dtFrequencyShow;
                //            this.dataGridView2.Invalidate();
                //        }
                //    }));

                //}
                if (totalWatch.ElapsedMilliseconds >= executionTime * 1000)
                {
                    timer.Stop();
                }
                else
                {
                    //watch.Start();
                    DataRow row = dtFrequencyTotalTest.NewRow();
                    data = new string('a', strNum + new Random().Next(1, 100));
                    lock (_sync)
                    {
                        //watch.Stop();
                        row[0] = index;
                        row[1] = interval;
                        row[2] = data.Length;
                        var aa = myServer.SendStrData(data);
                        while (!aa.IsCompleted)
                        {
                        }
                        row[3] = aa.Result.Length;
                        values.Add(row[2].ToString() == row[3].ToString() ? 0 : 1);
                        if (values.Count >= statisticNode)
                        {

                            int totalNum = dtFrequencyTotalTest.Rows.Count;
                            long wrongNum = values.Sum();
                            DataRow newRow = dtFrequencyShow.NewRow();
                            newRow[0] = statisticNode;
                            newRow[1] = interval;
                            newRow[2] = wrongNum;
                            newRow[3] = (wrongNum * 1.0 / totalNum).ToString("p");
                            statisticNode += 10;
                            dtFrequencyShow.Rows.Add(newRow);

                            this.Invoke(new Action(delegate()
                            {
                                if (!(this.dataGridView2.Disposing || this.dataGridView2.IsDisposed))
                                {
                                    this.dataGridView2.DataSource = null;
                                    this.dataGridView2.DataSource = dtFrequencyShow;
                                    this.dataGridView2.Invalidate();
                                }
                            }));

                        }
                        //row[2] = watch.ElapsedMilliseconds;
                        dtFrequencyTotalTest.Rows.Add(row);
                        //values.Add(watch.ElapsedMilliseconds);
                    }
                    index++;
                }
            };
            timer.Start();
            totalWatch.Start();
            //下面的循环保证Timer事件执行完毕再添加统计信息
            //while (timer.Enabled)
            //{
            //}
            //Thread.Sleep(1000);
            //DataRow rowSum = dt2.NewRow();
            //rowSum[0] = interval;
            //rowSum[1] = "均值";
            ////rowSum[2] = "";
            ////rowSum[3] = "";
            //rowSum[3] = string.Format("{0}±{1}", values.GetAverage().ToString(), values.GetBiaozhunCha().ToString());
            //dt2.Rows.Add(rowSum);
            //this.Invoke(new Action(delegate() { this.dataGridView2.DataSource = dt2; this.dataGridView2.Invalidate(); }));
        }

        private void button7_Click(object sender, EventArgs e)
        {
            dtFrequencyTotalTest.Rows.Clear();
            dtFrequencyShow.Rows.Clear();
            dataGridView2.DataSource = dtFrequencyShow;
            dataGridView2.Invalidate();
        }

        private void button4_Click(object sender, EventArgs e)
        {
            SaveFileDialog dialog = new SaveFileDialog();
            dialog.Filter = "Excel文件|*.xls";
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                ExcelRender.ExcelRender.RenderToExcel(dtFrequencyShow, dialog.FileName);
            }
        }

        //详表
        private void button9_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < dtFrequencyTotalTest.Rows.Count; i++)
            {
                dtFrequencyTotalTest.Rows[i][0] = i + 1;
            }
            FormDetail frm = new FormDetail(dtFrequencyTotalTest);
            frm.Show();
        }
        #endregion

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


    }


}
