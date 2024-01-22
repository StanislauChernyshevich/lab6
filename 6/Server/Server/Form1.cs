using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net; 
using System.Net.Sockets; 
using System.IO; 
using System.Threading;

namespace Server
{
    public partial class Form1 : Form
    {
        private int port = 5555;
        static string filePath = "D:\\data.txt";

        TcpListener listener = null;

        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            button1.BackColor = Color.Green;

            try
            {
                listener = new TcpListener(IPAddress.Any, port);
                listener.Start();

                while (true)
                {
                    TcpClient client = listener.AcceptTcpClient();
                    ClientObject clientObject = new ClientObject(client, filePath);

                    Thread clientThread = new Thread(new ThreadStart(clientObject.Process));
                    clientThread.Start();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
                if (listener != null)
                    listener.Stop();
            }
        }

    }

    public class ClientObject
    {
        public TcpClient client;
        private string filePath = "";

        public ClientObject(TcpClient tcpClient, string filePath)
        {
            this.client = tcpClient;
            this.filePath = filePath;
        }

        public void Process()
        {
            NetworkStream stream = null;
            try
            {
                stream = client.GetStream();
                byte[] data = new byte[64];
                byte[] response = new byte[1024];

                while (true)
                {
                    StringBuilder builder = new StringBuilder();
                    int bytes = 0;

                    do
                    {
                        bytes = stream.Read(data, 0, data.Length);
                        builder.Append(Encoding.Unicode.GetString(data, 0, bytes));
                    }
                    while (stream.DataAvailable);

                    string message = builder.ToString();

                    string cmd = message.Substring(0, message.IndexOf("|", 0));
                    string responseData = cmd;

                    switch (cmd)
                    {
                        case "view": responseData = getDataFromFile(); break;
                        case "add": {
                            appendDataToFile(message); 
                            responseData = "Данные были добавлены!\r\n" + getDataFromFile(); 
                            break;
                        }
                        case "find": responseData = "Результаты поиска!\r\n" + findData(message); break;
                        case "delete": responseData = "Запись была удалена !\r\n" + deleteFromFile(message); break;
                        case "change": changeFile(message); responseData = "Запись была изменена!\r\n"; break;
                        default: break;
                    }

                    response = Encoding.Unicode.GetBytes(responseData);
                    stream.Write(response, 0, response.Length);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
                if (stream != null)
                    stream.Close();
                if (client != null)
                    client.Close();
            }
        }

        /**
         *  File processing logic
         */

        /**
         *  Read all file data
         *  @return string data
         */
        private string getDataFromFile()
        {
            StreamReader sr = null;
            string data = "";

            try
            {
                sr = new StreamReader(this.filePath, System.Text.Encoding.Default);
                data = sr.ReadToEnd();

            }
            catch
            {
                Console.WriteLine("Ошибка при чтении");
            }
            finally
            {
                sr.Close();
            }

            return data;
        }

        /**
         *  Rewrite the file
         *  @return void
         */
        private void rewriteFile(string data)
        {
            StreamWriter sw = null;

            try
            {
                sw = new StreamWriter(this.filePath, false, System.Text.Encoding.Default);
                sw.WriteLine(data);
            }
            catch
            {
                Console.WriteLine("Ошибка при перезаписи файла:(");
            }
            finally
            {
                sw.Close();
            }
        }

        /**
         *  Add new item to the file
         *  
         *  @param { string }
         *  If split by '|' name = data[1], cost = data[2]
         *  
         *  @return { string } data
         */
        private void appendDataToFile(string data)
        {
            string[] dataArr = data.Split(new[] { '|' });
            StreamWriter sw = null;

            try
            {
                string name = dataArr[1];
                string cost = dataArr[2];

                sw = new StreamWriter(filePath, true, System.Text.Encoding.Default);
                sw.WriteLine(Config.numberCount++ + ": " + name + "  $" + cost);
            }
            catch
            {
                Console.WriteLine("Ошибка при записи в файл:(");
            }
            finally
            {
                sw.Close();
            }
        }


        /**
         *  Read data from file and returns data as List
         *  @return string[]
         */
        private IEnumerable<string> getDataListFromFile()
        {
            string notesString = getDataFromFile();

            return notesString.Split(new[] { '\r', '\n' }).Where(line => line != ""); ;
        }

        /**
         *  Finds note by the name 
         *  
         *  @param { string } if split by '|' name = data[1]
         *  @return string
         */
        private string findData(string data)
        {
            string name = data.Split(new[] { '|' })[1];
            string res = "";

            IEnumerable<string> notesList = getDataListFromFile();

            foreach (string line in notesList)
            {
                if (line.Contains(name))
                {
                    res += line + "\r\n";
                }

            }
            return res;
        }

        /**
         *  Delete note by the number 
         *  
         *  @param { string } if split by '|' number = data[1]
         *  @return string
         */
        private string deleteFromFile(string data)
        {
            char number = Convert.ToChar(data.Split(new[] { '|' })[1]);

            IEnumerable<string> newList = getDataListFromFile()
                .Where(line => line[0] != number);

            string newData = String.Join("\r\n", newList);

            rewriteFile(newData);
            return newData;
        }

        /**
         *  Gets cost from strign 
         *  
         *  @param { string }
         *  @return int 
         */
        private int getCost(string data)
        {
            int i = data.IndexOf('$');
            string cost = data.Substring(i + 1);
            return Convert.ToInt32(cost);
        }

        /**
         *  Change note's cost
         *  
         *  @param { string }
         *  If split by '|' number = data[1], cost = data[2]
         *  @return string
         */
        private void changeFile(string data)
        {
            string[] dataArr = data.Split(new[] { '|' });
            char number = Convert.ToChar(dataArr[1]);
            string newCost = dataArr[2];

            string[] iceCreams =  getDataListFromFile().ToArray<string>();

            for (int i = 0; i < iceCreams.Length; i++)
            {
                if (iceCreams[i][0] == number)
                {
                    string prevCost = Convert.ToString(getCost(iceCreams[i]));
                    iceCreams[i] = iceCreams[i].Replace(prevCost, newCost);
                }
            }

            rewriteFile(String.Join("\r\n", iceCreams));
        }

    }

    /**
     *As a way to control numbers of icecreams 
     */
    public class Config
    {
        public static int numberCount = 1;
    }

}
