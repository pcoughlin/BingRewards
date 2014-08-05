﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Runtime.InteropServices;

namespace WindowsFormsApplication1
{
    public partial class Form1 : Form
    {
        private string username;
        private string password;
        private int countDown = 0;
        private int accountNum = 0;
        private bool mobile = false; //start with desktop
        private const int INTERNET_OPTION_END_BROWSER_SESSION = 42;
        private string settingsFile = Application.StartupPath + @"\settings.ini";
        private string wordsFile = Application.StartupPath + @"\words.txt";
        private string accountsFile = Application.StartupPath + @"\accounts.txt";

        [DllImport("wininet.dll", SetLastError = true)]
        private static extern bool InternetSetOption(IntPtr hInternet, int dwOption, IntPtr lpBuffer, int lpdwBufferLength);

        [DllImport("kernel32")]
        private static extern int GetPrivateProfileString(string section,
          string key, string def, StringBuilder retVal, int size, string filePath);

        [DllImport("kernel32")]
        private static extern int GetPrivateProfileSection(string section, IntPtr lpReturnedString,
          int nSize, string lpFileName);

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            if (Convert.ToInt32(ReadSettings("settings", "startminimized")) >= 1)
                this.WindowState = FormWindowState.Minimized;
            searchTimer.Enabled = false;
            startTimer.Enabled = false;
            webBrowser1.ScriptErrorsSuppressed = true;
            fileCheck();
            if (fileExists(settingsFile) && Convert.ToInt32(ReadSettings("settings", "startspeed")) > 100)
                startTimer.Interval = Convert.ToInt32(ReadSettings("settings", "startspeed"));
            else
                startTimer.Interval = 100;
            if (fileExists(settingsFile) && Convert.ToInt32(ReadSettings("settings", "searchspeed")) > 100)
                searchTimer.Interval = Convert.ToInt32(ReadSettings("settings", "searchspeed"));
            else
                searchTimer.Interval = 100;
            if (fileExists(settingsFile) && Convert.ToInt32(ReadSettings("settings", "autostart")) >= 1)
                ReadAccounts(accountNum);
            if (fileExists(settingsFile) && Convert.ToInt32(ReadSettings("settings", "hidebrowser")) >= 1)
                webBrowser1.Visible = false;
            //MessageBox.Show("DEBUG: searchspeed=" + searchTimer.Interval.ToString() + " startspeed=" + startTimer.Interval.ToString());
        }

        public bool fileExists(string fileName)
        {
            if (!File.Exists(fileName))
                return false;
            else
                return true;
        }

        public void fileCheck()
        {
            if (!fileExists(settingsFile))
                MessageBox.Show("File " + settingsFile + " is missing!");
            if (!fileExists(accountsFile))
                MessageBox.Show("File " + accountsFile + " is missing!");
            if (!fileExists(wordsFile))
                MessageBox.Show("File " + wordsFile + " is missing!");
        }

        public string ReadSettings(string section, string key)
        {
            const int bufferSize = 255;
            StringBuilder temp = new StringBuilder(bufferSize);
            GetPrivateProfileString(section, key, "", temp, bufferSize, settingsFile);
            return temp.ToString();
        }

        private int randomNumber()
        {
            Random random = new Random();
            return random.Next(3, 6);
        }

        public string GetRandomSentence(int wordCount)
        {
            Random rnd = new Random();
            string[] words = System.IO.File.ReadAllLines(wordsFile);

            StringBuilder builder = new StringBuilder();

            for (int i = 0; i < wordCount; i++)
            {
                // Select a random word from the array
                builder.Append(words[rnd.Next(words.Length)]).Append(" ");
            }

            string sentence = builder.ToString().Trim();

            // Set the first letter of the first word in the sentenece to uppercase
            if (wordCount >= 4)
                sentence = char.ToUpper(sentence[0]) + sentence.Substring(1) + ".";

            builder = new StringBuilder();
            builder.Append(sentence);

            return builder.ToString();
        }

        private void ReadAccounts(int line)
        {
            try
            {
                //clearCookies();
                string content = File.ReadLines(accountsFile).ElementAt(line);
                string[] words = content.Split('/');
                startBtn.Enabled = false;
                username = words[0];
                password = words[1];
                webBrowser1.Navigate(new Uri("https://login.live.com/logout.srf"));
                return;
            }
            catch
            {
                startTimer.Enabled = false;
                startBtn.Enabled = true;
                webBrowser1.Navigate(new Uri("http://newagesoldier.com/myfiles/donations.html"));
                if (fileExists(settingsFile) && Convert.ToInt32(ReadSettings("settings", "autoclose")) >= 1)
                    closeTimer.Enabled = true;
                return;
            }
        }

        private void clearCookies()
        {
            string[] theCookies = System.IO.Directory.GetFiles(Environment.GetFolderPath(Environment.SpecialFolder.Cookies));
            foreach (string currentFile in theCookies)
                System.IO.File.Delete(currentFile);
            InternetSetOption(IntPtr.Zero, INTERNET_OPTION_END_BROWSER_SESSION, IntPtr.Zero, 0);
        }

        private void search(Boolean skip = false)
        {
            string query = GetRandomSentence(randomNumber());

            if (webBrowser1.Url.ToString().Contains(@"newagesoldier.com"))
                return;

            if (webBrowser1.Url.ToString().Contains(@"bing.com/rewards/dashboard"))
            {
                if (!skip)
                {
                    startTimer.Enabled = true;
                    return;
                }
            }

            if (!mobile)
            {
                if (countDown == 1) //Change to mobile when done with desktop searching.
                {
                    mobile = true;
                    countDown = Convert.ToInt32(ReadSettings("settings", "mobilesearches"));
                }
            }

            if (mobile)
            {
                webBrowser1.Navigate("http://bing.com/search?q=" + query, null, null, "User-Agent: Mozilla/5.0 (Linux; U; Android 4.0.3; ko-kr; LG-L160L Build/IML74K) AppleWebkit/534.30 (KHTML, like Gecko) Version/4.0 Mobile Safari/534.30");
                if (countDown == 1) //We're on our last search. Reset to desktop.
                    mobile = false;
            } 
            else
                webBrowser1.Navigate(new Uri("http://bing.com/search?q=" + query));

            if (webBrowser1.Url.ToString().Contains(@"?q="))
                countDown = countDown - 1;
        }

        private void webBrowser1_DocumentCompleted(object sender, WebBrowserDocumentCompletedEventArgs e)
        {
            if (webBrowser1.Url.ToString() == "about:blank" || webBrowser1.Url.ToString() == "" || webBrowser1.Url == null || webBrowser1.Url.ToString().Contains(@"newagesoldier.com"))
                return;

            if (webBrowser1.Url.ToString().Contains(@"msn.com"))
                webBrowser1.Navigate(new Uri("https://login.live.com/login.srf?wa=wsignin1.0&rpsnv=12&ct=1406628123&rver=6.0.5286.0&wp=MBI&wreply=https:%2F%2Fwww.bing.com%2Fsecure%2FPassport.aspx%3Frequrl%3Dhttp%253a%252f%252fwww.bing.com%252frewards%252fdashboard"));

            if (mobile)
                searchModeBox.Text = "mobile";
            else
                searchModeBox.Text = "desktop";

            searchesLeftBox.Text = countDown.ToString();
            accountBox.Text = username;

            notesBox.Text = webBrowser1.Url.ToString();

            if (webBrowser1.Url.ToString().Contains(@"login.live.com/login"))
            {
                foreach (HtmlElement HtmlElement1 in webBrowser1.Document.Body.All) //Force post (login).
                {
                    if (HtmlElement1.GetAttribute("name") == "login")
                        HtmlElement1.SetAttribute("value", username);
                    if (HtmlElement1.GetAttribute("name") == "passwd")
                        HtmlElement1.SetAttribute("value", password);
                    if (HtmlElement1.GetAttribute("value") == "Sign in")
                        HtmlElement1.InvokeMember("click");
                }
                return;
            }

            if (webBrowser1.Url.ToString().Contains(@"bing.com/rewards/dashboard"))
                startTimer.Enabled = true;

            if (webBrowser1.Url.ToString().Contains(@"bing.com/Passport") || webBrowser1.Url.ToString().Contains(@"login.live.com/gls") || webBrowser1.Url.ToString().Contains(@"login.live.com/logout") || webBrowser1.Url.ToString().Contains(@"bing.com/secure") || webBrowser1.Url.ToString().Contains(@"bing.com/rewards/dashboard") || webBrowser1.Url.ToString().Contains(@"msn.com"))
                return; //let timer finish the login process before reading another account OR going to the next search.

            if (!webBrowser1.Url.ToString().Contains(@"?q="))
                return;

            if (countDown >= 1)
                searchTimer.Enabled = true;
            else
                ReadAccounts(accountNum);
        }

        private void startTimer_Tick(object sender, EventArgs e)
        { //this is just so we can debug and watch to make sure we are really logged in.
            if (!webBrowser1.Url.ToString().Contains(@"bing.com/rewards/dashboard"))
                return;
            countDown = (Convert.ToInt32(ReadSettings("settings", "desktopsearches")));
            search(true);
            accountNum = accountNum + 1; //next account
        }

        private void searchTimer_Tick(object sender, EventArgs e)
        {
            if (!webBrowser1.Url.ToString().Contains(@"?q="))
                return;
            search();
            searchTimer.Enabled = false;
        }

        private void startBtn_Click(object sender, EventArgs e)
        {
            ReadAccounts(accountNum);
        }

        private void label4_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start("http://www.newagesoldier.com");
        }

        private void closeTimer_Tick(object sender, EventArgs e)
        {
            Application.Exit();
        }
    }
}
