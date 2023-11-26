using System;
using System.Drawing;
using System.Windows.Forms;
using System.Threading;
using EasyModbus;
using System.IO.Ports;
using AForge.Video.DirectShow;
using AForge.Video;
using Emgu.CV;
using Emgu.CV.Structure;

namespace GradutionThesis
{

    public partial class FormMain : Form
    {
        public FormMain()
        {
            InitializeComponent();
            //Control.CheckForIllegalCrossThreadCalls = true;
        }
        FilterInfoCollection infoCollection;
        VideoCaptureDevice videoCaptureDevice;
        Image<Bgr, Byte> currentFrame;

        ModbusClient myModbus = new ModbusClient();

        byte slaveID_Arduino = 1;
        byte slaveID_AVR = 29;
        int modeArduino = 300;
        int modeAVR = 0;
        int startAddress_Arduino = 301;
        int startAddress_AVR = 2;
        int speedStepJ1, speedStepJ2, speedStepJ3;

        /// <summary>
        /// Gọi hàm ghi tất cả giá trị các góc xuống Slave từ Master
        /// </summary>
        /// <param name="forward1"></param> Thông số các góc tại vị trí chứa hàng 
        /// <param name="forward2"></param> Thông số các góc tại vị trí gắp
        /// <param name="startAdd"></param> Địa chỉ bắt đầu ghi từ Modbus Master
        /// <param name="switchLocation"></param> Công tắc để robot thực hiện gắp thả vật đúng vị trí nhận hàng

        private void WriteMultipleAngle(RobotMath.Forward forward1, RobotMath.Forward forward2, int startAdd, int switchLocation)
        {
            try
            {
                myModbus.UnitIdentifier = slaveID_Arduino;
                int[] varTheta1 = ModbusClient.ConvertFloatToRegisters(forward1._theta1 + RobotMath.startAngle1);
                int[] varTheta2 = ModbusClient.ConvertFloatToRegisters(forward1._theta2 - RobotMath.startAngle2);
                int[] varTheta3 = ModbusClient.ConvertFloatToRegisters(forward1._theta3 + RobotMath.startAngle2 + RobotMath.startAngle3);
                int[] varTheta4 = ModbusClient.ConvertFloatToRegisters(forward1._theta4 - RobotMath.startAngle4);

                int[] varTheta5 = ModbusClient.ConvertFloatToRegisters(forward2._theta1 + RobotMath.startAngle1);
                int[] varTheta6 = ModbusClient.ConvertFloatToRegisters(forward2._theta2 - RobotMath.startAngle2);
                int[] varTheta7 = ModbusClient.ConvertFloatToRegisters(forward2._theta3 + RobotMath.startAngle2 + RobotMath.startAngle3);
                int[] varTheta8 = ModbusClient.ConvertFloatToRegisters(forward2._theta4 - RobotMath.startAngle4);

                int[] sendTheTa = { switchLocation, varTheta1[0], varTheta1[1], varTheta2[0], varTheta2[1], varTheta3[0], varTheta3[1], varTheta4[0], varTheta4[1],
                                    varTheta5[0], varTheta5[1], varTheta6[0], varTheta6[1], varTheta7[0], varTheta7[1], varTheta8[0], varTheta8[1], speedStepJ1, speedStepJ2, speedStepJ3};
                myModbus.WriteMultipleRegisters(startAdd, sendTheTa);
            }

            catch (Exception)
            {
                //MessageBox.Show(ex.Message);
            }
        }

        bool tempErros1 = false;
        bool temmErros2 = false;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="location1"></param>
        /// <param name="location2"></param>
        /// <param name="startAddress"></param>
        /// <param name="axis"></param>
        /// <param name="axisCamera"></param>
        /// <param name="text"></param>

        private void SaveLocations(RobotMath.Inverse location1, RobotMath.Inverse location2, int startAddress, float[] axis, float[] axisCamera)
        {

            location1._axisX = axis[0];
            location1._axisY = axis[1];
            location1._axisZ = axis[2];
            float[] arrTheta = RobotMath.InverseKinematics(location1);
            RobotMath.Forward angle = new RobotMath.Forward();
            if (!RobotMath.errWorkSpace)
            {
                tempErros1 = true;
                angle._theta1 = arrTheta[0];
                angle._theta2 = arrTheta[1];
                angle._theta3 = arrTheta[2];
                angle._theta4 = arrTheta[3];

            }

            location2._axisX = axisCamera[0];
            location2._axisY = axisCamera[1];
            location2._axisZ = axisCamera[2];
            float[] arrTheta2 = RobotMath.InverseKinematics(location2);
            RobotMath.Forward angle2 = new RobotMath.Forward();
            if (!RobotMath.errWorkSpace)
            {
                temmErros2 = true;
                angle2._theta1 = arrTheta2[0];
                angle2._theta2 = arrTheta2[1];
                angle2._theta3 = arrTheta2[2];
                angle2._theta4 = arrTheta2[3];
            }

            double Distance = Math.Sqrt(Math.Pow((RobotMath.LocationA4[0] - axisCamera[0]), 2) + Math.Pow((RobotMath.LocationA4[1] - axisCamera[1]), 2));
            double tempDistance = Math.Sqrt(Math.Pow((axis[0] - axisCamera[0]), 2) + Math.Pow((axis[1] - axisCamera[1]), 2));
            speedStepJ1 = Convert.ToInt32(7000 * Math.Pow((tempDistance / Distance), 0.4F));
            speedStepJ2 = Convert.ToInt32(5000 * Math.Pow((tempDistance / Distance), 0.4F));
            speedStepJ3 = Convert.ToInt32(1500 * Math.Pow((tempDistance / Distance), 0.4F));

            if (tempErros1 && temmErros2)
            {
                WriteMultipleAngle(angle, angle2, startAddress, locationQRCode);
            }
        }
        //private void SendLocationsHome()
        //{
        //    RobotMath.Forward fwHoming = new RobotMath.Forward
        //    {
        //        _theta1 = 120,
        //        _theta2 = 75,
        //        _theta3 = -90,
        //        _theta4 = -60
        //    };
        //    myModbus.UnitIdentifier = 1;
        //    int[] varTheta1 = ModbusClient.ConvertFloatToRegisters(fwHoming._theta1 + RobotMath.startAngle1);
        //    int[] varTheta2 = ModbusClient.ConvertFloatToRegisters(fwHoming._theta2 - RobotMath.startAngle2);
        //    int[] varTheta3 = ModbusClient.ConvertFloatToRegisters(fwHoming._theta3 + RobotMath.startAngle2 + RobotMath.startAngle3);
        //    int[] varTheta4 = ModbusClient.ConvertFloatToRegisters(fwHoming._theta4 - RobotMath.startAngle4);

        //    int[] sendTheTa = { varTheta1[0], varTheta1[1], varTheta2[0], varTheta2[1], varTheta3[0], varTheta3[1], varTheta4[0], varTheta4[1] };
        //    myModbus.WriteMultipleRegisters(0, sendTheTa);
        //}

        private void LoadCOM()
        {
            string[] ports = System.IO.Ports.SerialPort.GetPortNames();
            string[] baudrate = { "2400", "4800", "9600", "19200", "28800", "38400", "57600", "115200", "250000", "500000" };
            cbxCOM.Items.Clear();
            cbxBaudrate.Items.Clear();

            cbxCOM.Items.AddRange(ports);
            cbxBaudrate.Items.AddRange(baudrate);

            cbxBaudrate.SelectedIndex = 8;
        }

        private void LoadCamera()
        {
            infoCollection = new FilterInfoCollection(FilterCategory.VideoInputDevice);
            foreach (FilterInfo info in infoCollection)
            {
                CbxSelectCamera.Items.Add(info.Name);
            }
        }

        private void LoadSpeedConveyor()
        {
            string[] arrSpeed = { "1x", "2x", "3x", "4x" };
            cbxSpeedConveyor.Items.AddRange(arrSpeed);
        }

        private void FormMain_Load(object sender, EventArgs e)
        {
            lblInforUserName.Text = "Hello: " + DTO.Username.sTaiKhoan;
            LoadCOM();
            LoadCamera();
            LoadSpeedConveyor();
            //BtnStart.Enabled = false;
            GroupImport.Enabled = false;
            GroupExport.Enabled = false;

            listViewStorage.GridLines = true;
            listViewStorage.View = View.Details;

            listViewStorage.Columns.Add("", 60);
            listViewStorage.Columns.Add("Loc", 50);
            listViewStorage.Columns.Add("            DATE    ", 150);
            listViewStorage.Columns.Add("   QR CODE ", 148);
        }

        private void BtnFormRobot_Click(object sender, EventArgs e)
        {
            FormRobot formRobot = new FormRobot();
            if (DTO.Username.sMaQuyen == 1)
            {
                DisConnectModbus();
                formRobot.ShowDialog();

            }
            else
            {
                MessageBox.Show("Không được phép truy cập!\nChức năng chỉ dành cho Quản trị viên",
                    "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Stop);
            }
        }

        private void linkLabelStorage_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            FormProductActually formProduct = new FormProductActually();
            if (DTO.Username.sMaQuyen == 1)
            {
                formProduct.ShowDialog();
            }
            else
            {
                MessageBox.Show("Không được phép truy cập!\nChức năng chỉ dành cho Quản trị viên",
                    "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Stop);
            }
        }

        private void toolStripMenuItemLogOut_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void toolStripMenuItemChangePass_Click(object sender, EventArgs e)
        {
            FormPasswordChanged formPassword = new FormPasswordChanged();
            formPassword.ShowDialog();
        }

        private void DisConnectModbus()
        {
            myModbus.Disconnect();
            lblStatusCOM.Text = "Disconnect";
            lblStatusCOM.ForeColor = Color.Red;
            BtnConnect.Text = "Connect";
            BtnConnect.BackColor = Color.WhiteSmoke;
            cbxBaudrate.Enabled = true;
            cbxCOM.Enabled = true;

            TimerReadData.Stop();
        }

        private void FormMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                DialogResult result = MessageBox.Show("Do You Want To Exit?", "Notification", MessageBoxButtons.OKCancel, MessageBoxIcon.Question);
                if (result == DialogResult.OK)
                {
                    e.Cancel = false;
                    myModbus.Disconnect();
                    if (videoCaptureDevice != null)
                    {
                        videoCaptureDevice.Stop();
                        videoCaptureDevice = null;
                    }

                }

                if (result == DialogResult.Cancel)
                {
                    e.Cancel = true;
                }
            }
        }

        /// Groupbox COM Setting

        private void BtnConnect_Click(object sender, EventArgs e)
        {
            if (myModbus.Connected == false)
            {
                try
                {
                    myModbus.SerialPort = cbxCOM.Text;
                    myModbus.Baudrate = int.Parse(cbxBaudrate.Text);
                    myModbus.StopBits = StopBits.One;
                    myModbus.Parity = Parity.None;
                    myModbus.Connect();
                    lblStatusCOM.Text = "Connected";
                    lblStatusCOM.ForeColor = Color.Blue;
                    BtnConnect.Text = "Disconnect";
                    BtnConnect.BackColor = Color.OrangeRed;
                    cbxBaudrate.Enabled = false;
                    cbxCOM.Enabled = false;

                    TimerReadData.Start();
                    TimerReadData.Interval = 500;
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
            else
            {
                DisConnectModbus();
            }
        }

        private void BtnLoadCOM_Click(object sender, EventArgs e)
        {
            LoadCOM();
        }

        // Gọi hàm kiểm tra kết nối chặn các sự kiện khi Modbus chưa được khời động

        private bool check_Connect()
        {
            if (myModbus.Connected == false)
            {
                MessageBox.Show("No connected", "Notification", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
            else
                return true;
        }

        ZXing.BarcodeReader barcode;
        ZXing.Result[] result;
        ZXing.ResultPoint[] point;
        Point A0, B0, C0, D0, G0;
        int locationQRCode = 0;
        string textQR;

        private void CheckQRcode()
        {
            if (picFrameCamera.Image != null)
            {
                try
                {
                    barcode = new ZXing.BarcodeReader();
                    result = barcode.DecodeMultiple((Bitmap)picFrameCamera.Image);
                    if (result != null)
                    {
                        
                        textQR = result[0].ToString();
                        point = result[0].ResultPoints;
                        QRcode rcode = new QRcode();

                        if (textQR == rcode.CodeA1)
                        {
                            locationQRCode = 1;
                        }
                        else if (textQR == rcode.CodeA2)
                        {
                            locationQRCode = 2;
                        }
                        else if (textQR == rcode.CodeA3)
                        {
                            locationQRCode = 3;
                        }
                        else if (textQR == rcode.CodeA4)
                        {
                            locationQRCode = 4;
                        }
                        else if (textQR == rcode.CodeB1)
                        {
                            locationQRCode = 5;
                        }
                        else if (textQR == rcode.CodeB2)
                        {
                            locationQRCode = 6;
                        }
                        else if (textQR == rcode.CodeB3)
                        {
                            locationQRCode = 7;
                        }
                        else if (textQR == rcode.CodeB4)
                        {
                            locationQRCode = 8;
                        }
                        else if (textQR == rcode.CodeC1)
                        {
                            locationQRCode = 9;
                        }
                        else if (textQR == rcode.CodeC2)
                        {
                            locationQRCode = 10;
                        }
                        else if (textQR == rcode.CodeC3)
                        {
                            locationQRCode = 11;
                        }
                        else if (textQR == rcode.CodeC4)
                        {
                            locationQRCode = 12;
                        }
                        A0.X = Convert.ToInt32((point[0].X + point[1].X) / 2);
                        A0.Y = Convert.ToInt32((point[0].Y + point[1].Y) / 2);
                        B0.X = Convert.ToInt32((point[1].X + point[2].X) / 2);
                        B0.Y = Convert.ToInt32((point[1].Y + point[2].Y) / 2);
                        C0.X = Convert.ToInt32((point[2].X + point[3].X) / 2);
                        C0.Y = Convert.ToInt32((point[2].Y + point[3].Y) / 2);
                        D0.X = Convert.ToInt32((point[3].X + point[0].X) / 2);
                        D0.Y = Convert.ToInt32((point[3].Y + point[0].Y) / 2);
                        G0.X = (A0.X + C0.X) / 2;
                        G0.Y = (A0.Y + C0.Y) / 2;

                        CvInvoke.Line(currentFrame, A0, C0, new MCvScalar(200, 0, 0), 5);
                        CvInvoke.Line(currentFrame, B0, D0, new MCvScalar(200, 0, 0), 5);

                        RobotMath.LocationCam[0] = RobotMath.Point_TopLeftCAM[0] - (G0.Y * 12.86F / 480);
                        RobotMath.LocationCam[1] = RobotMath.Point_TopLeftCAM[1] - (G0.X * 18.5635F / 640);
                        RobotMath.LocationCam[2] = 13.6F;
                        txtQR_X.Text = RobotMath.LocationCam[0].ToString("0.0000");
                        txtQR_Y.Text = RobotMath.LocationCam[1].ToString("0.0000");
                        txtQR_Z.Text = RobotMath.LocationCam[2].ToString("0.0000");
                        //txtQR_X.Text = G0.X.ToString();
                        //txtQR_Y.Text = G0.Y.ToString();
                    }
                    else
                    {
                        locationQRCode = 0;
                    }
                    picFrameCamera.Image = currentFrame.ToBitmap();
                }
                catch (Exception) { }

            }

        }
        private void TimerQR_Tick(object sender, EventArgs e)
        {
            CheckQRcode();
        }

        // Group Control System

        private void BtnStart_Click(object sender, EventArgs e)
        {
            if (check_Connect())
            {
                try
                {
                    RobotMath.LocationCam[0] = -29.587F;
                    RobotMath.LocationCam[1] = 7.032F;
                    RobotMath.LocationCam[2] = 13.572F;

                    myModbus.UnitIdentifier = slaveID_AVR;
                    myModbus.WriteSingleRegister(modeAVR, 2);

                    myModbus.UnitIdentifier = slaveID_Arduino;
                    myModbus.WriteSingleRegister(modeArduino, 1);

                    TimerQR.Start();
                    TimerQR.Interval = 100;

                    TimerTransmitRB.Start();
                    TimerTransmitRB.Interval = 9000;

                    sLampStop.DiscreteValue1 = false;
                    sLampControl.DiscreteValue1 = true;
                }
                catch (Exception ex)
                {

                    MessageBox.Show(ex.Message);
                }

            }
        }

        private void TimerTransmitRB_Tick(object sender, EventArgs e)
        {
            SendQRcode();
            VisibleCommodity();
            ShowInforImport();
        }

        private void BtnStop_Click(object sender, EventArgs e)
        {
            if (check_Connect())
            {
                try
                {
                    myModbus.UnitIdentifier = slaveID_AVR;
                    myModbus.WriteSingleRegister(modeAVR, 0);
                    TimerQR.Stop();
                    TimerTransmitRB.Stop();

                    sLampStop.DiscreteValue1 = true;
                    sLampControl.DiscreteValue1 = false;
                }
                catch (Exception ex)
                {

                    MessageBox.Show(ex.Message);
                }
            }
        }

        private void BtnPause_Click(object sender, EventArgs e)
        {

        }

        RobotMath.Inverse location = new RobotMath.Inverse();
        RobotMath.Inverse locationCamera = new RobotMath.Inverse();

        private void SendQRcode()
        {
            if (check_Connect())
            {
                try
                {
                    switch (locationQRCode)
                    {

                        case 1:
                            SaveLocations(location, locationCamera, startAddress_Arduino, RobotMath.LocationA1, RobotMath.LocationCam);
                            lblCodeProduct.Text = "Vị trí: A1";
                            break;

                        case 2:
                            SaveLocations(location, locationCamera, startAddress_Arduino, RobotMath.LocationA2, RobotMath.LocationCam);
                            lblCodeProduct.Text = "Vị trí: A2";
                            break;

                        case 3:
                            SaveLocations(location, locationCamera, startAddress_Arduino, RobotMath.LocationA3, RobotMath.LocationCam);
                            lblCodeProduct.Text = "Vị trí : A3";
                            break;

                        case 4:
                            SaveLocations(location, locationCamera, startAddress_Arduino, RobotMath.LocationA4, RobotMath.LocationCam);
                            lblCodeProduct.Text = "Vị trí : A4";
                            break;

                        case 5:
                            SaveLocations(location, locationCamera, startAddress_Arduino, RobotMath.LocationB1, RobotMath.LocationCam);
                            lblCodeProduct.Text = "Vị trí : B1";
                            break;

                        case 6:
                            SaveLocations(location, locationCamera, startAddress_Arduino, RobotMath.LocationB2, RobotMath.LocationCam);
                            lblCodeProduct.Text = "Vị trí : B2";
                            break;

                        case 7:
                            SaveLocations(location, locationCamera, startAddress_Arduino, RobotMath.LocationB3, RobotMath.LocationCam);
                            lblCodeProduct.Text = "Vị trí : B3";
                            break;

                        case 8:
                            SaveLocations(location, locationCamera, startAddress_Arduino, RobotMath.LocationB4, RobotMath.LocationCam);
                            lblCodeProduct.Text = "Vị trí : B4";
                            break;

                        case 9:
                            SaveLocations(location, locationCamera, startAddress_Arduino, RobotMath.LocationC1, RobotMath.LocationCam);
                            lblCodeProduct.Text = "Vị trí : C1";
                            break;

                        case 10:
                            SaveLocations(location, locationCamera, startAddress_Arduino, RobotMath.LocationC2, RobotMath.LocationCam);
                            lblCodeProduct.Text = "Vị trí : C2";
                            break;

                        case 11:
                            SaveLocations(location, locationCamera, startAddress_Arduino, RobotMath.LocationC3, RobotMath.LocationCam);
                            lblCodeProduct.Text = "Vị trí : C3";
                            break;

                        case 12:
                            SaveLocations(location, locationCamera, startAddress_Arduino, RobotMath.LocationC4, RobotMath.LocationCam);
                            lblCodeProduct.Text = "Vị trí : C4";
                            break;
                        default:
                            myModbus.WriteSingleRegister(301, 0);
                            break;
                    }

                }
                catch (Exception)
                {

                    // MessageBox.Show(ex.Message);
                }
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {

        }

        private void TimerCommodity_Tick(object sender, EventArgs e)
        {
            try
            {
                SendQRcode();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        // Groupbox Camera Setting and Detect QR code
        // Button Start
        // Button Stop


        private void BtnStartCamera_Click(object sender, EventArgs e)
        {
            try
            {
                if (videoCaptureDevice == null)
                {
                    videoCaptureDevice = new VideoCaptureDevice(infoCollection[CbxSelectCamera.SelectedIndex].MonikerString);
                    videoCaptureDevice.NewFrame += NewFrameConveyor;
                    videoCaptureDevice.Start();


                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

        }

        private void BtnPauseCamera_Click(object sender, EventArgs e)
        {
            if (videoCaptureDevice != null)
            {
                videoCaptureDevice.Stop();
                videoCaptureDevice = null;
                if (picFrameCamera.Image != null)
                {
                    picFrameCamera.Image = null;
                }
                TimerQR.Stop();
            }
        }

        private void NewFrameConveyor(object sender, NewFrameEventArgs eventArgs)
        {
            try
            {
                currentFrame = eventArgs.Frame.ToImage<Bgr, Byte>();
                picFrameCamera.Image = currentFrame.ToBitmap();
            }
            catch (Exception) { }
        }

        int tempMODE = 0;

        // Group Import Commodity 
        // Textbox Type, Count 
        // Button Set Type, Count
        // Button Import All 

        private void BtnImport_Click(object sender, EventArgs e)
        {
            if (check_Connect())
            {
                GroupImport.Enabled = true;
                GroupExport.Enabled = false;
                tempMODE = 1;
            }
         
        }

        private void BtnSetImport_Click(object sender, EventArgs e)
        {

        }

        private void BtnImportAll_Click(object sender, EventArgs e)
        {

        }

        // Group Export Commodity 
        // Textbox Type, Count 
        // Button Set Type, Count
        // Button Export All 

        private void BtnExport_Click(object sender, EventArgs e)
        {
            try
            {
                GroupImport.Enabled = false;
                GroupExport.Enabled = true;
                myModbus.UnitIdentifier = slaveID_AVR;
                myModbus.WriteSingleRegister(modeAVR, 2);

                myModbus.UnitIdentifier = slaveID_Arduino;
                myModbus.WriteSingleRegister(modeArduino, 2);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

        }

        private void BtnSetExport_Click(object sender, EventArgs e)
        {
            locationQRCode = 1;
            RobotMath.LocationCam[0] = -29;
            RobotMath.LocationCam[1] = 9;
            RobotMath.LocationCam[2] = 16;
            string text = txtTypeExport.Text;
            switch (text)
            {
                case "A1":
                    SaveLocations(location, locationCamera, startAddress_Arduino, RobotMath.LocationA1, RobotMath.LocationCam);
                    break;

                case "A2":
                    SaveLocations(location, locationCamera, startAddress_Arduino, RobotMath.LocationA2, RobotMath.LocationCam);
                    break;

                case "A3":
                    SaveLocations(location, locationCamera, startAddress_Arduino, RobotMath.LocationA3, RobotMath.LocationCam);
                    break;

                case "A4":
                    SaveLocations(location, locationCamera, startAddress_Arduino, RobotMath.LocationA4, RobotMath.LocationCam);
                    break;

                case "B1":
                    SaveLocations(location, locationCamera, startAddress_Arduino, RobotMath.LocationB1, RobotMath.LocationCam);
                    break;
                case "B2":
                    SaveLocations(location, locationCamera, startAddress_Arduino, RobotMath.LocationB2, RobotMath.LocationCam);
                    break;
                case "B3":
                    SaveLocations(location, locationCamera, startAddress_Arduino, RobotMath.LocationB3, RobotMath.LocationCam);
                    break;
                case "B4":
                    SaveLocations(location, locationCamera, startAddress_Arduino, RobotMath.LocationB4, RobotMath.LocationCam);
                    break;
                case "C1":
                    SaveLocations(location, locationCamera, startAddress_Arduino, RobotMath.LocationC1, RobotMath.LocationCam);
                    break;
                case "C2":
                    SaveLocations(location, locationCamera, startAddress_Arduino, RobotMath.LocationC2, RobotMath.LocationCam);
                    break;
                case "C3":
                    SaveLocations(location, locationCamera, startAddress_Arduino, RobotMath.LocationC3, RobotMath.LocationCam);
                    break;
                case "C4":
                    SaveLocations(location, locationCamera, startAddress_Arduino, RobotMath.LocationC4, RobotMath.LocationCam);
                    break;

                default:
                    break;
            }
        }

        private void BtnExportAll_Click(object sender, EventArgs e)
        {

        }


        private void BtnHoming_Click(object sender, EventArgs e)
        {
            //if (check_Connect())
            //{
            //    try
            //    {
            //        BtnStart.Enabled = true;
            //    }
            //    catch (Exception ex)
            //    {

            //        MessageBox.Show(ex.Message);
            //    }

            //}
        }

        private void ShowLocations(float[] axis)
        {
            txtX_AxisRobot.Text = axis[0].ToString("0.000");
            txtY_AxisRobot.Text = axis[1].ToString("0.000");
            txtZ_AxisRobot.Text = axis[2].ToString("0.000");
        }
        string textLocal;
        private void VisibleCommodity()
        {
            switch (locationQRCode)
            {
                case 1:
                    sControlCommodityA1.Visible = true;
                    ShowLocations(RobotMath.LocationA1);
                    textLocal = "A1";
                    break;

                case 2:
                    sControlCommodityA2.Visible = true;
                    ShowLocations(RobotMath.LocationA2);
                    textLocal = "A2";
                    break;

                case 3:
                    sControlCommodityA3.Visible = true;
                    ShowLocations(RobotMath.LocationA3);
                    textLocal = "A3";
                    break;

                case 4:
                    sControlCommodityA4.Visible = true;
                    ShowLocations(RobotMath.LocationA4);
                    textLocal = "A4";
                    break;

                case 5:
                    sControlCommodityB1.Visible = true;
                    ShowLocations(RobotMath.LocationB1);
                    textLocal = "B1";
                    break;

                case 6:
                    sControlCommodityB2.Visible = true;
                    ShowLocations(RobotMath.LocationB2);
                    textLocal = "B2";
                    break;

                case 7:
                    sControlCommodityB3.Visible = true;
                    ShowLocations(RobotMath.LocationB3);
                    textLocal = "B3";
                    break;

                case 8:
                    sControlCommodityB4.Visible = true;
                    ShowLocations(RobotMath.LocationB4);
                    textLocal = "B4";
                    break;

                case 9:
                    sControlCommodityC1.Visible = true;
                    ShowLocations(RobotMath.LocationC1);
                    textLocal = "C1";
                    break;

                case 10:
                    sControlCommodityC2.Visible = true;
                    ShowLocations(RobotMath.LocationC2);
                    textLocal = "C2";
                    break;

                case 11:
                    sControlCommodityC3.Visible = true;
                    ShowLocations(RobotMath.LocationC3);
                    textLocal = "C3";

                    break;
                case 12:

                    sControlCommodityC4.Visible = true;
                    ShowLocations(RobotMath.LocationC4);
                    textLocal = "C4";
                    break;

                default:
                    break;
            }

        }

        private void TimerVisibleCommodity_Tick(object sender, EventArgs e)
        {
            //TimerVisibleCommodity.Stop();
        }

        // Đặt tốc độ cho băng tải : 1,2,3,4 cấp

        private void BtnSetSpeedConveyor_Click_1(object sender, EventArgs e)
        {
            if (check_Connect())
            {
                myModbus.UnitIdentifier = slaveID_AVR;
                string strSpeed = cbxSpeedConveyor.Text;
                switch (strSpeed)
                {
                    case "1x":
                        myModbus.WriteSingleRegister(1, 1);
                        break;

                    case "2x":
                        myModbus.WriteSingleRegister(1, 2);
                        break;

                    case "3x":
                        myModbus.WriteSingleRegister(1, 3);
                        break;

                    case "4x":
                        myModbus.WriteSingleRegister(1, 4);
                        break;

                    default:
                        break;
                }
            }
        }


        // Đọc dữ liệu từ vi điều khiển

        private void CheckCoilandSymbol(int var, SymbolFactoryDotNet.StandardControl control)
        {
            if (var == 1)
            {
                control.DiscreteValue1 = true;
            }
            else
            {
                control.DiscreteValue1 = false;
            }
        }

        private void CheckCoilCylinder(int var)
        {
            if (var == 1)
            {
                sControlRunCommodity.Left = 912;
                sControlCylindersIN.Visible = false;
                sControlCylindersOut.Visible = true;
            }
            else
            {
                sControlRunCommodity.Left = 871;
                sControlCylindersIN.Visible = true;
                sControlCylindersOut.Visible = false;
            }
        }

        private void CheckSensorMidConveyor(int valueSensor)
        {
            if (valueSensor == 1)
            {
                try
                {
                    SendQRcode();
                    VisibleCommodity();
                    ShowInforImport();
                }
                catch (Exception)
                {

                }
            }
        }

        private void button1_Click_1(object sender, EventArgs e)
        {
            TimerQR.Start();
        }



        private void TimerMoveCommodity_Tick(object sender, EventArgs e)
        {
            //sControlRunCommodity.Left--;
        }

        //int[] arrCoils;
        private void TimerReadData_Tick(object sender, EventArgs e)
        {
            ReadDATA();
        }

        int[] arrCoils;
        void ReadDATA()
        {
            try
            {
                myModbus.UnitIdentifier = slaveID_AVR;
                arrCoils = myModbus.ReadHoldingRegisters(startAddress_AVR, 4);

                textBox1.Text = arrCoils[0].ToString();
                textBox2.Text = arrCoils[1].ToString();
                textBox3.Text = arrCoils[2].ToString();
                textBox4.Text = arrCoils[3].ToString();

                CheckCoilandSymbol(arrCoils[0], sControlMotorConveyor);
                CheckCoilandSymbol(arrCoils[0], sControlConveyor);
                CheckCoilandSymbol(arrCoils[0], sControlConveyor1);

                CheckCoilandSymbol(arrCoils[1], sControlSS_Commodity);

                CheckCoilandSymbol(arrCoils[2], sControlSS_MidConveyor);

                CheckCoilCylinder(arrCoils[3]);

            }
            catch (Exception)
            {

            }
        }


        private void ShowInforImport()
        {
            string timeQR = DateTime.Now.ToString();

            string[] arr = new string[4];
            ListViewItem item;

            arr[0] = "Import";
            arr[1] = textLocal;
            arr[2] = timeQR;
            arr[3] = textQR;
            item = new ListViewItem(arr);
            listViewStorage.Items.Add(item);
            listViewStorage.Items[listViewStorage.Items.Count - 1].EnsureVisible();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            //   ShowInforImport();
        }
    }
}
