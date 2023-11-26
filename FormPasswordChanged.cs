using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GradutionThesis
{
    public partial class FormPasswordChanged : Form
    {
        DTO.Username user = new DTO.Username();
        BLL.UserNameBLL userBLL = new BLL.UserNameBLL();
        public FormPasswordChanged()
        {
            InitializeComponent();
            txtUsername.Texts = DTO.Username.sTaiKhoan;
            txtNewPassword.PasswordChar = true;
            txtConfirmNewPass.PasswordChar = true;
            lblStatus.Text = null;
            panelStatus.Visible = false;
        }

        void ShowPanelCancel()
        {
            panelStatus.Visible = true;
            picStatus.Image = Properties.Resources.Cancel;
            picStatus.BackColor = Color.OrangeRed;
            panelStatus.BackColor = Color.OrangeRed;
        }

        private void BtnSubmit_Click(object sender, EventArgs e)
        {
            DTO.Username.sTaiKhoan = txtUsername.Texts;
            DTO.Username.sMatKhau = txtOldPass.Texts;
            user.NewPass = txtNewPassword.Texts;
            user.ConfirmNewPass = txtConfirmNewPass.Texts;

            string getUser = userBLL.CheckPassChangeBLL(user);
            switch (getUser)
            {
                case "Required_Username":
                    ShowPanelCancel();
                    lblStatus.Text = "Username cannot be empty";
                    break;
                case "Required_Password":
                    ShowPanelCancel();
                    lblStatus.Text = "Old Password cannot be empty";
                    break;
                case "required_newpass":
                    ShowPanelCancel();
                    lblStatus.Text = "New Password cannot be empty";
                    break;
                case "errors_confirmpass":
                    ShowPanelCancel();
                    lblStatus.Text = "Password Confirm incorrect";
                    break;
                case "password_incorrect":
                    ShowPanelCancel();
                    lblStatus.Text = "Old password incorrect";
                    break;
                case "Success":
                    panelStatus.Visible = true;
                    picStatus.Image = Properties.Resources.Ok;
                    picStatus.BackColor = Color.LawnGreen;
                    panelStatus.BackColor = Color.LawnGreen;
                    lblStatus.Text = "Password be change successfully";

                    txtOldPass.Texts = null;
                    txtNewPassword.Texts = null;
                    txtConfirmNewPass.Texts = null;
                    txtUsername.Focus();
                    break;
            }
            TimerPanel.Start();
        }

        private void BtnExit_Click(object sender, EventArgs e)
        {
            if (DialogResult.OK == MessageBox.Show("Do You Want To Exit?", "Notification", MessageBoxButtons.OKCancel, MessageBoxIcon.Question))
            {
                this.Close();
            }
        }

        private void TimerPanel_Tick(object sender, EventArgs e)
        {
            panelStatus.Visible = false;
            TimerPanel.Stop();
        }
    }
}
