using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Microfilm_Dafater_D810.forms
{
    public partial class main_form : Form
    {
        public string user_per_name = "";
        public string user_code = "";
        public string user_group = "";

        public decimal scanner_code = 0;

        public main_form()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            open_task();
        }
        void bind_maingrid()
        {
            this.tasks12TableAdapter.Fill(this.dataSet1.Tasks12);
            dataGridView1.Focus();
        }
        private void panel3_Click(object sender, EventArgs e)
        {
            this.Close();

           // Application.Exit();
        }

        private void main_form_Load(object sender, EventArgs e)
        {
            // TODO: This line of code loads data into the 'dataSet1.sub_group_now' table. You can move, or remove it, as needed.
         //   this.sub_group_nowTableAdapter.Fill(this.dataSet1.sub_group_now);
            // TODO: This line of code loads data into the 'dataSet1.Tasks12' table. You can move, or remove it, as needed.
            //this.tasks12TableAdapter.Fill(this.dataSet1.Tasks12);
            bind_maingrid();

        }

        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {
            comboBox2.Visible = checkBox2.Checked;
        }

        private void checkBox3_CheckedChanged(object sender, EventArgs e)
        {
            textBox1.Visible = checkBox3.Checked;
        }

        private void panel4_Click(object sender, EventArgs e)
        {
            change_pw f = new change_pw();
            f.Show();
        }

        private void main_form_Shown(object sender, EventArgs e)
        {
            dataGridView1.Focus();
        }
        void  open_task()
        {
            try
            {
                tasweer_daf f = new tasweer_daf();

                f.user_per_name = user_per_name;
                f.user_code = user_code;
                f.user_group = user_group;
                f.task_code = dataGridView1.SelectedRows[0].Cells["tASKIDDataGridViewTextBoxColumn"].Value.ToString();
                f.task_status = dataGridView1.SelectedRows[0].Cells["tASKSTATUSDataGridViewTextBoxColumn"].Value.ToString();
                 f.daf_code = dataGridView1.SelectedRows[0].Cells["dAFTARCODEDataGridViewTextBoxColumn"].Value.ToString();

                f.scanner_code = scanner_code;

                f.from_page = Convert.ToDecimal(dataGridView1.SelectedRows[0].Cells["fROMPAGEDataGridViewTextBoxColumn"].Value.ToString());
                f.to_page = Convert.ToDecimal(dataGridView1.SelectedRows[0].Cells["tOPAGEDataGridViewTextBoxColumn"].Value.ToString());

                f.sub_lbl.Text = dataGridView1.SelectedRows[0].Cells["sUBNAMEDAFTARDataGridViewTextBoxColumn"].Value.ToString();
                f.daf_nm_lbl.Text = dataGridView1.SelectedRows[0].Cells["dAFTARNAMEDataGridViewTextBoxColumn"].Value.ToString();
                f.task_nm_lbl.Text = dataGridView1.SelectedRows[0].Cells["tASKNAMEDataGridViewTextBoxColumn"].Value.ToString();


                f.ShowDialog();
                bind_maingrid();
            }
            catch
            {
                MessageBox.Show("أختر المهمة");
            }
        }
        private void dataGridView1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                open_task();
            }
        }

        private void panel10_Click(object sender, EventArgs e)
        {
            bind_maingrid();
        }
    }
}
