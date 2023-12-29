using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Drawing.Imaging;
using Image = System.Drawing.Image;

namespace ChatServer
{
    public partial class frmServer : Form
    {
        List<Socket> clientList;
        Socket server, client;
        IPEndPoint ipe;
        IPAddress ia;
        string path = "";
        bool isSendImage = false;
        bool isSendIcon = false;
        int port = 9999;
        int id = -1;

        public frmServer()
        {
            InitializeComponent();
            CheckForIllegalCrossThreadCalls = false;
            initialSocket();
            addEmoji();
            listIcons.Visible = false;
        }

        // Khỏi tạo
        private void initialSocket()
        {
            clientList = new List<Socket>();
            server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            ia = IPAddress.Any;
            ipe = new IPEndPoint(ia, port);           
            server.Bind(ipe);
            Thread thread = new Thread(() => {
                try
                {
                    while (true)
                    {
                        server.Listen(10);
                        client = server.Accept();
                        clientList.Add(client);
                        checkedListBox1.Items.Add(client.RemoteEndPoint.ToString());
                        Thread receive = new Thread(Receive);
                        receive.SetApartmentState(ApartmentState.STA);
                        receive.IsBackground = true;
                        receive.Start(client);
                        for (int i = 0; i < clientList.Count; i++)
                        {
                            for (int j = 0; j < clientList.Count; j++)
                            {
                                if (!clientList[i].RemoteEndPoint.Equals(clientList[j].RemoteEndPoint))
                                {
                                    Send(clientList[i], "&" + checkedListBox1.Items[j]);
                                }
                            }
                        }
                    }
                
                }
                catch
                {
                    server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    ia = IPAddress.Any;
                    ipe = new IPEndPoint(ia, port);
                }
            });

            thread.IsBackground = true;
            thread.Start();


        }

        // Khởi tạo emoji khi chạy lần đầu
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

        // Lấy đường dẫn thư mục
        public string getPathName()
        {
            string duongDan = Environment.CurrentDirectory.ToString();
            var url = Directory.GetParent(Directory.GetParent(duongDan).ToString());

            return url.ToString();

        }

        // Gửi tin khi kết nối thành công và đã soạn tin
        private void Send(Socket client, string message)
        {
            
            if ((client != null) && (message != string.Empty))
            {
                client.Send(Serialize(message));
            }
        }

        // Encode
        private byte[] Serialize(object obj)
        {
            MemoryStream stream = new MemoryStream();
            BinaryFormatter formatter = new BinaryFormatter();
            formatter.Serialize(stream, obj);
            return stream.ToArray();
        }

        // Decode
        private object Deserialize(byte[] data)
        {
            MemoryStream stream = new MemoryStream(data);
            BinaryFormatter formatter = new BinaryFormatter();
            return formatter.Deserialize(stream);
        }
  
        private void SendMessage(Socket client)
        {
            if (client != null && txtMessage.Text != string.Empty)
            {
                client.Send(Serialize(txtMessage.Text));
            }
          
        }

         private void SendImage(Socket client)
        {
            if (client != null)
            {
                MemoryStream ms;
                ms = new MemoryStream();
                Bitmap bmp = new Bitmap(path);
                bmp.Save(ms, ImageFormat.Jpeg);
                byte[] byteArray = ms.ToArray();
                client.Send(byteArray);
            }
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
        private void CloseConnect()
        {
            server.Close(); 
        }

        // Nhận tin nhắn socket khi có người gửi
        private void Receive( object obj)
        {
            Socket client = obj as Socket;
            try
            {
                while (true)
                {
                    byte[] data = new byte[1024 * 5000];
                    client.Receive(data);
                    bool checkedData = check(data);
                    if(checkedData) {

                        string dataMulti = (string)Deserialize(data);
                        string[] tokens = dataMulti.Split(new[] { "|" }, StringSplitOptions.None);
                        string port = tokens[1];
                        string message = tokens[0];
                        int index = GetItemIndex("127.0.0.1:" + port);
                        if (!port.Contains("9999"))
                        {
                            SendMessage(clientList[index]);
                            Socket item = clientList[index];
                            item.Send(Serialize(message));
                        }
                        lvMessage.SelectionAlignment = HorizontalAlignment.Left;
                        lvMessage.AppendText(message);
                        lvMessage.AppendText("\n");
                        lvMessage.AppendText("Client" + "\t" + DateTime.Now.ToString("hh:mm:ss dd/MM/yyyy") + "\n");
                        lvMessage.AppendText("\n");

                        lvMessage.ReadOnly = false;
                        lvMessage.Select(lvMessage.Text.ToString().Length, 0);
                        lvMessage.ReadOnly = true;
                    }
                    else
                    {
                        ImageConverter convertData = new ImageConverter();
                        Image image = (Image)convertData.ConvertFrom(data);
                        pictureBox3.Image = image;
                        pictureBox3.Visible = false;
                        Clipboard.SetImage(pictureBox3.Image);
                        lvMessage.SelectionAlignment = HorizontalAlignment.Left;
                        lvMessage.ReadOnly = false;
                        lvMessage.Select(lvMessage.Text.ToString().Length, 0);
                        lvMessage.Paste();
                        lvMessage.AppendText("\n");
                        lvMessage.AppendText("Client" + "\t" + DateTime.Now.ToString("hh:mm:ss dd/MM/yyyy") + "\n");

                        lvMessage.ReadOnly = true;
                    }


                }
            }
            catch{
                clientList.Remove(client);
                client.Close();
            }
        }

        // Check danh sách client
        private int GetItemIndex(string item)
        {
            int index = 0;
            foreach (object o in checkedListBox1.Items)
            {
                if (item == o.ToString())
                {
                    return index;
                }

                index++;
            }

            return -1;
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            Close();
        }

        // Covert Image to Byte
        public byte[] ImageToByteArray(Image imageIn)
        {
            using (var ms = new MemoryStream())
            {
                imageIn.Save(ms, ImageFormat.Png);
                return ms.ToArray();
            }
        }
        private void SendIcon(Socket client, byte[] byteArray)
        {
            if (client != null)
            {
                client.Send(byteArray);
            }
        }
        private void btnSend_Click(object sender, EventArgs e)
        {
            if(checkedListBox1.CheckedItems.Count == 0)
            {
                MessageBox.Show("Vui lòng chọn ít nhất 1 client để gửi tin nhắn!");
                return;

            }
            if(isSendImage)
            {
                for (int i = 0; i < checkedListBox1.CheckedIndices.Count; i++)
                {
                    SendImage(clientList[checkedListBox1.CheckedIndices[i]]);
                }

                lvMessage.SelectionAlignment = HorizontalAlignment.Right;
                lvMessage.ReadOnly = false;
                lvMessage.Select(lvMessage.Text.ToString().Length, 0);
                lvMessage.Paste();
                lvMessage.AppendText("\n");
                lvMessage.AppendText("Tôi: " + "\t" + DateTime.Now.ToString("hh:mm:ss dd/MM/yyyy") + "\n");

                lvMessage.ReadOnly = true;
                isSendImage= false;
                pictureBox2.Image = null;

            }
            if(isSendIcon)
            {
                for (int i = 0; i < checkedListBox1.CheckedIndices.Count; i++)
                {
                    Image image = imageList1.Images[id];
                    byte[] data = new byte[1024 * 10000];
                    data = ImageToByteArray(image);
                    SendIcon(clientList[checkedListBox1.CheckedIndices[i]], data);
                }

                lvMessage.SelectionAlignment = HorizontalAlignment.Right;
                lvMessage.ReadOnly = false;
                lvMessage.Select(lvMessage.Text.ToString().Length, 0);
                lvMessage.Paste();
                listIcons.Visible = false;
                lvMessage.ReadOnly = true;
                lvMessage.AppendText("\n");
                lvMessage.AppendText("Tôi: " + "\t" + DateTime.Now.ToString("hh:mm:ss dd/MM/yyyy") + "\n");

                txtMessage.Text = "";
                isSendIcon = false;
            }
            else
            { 
                if(txtMessage.Text != String.Empty)
                {
                    for (int i = 0; i < checkedListBox1.CheckedIndices.Count; i++)
                    {
                        SendMessage(clientList[checkedListBox1.CheckedIndices[i]]);
                    }

                    lvMessage.SelectionAlignment = HorizontalAlignment.Right;
                    lvMessage.AppendText("\n");
                    lvMessage.AppendText(txtMessage.Text);
                    lvMessage.AppendText("\n");
                    lvMessage.AppendText("Tôi: " + "\t" + DateTime.Now.ToString("hh:mm:ss dd/MM/yyyy") + "\n");
                    txtMessage.Clear();
                }
            }
        
        }

   

        private void btnSendAll_Click(object sender, EventArgs e)
        {
            if (checkedListBox1.CheckedItems.Count == 0)
            {
                MessageBox.Show("Vui lòng chọn ít nhất 1 client để gửi tin nhắn!");
                return;

            }
            if(isSendImage)
            {
                foreach (Socket item in clientList) {
                    SendImage(item);
                }
                lvMessage.SelectionAlignment = HorizontalAlignment.Right;
                lvMessage.AppendText("Me" + "\t" + DateTime.Now.ToString("hh:mm:ss dd/MM/yyyy") + "\n");
                lvMessage.ReadOnly = false;
                lvMessage.Select(lvMessage.Text.ToString().Length, 0);
                lvMessage.Paste();
                lvMessage.AppendText("\n");
                lvMessage.ReadOnly = true;
                isSendImage = false;
                pictureBox2.Image = null;
            }
            else
            {
                foreach (Socket item in clientList)
                {
                    SendMessage(item);
                }
                lvMessage.AppendText("\n");
                lvMessage.SelectionAlignment = HorizontalAlignment.Right;
                lvMessage.AppendText("Me" + "\t" + DateTime.Now.ToString("hh:mm:ss dd/MM/yyyy") + "\n");
                lvMessage.AppendText(txtMessage.Text);
                txtMessage.Clear();

            }
        }

        // Open popup choose image
        private void btnOpenImg_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Filter = "Image Files(*.jpg; *.jpeg; *.gif; *.png; *.bmp)|*.jpg; *.jpeg; *.gif; *.png; *.bmp";
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    isSendImage = true;
                    path = ofd.FileName;
                    Clipboard.SetImage(Image.FromFile(path));
                    pictureBox2.Image = Image.FromFile(path);
                }
            }
        }

        // remove message
        private void removeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            int firstcharindex = lvMessage.GetFirstCharIndexOfCurrentLine();
            int currentline = lvMessage.GetLineFromCharIndex(firstcharindex);
            string currentlinetext = lvMessage.Lines[currentline];
            lvMessage.Select(firstcharindex, currentlinetext.Length);
            lvMessage.ReadOnly = false;
            lvMessage.ReadOnly = true;
            RichTextBox tmp = new RichTextBox();
            tmp.Text = "Tin nhắn đã được thu hồi";
            tmp.ForeColor = Color.Red;
            tmp.SelectionAlignment = lvMessage.SelectionAlignment;
            lvMessage.SelectedRtf = tmp.Rtf;
        }

        private void frmServer_Load(object sender, EventArgs e)
        {

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

        private void pictureBox1_Click(object sender, EventArgs e)
        {
            _ = listIcons.Visible == true ? listIcons.Visible = false : listIcons.Visible = true;
        }

        private void lvMessage_TextChanged(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {
            DialogResult result = MessageBox.Show("Bạn có chắc chắn muốn xoá tất cả tin nhắn?",
                                                 "Xác nhận xoá",
                                                 MessageBoxButtons.YesNo,
                                                 MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                lvMessage.Clear();
            }
        }

        private void btnSendFile_Click(object sender, EventArgs e)
        {
            OpenFileDialog opentext = new OpenFileDialog();

            if (opentext.ShowDialog() == DialogResult.OK)
            {
                System.IO.StreamReader sr = new System.IO.StreamReader(opentext.FileName);
                string pathFile = opentext.FileName;
                Stream fileStream = File.OpenRead(pathFile);
                byte[] fileBuffer = new byte[fileStream.Length];
                fileStream.Read(fileBuffer, 0, (int)fileStream.Length);
                foreach ( Socket item in clientList )
                {
                    item.SendFile(pathFile);
                }
                sr.Close();
            }
        }
    }
}
