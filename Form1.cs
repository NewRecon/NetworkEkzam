using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace NetworkEkzam
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            UrlTextBox.Text = @"https://w.forfun.com/fetch/9d/9db2d4683d92f5f2045e9142fbd82633.jpeg";
        }

        private async void button1_Click(object sender, EventArgs e)
        {
            if (folderBrowserDialog1.ShowDialog() == DialogResult.OK)
            {
                Downloader downloader = new Downloader(folderBrowserDialog1.SelectedPath + UrlTextBox.Text.Substring(UrlTextBox.Text.LastIndexOf("/")));

                progressBar1.Maximum = (int)GetFileSize(UrlTextBox.Text);
                MessageBox.Show(progressBar1.Maximum.ToString());

                listBox1.Items.Add(downloader.name);
                button2.Click += downloader.Reset;
                button4.Click += downloader.Set;
                button3.Click += downloader.Cancel;

                downloader.client = new HttpClient();
                downloader.stream = await downloader.client.GetStreamAsync(UrlTextBox.Text);

                await Task.Run(()=> Download(downloader));
            }
        }

        async Task Download(Downloader downloader)
        {
            try
            {
                byte[] bytes = new byte[progressBar1.Maximum];
                using (FileStream fs = new FileStream(downloader.path, FileMode.Create))
                {
                    int size = await downloader.stream.ReadAsync(bytes, 0, bytes.Length, downloader.token);
                    while (size > 0)
                    {
                        await Task.Yield();
                        downloader.m_.WaitOne();
                        BeginInvoke(new Action(() =>
                        {
                            progressBar1.Value += size;
                        }));
                        await fs.WriteAsync(bytes, 0, size);
                        size = await downloader.stream.ReadAsync(bytes, 0, bytes.Length, downloader.token);
                    }
                }
                BeginInvoke(new Action(() =>
                {
                    progressBar1.Value = 0;
                }));
                MessageBox.Show("Done!");
            }
            catch (Exception ex) {}
        }

        public long GetFileSize(string url)
        {
            long result = -1;
            System.Net.WebRequest req = System.Net.WebRequest.Create(url);
            req.Method = "HEAD";
            using (System.Net.WebResponse resp = req.GetResponse())
            {
                if (long.TryParse(resp.Headers.Get("Content-Length"), out long ContentLength))
                {
                    result = ContentLength;
                }
            }
            return result;
        }
    }

    class Downloader
    {
        
        public string path;
        public string name;
        public ManualResetEvent m_ = new ManualResetEvent(true);
        CancellationTokenSource cancelTokenSource = new CancellationTokenSource();
        public CancellationToken token;

        public HttpClient client = new HttpClient();
        public Stream stream;


        public Downloader(string path)
        {
            this.path = path;
            name = path.Substring(path.LastIndexOf("/"));
            token = cancelTokenSource.Token;
        }

        public void Reset(object sender, EventArgs e) // button 2
        {
            m_.Reset();
        }

        public void Set(object sender, EventArgs e) // button 4
        {
            m_.Set();
        }

        public void Cancel(object sender, EventArgs e) // button 3
        {
            cancelTokenSource.Cancel();
        }
    }
}
