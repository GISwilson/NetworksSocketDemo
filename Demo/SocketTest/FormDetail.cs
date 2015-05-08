using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace SocketTest
{
    public partial class FormDetail : Form
    {
        private DataTable dt;

        public FormDetail(DataTable dt)
        {
            InitializeComponent();
            this.dt = dt;
            this.dataGridView1.DataSource = dt;
        }

        private void button1_Click(object sender, EventArgs e)
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
