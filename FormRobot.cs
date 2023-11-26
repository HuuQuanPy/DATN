using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using EasyModbus;
using System.IO.Ports;


namespace GradutionThesis
{
    public partial class FormRobot : Form
    {

        ModbusClient myModbus = new ModbusClient();
        float[] theta = new float[4];
        float[] positions = new float[3];
        public FormRobot()
        {
            InitializeComponent();
            //GroupForward.Enabled = true;
            //GroupInverse.Enabled = true;
            //button1.Visible = true;
            //button2.Visible = true;
        }
        private void LoadLocations(float[] axis, TextBox text1, TextBox text2, TextBox text3)
        {
            text1.Text = Convert.ToString(axis[0]);
            text2.Text = Convert.ToString(axis[1]);
            text3.Text = Convert.ToString(axis[2]);
        }
        private void FormRobot_Load(object sender, EventArgs e)
        {
            string[] ports = SerialPort.GetPortNames();
            string[] baudrade = { "2400", "4800", "9600", "19200", "38400", "57600", "115200", "250000", "500000" };
            cbxCOM.Items.AddRange(ports);
            cbxBaudrate.Items.AddRange(baudrade);
            cbxBaudrate.SelectedIndex = 7;
            TxtForwardTheta1.Text = "90";
            TxtForwardTheta2.Text = "90";
            TxtForwardTheta3.Text = "-90";
            TxtForwardTheta4.Text = "-90";

            LoadLocations(RobotMath.LocationA1, TxtAxisX_CommodityA1, TxtAxisY_CommodityA1, TxtAxisZ_CommodityA1);
            LoadLocations(RobotMath.LocationA2, TxtAxisX_CommodityA2, TxtAxisY_CommodityA2, TxtAxisZ_CommodityA2);
            LoadLocations(RobotMath.LocationA3, TxtAxisX_CommodityA3, TxtAxisY_CommodityA3, TxtAxisZ_CommodityA3);
            LoadLocations(RobotMath.LocationA4, TxtAxisX_CommodityA4, TxtAxisY_CommodityA4, TxtAxisZ_CommodityA4);
            LoadLocations(RobotMath.LocationB1, TxtAxisX_CommodityB1, TxtAxisY_CommodityB1, TxtAxisZ_CommodityB1);
            LoadLocations(RobotMath.LocationB2, TxtAxisX_CommodityB2, TxtAxisY_CommodityB2, TxtAxisZ_CommodityB2);
            LoadLocations(RobotMath.LocationB3, TxtAxisX_CommodityB3, TxtAxisY_CommodityB3, TxtAxisZ_CommodityB3);
            LoadLocations(RobotMath.LocationB4, TxtAxisX_CommodityB4, TxtAxisY_CommodityB4, TxtAxisZ_CommodityB4);
            LoadLocations(RobotMath.LocationC1, TxtAxisX_CommodityC1, TxtAxisY_CommodityC1, TxtAxisZ_CommodityC1);
            LoadLocations(RobotMath.LocationC2, TxtAxisX_CommodityC2, TxtAxisY_CommodityC2, TxtAxisZ_CommodityC2);
            LoadLocations(RobotMath.LocationC3, TxtAxisX_CommodityC3, TxtAxisY_CommodityC3, TxtAxisZ_CommodityC3);
            LoadLocations(RobotMath.LocationC4, TxtAxisX_CommodityC4, TxtAxisY_CommodityC4, TxtAxisZ_CommodityC4);
        }

        private void btnConnect_Click(object sender, EventArgs e)
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
                    btnConnect.Text = "Disconnect";
                    btnConnect.BackColor = Color.OrangeRed;
                    cbxBaudrate.Enabled = false;
                    cbxCOM.Enabled = false;
                    //TimerAVR.Start();
                    //TimerAVR.Interval = 500;

                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
            else
            {
                myModbus.Disconnect();
                lblStatusCOM.Text = "Disconnect";
                lblStatusCOM.ForeColor = Color.Red;
                btnConnect.Text = "Connect";
                btnConnect.BackColor = Color.WhiteSmoke;
                cbxBaudrate.Enabled = true;
                cbxCOM.Enabled = true;
                //txtModbusStatus.Text = null;
                //TimerAVR.Stop();
            }

        }
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
        private void WriteSingleAngle(TrackBar trackBar, TextBox textBox, int startAdd, int startAngle)
        {
            if (check_Connect())
            {
                try
                {
                    if (Convert.ToSingle(textBox.Text) >= trackBar.Minimum && Convert.ToSingle(textBox.Text) <= trackBar.Maximum)
                    {
                        trackBar.Value = Convert.ToInt32(float.Parse(textBox.Text));
                        myModbus.UnitIdentifier = 1;
                        int[] temp = ModbusClient.ConvertFloatToRegisters(float.Parse(textBox.Text) - startAngle);
                        myModbus.WriteMultipleRegisters(startAdd, temp);
                    }
                    else
                    {
                        MessageBox.Show("Giá trị góc không hợp lệ", "Thông báo");
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }

            }
        }

        private void BtnForward_Click(object sender, EventArgs e)
        {
            if (check_Connect())
            {
                try
                {
                    myModbus.WriteSingleRegister(300, 0);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
                GroupForward.Enabled = true;
                GroupInverse.Enabled = false;
                GroupTrajectory.Enabled = false;
            }
        }
        private void DisplayForwardKinematics(TextBox textInput1, TextBox textInput2, TextBox textInput3, TextBox textInput4, TextBox textOutput1, TextBox textOutput2, TextBox textOutput3)
        {
            RobotMath.Forward forwardKinematic = new RobotMath.Forward
            {
                _theta1 = Convert.ToSingle(textInput1.Text),
                _theta2 = Convert.ToSingle(textInput2.Text),
                _theta3 = Convert.ToSingle(textInput3.Text),
                _theta4 = Convert.ToSingle(textInput4.Text)
            };
            float[] arrAxis = RobotMath.ForwardKinematics(forwardKinematic);
            textOutput1.Text = arrAxis[0].ToString("0.000");
            textOutput2.Text = arrAxis[1].ToString("0.000");
            textOutput3.Text = arrAxis[2].ToString("0.000");
        }

        private void DisplayInverseKinematics()
        {
            RobotMath.Inverse inverseKinematic = new RobotMath.Inverse
            {
                _axisX = Convert.ToSingle(TxtInverseAxisX.Text),
                _axisY = Convert.ToSingle(TxtInverseAxisY.Text),
                _axisZ = Convert.ToSingle(TxtInverseAxisZ.Text)
            };
            float[] arrtheta = RobotMath.InverseKinematics(inverseKinematic);
            TxtInverseTheta1.Text = arrtheta[0].ToString("0.000");
            TxtInverseTheta2.Text = arrtheta[1].ToString("0.000");
            TxtInverseTheta3.Text = arrtheta[2].ToString("0.000");
            TxtInverseTheta4.Text = arrtheta[3].ToString("0.000");
        }

        private void BtnForwardTheta1_Click(object sender, EventArgs e)
        {
            WriteSingleAngle(TrbTheta1, TxtForwardTheta1, 0, -RobotMath.startAngle1);
            DisplayForwardKinematics(TxtForwardTheta1, TxtForwardTheta2, TxtForwardTheta3, TxtForwardTheta4, TxtForwardAxisX, TxtForwardAxisY, TxtForwardAxisZ);
        }
        private void TrbTheta1_Scroll(object sender, EventArgs e)
        {
            if (check_Connect())
            {
                TxtForwardTheta1.Text = TrbTheta1.Value.ToString();
            }
        }

        private void BtnForwardTheta2_Click(object sender, EventArgs e)
        {
            WriteSingleAngle(TrbTheta2, TxtForwardTheta2, 2, RobotMath.startAngle2);
            DisplayForwardKinematics(TxtForwardTheta1, TxtForwardTheta2, TxtForwardTheta3, TxtForwardTheta4, TxtForwardAxisX, TxtForwardAxisY, TxtForwardAxisZ);
        }
        private void TrbTheta2_Scroll(object sender, EventArgs e)
        {
            if (check_Connect())
            {
                TxtForwardTheta2.Text = TrbTheta2.Value.ToString();
            }
        }

        private void BtnForwardTheta3_Click(object sender, EventArgs e)
        {
            WriteSingleAngle(TrbTheta3, TxtForwardTheta3, 4, -(RobotMath.startAngle2 + RobotMath.startAngle3));
            DisplayForwardKinematics(TxtForwardTheta1, TxtForwardTheta2, TxtForwardTheta3, TxtForwardTheta4, TxtForwardAxisX, TxtForwardAxisY, TxtForwardAxisZ);

        }
        private void TrbTheta3_Scroll(object sender, EventArgs e)
        {
            if (check_Connect())
            {
                TxtForwardTheta3.Text = TrbTheta3.Value.ToString();
            }
        }

        private void BtnForwardTheta4_Click(object sender, EventArgs e)
        {
            WriteSingleAngle(TrbTheta4, TxtForwardTheta4, 6, RobotMath.startAngle4);
            DisplayForwardKinematics(TxtForwardTheta1, TxtForwardTheta2, TxtForwardTheta3, TxtForwardTheta4, TxtForwardAxisX, TxtForwardAxisY, TxtForwardAxisZ);
        }
        private void TrbTheta4_Scroll(object sender, EventArgs e)
        {
            if (check_Connect())
            {
                TxtForwardTheta4.Text = TrbTheta4.Value.ToString();
            }
        }

        private void BtnForwardTheta5_Click(object sender, EventArgs e)
        {
            WriteSingleAngle(TrbTheta5, TxtForwardTheta5, 8, 0);
        }
        private void TrbTheta5_Scroll(object sender, EventArgs e)
        {
            if (check_Connect())
            {
                TxtForwardTheta5.Text = TrbTheta5.Value.ToString();
            }
        }

        private void FormRobot_FormClosed(object sender, FormClosedEventArgs e)
        {
            myModbus.Disconnect();
        }

        private void AxisText_TextChanged(object sender, EventArgs e)
        {
            if (TxtInverseAxisX.Text == "" || TxtInverseAxisY.Text == "" || TxtInverseAxisZ.Text == "")
            {
                BtnInverseSetpoint.Enabled = false;
            }
            else
            {
                BtnInverseSetpoint.Enabled = true;
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            //RobotMath.Forward fw1 = new RobotMath.Forward();
            ////Forward fw1 = new Forward();
            //fw1._theta1 = Convert.ToSingle(TxtForwardTheta1.Text);
            //fw1._theta2 = Convert.ToSingle(TxtForwardTheta2.Text);
            //fw1._theta3 = Convert.ToSingle(TxtForwardTheta3.Text);
            //fw1._theta4 = Convert.ToSingle(TxtForwardTheta4.Text);
            //float[] var = RobotMath.ForwardKinematics(fw1);
            ////float[] var = ForwardKinematics(fw1);
            //TxtForwardAxisX.Text = var[0].ToString("0.0000");
            //TxtForwardAxisY.Text = var[1].ToString("0.0000");
            //TxtForwardAxisZ.Text = var[2].ToString("0.0000");
        }

        private void button2_Click(object sender, EventArgs e)
        {
            //RobotMath.Inverse iv1 = new RobotMath.Inverse();
            ////inverse iv1 = new inverse();
            //iv1._axisX = Convert.ToSingle(TxtInverseAxisX.Text);
            //iv1._axisY = Convert.ToSingle(TxtInverseAxisY.Text);
            //iv1._axisZ = Convert.ToSingle(TxtInverseAxisZ.Text);
            //float[] vartheta = RobotMath.InverseKinematics(iv1);
            ////float[] vartheta = inversekinematics(iv1);
            //TxtInverseTheta1.Text = vartheta[0].ToString("0.000");
            //TxtInverseTheta2.Text = vartheta[1].ToString("0.000");
            //TxtInverseTheta3.Text = vartheta[2].ToString("0.000");
            //TxtInverseTheta4.Text = vartheta[3].ToString("0.000");
        }
        private void WriteMultipleAngle(RobotMath.Forward forward, int startAdd)
        {
            try
            {
                myModbus.UnitIdentifier = 1;
                int[] varTheta1 = ModbusClient.ConvertFloatToRegisters(forward._theta1 + RobotMath.startAngle1);
                int[] varTheta2 = ModbusClient.ConvertFloatToRegisters(forward._theta2 - RobotMath.startAngle2);
                int[] varTheta3 = ModbusClient.ConvertFloatToRegisters(forward._theta3 + RobotMath.startAngle2 + RobotMath.startAngle3);
                int[] varTheta4 = ModbusClient.ConvertFloatToRegisters(forward._theta4 - RobotMath.startAngle4);
                int[] sendTheTa = { varTheta1[0], varTheta1[1], varTheta2[0], varTheta2[1], varTheta3[0], varTheta3[1], varTheta4[0], varTheta4[1] };
                myModbus.WriteMultipleRegisters(startAdd, sendTheTa);
            }

            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        private void SaveLocations(RobotMath.Inverse location, int startAddress, TextBox text1, TextBox text2, TextBox text3)
        {
            if (check_Connect())
            {
                location._axisX = Convert.ToSingle(text1.Text);
                location._axisY = Convert.ToSingle(text2.Text);
                location._axisZ = Convert.ToSingle(text3.Text);

                float[] arrAngle = RobotMath.InverseKinematics(location);
                if (RobotMath.errWorkSpace)
                {
                    MessageBox.Show("Tọa độ nằm ngoài không gian làm việc", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    RobotMath.Forward arrTheta = new RobotMath.Forward
                    {
                        _theta1 = arrAngle[0],
                        _theta2 = arrAngle[1],
                        _theta3 = arrAngle[2],
                        _theta4 = arrAngle[3]
                    };
                    WriteMultipleAngle(arrTheta, startAddress);
                }
            }
        }

        private void BtnTrajectory_Click(object sender, EventArgs e)
        {
            if (check_Connect())
            {
                GroupTrajectory.Enabled = true;
                GroupInverse.Enabled = false;
                GroupForward.Enabled = false;
            }
            
        }
        RobotMath.Inverse iv3, iv4;

        private void BtnSetFirstPoint_Click(object sender, EventArgs e)
        {
            iv3 = new RobotMath.Inverse
            {
                _axisX = Convert.ToSingle(TxtOrbitAxisX.Text),
                _axisY = Convert.ToSingle(TxtOrbitAxisY.Text),
                _axisZ = Convert.ToSingle(TxtOrbitAxisZ.Text)
            };
            RobotMath.InverseKinematics(iv3);
        }

        private void BtnSetSecondPoint_Click(object sender, EventArgs e)
        {
            iv4 = new RobotMath.Inverse
            {
                _axisX = Convert.ToSingle(TxtOrbitAxisX_2nd.Text),
                _axisY = Convert.ToSingle(TxtOrbitAxisY_2nd.Text),
                _axisZ = Convert.ToSingle(TxtOrbitAxisZ_2nd.Text)
            };
        }

        private void BtnLineOrbit_Click(object sender, EventArgs e)
        {
            if (check_Connect())
            {
                if (iv3._axisZ == iv4._axisZ)
                {
                    float[] arrTheTa = RobotMath.InverseKinematics(iv3);
                    RobotMath.Forward fw3 = new RobotMath.Forward
                    {
                        _theta1 = arrTheTa[0],
                        _theta2 = arrTheTa[1],
                        _theta3 = arrTheTa[2],
                        _theta4 = arrTheTa[3]
                    };
                    WriteMultipleAngle(fw3, 0);
                    TimerOrbit.Start();
                    TimerOrbit.Interval = 100;
                }

            }
        }
        RobotMath.Inverse iv5 = new RobotMath.Inverse();
        RobotMath.Forward fw4 = new RobotMath.Forward();
        int index = 0;

        private void TimerOrbit_Tick(object sender, EventArgs e)
        {

            //iv5 = new Inverse();
            iv5._axisX = iv3._axisX + (iv4._axisX - iv3._axisX) * index / 100;
            iv5._axisY = ((iv4._axisY - iv3._axisY) / (iv4._axisX - iv3._axisX)) * (iv5._axisX - iv3._axisX) + iv3._axisY;
            iv5._axisZ = iv3._axisZ;
            float[] arrTemp = RobotMath.InverseKinematics(iv5);

            //fw4 = new Forward

            fw4._theta1 = arrTemp[0];
            fw4._theta2 = arrTemp[1];
            fw4._theta3 = arrTemp[2];
            fw4._theta4 = arrTemp[3];

            WriteMultipleAngle(fw4, 0);
            index += 1;
            if (index >= 100)
            {
                TimerOrbit.Stop();
            }

        }
        


        private void btnRunSystem_Click(object sender, EventArgs e)
        {
            if (check_Connect())
            {
                myModbus.WriteSingleRegister(300, 1);
            }
        }

        private void btnRunHome_Click(object sender, EventArgs e)
        {
            if (check_Connect())
            {
                try
                {
                    myModbus.WriteSingleRegister(300, 10);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);

                }

            }
        }

        private void BtnInverse_Click(object sender, EventArgs e)
        {
            if (check_Connect())
            {
                try
                {
                    GroupInverse.Enabled = true;
                    GroupForward.Enabled = false;
                    GroupTrajectory.Enabled = false;
                    myModbus.WriteSingleRegister(300, 0);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
        }
        private void BtnInverseSetpoint_Click(object sender, EventArgs e)
        {
            RobotMath.Inverse locationHome = new RobotMath.Inverse();
            SaveLocations(locationHome, 0, TxtInverseAxisX, TxtInverseAxisY, TxtInverseAxisZ);
            DisplayInverseKinematics();
        }

        private void SaveCommodity(float[] varAxis, TextBox text1, TextBox text2, TextBox text3)
        {
            if (check_Connect())
            {
                RobotMath.Inverse local = new RobotMath.Inverse
                {
                    _axisX = Convert.ToSingle(text1.Text),
                    _axisY = Convert.ToSingle(text2.Text),
                    _axisZ = Convert.ToSingle(text3.Text),
                };
                RobotMath.InverseKinematics(local);
                if(!RobotMath.errWorkSpace)
                {
                    varAxis[0] = Convert.ToSingle(text1.Text);
                    varAxis[1] = Convert.ToSingle(text2.Text);
                    varAxis[2] = Convert.ToSingle(text3.Text);
                    MessageBox.Show("Thay đổi vị trí thành công!","", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show("Vị trí vừa nhập nằm ngoài không gian làm việc!","",MessageBoxButtons.OK,MessageBoxIcon.Error);
                }
            }
        }

        private void BtnSetPointCommodutyA1_Click(object sender, EventArgs e)
        {
            SaveCommodity(RobotMath.LocationA1, TxtAxisX_CommodityA1, TxtAxisY_CommodityA1, TxtAxisZ_CommodityA1);
        }

        private void BtnSetPointCommodutyA2_Click(object sender, EventArgs e)
        {
            SaveCommodity(RobotMath.LocationA2, TxtAxisX_CommodityA2, TxtAxisY_CommodityA2, TxtAxisZ_CommodityA2);
        }

        private void BtnSetPointCommodutyA3_Click(object sender, EventArgs e)
        {
            SaveCommodity(RobotMath.LocationA3, TxtAxisX_CommodityA3, TxtAxisY_CommodityA3, TxtAxisZ_CommodityA3);
        }

        private void BtnSetPointCommodutyA4_Click(object sender, EventArgs e)
        {
            SaveCommodity(RobotMath.LocationA4, TxtAxisX_CommodityA4, TxtAxisY_CommodityA4, TxtAxisZ_CommodityA4);
        }

        private void BtnSetPointCommodutyB1_Click(object sender, EventArgs e)
        {
            SaveCommodity(RobotMath.LocationB1, TxtAxisX_CommodityB1, TxtAxisY_CommodityB1, TxtAxisZ_CommodityB1);
        }

        private void BtnSetPointCommodutyB2_Click(object sender, EventArgs e)
        {
            SaveCommodity(RobotMath.LocationB2, TxtAxisX_CommodityB2, TxtAxisY_CommodityB2, TxtAxisZ_CommodityB2);
        }

        private void BtnSetPointCommodutyB3_Click(object sender, EventArgs e)
        {
            SaveCommodity(RobotMath.LocationB3, TxtAxisX_CommodityB3, TxtAxisY_CommodityB3, TxtAxisZ_CommodityB3);
        }

        private void BtnSetPointCommodutyB4_Click(object sender, EventArgs e)
        {
            SaveCommodity(RobotMath.LocationB4, TxtAxisX_CommodityB4, TxtAxisY_CommodityB4, TxtAxisZ_CommodityB4);
        }

        private void BtnSetPointCommodutyC1_Click(object sender, EventArgs e)
        {
            SaveCommodity(RobotMath.LocationC1, TxtAxisX_CommodityC1, TxtAxisY_CommodityC1, TxtAxisZ_CommodityC1);
        }

        private void BtnSetPointCommodutyC2_Click(object sender, EventArgs e)
        {
            SaveCommodity(RobotMath.LocationC2, TxtAxisX_CommodityC2, TxtAxisY_CommodityC2, TxtAxisZ_CommodityC2);
        }

        private void BtnSetPointCommodutyC3_Click(object sender, EventArgs e)
        {
            SaveCommodity(RobotMath.LocationC3, TxtAxisX_CommodityC3, TxtAxisY_CommodityC3, TxtAxisZ_CommodityC3);
        }


        private void BtnSetPointCommodutyC4_Click(object sender, EventArgs e)
        {
            SaveCommodity(RobotMath.LocationC4, TxtAxisX_CommodityC4, TxtAxisY_CommodityC4, TxtAxisZ_CommodityC4);
        }



        private void TxtLocationTest_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)Keys.Enter)
            {
                e.Handled = true;
                if (check_Connect())
                {
                    string text = TxtLocationTest.Text;
                    switch (text)
                    {
                        case "A1":
                            TxtInverseAxisX.Text = Convert.ToString(RobotMath.LocationA1[0]);
                            TxtInverseAxisY.Text = Convert.ToString(RobotMath.LocationA1[1]);
                            TxtInverseAxisZ.Text = Convert.ToString(RobotMath.LocationA1[2]);
                            break;
                        case "A2":
                            TxtInverseAxisX.Text = Convert.ToString(RobotMath.LocationA2[0]);
                            TxtInverseAxisY.Text = Convert.ToString(RobotMath.LocationA2[1]);
                            TxtInverseAxisZ.Text = Convert.ToString(RobotMath.LocationA2[2]);
                            break;

                        case "A3":
                            TxtInverseAxisX.Text = Convert.ToString(RobotMath.LocationA3[0]);
                            TxtInverseAxisY.Text = Convert.ToString(RobotMath.LocationA3[1]);
                            TxtInverseAxisZ.Text = Convert.ToString(RobotMath.LocationA3[2]);
                            break;

                        case "A4":
                            TxtInverseAxisX.Text = Convert.ToString(RobotMath.LocationA4[0]);
                            TxtInverseAxisY.Text = Convert.ToString(RobotMath.LocationA4[1]);
                            TxtInverseAxisZ.Text = Convert.ToString(RobotMath.LocationA4[2]);
                            break;

                        case "B1":
                            TxtInverseAxisX.Text = Convert.ToString(RobotMath.LocationB1[0]);
                            TxtInverseAxisY.Text = Convert.ToString(RobotMath.LocationB1[1]);
                            TxtInverseAxisZ.Text = Convert.ToString(RobotMath.LocationB1[2]);
                            break;

                        case "B2":
                            TxtInverseAxisX.Text = Convert.ToString(RobotMath.LocationB2[0]);
                            TxtInverseAxisY.Text = Convert.ToString(RobotMath.LocationB2[1]);
                            TxtInverseAxisZ.Text = Convert.ToString(RobotMath.LocationB2[2]);
                            break;

                        case "B3":
                            TxtInverseAxisX.Text = Convert.ToString(RobotMath.LocationB3[0]);
                            TxtInverseAxisY.Text = Convert.ToString(RobotMath.LocationB3[1]);
                            TxtInverseAxisZ.Text = Convert.ToString(RobotMath.LocationB3[2]);
                            break;

                        case "B4":
                            TxtInverseAxisX.Text = Convert.ToString(RobotMath.LocationB4[0]);
                            TxtInverseAxisY.Text = Convert.ToString(RobotMath.LocationB4[1]);
                            TxtInverseAxisZ.Text = Convert.ToString(RobotMath.LocationB4[2]);
                            break;

                        case "C1":
                            TxtInverseAxisX.Text = Convert.ToString(RobotMath.LocationC1[0]);
                            TxtInverseAxisY.Text = Convert.ToString(RobotMath.LocationC1[1]);
                            TxtInverseAxisZ.Text = Convert.ToString(RobotMath.LocationC1[2]);
                            break;

                        case "C2":
                            TxtInverseAxisX.Text = Convert.ToString(RobotMath.LocationC2[0]);
                            TxtInverseAxisY.Text = Convert.ToString(RobotMath.LocationC2[1]);
                            TxtInverseAxisZ.Text = Convert.ToString(RobotMath.LocationC2[2]);
                            break;

                        case "C3":
                            TxtInverseAxisX.Text = Convert.ToString(RobotMath.LocationC3[0]);
                            TxtInverseAxisY.Text = Convert.ToString(RobotMath.LocationC3[1]);
                            TxtInverseAxisZ.Text = Convert.ToString(RobotMath.LocationC3[2]);
                            break;

                        case "C4":
                            TxtInverseAxisX.Text = Convert.ToString(RobotMath.LocationC4[0]);
                            TxtInverseAxisY.Text = Convert.ToString(RobotMath.LocationC4[1]);
                            TxtInverseAxisZ.Text = Convert.ToString(RobotMath.LocationC4[2]);
                            break;


                        default:
                            break;
                    }
                }
            }
        }
    }
}

