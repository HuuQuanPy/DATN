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
    public partial class FormRegis : Form
    {
        DTO.Username user = new DTO.Username();
        BLL.UserNameBLL userBLL = new BLL.UserNameBLL();
        public FormRegis()
        {
            InitializeComponent();
            txtPassword.PasswordChar = true;
            txtConfirmPass.PasswordChar = true;
        }

        private void BtnSubmit_Click(object sender, EventArgs e)
        {
            DTO.Username.sTaiKhoan = txtUsername.Texts; 
            DTO.Username.sMatKhau = txtPassword.Texts;
            user.ConfirmPass = txtConfirmPass.Texts;

            string getUser = userBLL.CheckRegisBLL(user);
            switch (getUser)
            {
                case "Required_Username":
                    MessageBox.Show("Username cannot be empty", "Notification",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    break;
                case "Required_Password":
                    MessageBox.Show("Password cannot be empty", "Notification",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    break;
                case "errors_ConfirmPass":
                    MessageBox.Show("Password Confirm incorrect", "Notification",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    break;
                case "username_exists":
                    MessageBox.Show("Username already taken", "Notification",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    break;
                default:
                    MessageBox.Show("Sign up username successfully. Hi : "+ txtUsername.Texts +"", "Notification",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    txtPassword.Texts = null;
                    txtConfirmPass.Texts = null;
                    txtUsername.Focus();
                    break;
            }
        }

        private void BtnExit_Click(object sender, EventArgs e)
        {
            if (DialogResult.OK == MessageBox.Show("Do You Want To Exit?", "Notification", MessageBoxButtons.OKCancel, MessageBoxIcon.Question))
            {
                this.Close();
            }
        }
    }
}
