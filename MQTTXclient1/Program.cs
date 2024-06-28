using System;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;

namespace csharpMQTT
{
    class Program
    {
        static MqttClient ConnectMQTT(string broker, int port, string clientId, string username, string password)
        {
            MqttClient client = new MqttClient(broker, port, true, MqttSslProtocols.TLSv1_2, null, null);
            client.Connect(clientId, username, password);
            if (client.IsConnected)
            {
                Console.WriteLine("Connected to MQTT Broker");
            }
            else
            {
                Console.WriteLine("Failed to connect");
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

        static void PublishMsg(MqttClient client, string topic, string msg, byte qostype, bool retain)
        {
            //int msg_count = 0;
            //while (true)
            if (topic.Contains("+") || topic.Contains("#"))
            {
                return;
            }
            {
                System.Threading.Thread.Sleep(1 * 500);
                //string msg = "messages: " + msg_count.ToString();
                try
                {
                    client.Publish(topic, System.Text.Encoding.UTF8.GetBytes(msg), qostype, retain);
                    Console.WriteLine("Send `{0}` to topic `{1}` QoS={2} Retain={3}", msg, topic, qostype, retain);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("exception:" + ex.Message);
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
            string payload = System.Text.Encoding.Default.GetString(e.Message);
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
        static bool sendGOT = false;
        static string broker = "f16e3b17.ala.asia-southeast1.emqxsl.com";//"broker.emqx.io";
        static int port = 8883;
        static string topic = "testtopic1/1";// "Csharp/mqtt";
        static string clientId = Guid.NewGuid().ToString();
        static string username = "mark";//"emqx";
        static string password = "123456";//"public";
        static MqttClient client;
        static void Main(string[] args)
        {
            
            client = ConnectMQTT(broker, port, clientId, username, password);
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