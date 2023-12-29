using System;
using System.Net.Sockets;
using System.Net;
using System.Windows.Forms;
using System.Threading;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Linq;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Diagnostics.Eventing.Reader;

namespace MiniChat
{
    public partial class frmClient : Form
    {
        Socket client;
        IPAddress ia;
        IPEndPoint ep;
        int port = 9999;
        string path;
        bool isReceived;
        bool isSendImage;
        bool isSendIcon;
        List<string> clientActive = new List<string>();
        int id = -1;
        public frmClient()
        {
            InitializeComponent();
            CheckForIllegalCrossThreadCalls = false;
            Connect();
            addEmoji();
            listIcons.Visible = false;
            listClientOnline.Items.Add("127.0.0.1:9999");
            
                
        }
        private void Connect()
        {
            client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            ia = IPAddress.Parse("127.0.0.1");
            ep = new IPEndPoint(ia, port);
            try
            {
                client.Connect(ep);

            }
            catch {
                MessageBox.Show("Can't connect to server!");
                return;
            }
            Thread listen = new Thread(ReceiveMessage);
            listen.SetApartmentState(ApartmentState.STA);
            listen.IsBackground = true;
            listen.Start();
        }

        private void CloseClient() { 
            client.Close();
        }

        private void ReceiveMessage()
        {
            try
            {
                while (true)
                {
                    byte[] data = new byte[1024 * 5000];
                    client.Receive(data);
                    bool checkedData = check(data);
                    if(checkedData)
                    {
                        string message = (string)Deserialize(data);
                        if (message.Contains('&'))
                        {
                            string myMessage = message.TrimStart('&');
                            if (!listClientOnline.Items.Contains(myMessage))
                            {
                                listClientOnline.Items.Add(myMessage);
                            }
                        }
                        else
                        {
                            AddMessage(message);
                            isReceived = true;
                        }
                    }
                    else
                    {
                        ImageConverter convertData = new ImageConverter();
                        Image image = (Image)convertData.ConvertFrom(data);
                        pictureBox2.Image = image;
                        pictureBox2.Visible = false;
                        Clipboard.SetImage(pictureBox2.Image);
                        lvMessage.SelectionAlignment = HorizontalAlignment.Left;
                        lvMessage.AppendText("server" + "\t" + DateTime.Now.ToString("hh:mm:ss dd/MM/yyyy") + "\n");
                        lvMessage.ReadOnly = false;
                        lvMessage.Select(lvMessage.Text.ToString().Length, 0);
                        lvMessage.Paste();
                        lvMessage.AppendText("\n");
                        lvMessage.ReadOnly = true;
                    }
                }
            }
            catch
            {
                CloseClient();
            }
        }
        private void AddMessage(string s)
        {
            lvMessage.SelectionAlignment = HorizontalAlignment.Left;
            lvMessage.AppendText("Client another" + "\t" + DateTime.Now.ToString("hh:mm:ss dd/MM/yyyy") + "\n");
            lvMessage.ReadOnly = false;
            lvMessage.Select(lvMessage.Text.ToString().Length, 0);
            lvMessage.AppendText(s);
            lvMessage.AppendText("\n");
            lvMessage.ReadOnly = true;
        }

        private void SendMessage(string s, string portTarget)
        {
            string data = s + "|" + portTarget;
            client.Send(Serialize(data));
            isReceived = false;
        }

        private byte[] Serialize(object obj)
        {
            MemoryStream stream = new MemoryStream();
            BinaryFormatter formatter = new BinaryFormatter();
            formatter.Serialize(stream, obj);
            return stream.ToArray();
        }
        private bool check(byte[] data)
        {
            try
            {
                MemoryStream stream = new MemoryStream(data);
                BinaryFormatter formatter = new BinaryFormatter();
                object obj = formatter.Deserialize(stream);
                if (obj.ToString() != null)
                {

                    return true;
                }
                return false;
            }
            catch
            {
                return false;
            }
        }
        private void SendImage()
        {
                MemoryStream ms;
                ms = new MemoryStream();
                byte[] byteArray;
                Bitmap bmp = new Bitmap(path);
                bmp.Save(ms, ImageFormat.Jpeg);
                byteArray = ms.ToArray();
                client.Send(byteArray);
                isSendImage = false;
                pictureBox3.Image = null;
        }
        private object Deserialize(byte[] data)
        {
            MemoryStream stream = new MemoryStream(data);
            BinaryFormatter formatter = new BinaryFormatter();
            return formatter.Deserialize(stream);
        }
        public byte[] ImageToByteArray(Image imageIn)
        {
            using (var ms = new MemoryStream())
            {
                imageIn.Save(ms, ImageFormat.Png);
                return ms.ToArray();
            }
        }
        private void btnSend_Click(object sender, EventArgs e)
        {
            if (listClientOnline.CheckedItems.Count == 0)
            {
                MessageBox.Show("Vui lòng chọn ít nhất 1 client để gửi tin nhắn!" , "Notification");
                return;

            }
            for (int i = 0; i < listClientOnline.CheckedItems.Count; i++)
                {
                    if(isSendImage)
                    {
                        SendImage();
                        lvMessage.SelectionAlignment = HorizontalAlignment.Right;
                        lvMessage.AppendText("Me" + "\t" + DateTime.Now.ToString("hh:mm:ss dd/MM/yyyy") + "\n");
                        lvMessage.ReadOnly = false;
                        lvMessage.Select(lvMessage.Text.ToString().Length, 0);
                        lvMessage.Paste();
                        lvMessage.AppendText("\n");
                        lvMessage.ReadOnly = true;

                    }
                    if(isSendIcon)
                    {
                        Image image = imageList1.Images[id];
                        byte[] data = new byte[1024 * 10000];
                        data = ImageToByteArray(image);
                        client.Send(data);
                        lvMessage.SelectionAlignment = HorizontalAlignment.Right;
                        lvMessage.AppendText("Me" + "\t" + DateTime.Now.ToString("hh:mm:ss dd/MM/yyyy") + "\n");
                        lvMessage.ReadOnly = false;
                        lvMessage.Select(lvMessage.Text.ToString().Length, 0);
                        lvMessage.Paste();
                        listIcons.Visible = false;
                        lvMessage.ReadOnly = true;
                        lvMessage.AppendText("\n");
                        txtMessage.Text = "";
                        isSendIcon = false;
                    }
                    else
                    {
                        string client = listClientOnline.CheckedItems[i].ToString();
                        if (client.Contains("127.0.0.1:9999"))
                        {
                            SendMessage(txtMessage.Text, "9999");
                        }
                        else
                        {
                            string[] tokens = client.Split(new[] { ":" }, StringSplitOptions.None);
                            SendMessage(txtMessage.Text, tokens[1]);
                        }
                        lvMessage.AppendText("\n");
                        lvMessage.SelectionAlignment = HorizontalAlignment.Right;
                        lvMessage.AppendText("Tôi" + "\t" + DateTime.Now.ToString("hh:mm:ss dd/MM/yyyy") + "\n");
                        lvMessage.AppendText(txtMessage.Text);
                        lvMessage.AppendText("\n");
                        txtMessage.Clear();
                        txtMessage.Focus();
                    }
                }

        }


        private void btnSendImage_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Filter = "Image Files(*.jpg; *.jpeg; *.gif; *.png; *.bmp)|*.jpg; *.jpeg; *.gif; *.png; *.bmp";
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    isSendImage = true;
                    path = ofd.FileName;
                    Clipboard.SetImage(Image.FromFile(path));
                    pictureBox3.Image = Image.FromFile(path);
                }
            }
        }

        private void listIcons_SelectedIndexChanged(object sender, EventArgs e)
        {
                if (listIcons.SelectedIndices.Count <= 0) return;
                if (listIcons.FocusedItem == null) return;
                id = listIcons.SelectedIndices[0];
                if (id < 0) return;
                Clipboard.SetImage((imageList1.Images[id]));
                txtMessage.Paste();
                listIcons.Visible = false;
                isSendIcon = true;

        }


        private void addEmoji()
        {
            string path = getPathName() + @"\Emoij";

            string[] files = Directory.GetFiles(path);

            foreach (String f in files)
            {
                Image img = Image.FromFile(f);
                imageList1.Images.Add(img); 
            }
            this.listIcons.View = View.LargeIcon;
            this.imageList1.ImageSize = new Size(32, 32);

            this.listIcons.LargeImageList = this.imageList1;
            for (int i = 0; i < this.imageList1.Images.Count; i++)
            {
                this.listIcons.Items.Add(" ", i);
            }

        }
        public string getPathName()
        {
            string getPath = Environment.CurrentDirectory.ToString();
            var url = Directory.GetParent(Directory.GetParent(getPath).ToString());

            return url.ToString();

        }
        private void removeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            int firstcharindex = lvMessage.GetFirstCharIndexOfCurrentLine();
            int currentline = lvMessage.GetLineFromCharIndex(firstcharindex);
            string currentlinetext = lvMessage.Lines[currentline];
            lvMessage.Select(firstcharindex, currentlinetext.Length);
            lvMessage.ReadOnly = false;
            lvMessage.ReadOnly = true;
            RichTextBox tmp = new RichTextBox();
            tmp.Text = "Message is deleted!";
            tmp.SelectionAlignment = lvMessage.SelectionAlignment;
            lvMessage.SelectedRtf = tmp.Rtf;
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {
            _ = listIcons.Visible == true ? listIcons.Visible = false : listIcons.Visible = true;
        }

        private void frmClient_Load(object sender, EventArgs e)
        {

        }
    }
}