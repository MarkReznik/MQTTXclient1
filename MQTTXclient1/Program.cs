using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
//using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;

namespace csharpMQTT
{
    class Program
    {
        static MqttClient ConnectMQTT(string broker, int port, string clientId, string username, string password)
        {
            MqttClient client;
            
            if (username==String.Empty)
            {
                client = new MqttClient(broker);//, port, false, MqttSslProtocols.None, null, null);
                client.Connect(clientId);//, username, password);
            }
            else
            {
                client = new MqttClient(broker, port, true, MqttSslProtocols.TLSv1_2, null, null);
                client.Connect(clientId, username, password);
            }
            //client = null;
            try
            {
                if (client.IsConnected)
                {
                    Console.WriteLine("Connected to MQTT Broker");
                }
                else
                {
                    Console.WriteLine("Failed to connect");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed to connect: exception - "+ex.Message);
                //throw;
            }
            
            return client;
        }

        static void Publish(MqttClient client, string topic)
        {
            int msg_count = 0;
            //while (true)
            {
                System.Threading.Thread.Sleep(1 * 1000);
                string msg = "messages: " + msg_count.ToString();
                try
                {
                    client.Publish(topic, System.Text.Encoding.UTF8.GetBytes(msg), MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE, true);
                    Console.WriteLine("Send `{0}` to topic `{1}`", msg, topic);
                    msg_count++;
                }
                catch (Exception ex)
                {
                    Console.WriteLine("exception:" + ex.Message);
                    //throw;
                }
                
                
            }
        }

        static void PublishMsg(MqttClient client, string topic, string msg, byte qostype, bool retain, byte[] bytes=null)
        {
            //int msg_count = 0;
            //while (true)
            if (topic.Contains("+") || topic.Contains("#"))
            {
                return;
            }
            int sentcnt = 0;
            while (sentcnt<4)
            {
                //System.Threading.Thread.Sleep(1 * 500);
                //string msg = "messages: " + msg_count.ToString();
                byte[] bytesmsg;
                if (bytes != null)
                {
                    bytesmsg = bytes;
                }
                else
                {
                    bytesmsg = System.Text.Encoding.UTF8.GetBytes(msg);
                } 
                
                try
                {
                    client.Publish(topic, bytesmsg, qostype, retain);
                    if (bytes != null)
                    {
                        Console.WriteLine("Send `{0}` to topic `{1}` QoS={2} Retain={3} Retry={4}", bytesmsg, topic, qostype, retain, sentcnt);
                    }
                    else
                    {
                        Console.WriteLine("Send `{0}` to topic `{1}` QoS={2} Retain={3} Retry={4}", msg, topic, qostype, retain, sentcnt);
                    }
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine("exception:" + ex.Message);
                    sentcnt++;
                    System.Threading.Thread.Sleep(100);
                    //throw;
                }   
                //msg_count++;
            }
        }
        
        static void Subscribe(MqttClient client, string topic)
        {
            client.MqttMsgPublishReceived += client_MqttMsgPublishReceived;
            client.Subscribe(new string[] { topic }, new byte[] { MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE });
        }

        static void client_MqttMsgPublishReceived(object sender, MqttMsgPublishEventArgs e)
        {
            sendGOT = true;
            Console.WriteLine("DupFlag {0}", e.DupFlag);
            string payload = System.Text.Encoding.Default.GetString(e.Message);
            if (e.Topic.ToLower() == (topic_base+topic_connected))
            {
                if ((payload.ToLower() == "?")||(payload.ToLower() == "help"))
                {
                    Console.WriteLine("Received `{0}` from `{1}` topic", payload, e.Topic.ToString());
                    PublishMsg(client, e.Topic, topic_base + topic_connected + topic_deviceid, 2, false);
                }      
            }
            else if (e.Topic.ToLower() == (topic_base + topic_connected + topic_deviceid))
            {
                if ((payload.ToLower() == "?") || (payload.ToLower() == "help"))
                {
                    Console.WriteLine("Received `{0}` from `{1}` topic", payload, e.Topic.ToString());
                    PublishMsg(client, e.Topic, topic_com1, 2, false);
                    PublishMsg(client, e.Topic, topic_cmd, 2, false);
                    PublishMsg(client, e.Topic, topic_counter, 2, false);
                }
            }
            else if (e.Topic.ToLower() == (topic_base + topic_connected + topic_deviceid + topic_cmd))
            {
                if ((payload.ToLower() == "?") || (payload.ToLower() == "help"))
                {
                    Console.WriteLine("Received `{0}` from `{1}` topic", payload, e.Topic.ToString());
                    PublishMsg(client, e.Topic, topic_cmd + topic_rx, 2, false);
                    PublishMsg(client, e.Topic, topic_cmd + topic_rx + topic_tx, 2, false);
                }
            }
            else if (e.Topic.ToLower() == (topic_base + topic_connected + topic_deviceid + topic_cmd + topic_rx))
            {
                int cmdwaitms = 20000;
                if (payload.ToLower().Contains(" waitms ")){
                    
                    if(int.TryParse(payload.ToLower().Substring(payload.ToLower().IndexOf(" waitms ") + " waitms ".Length), out cmdwaitms)==false)
                    {
                        cmdwaitms = 20000;
                    }
                    payload = payload.ToLower().Substring(0, (payload.ToLower().IndexOf(" waitms ")));
                }
                string cmdstr = RunCmd("cmd.exe","/c "+payload, cmdwaitms);
                //if ((payload.ToLower() == "?") || (payload.ToLower() == "help"))
                {
                    Console.WriteLine("Received `{0}` from `{1}` topic", payload, e.Topic.ToString() + topic_tx);
                    PublishMsg(client, e.Topic + topic_tx, cmdstr, 2, false);
                }
            }
            else if (e.Topic.ToLower() == (topic_base + topic_connected + topic_deviceid + topic_com1))
            {
                if ((payload.ToLower() == "?") || (payload.ToLower() == "help"))
                {
                    Console.WriteLine("Received `{0}` from `{1}` topic", payload, e.Topic.ToString());
                    PublishMsg(client, e.Topic, topic_com1 + topic_rx, 2, false);
                    PublishMsg(client, e.Topic, topic_com1 + topic_rx + topic_tx, 2, false);
                }
            }
            else if (e.Topic.ToLower() == (topic_base + topic_connected + topic_deviceid + topic_com1 + topic_rx))
            {
                //if ((payload.ToLower() == "?") || (payload.ToLower() == "help"))
                {
                    Console.WriteLine("Received `{0}` from `{1}` topic {2} retain", payload, e.Topic.ToString(), e.Retain);
                    //PublishMsg(client, e.Topic, topic_com1 + topic_rx, 2, false);
                    PublishMsg(client, e.Topic+topic_tx, "OK", 2, false);
                }
            }
            else if (e.Topic.ToLower().Contains(topic_base + topic_connected + topic_deviceid + topic_com1 + topic_rx + "/"))
            {
                //if ((payload.ToLower() == "?") || (payload.ToLower() == "help"))
                {
                    Console.WriteLine("Received  from `{1}` topic {2} retain", payload, e.Topic.ToString(), e.Retain);
                    //PublishMsg(client, e.Topic, topic_com1 + topic_rx, 2, false);
                    //PublishMsg(client, e.Topic + topic_tx, "OK", 2, false);
                }
            }
            else if (e.Topic.ToLower() == (topic_base + topic_connected + topic_deviceid + topic_com1 + topic_rx + topic_tx))
            {
                if ((payload.ToLower() == "ok"))// || (payload.ToLower() == "help"))
                {
                    Console.WriteLine("Received `{0}` from `{1}` topic {2} retain", payload, e.Topic.ToString(), e.Retain);
                    //PublishMsg(client, e.Topic, topic_com1 + topic_rx, 2, false);
                    //PublishMsg(client, e.Topic + topic_tx, "OK", 2, false);
                    waitOK = false;
                }
            }
            if (payload=="ON")
            {
                Console.WriteLine("Received `{0}` from `{1}` topic", payload, e.Topic.ToString());
                
            }
            else if (payload == "OFF")
            {
                Console.WriteLine("Received `{0}` from `{1}` topic", payload, e.Topic.ToString());

            }
            else
            {
                 sendGOT = false;  
            }
            if (sendGOT)
            {
                sendGOT = false;
                PublishMsg(client, topic, "GOT", 2, false);
            }
        }
        static string RunCmd(string filename,string args, int waitms) {
            string stdout;
            string stderr;
            string exitcode;
            //string retstring;
            try
            {
                var p = Process.Start(
                    new ProcessStartInfo(filename, args)
                    {
                        CreateNoWindow = true,
                        UseShellExecute = false,
                        RedirectStandardError = true, // falsetrue,
                        RedirectStandardOutput = true,
                        WorkingDirectory = Environment.CurrentDirectory
                    }
                );
                int waitseccounter = waitms/1000;
                bool waitdone = false;
                if (waitseccounter==0)
                {
                    waitdone = p.WaitForExit(waitms);//wait < 1sec
                }
                while (waitseccounter > 0 )
                {
                    waitdone=p.WaitForExit(1000);//wait 1 sec
                    if( waitdone ) 
                    { 
                        break; 
                    }
                    waitseccounter--;
                }

                //if (!p.StandardOutput.EndOfStream)
                if (!waitdone)
                {
                    //p.CancelOutputRead();//Async
                    //p.CancelErrorRead();//Async
                    p.Close();
                    stdout = "";
                    stderr = "Wait Timeout "+waitms.ToString()+"ms";
                    exitcode = "-1";
                }
                else
                {
                    stdout = p.StandardOutput.ReadToEnd().TrimEnd();
                    stderr = p.StandardError.ReadToEnd().TrimEnd();
                    exitcode = p.ExitCode.ToString();
                }

                if (stderr.Length != 0)
                {
                    Console.WriteLine(String.Format("error: {stderr}"));
                    //retstring = stderr;
                }
                else
                {
                    Console.WriteLine(String.Format("stdout: {stdout}"));
                    //retstring = stdout;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("cmd error: "+e.Message);
                stdout = "";
                stderr = e.Message;
                exitcode = "-1";
                //throw;
            }
            return "Run: "+filename+" "+args+"\r\nStdOutput: "+ stdout+"\r\nStdError: "+ stderr + "\r\nExitCode: " + exitcode;

        }
        private static void OnTimedEvent(object source, ElapsedEventArgs e)
        {
            PublishMsg(client, topic_base + topic_connected + topic_deviceid + topic_counter, onesec_counter.ToString(), 2, true);
            onesec_counter++;
        }

        private static void SendBinFile()
        {
            //string myString;
            using (FileStream fs = new FileStream("C:\\Temp\\readtest.fw", FileMode.Open))
            using (BinaryReader br = new BinaryReader(fs))
            {
                byte[] bin = br.ReadBytes(Convert.ToInt32(fs.Length));
                //prepare subscribe to my "pc-win10" connected id
                //Subscribe(client, topic_base + topic_connected + topic_deviceid + topic_com1 + topic_rx);
                int sendlength = bin.Length;
                int sendindex = 0;
                int sendline = 0;
                int sendbuffsize = 0x7F;// 0x7FF;
                bool lastline = false;
                int retry = 0;
                while (sendindex < sendlength)
                {
                    if (lastline == true)
                    {
                        break;
                    }
                    System.Threading.Thread.Sleep(250);
                    if ((sendlength - sendindex) <= sendbuffsize)
                    {
                        sendbuffsize = (sendlength - sendindex);
                        lastline = true;
                    }
                    byte[] datapart = new byte[sendbuffsize+1];
                    Array.Copy(bin, sendindex, datapart, 1, sendbuffsize);
                    //datapart[0] = sendline;
                    waitOK = true;
                    //PublishMsg(client, topic_base + topic_connected + topic_deviceid + topic_com1 + topic_rx, "", 2, false, datapart);
                    PublishMsg(client, topic_base + topic_connected + topic_deviceid + topic_com1 + topic_rx + "/" + sendline, "", 2, false, datapart);
                    //break;
                    /*
                    int waitokms = 0;
                    while (waitOK)
                    {
                        System.Threading.Thread.Sleep(1);
                        waitokms++;
                        if (waitokms>500)
                        {
                            break; 
                        }
                    }
                    if (waitOK)
                    {
                        retry++;
                        if (retry > 3)
                        {
                            Console.WriteLine("Failed send/receive file");
                            break;
                        }
                        continue;
                    }
                    */
                    sendindex += sendbuffsize;
                    sendline++;
                    retry = 0;
                    //myString = Convert.ToBase64String(bin);
                }
            }
        }

        //Download File From FTP Server 
        //Base url of FTP Server
        //if file is in root then write FileName Only if is in use like "subdir1/subdir2/filename.ext"
        //Username of FTP Server
        //Password of FTP Server
        //Folderpath where you want to Download the File
        // Status String from Server
        public static string DownloadFile(string FtpUrl, string FileNameToDownload,
                            string userName, string password, string tempDirPath)
        {
            string ResponseDescription = "";
            string PureFileName = new FileInfo(FileNameToDownload).Name;
            string DownloadedFilePath = tempDirPath + "/" + PureFileName;
            string downloadUrl = String.Format("{0}/{1}", FtpUrl, FileNameToDownload);
            FtpWebRequest req = (FtpWebRequest)FtpWebRequest.Create(downloadUrl);
            req.Method = WebRequestMethods.Ftp.DownloadFile;
            req.Credentials = new NetworkCredential(userName, password);
            req.UseBinary = true;
            //req.Proxy = null;
            req.KeepAlive = true;
            req.UsePassive = false;
            try
            {
                FtpWebResponse response = (FtpWebResponse)req.GetResponse();
                Stream stream = response.GetResponseStream();
                byte[] buffer = new byte[2048];
                FileStream fs = new FileStream(DownloadedFilePath, FileMode.Create);
                int ReadCount = stream.Read(buffer, 0, buffer.Length);
                while (ReadCount > 0)
                {
                    fs.Write(buffer, 0, ReadCount);
                    ReadCount = stream.Read(buffer, 0, buffer.Length);
                }
                ResponseDescription = response.StatusDescription;
                fs.Close();
                stream.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            return ResponseDescription;
        }

        private static async Task DownloadFileAsync()
        {
            //WebClient client = new WebClient();
            //await client.DownloadFileTaskAsync(new Uri("https://972526435150.ucoz.org/files/readtest.fw"), "c:/temp/mytxtFile.txt");
            using (WebClient client = new WebClient()) {
                Uri ur = new Uri("https://972526435150.ucoz.org/files/stx_support.xex");

                //client.Credentials = new NetworkCredential("username", "password");
                //String credentials = Convert.ToBase64String(Encoding.ASCII.GetBytes("Username" + ":" + "MyNewPassword"));
                //client.Headers[HttpRequestHeader.Authorization] = $"Basic {credentials}";

                client.DownloadProgressChanged += (o, e) =>
                {
                    Console.WriteLine(String.Format("Download status: {0}%.", e.ProgressPercentage));

                    // updating the UI
                    /*
                    Dispatcher.Invoke(() => {
                        progressBar.Value = e.ProgressPercentage;
                    });
                    */
                };

                client.DownloadFileCompleted += (o, e) => 
                {
                    Console.WriteLine("Download finished!");
                };

                client.DownloadFileAsync(ur, @"C:\temp\newfile.exe");
            }
        }

        static bool waitOK = false;
        static int onesec_counter = 0;
        static bool sendGOT = false;
        static string broker = "f16e3b17.ala.asia-southeast1.emqxsl.com";
        static string brokerpub = "broker.emqx.io";
        static int port = 8883;
        static int portpub = 1883;
        static string topic = "testtopic1/1";// "Csharp/mqtt";
        static string clientId = Guid.NewGuid().ToString();
        static string username = "mark";//"emqx";
        static string password = "123456";//"public";

        static string topic_base        = "972526435150/root";
        static string topic_connected   = "/connected";
        static string topic_deviceid    = "/pc-win10";
        static string topic_rx          = "/rx";
        static string topic_tx          = "/tx";
        static string topic_com1        = "/com1";
        static string topic_cmd         = "/cmd";
        static string topic_counter     = "/counter";

        static MqttClient client;
        static void Main(string[] args)
        {
            string host = "ftp://972526435150.ucoz.org";
            string UserId = "f972526435150";
            string Password = "123456";
            //DownloadFileAsync();//.GetAwaiter();
            //while (true)
            {
                System.Threading.Thread.Sleep(1 * 500);
            }
            //string ftpresp = DownloadFile(host, "files/sendtest.exe", UserId, Password, "c:/temp");
            //string cmdstring;
            //cmdstring = RunCmd("cmd.exe","/c ver",1000);
            // Register to events
            //client.Settings += MqttConnectionOpened;
            //Subscribe(client, topic);
            //Publish(client, topic);
            //client.MqttMsgPublishReceived -= client_MqttMsgPublishReceived;
            //Console.WriteLine("exiting...");
            string readkeys;
            string msg;
            //string[] strings;
            byte qostype = 2;
            bool retain = false;
            string SorP = "P";
            readkeys = "P";
            /*
            Console.WriteLine("Input EMQX Broker type. D-Deploiment1 P-public: (default: P)");
            readkeys = Console.ReadLine();
            */
            if (readkeys == "D")
            {
                client = ConnectMQTT(broker, port, clientId, username, password);
            }
            else
            {
                client = ConnectMQTT(brokerpub, portpub, clientId,"","");
            }
            if (client==null) { return; }

            PublishMsg(client, topic_base + topic_connected + topic_deviceid + topic_com1 + topic_rx, "", qostype, true);
            System.Threading.Thread.Sleep(1 * 500);

            var aTimer = new System.Timers.Timer(60000);
            aTimer.Elapsed += new ElapsedEventHandler(OnTimedEvent);
            //aTimer.Interval = 10000;
            aTimer.Enabled = true;

            //if your code is not registers timer globally then uncomment following code

            //GC.KeepAlive(aTimer);

            //prepare subscribe to my "pc-win10" connected id
            Subscribe(client, topic_base+topic_connected+"/#");
            //PublishMsg(client, topic, msg, qostype, retain);
            //
            //PublishMsg(client, topic_base + topic_connected + topic_deviceid + topic_com1 + topic_rx, "", qostype, true);
            SendBinFile();

            while (true)
            {
                System.Threading.Thread.Sleep(1 * 500);
                Console.WriteLine("Input S-subscribe P-publish action: (default: " + SorP + ")");
                readkeys = Console.ReadLine();
                if ((readkeys == "S")||(readkeys == "P"))
                {
                    SorP = readkeys;
                }
                else if (readkeys == String.Empty)
                {
                    
                }
                else
                {
                    continue;
                }
                Console.WriteLine("Input topic: (default:" + topic + ")");
                readkeys=Console.ReadLine();
                if (readkeys!=String.Empty)
                {
                    topic = readkeys;
                }
                Console.WriteLine("topic is: " + topic);
                if (SorP=="S")
                {
                    Subscribe(client, topic);
                    continue;
                }
                if (topic.Contains("+") || topic.Contains("#"))
                {
                    continue;
                }
                Console.WriteLine("Input message:");
                readkeys = Console.ReadLine();
                msg = readkeys;
                Console.WriteLine("message is: " + msg);
                Console.WriteLine("Input QoS type 0,1,2: (default:" + qostype + ")");
                readkeys = Console.ReadLine();               
                if (readkeys=="0")
                {
                    qostype = MqttMsgBase.QOS_LEVEL_AT_LEAST_ONCE;
                }
                else if (readkeys == "1")
                {
                    qostype = MqttMsgBase.QOS_LEVEL_AT_MOST_ONCE;
                }
                else if (readkeys == "2")
                {
                    qostype = MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE;
                }
                Console.WriteLine("QoS is: " + qostype);
                Console.WriteLine("Input Retain 0,1: (default:" + retain + ")");
                readkeys = Console.ReadLine();
                if (readkeys == "0")
                {
                    retain = false;
                }
                else if (readkeys == "1")
                {
                    retain = true;
                }
                Console.WriteLine("Retain is: " + retain);
                //strings = readkeys.Split(new char[] { ',' });
                PublishMsg(client, topic,msg,qostype,retain);
            }
        }
    }
}