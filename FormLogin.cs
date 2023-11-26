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
    public partial class FormLogin : Form
    {
        
        BLL.UserNameBLL userBLL = new BLL.UserNameBLL();
        public FormLogin()
        {
            InitializeComponent();
            userBLL.GetDataCookiesBLL();
            txtUsername.Texts = DTO.Username.sTaiKhoan;
            txtPassword.Texts = DTO.Username.sMatKhau;
            if (txtUsername.Texts != "" && txtPassword.Texts != "")
            {
                chkRemember.Checked = true;
            }
            else
            {
                chkRemember.Checked = false;
            }
        }

        private void BtnExit_Click(object sender, EventArgs e)
        {
            if (DialogResult.OK == MessageBox.Show("Do You Want To Exit?", "Notification", MessageBoxButtons.OKCancel,MessageBoxIcon.Question))
            {
                Application.Exit();
            }
        }

        private void LinkChangePass_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            FormPasswordChanged formPassword = new FormPasswordChanged();
            formPassword.ShowDialog();
        }

        private void LinkRegis_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            FormRegis formRegis = new FormRegis();
            formRegis.ShowDialog();
        }

        private void BtnLogin_Click(object sender, EventArgs e)
        {
            DTO.Username.sTaiKhoan = txtUsername.Texts;
            DTO.Username.sMatKhau = txtPassword.Texts;
            
            string getUser = userBLL.CheckLoginBLL();
            switch (getUser)
            {
                case "Required_Username":
                    MessageBox.Show("Username cannot be empty", "Notification",
                        MessageBoxButtons.OK,MessageBoxIcon.Warning);
                    break;
                case "Required_Password":
                    MessageBox.Show("Password cannot be empty", "Notification",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    break;
                case "Tai khoan hoac Mat khau khong chinh xac":
                    MessageBox.Show("Username or password incorrect", "Notification",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    break;
                default:
                    FormMain main = new FormMain();
                    txtUsername.Focus();
                    DTO.Username.sMaQuyen = userBLL.CheckQuyenBLL();
                    main.ShowDialog();

                    if (chkRemember.Checked)
                    {
                        userBLL.CheckInsertCookiesBLL();
                        DTO.Username.CheckRemember = true;
                    }
                    else
                    {
                        userBLL.CheckDeleteCookiesBLL();
                        DTO.Username.CheckRemember = false;
                        txtUsername.Texts = null;
                        txtPassword.Texts = null;
                    }
                    break;
            }
        }

        private void FormLogin_Load(object sender, EventArgs e) { }

    }
}
