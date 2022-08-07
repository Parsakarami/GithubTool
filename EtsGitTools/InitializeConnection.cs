using EtsGitTools.Helpers;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace EtsGitTools
{
    public partial class InitializeConnectionForm : Form
    {
        private readonly string LoginInfoFileName = "UserInfo.txt";
        private readonly string LoginInfoFilePath;
        public InitializeConnectionForm()
        {
            InitializeComponent();
            LoginInfoFilePath = Path.Combine(Environment.CurrentDirectory, LoginInfoFileName);
        }

        private void InitializeConnectionForm_Load(object sender, EventArgs e)
        {
            if (File.Exists(LoginInfoFilePath))
            {
                try
                {
                    var encryptedText = File.ReadAllText(LoginInfoFilePath);
                    var json = StringEncryption.Decrypt(encryptedText);
                    var userInfo = JsonConvert.DeserializeObject<UserInfo>(json);
                    FillForm(userInfo);
                }
                catch
                {
                    File.Delete(LoginInfoFilePath);
                }
            }
        }

        private void LoginButton_Click(object sender, EventArgs e)
        {
            bool isAuthenticated = ValidateToken(TokenTextBox.Text);
            if (isAuthenticated)
            {
                UserHelper.User = new UserInfo
                {
                    IsAuthenticated = isAuthenticated,
                    Username = UsernameTextBox.Text,
                    Password = PasswordTextBox.Text,
                    Token = TokenTextBox.Text,
                };
                WriteUserCredentialFiles();
                Close();
            }
            else
            {
                MessageBox.Show("The user credentials is invalid!", "Authentication", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private bool ValidateToken(string token)
        {
            using (HttpClient client = HttpClientHelper.CreateGitHubHttpClient(token))
            {
                var response = client.GetAsync($"/user/repos?per_page=1").Result;
                return response.IsSuccessStatusCode;
            }
        }

        private void ExitButton_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void FillForm(UserInfo userInfo)
        {
            UsernameTextBox.Text = userInfo.Username;
            PasswordTextBox.Text = userInfo.Password;
            TokenTextBox.Text = userInfo.Token;
        }

        private void WriteUserCredentialFiles()
        {
            if (File.Exists(LoginInfoFilePath))
                File.Delete(LoginInfoFilePath);

            using (FileStream configFileStream = new FileStream(LoginInfoFilePath, FileMode.Create))
            {
                var json = JsonConvert.SerializeObject(UserHelper.User);
                var encryptedText = StringEncryption.Encrypt(json);
                var contentBytes = Encoding.UTF8.GetBytes(encryptedText);
                configFileStream.Write(contentBytes, 0, contentBytes.Length);
            }
        }
    }
}
