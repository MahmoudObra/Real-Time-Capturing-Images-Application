using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Nikon;
using System.IO;
using Microfilm_Dafater_D810.Classes;
using System.Drawing.Imaging;
using System.Diagnostics;
using System.Data.OracleClient;
using AForge.Imaging.Filters;
using AForge.Imaging;

namespace Microfilm_Dafater_D810.forms
{
    public partial class tasweer_daf : Form
    {
        static DataSet ds = new DataSet();

        private NikonManager manager;
        private NikonDevice device;
        private System.Windows.Forms.Timer liveViewTimer;

       
        public string user_per_name = "";
        public string user_code = "";
        public string user_group = "";
        public string task_code = "";
        public string task_status = "";
        public string daf_code = "";

        public decimal scanner_code = 0;

        public decimal from_page = 0;
        public decimal to_page = 0;

       


        int click_no = 0;
        System.Drawing.Rectangle recs;
        int rect_x = 0;
        int rect_y = 0;
        int rect_w = 0;
        int rect_h = 0;

        long rect_x_r = 0;
        long rect_y_r = 0;
        long rect_w_r = 0;
        long rect_h_r = 0;

        int curr_photo = 0;

        public tasweer_daf()
        {
            InitializeComponent();

            // Disable buttons
            not_tasweer_design(false);

            // Initialize live view timer
            liveViewTimer = new System.Windows.Forms.Timer();
            liveViewTimer.Tick += new EventHandler(liveViewTimer_Tick);
            liveViewTimer.Interval = 1000 / 30;

            // Initialize Nikon manager
            manager = new NikonManager("Type0014.md3");
            manager.DeviceAdded += new DeviceAddedDelegate(manager_DeviceAdded);
            manager.DeviceRemoved += new DeviceRemovedDelegate(manager_DeviceRemoved);
        }
        void not_tasweer_design(bool status)
        {
            cap_btn.Visible = status;
            live_btn.Visible = status;
        }
        void manager_DeviceAdded(NikonManager sender, NikonDevice device)
        {
            this.device = device;

            // Set the device name
            label_name.Text = device.Name;
            device.SaveMedia(1);
            // Enable buttons
            not_tasweer_design(true);

            // Hook up device capture events
            device.ImageReady += new ImageReadyDelegate(device_ImageReady);
            device.CaptureComplete += new CaptureCompleteDelegate(device_CaptureComplete);
        }
        void device_ImageReady(NikonDevice sender, NikonImage image)
        {
            if (single_path_tb.Text == string.Empty)
            {
                MessageBox.Show("Select save path");
                return;
            }



            string path = single_path_tb.Text + page_no_lbl.Text + ".jpg";

            if (click_no == 2)
            {

                transfre_to_storage(path, image);
            }
            else
            {
                using (FileStream stream = new FileStream(path, FileMode.Create, FileAccess.Write))
                {
                    stream.Write(image.Buffer, 0, image.Buffer.Length);
                }
            }
            LoadImageSync_forStrip(single_path_tb.Text + page_no_lbl.Text + ".jpg", page_no_lbl.Text);
            if(page_in_range((Convert.ToDecimal(page_no_lbl.Text) + 1).ToString()))
            {
                 page_no_lbl.Text = (Convert.ToDecimal(page_no_lbl.Text) + 1).ToString();
            }
            else
            {
                MessageBox.Show("أخر صفحة فى المهمة");
            }
           

        }
        public static System.Drawing.Image AforgeAutoCrop(Bitmap selectedImage)
        {
            Bitmap autoCropImage = null;
            try
            {

                autoCropImage = selectedImage;
                // create grayscale filter (BT709)
                Grayscale filter = new Grayscale(0.2125, 0.7154, 0.0721);
                Bitmap grayImage = filter.Apply(autoCropImage);
                // create instance of skew checker
                DocumentSkewChecker skewChecker = new DocumentSkewChecker();
                // get documents skew angle
                double angle = 0; // skewChecker.GetSkewAngle(grayImage);
                // create rotation filter
                RotateBilinear rotationFilter = new RotateBilinear(-angle);
                rotationFilter.FillColor = Color.White;
                // rotate image applying the filter
                Bitmap rotatedImage = rotationFilter.Apply(grayImage);
                new ContrastStretch().ApplyInPlace(rotatedImage);
                new Threshold(50).ApplyInPlace(rotatedImage);
                BlobCounter bc = new BlobCounter();
                bc.FilterBlobs = true;
                // bc.MinWidth = 500;
                //bc.MinHeight = 500;
                bc.ProcessImage(rotatedImage);
                Rectangle[] rects = bc.GetObjectsRectangles();

                if (rects.Length == 0)
                {
                    // CAN'T CROP
                }
                else if (rects.Length == 1)
                {
                    autoCropImage = autoCropImage.Clone(rects[0], autoCropImage.PixelFormat); ;
                }
                else if (rects.Length > 1)
                {
                    // get largets rect
                    Console.WriteLine("Using largest rectangle found in image ");
                    var r2 = rects.OrderByDescending(r => r.Height * r.Width).ToList();
                    autoCropImage = autoCropImage.Clone(r2[0], autoCropImage.PixelFormat);
                }
                else
                {
                    Console.WriteLine("Huh? on image ");
                }
            }
            catch (Exception ex)
            {
                //MessageBox.Show(ex.Message);
                //CAN'T CROP
            }

            return autoCropImage;
        }
        void transfre_to_storage(string path, NikonImage image)
        {

            /////// need to enhance
            using (FileStream stream = new FileStream("1.jpg", FileMode.Create, FileAccess.Write))
            {
                stream.Write(image.Buffer, 0, image.Buffer.Length);
            }

            Bitmap mybitmap = new Bitmap("1" + ".jpg");


            Bitmap mybitmap2 = new Bitmap(get_crop_pict(mybitmap));

            SaveJpeg(path, mybitmap2, 50);

            mybitmap.Dispose();

        }
        System.Drawing.Image get_crop_pict(System.Drawing.Image m)
        {
            int x = Int32.Parse(((rect_x_r * m.Width) / 100).ToString());
            int y = Int32.Parse(((rect_y_r * m.Height) / 100).ToString());
            int w = Int32.Parse(((rect_w_r * m.Width) / 100).ToString());
            int h = Int32.Parse(((rect_h_r * m.Height) / 100).ToString());

            System.Drawing.Rectangle new_rect = new System.Drawing.Rectangle(x, y, w, h);

            Bitmap nb = new Bitmap(new_rect.Width, new_rect.Height);

            Graphics g = Graphics.FromImage(nb);
            g.DrawImage(m, new System.Drawing.Rectangle(0, 0, new_rect.Width, new_rect.Height), new_rect.X, new_rect.Y, new_rect.Width, new_rect.Height, GraphicsUnit.Pixel);

            return AforgeAutoCrop(nb);
        }
        public static void SaveJpeg(string path, System.Drawing.Image img, int quality)
        {
            if (quality < 0 || quality > 100)
                throw new ArgumentOutOfRangeException("quality must be between 0 and 100.");

            // Encoder parameter for image quality 
            EncoderParameter qualityParam = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, quality);
            // JPEG image codec 
            ImageCodecInfo jpegCodec = GetEncoderInfo("image/jpeg");
            EncoderParameters encoderParams = new EncoderParameters(1);
            encoderParams.Param[0] = qualityParam;
            img.Save(path, jpegCodec, encoderParams);
        }

        /// <summary> 
        /// Returns the image codec with the given mime type 
        /// </summary> 
        private static ImageCodecInfo GetEncoderInfo(string mimeType)
        {
            // Get image codecs for all image formats 
            ImageCodecInfo[] codecs = ImageCodecInfo.GetImageEncoders();

            // Find the correct image codec 
            for (int i = 0; i < codecs.Length; i++)
                if (codecs[i].MimeType == mimeType)
                    return codecs[i];

            return null;
        }
        void device_CaptureComplete(NikonDevice sender, int data)
        {
            // Re-enable buttons when the capture completes
            not_tasweer_design(true);

            if (device.LiveViewEnabled)
            {

                live_now_1.Visible = true;
                live_now_2.Visible = true;
            }

            pic_count_tb.Text = get_pic_count().ToString();

        }
        int get_pic_count()
        {
            int count = 0;
            for (int i = Int32.Parse(from_page.ToString()); i < Int32.Parse(to_page.ToString()) + 1; i++)
            {
                if (File.Exists(single_path_tb.Text + i.ToString() + ".jpg"))
                {
                    count++;
                }
            }
            return count;
        }
        List<string> images_only(String path)
        {
            var files = from file in Directory.GetFiles(path, "*", SearchOption.TopDirectoryOnly)
                        where file.ToLower().EndsWith(".jpeg") || file.ToLower().EndsWith(".jpg") ||
                        file.ToLower().EndsWith(".png") || file.ToLower().EndsWith(".gif") ||
                        file.ToLower().EndsWith(".tif") || file.ToLower().EndsWith(".tiff") ||
                        file.ToLower().EndsWith(".bmp")
                        select file;

            return files.OrderBy(x => x, new NaturalStringComparer()).ToList();
        }
        void manager_DeviceRemoved(NikonManager sender, NikonDevice device)
        {
            try
            {
                this.device = null;

                // Stop live view timer
                liveViewTimer.Stop();

                // Clear device name
                label_name.Text = "No Camera";

                // Disable buttons
                not_tasweer_design(false);

                // Clear live view picture
                pictureBox1.Image = null;
            }
            catch
            {
            }
        }
        void liveViewTimer_Tick(object sender, EventArgs e)
        {
            if (device == null)
            {
                return;
            }
            // Get live view image
            NikonLiveViewImage image = null;

            //niko

            try
            {
                image = device.GetLiveViewImage();
            }
            catch (NikonException)
            {
                liveViewTimer.Stop();
            }

            // Set live view image on picture box
            if (image != null)
            {
                MemoryStream stream = new MemoryStream(image.JpegBuffer);
                pictureBox1.Image = getlife_pic(System.Drawing.Image.FromStream(stream));

            }
        }
        private void panel3_Click(object sender, EventArgs e)
        {
            if (device != null)
            {

                device.LiveViewEnabled = false;

                liveViewTimer.Stop();
                pictureBox1.Image = null;
                live_btn.BackgroundImage = Microfilm_Dafater_D810.Properties.Resources.play;

                live_now_1.Visible = false;
                live_now_2.Visible = false;


            }



            // Shut down the Nikon manager
            if (MessageBox.Show("تأكيد الخروج", "خروج", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                manager.Shutdown();
                this.Close();
            }
           
        }
        protected override void OnClosing(CancelEventArgs e)
        {

            //Disable live view (in case it's enabled)
            //if (device != null)
            //{

            //    device.LiveViewEnabled = false;

            //    MessageBox.Show("closing");
            //}

            //// Shut down the Nikon manager

            //manager.Shutdown();

            base.OnClosing(e);



        }
        System.Drawing.Image getlife_pic(System.Drawing.Image om)
        {
            Bitmap m = new Bitmap(om, pictureBox1.Size);

            if (click_no == 2)
            {

                int x = Int32.Parse(((rect_x_r * m.Width) / 100).ToString());
                int y = Int32.Parse(((rect_y_r * m.Height) / 100).ToString());
                int w = Int32.Parse(((rect_w_r * m.Width) / 100).ToString());
                int h = Int32.Parse(((rect_h_r * m.Height) / 100).ToString());

                System.Drawing.Rectangle new_rect = new System.Drawing.Rectangle(x, y, w, h);

                Bitmap nb = new Bitmap(new_rect.Width, new_rect.Height);
                Graphics g = Graphics.FromImage(nb);

                g.DrawImage(m, -new_rect.X, -new_rect.Y);
                Bitmap nb1 = new Bitmap(nb, pictureBox1.Size);
                m.Dispose();
                nb.Dispose();
                return nb1;
            }
            else
            {
                return m;
            }
        }

        private void live_btn_Click(object sender, EventArgs e)
        {
            live_now();
        }
        void live_now()
        {
            if (device == null)
            {
                return;
            }

            if (device.LiveViewEnabled)
            {
                device.LiveViewEnabled = false;
                liveViewTimer.Stop();
                pictureBox1.Image = null;
                live_btn.BackgroundImage = Microfilm_Dafater_D810.Properties.Resources.play;

                live_now_1.Visible = false;
                live_now_2.Visible = false;
            }
            else
            {
                device.LiveViewEnabled = true;
                liveViewTimer.Start();
                live_btn.BackgroundImage = Microfilm_Dafater_D810.Properties.Resources.flag_red;


                live_now_1.Visible = true;
                live_now_2.Visible = true;
            }
            



        }

        private void tasweer_daf_Load(object sender, EventArgs e)
        {
            try
            {
                string oradb = "Data Source=unix;Persist Security Info=True;User ID=segelat;Password=segelatadminsite;Unicode=True";
                OracleConnection con = new OracleConnection(oradb);
                OracleCommand cmd = new OracleCommand();

                try
                {
                    cmd.CommandText = "select * from MTD_DAFATER WHERE DAFTAR_CODE = "+daf_code;
                    cmd.Connection = con;
                    con.Open();
                    OracleDataReader dr2 = cmd.ExecuteReader();
                    DataSet ds2 = new DataSet();
                    ds2.Clear();
                    OracleDataAdapter da2 = new OracleDataAdapter(cmd);
                    da2.Fill(ds2);


                    single_path_tb.Text = ds2.Tables[0].Rows[0]["SERVER_PATH"].ToString() + ds2.Tables[0].Rows[0]["MASTER_PATH"].ToString() + ds2.Tables[0].Rows[0]["SUB_PATH"].ToString() +daf_code+ @"\";
                    con.Close();


                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message + " ... MICROFILM_SCANNER_SETTING ...");
                    single_path_tb.Text = "";
                    con.Close();
                    return;
                }



                if (task_status == "1")
                {

                    try
                    {


                        cmd.CommandText = "update MTD_TASKS set TASK_STATUS= 2  , START_DATE = sysdate where TASK_ID=" + task_code.ToString() + "";
                        cmd.Connection = con;
                        con.Open();
                        //cmd.ExecuteReader();
                        cmd.ExecuteNonQuery();
                        con.Close();


                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message);
                        con.Close();
                    }
                }


                if (!Directory.Exists(single_path_tb.Text))
                {
                    Directory.CreateDirectory(single_path_tb.Text);
                }

                fill_img();
                pic_count_tb.Text = get_pic_count().ToString();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                //con.Close();
            }
        }

        void fill_img()
        {
            int counter = images_pane.Controls.Count;
            for (int i = 0; i < counter; i++)
            {
                try

                {
                    images_pane.Controls.RemoveAt(0);
                }
                catch { }
            }

            List<string> images = images_only(single_path_tb.Text);

            pic_count_tb.Text = get_pic_count().ToString();

            for (int i = 0; i < images.Count(); i++)
            {
                string page_no = images.ElementAt(i).Substring(single_path_tb.Text.Length, images.ElementAt(i).Length - single_path_tb.Text.Length - 4);

                if (page_in_range(page_no))
                {
                    LoadImageSync_forStrip(images.ElementAt(i), page_no);
                }
            }

           tot_lbl.Text = (to_page - from_page + 1).ToString();

        }

        bool page_in_range(string page_no)
        {
            try
            {
                if (Convert.ToDecimal(page_no) >= from_page && Convert.ToDecimal(page_no) <= to_page)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch
            {
                return false;
            }
           
        }
        double LoadImageSync_forStrip(string imageUrl, string page_no)
        {
            Bitmap bmpOriginal = new Bitmap(imageUrl);

            int oheight = bmpOriginal.Height;

            int owidth = bmpOriginal.Width;

            //oheight = bmpOriginal.Height;
            //owidth = bmpOriginal.Width;

            PictureBox dynamicPictureBox = new PictureBox();
            //dynamicPictureBox.Width = (images_pane.Height - 50) * bmpOriginal.Width / bmpOriginal.Height;
            //dynamicPictureBox.Height = (images_pane.Height - 50);

            dynamicPictureBox.Width = 150;
            dynamicPictureBox.Height = 90;

            dynamicPictureBox.SizeMode = PictureBoxSizeMode.Normal;
            Bitmap dbm = new Bitmap(bmpOriginal, dynamicPictureBox.Width, dynamicPictureBox.Height);
            dynamicPictureBox.Image = dbm;
            ////////////////////////////
            ///////////////////////////
            //int index = (images_pane.Controls.Count) - 1;

            //if (index >= 0)
            //{
            //    Rectangle ccc = images_pane.Controls[index].Bounds;
            //    dynamicPictureBox.Location = new Point(ccc.X + ccc.Width + 10, 5);

            //}
            //else
            //{ dynamicPictureBox.Location = new Point(2, 5); }
            int index = (images_pane.Controls.Count) - 1;

            if (index >= 0)
            {
                Rectangle ccc = images_pane.Controls[index].Bounds;
                dynamicPictureBox.Location = new Point(ccc.X + 150 + 20, 5);

            }
            else
            { dynamicPictureBox.Location = new Point(2, 5); }
            ////////////////////////////////
            ///////////////////////////////////////////
            //int count = (images_pane.Controls.Count);

            //if (count > 0)
            //{

            //    dynamicPictureBox.Location = new Point((count/2)*155 + 5, 5);

            //}
            //else
            //{ dynamicPictureBox.Location = new Point(5, 5); }
            ////////////////////////////////////////
            ///////////////////////////////////////
            //dynamicPictureBox.Location = new Point((panel_imges * 155) + 5, 5);
            //panel_imges++;
            /////////////////////////////////////
            /////////////////////////////////////

            ContextMenuStrip myStrip = new ContextMenuStrip();
            myStrip.Items.Add("delete");
            myStrip.Items[0].Click += (s, e) =>
            {
                try
                {
                    //foreach (Control p in images_pane.Controls)
                    //{
                    //    if (p.Location.X > dynamicPictureBox.Location.X)
                    //        p.Location = new Point(p.Location.X - dynamicPictureBox.Width - 12, p.Location.Y);
                    //}
                    File.Delete(imageUrl);
                    //images_pane.Controls.Remove(dynamicPictureBox);

                    //pic_count_tb.Text = get_pic_count().ToString() ;
                    //panel_imges--;
                    fill_img();
                }
                catch
                {
                    MessageBox.Show("خارج صلاحياتك");
                }
            };


            dynamicPictureBox.ContextMenuStrip = myStrip;

            dynamicPictureBox.Click += (s, e) =>
            {
                //LoadImageSync_forMain(pictureBox, imageUrl);
                //Bitmap bmpOriginal2 = new Bitmap(imageUrl);
                //Bitmap bmpOriginal3 = new Bitmap(bmpOriginal2, pictureBox1.Size);



                //pictureBox1.Image = bmpOriginal3;
                //bmpOriginal2.Dispose();


                page_no_lbl.Text = page_no;

                show_img();




            };

            images_pane.Controls.Add(dynamicPictureBox);

            /////////////////////
            Label coun = new Label();
            coun.Text = page_no;
            coun.ForeColor = Color.Red;
            coun.Font = new System.Drawing.Font("Arial", 12F, System.Drawing.FontStyle.Bold);
            coun.Location = new Point(dynamicPictureBox.Location.X + (dynamicPictureBox.Size.Width / 2) - 85, dynamicPictureBox.Location.Y + dynamicPictureBox.Size.Height + 5);
            images_pane.Controls.Add(coun);
            /////////////////////

            //// Dispose the large image to free the memory.
            bmpOriginal.Dispose();
            bmpOriginal = null;
            dbm = null;
            //bmpOriginal.Dispose();
            images_pane.HorizontalScroll.Value = images_pane.HorizontalScroll.Maximum;
            return 0.1;
        }

        void show_img()
        {

            if (device != null)
            {
                if (device.LiveViewEnabled)
                {
                    device.LiveViewEnabled = false;
                    liveViewTimer.Stop();
                    pictureBox1.Image = null;
                    live_btn.BackgroundImage = Microfilm_Dafater_D810.Properties.Resources.play;

                    live_now_1.Visible = false;
                    live_now_2.Visible = false;
                }
            }
            

            if (File.Exists(single_path_tb.Text + page_no_lbl.Text + ".jpg"))
            {
                Bitmap pic = new Bitmap(single_path_tb.Text + page_no_lbl.Text + ".jpg");
                Bitmap pic2 = new Bitmap(pic, pictureBox1.Size);
                pictureBox1.Image = pic2;

                pic.Dispose();
                //pic2.Dispose();
            }
            else
            {
                pictureBox1.Image = null;
            }
        }

        private void cap_btn_Click(object sender, EventArgs e)
        {
            capture_now();
        }
        void capture_now()
        {
            if (device == null)
            {
                return;
            }
            if (page_in_range(page_no_lbl.Text))
            {
                if (File.Exists(single_path_tb.Text + page_no_lbl.Text + ".jpg"))
                {
                    MessageBox.Show("تم أخذ اللقطة من قبل ... قم بمسح اللقطة أولا لإعادة التصوير");
                }
                else
                {
                    not_tasweer_design(false);

                    live_now_1.Visible = false;
                    live_now_2.Visible = false;

                    try
                    {
                        curr_photo++;
                        if (curr_photo == 1)
                        {
                            auto_focus_now();
                        }

                        //auto_focus_now();

                        device.Capture();


                    }
                    catch (NikonException ex)
                    {
                        MessageBox.Show(ex.Message);
                        not_tasweer_design(true);
                    }

                    pictureBox1.Image = null;
                }
            }
            else
            {
                MessageBox.Show("رقم الصفحة المراد تصويرها خارج المهمة المطلوبة");
            }
        }

        void auto_focus_now()
        {
            try
            {
                if (device.LiveViewEnabled)
                {
                    device.LiveViewEnabled = false;
                    liveViewTimer.Stop();
                    pictureBox1.Image = null;
                    live_btn.BackgroundImage = Microfilm_Dafater_D810.Properties.Resources.play;

                    live_now_1.Visible = false;
                    live_now_2.Visible = false;

                    device.AutoFocus();

                    device.LiveViewEnabled = true;
                    liveViewTimer.Start();
                    live_btn.BackgroundImage = Microfilm_Dafater_D810.Properties.Resources.flag_red;


                    live_now_1.Visible = true;
                    live_now_2.Visible = true;
                }

                else
                {
                    device.AutoFocus();
                }



            }
            catch
            {

                MessageBox.Show("Cannot be able to auto-focus the photo");
            }
        }

        private void pictureBox1_MouseDown(object sender, MouseEventArgs e)
        {
            if (live_now_1.Visible == true)
            {
                if (click_no == 0)
                {

                    click_no = 1;

                    rect_x = e.X;
                    rect_y = e.Y;


                }
            }

        }
        private void pictureBox1_MouseMove(object sender, MouseEventArgs e)
        {
            if (live_now_1.Visible == true)
            {
                if (click_no == 1)
                {
                    Graphics g;
                    g = pictureBox1.CreateGraphics();

                    //   g.Clear(Color.Transparent);



                    if (e.X > rect_x && e.Y > rect_y)
                    {
                        rect_w = e.X - rect_x;
                        rect_h = e.Y - rect_y;
                    }



                    recs = new System.Drawing.Rectangle(rect_x, rect_y, rect_w, rect_h);
                    Pen pp = new Pen(Color.Red, 2);

                    g.DrawRectangle(pp, recs);
                }
            }
        }

        private void pictureBox1_MouseUp(object sender, MouseEventArgs e)
        {
            if (live_now_1.Visible == true)
            {
                click_no = 2;

                get_ratio();

                crop_cancel_btn.Visible = true;
            }

        }
        void get_ratio()
        {
            rect_x_r = ((rect_x * 100) / pictureBox1.Width);
            rect_w_r = ((rect_w * 100) / pictureBox1.Width);
            rect_y_r = ((rect_y * 100) / pictureBox1.Height);
            rect_h_r = ((rect_h * 100) / pictureBox1.Height);
        }

        private void crop_cancel_btn_Click(object sender, EventArgs e)
        {
            set_view("l");
            click_no = 0;
            crop_cancel_btn.Visible = false;
        }
        void set_view(string status)
        {
            if (status == "p")
            {
                panel4.SetBounds(473, 247, 462, 509);
                // show_img();
            }
            if (status == "l")
            {
                panel4.SetBounds(245, 247, 879, 509);

                //  show_img();
            }

            if (device == null)
            {
                return;
            }

            if (!device.LiveViewEnabled)
            {
                show_img();
            }
        }

        private void radioButton1_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton1.Checked)
                set_view("l");
        }

        private void radioButton2_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton2.Checked)
                set_view("p");
        }
        void right_now()
        {
            if (page_in_range((Convert.ToDecimal(page_no_lbl.Text) + 1).ToString()))
            {
                page_no_lbl.Text = (Convert.ToDecimal(page_no_lbl.Text) + 1).ToString();
                show_img();
            }
            else
            {
                page_no_lbl.Text = to_page.ToString();
                MessageBox.Show("أخر صفحة");
            }
        }
        void left_now()
        {
            if (page_in_range((Convert.ToDecimal(page_no_lbl.Text) - 1).ToString()))
            {
                page_no_lbl.Text = (Convert.ToDecimal(page_no_lbl.Text) - 1).ToString();
                show_img();
            }
            else
            {
                page_no_lbl.Text = from_page.ToString();
                MessageBox.Show("أول صفحة");
            }
        }

        private void panel22_Click(object sender, EventArgs e)
        {
            right_now();
        }

        private void panel23_Click(object sender, EventArgs e)
        {
            left_now();
        }

        private void panel15_Click(object sender, EventArgs e)
        {
            try
            {
                Process.Start(single_path_tb.Text + page_no_lbl.Text + ".jpg");
            }
            catch
            {
            }
        }

        private void panel14_Click(object sender, EventArgs e)
        {
            fill_img();
        }

        private void panel8_Click(object sender, EventArgs e)
        {
            panel16.Visible = !panel16.Visible;
            textBox20.Text = curr_photo.ToString();
        }

        
        int get_last_img()
        {
            string my_loc = page_no_lbl.Text;

            try
            {
                
                //List<string> images = images_only(single_path_tb.Text);

                for (int i = 0; i < Convert.ToDecimal(tot_lbl.Text); i++)
                {
                    //string page_no = images.ElementAt(i).Substring(single_path_tb.Text.Length, images.ElementAt(i).Length - single_path_tb.Text.Length - 4);

                    string page_no = (Convert.ToDecimal(my_loc) + i+1).ToString();

                    if (page_in_range(page_no))
                    {
                        if (!File.Exists(single_path_tb.Text + page_no + ".jpg"))
                        {
                            if (page_no_lbl.Text == page_no)
                            {
                                MessageBox.Show("متبقى هذة اللقطة فقط");
                            }
                            else
                            {
                                page_no_lbl.Text = page_no;
                                open_live_anywhere();
                               
                            }
                            return Int32.Parse(page_no);
                        }

                    }
                    else
                    {
                        page_no = (from_page).ToString();

                        if (!File.Exists(single_path_tb.Text + page_no + ".jpg"))
                        {
                            if (page_no_lbl.Text == page_no)
                            {
                                MessageBox.Show("متبقى هذة اللقطة فقط");
                            }
                            else
                            {
                                page_no_lbl.Text = page_no;
                                open_live_anywhere();
                            }
                                return Int32.Parse(page_no);
                            
                        }
                        else
                        {
                            my_loc = (Convert.ToDecimal(page_no)-i-1).ToString();
                        }

                        
                    }
                }

                MessageBox.Show("تم تصوير المهمة بالكامل");
                return 0;

            }
            catch
            {
                MessageBox.Show("ERROR");
                return 0;
            }
        }

        void open_live_anywhere()
        {
            try
            {
                if (device != null)
                {
                    if (!device.LiveViewEnabled)
                    {
                        device.LiveViewEnabled = true;
                        liveViewTimer.Start();
                        live_btn.BackgroundImage = Microfilm_Dafater_D810.Properties.Resources.flag_red;


                        live_now_1.Visible = true;
                        live_now_2.Visible = true;
                    }
                }
            }
            catch
            {
            }
        }

        private void panel27_Click(object sender, EventArgs e)
        {
            get_last_img();
        }

        private void panel12_Click(object sender, EventArgs e)
        {
            end_of_task();
        }

        void end_of_task()
        {
            try
            {
                string oradb = "Data Source=unix;Persist Security Info=True;User ID=segelat;Password=segelatadminsite;Unicode=True";
                OracleConnection con = new OracleConnection(oradb);

                for (int i = Int32.Parse(from_page.ToString()); i <= Int32.Parse(to_page.ToString()); i++)
                {
                    //File.Exists(single_path_tb.Text + textBox2.Text + ".jpg")
                    if (!File.Exists(single_path_tb.Text + i.ToString() + ".jpg"))
                    {
                        MessageBox.Show("لم يتم تصوير المهمة بالكامل");
                        return;
                    }
                  
                }

                
                    
                    try
                    {

                        OracleCommand cmd = new OracleCommand();
                        cmd.CommandText = "update MTD_DAF_PAGES set  TASWEER_DATE = sysdate , TASWEER_USER = " + user_code + " , TASWEER_STATUS = 1  where TASK_CODE=" + task_code.ToString() + "";
                        cmd.Connection = con;
                        con.Open();
                        //cmd.ExecuteReader();
                        cmd.ExecuteNonQuery();
                        con.Close();


                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message);
                        con.Close();
                    }
               
               
                try
                {

                    OracleCommand cmd = new OracleCommand();
                    cmd.CommandText = "update MTD_TASKS set TASK_STATUS=3  , END_DATE = sysdate where TASK_ID=" + task_code.ToString() + "";
                    cmd.Connection = con;
                    con.Open();
                    //cmd.ExecuteReader();
                    cmd.ExecuteNonQuery();
                    con.Close();

                    this.Close();

                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                    con.Close();
                }


            }
            catch
            {
                MessageBox.Show("خطأ فى العملية .. راجع مدير النظام");
            }
        }

        private void tasweer_daf_KeyDown(object sender, KeyEventArgs e)
        {
            k_down(e);
        }

        private void radioButton2_KeyDown(object sender, KeyEventArgs e)
        {
            k_down(e);
        }

        private void radioButton1_KeyDown(object sender, KeyEventArgs e)
        {
            k_down(e);
        }
        void k_down(KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter && cap_btn.Visible == true)
            {
                capture_now();
            }
          /*  if (e.KeyCode == Keys.Enter && cap_btn.Visible == false)
            {
                MessageBox.Show("إنتظر حتى يتم تصوير اللقطة السابقة");
            } */
            if (e.KeyCode == Keys.End)
            {
                get_last_img();
            }
            if (e.KeyCode == Keys.F12)
            {
                end_of_task();
            }
        }

        private void panel12_Paint(object sender, PaintEventArgs e)
        {

        }

        private void images_pane_Paint(object sender, PaintEventArgs e)
        {

        }

        private void panel3_Paint(object sender, PaintEventArgs e)
        {

        }
    }
}
