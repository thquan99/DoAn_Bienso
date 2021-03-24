using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.OleDb;
using System.Drawing;
using System.Drawing.Printing;
using System.Drawing.Imaging;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Threading;
using System.IO.Ports;

using AForge;
using AForge.Imaging;
using AForge.Math;
using AForge.Imaging.Filters;
using AForge.Imaging.Textures;
using AForge.Video;
using AForge.Video.DirectShow;
using AForge.Vision.Motion;



namespace prjNhanDang
{
    public partial class frmMain : Form
    {
        //=======class============
        clsImagePlate ImagePlate;
        clsLicensePlate LicensePlate;
        clsNetwork Network;
        clsBLL obj;
        //=======class============
        //====== motion detector================
        private IMotionDetector detector = null;
        List<Bitmap> array_motion = new List<Bitmap>();
        Bitmap backgroundFrame, currentFrame;
        bool flag_motion;
        double alarm;
        int time;
        int count;
               
        TimeSpan tsp;
        DateTime tbegin;
        //====== motion detector================     
//================================================================        
        
        public frmMain()
        {
            InitializeComponent();
            serialPort1.DataReceived += new SerialDataReceivedEventHandler(serialPort1_DataReceived);
        }
//---------------------------------------------------------------
        private void frmMain_Load(object sender, EventArgs e)
        {
            
            statusStrip1.Items[0].Text = DateTime.Now.ToString();
            Network=new clsNetwork();
            Network.AutoLoadNetworkChar();
            Network.AutoLoadNetworkNum();
            obj = new clsBLL();
            timer2.Start();
            //serialport
            if (serialPort1.IsOpen)
                serialPort1.Close();
            
            try
            {               
                setting();
                serialPort1.Open();
                if (serialPort1.IsOpen)
                    serialPort1.WriteLine("B");
                else
                    MessageBox.Show("can't connect to copmport");   
            }
            catch (System.IO.IOException ex)
            {

                MessageBox.Show(ex.Message);
            }
            
        }
//----------------------------------------------------------------------------
        private void loadVideoCaptureDeviceToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                frmVideoCaptureDevice form = new frmVideoCaptureDevice();

                if (form.ShowDialog(this) == DialogResult.OK)
                {
                    // create video source
                    VideoCaptureDevice videoSource = new VideoCaptureDevice(form.VideoDevice);

                    // open it
                    OpenVideoSource(videoSource);

                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
//-------------------------------------------------------------------------
        private void OpenVideoSource(IVideoSource source)
        {
            // set busy cursor
            this.Cursor = Cursors.WaitCursor;
            // close previous video source
            CloseVideoSource();

            // create camera
            Camera camera= new Camera(source, detector);
            // start camera
            camera.Start();

            // attach camera to camera window
            cameraWindow1.Camera = camera;
            flag_motion = false;
            array_motion.Clear();
            alarm = 0;
            // start timer
            time = 0;
            timer1.Start();
            this.Cursor = Cursors.Default;
        }
//-------------------------------------------------------------------------
        private void CloseVideoSource()
        {
            Camera camera = cameraWindow1.Camera;

            if (camera != null)
            {
                // stop timer
                timer1.Stop();

                // detach camera from camera window
                cameraWindow1.Camera = null;
                Application.DoEvents();

                // signal camera to stop
                camera.SignalToStop();
                // wait 2 seconds until camera stops
                for (int i = 0; (i < 20) && (camera.IsRunning); i++)
                {
                    Thread.Sleep(100);
                }
                if (camera.IsRunning)
                    camera.Stop();
                camera = null;

                // reset motion detector
                if (detector != null)
                    detector.Reset();
            }
        }
//------------------------------------------------------------------------------
        private void openVideoFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                OpenFileDialog op = new OpenFileDialog();
                op.Filter = "All File(*.*)|*.*";
                op.FileName = "Video File";
                if (op.ShowDialog() == DialogResult.OK)
                {
                    // create video source
                    FileVideoSource fileSource = new FileVideoSource(op.FileName);

                    // open it
                    OpenVideoSource(fileSource);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
//-------------------------------------------------------------------------------       
        private void saveImageToolStripMenuItem_Click(object sender, EventArgs e)
        {
            save_bitmap();
        }
//---------------------------------------------------------------------------------
        private void save_bitmap()
        {
            try
            {
                SaveFileDialog sp = new SaveFileDialog();
                sp.Filter = "Image(*.bmp)|*.bmp";
                if (sp.ShowDialog() == DialogResult.OK)
                {
                    ImageFormat format = ImageFormat.Bmp;
                    if (sp.FileName != null)
                    {
                        picture.Image.Save(sp.FileName, format);
                    }
                }
            }
            catch (Exception ex)
            {
                 statusStrip1.Items[1].Text=ex.Message;
            }
        }
//--------------------------------------------------------------------------------
       
 //---------------------------------------------------------------------------
        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CloseVideoSource();
            serialPort1.Close();
            Application.Exit();

        }
//------------------------------------------------------------------------------      

        private void show_result()
        {
            tsp = DateTime.Now - tbegin;
            //hien thi ket qua
            tbegin = DateTime.Now;
            string tdate =  DateTime.Today.Day.ToString("0#") + "/" + DateTime.Today.Month.ToString("0#") + "/" + DateTime.Today.Year.ToString();
            string ttime = tbegin.Hour.ToString("0#") + ":" + tbegin.Minute.ToString("0#") + ":" + tbegin.Second.ToString("0#");
            lbDate.Text = tdate;
            lbTime.Text = ttime;
            loadListView(lbPlate.Text.ToString(), lbDate.Text,lbTime.Text);
        }
//---------------------------------------------------------------------
        private void timer1_Tick(object sender, EventArgs e)
        {
            try
            {
                if (cameraWindow1.Camera.LastFrame != null)
                {
                    cameraWindow1.Camera.Lock();
                    if (!flag_motion)
                    {
                        backgroundFrame = AForge.Imaging.Image.Clone(cameraWindow1.Camera.LastFrame);
                        flag_motion = true;
                    }
                    currentFrame = AForge.Imaging.Image.Clone(cameraWindow1.Camera.LastFrame);
                    cameraWindow1.Camera.Unlock();
                    process_motion();
                    //textBox1.Text = alarm.ToString("0.###") + "  " + time.ToString();
                }
                if (alarm > 0.3)
                {
                    array_motion.Add(AForge.Imaging.Image.Clone(currentFrame));
                    time++;
                }
                else if (time > 10)
                {
                    Bitmap[] bitmaps = array_motion.ToArray();
                    int index = bitmaps.Length;
                    //textBox1.Text = index.ToString();
                    index = (int)(index * 0.54);
                    ImagePlate = new clsImagePlate(new Bitmap(bitmaps[index]));
                    picture.Image = ImagePlate.IMAGE;
                    array_motion.Clear();
                    alarm = 0;
                    time = 0;
                    flag_motion = false;
                    timer1.Stop();


                    tbegin = DateTime.Now;
                    //input_image = sub_img.Apply(input_image);
                    ImagePlate.Get_Plate();
                    //tao anh bang so
                    LicensePlate = new clsLicensePlate();
                    LicensePlate.PLATE = ImagePlate.PLATE;
                    picture.Image = LicensePlate.PLATE;
                    //cat ky tu
                    LicensePlate.Split(ImagePlate.Plate_Type);
                    //display picturebox
                    pictureBox1.Image = LicensePlate.IMAGEARR[0];
                    pictureBox2.Image = LicensePlate.IMAGEARR[1];
                    pictureBox3.Image = LicensePlate.IMAGEARR[2];
                    pictureBox4.Image = LicensePlate.IMAGEARR[3];
                    pictureBox5.Image = LicensePlate.IMAGEARR[4];
                    pictureBox6.Image = LicensePlate.IMAGEARR[5];
                    pictureBox7.Image = LicensePlate.IMAGEARR[6];
                    pictureBox8.Image = LicensePlate.IMAGEARR[7];
                    pictureBox9.Image = LicensePlate.IMAGEARR[8];
                    int sum = LicensePlate.getsumcharacter();
                    //recognize
                    Network.IMAGEARR = LicensePlate.IMAGEARR;
                    Network.recognition(sum,ImagePlate.Plate_Type);
                    lbPlate.Text = Network.LICENSETEXT;
                    show_result();
                }
                
            }
            catch (Exception ex)
            {
                statusStrip1.Items[1].Text = ex.Message;
            }
        }
        
//--------------------------------------------------------------------
        private void process_motion()
        {
            IFilter filt = new GrayscaleY();
            currentFrame = filt.Apply(currentFrame);
            backgroundFrame = filt.Apply(backgroundFrame);
            FiltersSequence filters = new FiltersSequence();
            Morph filt_morph = new Morph();
            filt_morph.OverlayImage = currentFrame;
            Bitmap tmp = filt_morph.Apply(backgroundFrame);
            filters.Add(new Difference(tmp));
            filters.Add(new Threshold(15));
            Bitmap tmp1 = filters.Apply(currentFrame);
            alarm = CalculateWhitePixels(tmp1);
        }
//------------------------------------------------------------------
        private double CalculateWhitePixels(Bitmap image)
        {
            double count = 0;
            int width = image.Width;
            int height = image.Height;
            // lock difference image
            BitmapData data = image.LockBits(new Rectangle(0, 0, width, height),
                ImageLockMode.ReadOnly, PixelFormat.Format8bppIndexed);
            int offset = data.Stride - width;
            unsafe
            {
                byte* ptr = (byte*)data.Scan0.ToPointer();
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++, ptr++)
                    {
                        count += ((*ptr) >> 7);
                    }
                    ptr += offset;
                }
            }
            // unlock image
            image.UnlockBits(data);
            return count / (width * height);
        }

        
//------------------------------------------------------------------------------
        private void openImageToolStripMenuItem_Click(object sender, EventArgs e)
        {            
            open_bitmap();
        }
//------------------------------------------------------------------------------
        private void open_bitmap()
        {
            try
            {
                OpenFileDialog op = new OpenFileDialog();
                op.InitialDirectory = Application.StartupPath + "\\foreground";
                op.Filter = ("Image files (*.jpg,*.png,*.tif,*.bmp,*.gif)|*.jpg;*.png;*.tif;*.bmp;*.gif|JPG files (*.jpg)|*.jpg|PNG files (*.png)|*.png|TIF files (*.tif)|*.tif|BMP files (*.bmp)|*.bmp|GIF files (*.gif)|*.gif|All files(*.*)|*.*");
                if (op.ShowDialog() == DialogResult.OK)
                {
                    if (op.FileName != null)
                    {
                        StreamReader bitmap_file_stream = new StreamReader(op.FileName);
                        string bmp_file_name = Path.GetFileName(op.FileName);
                        ImagePlate = new clsImagePlate(new Bitmap(op.FileName));

                        bitmap_file_stream.Close();
                        picture.Image = ImagePlate.IMAGE;
                        DisplayNumberPalate();
                    }

                }
            }
            catch (Exception ex)
            {
                statusStrip1.Items[1].Text=ex.Message;
            }
        }
//-------------------------------------------------------------------------------- 
       
        private void DisplayNumberPalate()
        {
            try
            {


                //statusStrip1.Items[1].Text = "";
                //lay anh bang so
                ImagePlate.Get_Plate();
                //liembt
                //pictureBox10.Image = ImagePlate.CutTopBottom;
                //pictureBox11.Image = ImagePlate.Cutplate;
                //liembt
                //tao anh bang so
                LicensePlate = new clsLicensePlate();
                LicensePlate.PLATE = ImagePlate.PLATE;
                //picture.Image = ImagePlate.p;
                //cat ky tu
                LicensePlate.Split(ImagePlate.Plate_Type);
                pbox_area.Image = LicensePlate.p;
                //display picturebox
                pictureBox1.Image = LicensePlate.IMAGEARR[0];
                pictureBox2.Image = LicensePlate.IMAGEARR[1];
                pictureBox3.Image = LicensePlate.IMAGEARR[2];
                pictureBox4.Image = LicensePlate.IMAGEARR[3];
                pictureBox5.Image = LicensePlate.IMAGEARR[4];
                pictureBox6.Image = LicensePlate.IMAGEARR[5];
                pictureBox7.Image = LicensePlate.IMAGEARR[6];
                pictureBox8.Image = LicensePlate.IMAGEARR[7];
                pictureBox9.Image = LicensePlate.IMAGEARR[8];
                //recognize
                Network.IMAGEARR = LicensePlate.IMAGEARR;
                int sum = LicensePlate.getsumcharacter();
                Network.recognition(sum,ImagePlate.Plate_Type);
                lbPlate.Text = Network.LICENSETEXT.Trim();
                show_result();

                InsertDataBase();
            }
            catch
            {
                statusStrip1.Items[1].Text = "Not Recognized";
            }
        }
//--------------------------------------------------------------------------------- 
        private void btnCapture_Click(object sender, EventArgs e)
        {
            try
            {

                timer1.Stop();
                cameraWindow1.Camera.Lock();
                picture.Image = AForge.Imaging.Image.Clone(cameraWindow1.Camera.LastFrame);
                cameraWindow1.Camera.Unlock();
                backgroundFrame = new Bitmap(Application.StartupPath + "\\anh\\nen.jpg");
                ImagePlate = new clsImagePlate(new Bitmap(picture.Image));
                
            }
            catch (Exception ex)
            {
                statusStrip1.Items[1].Text = ex.Message;
            }

        }
//-------------------------------------------------------------------------------------
        private void frmMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            exitToolStripMenuItem_Click(sender, e);
            
        }      
//---------------------------------------------------------------------
        private void toolStripMenuItem3_Click(object sender, EventArgs e)
        {
            CloseVideoSource();
        }
//--------------------------------------------------------------------------
        private void btnOpenData_Click(object sender, EventArgs e)
        {
            LoadList(dateTimePicker1.Text);
        }
//--------------------------------------------------------------------------
        public void LoadList(string date)
        {
            DataTable dt;
            ListViewItem it;
            dt = obj.GetBasicData(date);
            
            lvwData.Items.Clear();
            
            foreach (DataRow dr in dt.Rows)
            {
                it = lvwData.Items.Add(dr[0].ToString());
                for (int j = 1; j < dt.Columns.Count - 1; j++)
                {
                    it.SubItems.Add(dr[j].ToString());
                }
            }
        }
        
//==========serialPort==========================================
        private void setting()
        {

            serialPort1.PortName = "COM1";
            serialPort1.BaudRate = 9600;
            serialPort1.DataBits = 8;
            serialPort1.Encoding = Encoding.ASCII;
            serialPort1.Parity = Parity.None;
            serialPort1.StopBits = StopBits.One;
            serialPort1.ReceivedBytesThreshold = 1;
        }

      

        private void serialPort1_DataReceived(object sender, System.IO.Ports.SerialDataReceivedEventArgs e)
        {
            string datareceive = "";
            
            if (sender == serialPort1)
            {

                datareceive = serialPort1.ReadExisting();
                
                //MessageBox.Show(datareceive);
                
                if (datareceive=="A")
                {
                    btnCapture_Click(sender, e);                    
                    DisplayNumberPalate();
                    if (!(lbPlate.Text.Contains("?")))
                    {

                        if (serialPort1.IsOpen)
                        {
                            serialPort1.WriteLine("B");
                        }
                        else
                            MessageBox.Show("can't connect to comport");
                    }
                    else
                    {
                        if (serialPort1.IsOpen)
                            serialPort1.WriteLine("");

                        else
                            MessageBox.Show("can't connect to comport");
                    }
                    
                }
            }
        }
 //================database========================================
        private void loadListView(string num_plate, string  date,string time)
        {
            if (!(num_plate.Contains("?")))
            {
                ListViewItem li = new ListViewItem();
                li.Text = num_plate.ToString();
                li.SubItems.Add(date.ToString());
                li.SubItems.Add(time.ToString());
                //lvwData.Items.Clear();
                lvwData.Items.Add(li);
            }
        }
 //------------------------------------------------------------------- 
        private void InsertDataBase()
        {
            string num_plate;
            string date;
            string time;
            try
            {
                if (!(lbPlate.Text.Contains("?")))
                {
                    num_plate = lbPlate.Text;
                    date = lbDate.Text;
                    time = lbTime.Text;
                    obj.InsertBasicData(num_plate, date, time);
                }
                else throw new Exception("Not Recognized");
            }
            
            catch (Exception ex)
            {
                statusStrip1.Items[1].Text = ex.Message; 
            }
        }

        private void timer2_Tick(object sender, EventArgs e)
        {
            statusStrip1.Items[0].Text = DateTime.Now.ToString();
        }

        private void btnSend_Click(object sender, EventArgs e)
        {
            if (serialPort1.IsOpen)

                serialPort1.WriteLine("");

            else
                MessageBox.Show("can't connect to copmport");
        }

        private void button1_Click(object sender, EventArgs e)
        {
            
        }

    }

       
}