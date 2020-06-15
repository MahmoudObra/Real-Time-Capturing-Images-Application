using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Data.OracleClient;

namespace Microfilm_Dafater_D810
{
    
    public partial class Form1 : Form
    {
        public static string activeUser = "";

        string oradb = "Data Source=unix;Persist Security Info=True;User ID=segelat;Password=segelatadminsite;Unicode=True";

        static DataSet ds = new DataSet();
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }
        void open_run()
        {
            if (textBox1.Text == string.Empty || textBox2.Text == string.Empty)
            {
                MessageBox.Show("أدخل أسم المستخدم وكلمة المرور");
            }
            else
            {
                try
                {
                   
                    OracleConnection con = new OracleConnection(oradb);
                    OracleCommand cmd = new OracleCommand();
                    cmd.CommandText = "select * from users where user_name  ='" + textBox1.Text + "' and user_password = '" + textBox2.Text + "'";
                    cmd.Connection = con;
                    con.Open();
                    OracleDataReader dr = cmd.ExecuteReader();

                  

                    // DataSet ds = new System.Data.DataSet();
                    ds.Clear();
                    OracleDataAdapter da = new OracleDataAdapter(cmd);
                    da.Fill(ds);

                    if (dr.HasRows)
                    {
                        if (ds.Tables[0].Rows[0]["GROUP_CODE"].ToString() == "1" || ds.Tables[0].Rows[0]["GROUP_CODE"].ToString() == "606060" || ds.Tables[0].Rows[0]["GROUP_CODE"].ToString() == "606062")
                        {

                            //Forms.main_form_1s f = new Forms.main_form_1s();
                            //f.user_name = ds.Tables[0].Rows[0]["user_name"].ToString();
                            //f.scanner_code = 1;
                            //f.ShowDialog();

                            activeUser = textBox1.Text;

                            textBox2.Clear() ;

                            forms.main_form f = new forms.main_form();



                            // f.mil_no = textBox1.Text;

                            f.user_per_name = ds.Tables[0].Rows[0]["USER_DESC"].ToString();
                            f.user_code = ds.Tables[0].Rows[0]["USER_CODE"].ToString();
                            f.user_group = ds.Tables[0].Rows[0]["GROUP_CODE"].ToString();

                            f.scanner_code = 1;
                            f.ShowDialog();
                        }
                        else
                        {
                            MessageBox.Show("خارج صلاحيات المستخدم" + ds.Tables[0].Rows[0]["GROUP_CODE"].ToString());
                        }

                    }
                    else
                    {
                        MessageBox.Show("تأكد من أسم المستخدم وكلمة المرور");
                    }
                }
                catch
                {
                    MessageBox.Show("فقد الإتصال بقاعدة البيانات");
                }
            }



        }
        private void panel1_Click(object sender, EventArgs e)
        {
            open_run();
        }
        private void panel6_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void textBox1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
                open_run();
        }

        private void Form1_Shown(object sender, EventArgs e)
        {
            textBox1.Focus();
        }

        private void panel6_Click_1(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void panel6_Paint(object sender, PaintEventArgs e)
        {

        }
    }
}
