using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace CSharp_UDPServerClientApp
{
    internal class Program
    {
        const string helpstring = "myudp <mode> [file] [address]\n" +
                    "\tmode:\n" +
                    "\t\t-s - sender mode\n" +
                    "\t\t-r - receiver mode\n" +
                    "\tfile - file to send\n" +
                    "\taddress - host to send";
        static UdpClient udpSender;
        static UdpClient udpReceiver;
        const int sendPort = 20600;
        const int receivePort = 20601;
        const int pocketSize = 8192;

        static void Sender(string path, string address)
        {
            IPAddress ipAddr;
            try
            {
                IPHostEntry entry = Dns.GetHostEntry(address);
                ipAddr = entry.AddressList.FirstOrDefault(a => a.AddressFamily == AddressFamily.InterNetwork);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                return;
            }
            IPEndPoint sendEndpoint = new IPEndPoint(ipAddr, sendPort);
            IPEndPoint receiveEndpoint = null;
            try
            {
                udpReceiver = new UdpClient(receivePort);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                return;
            }
            using (FileStream fsSource = new FileStream(path, FileMode.Open, FileAccess.Read))
            {
                int numBytesToRead = (int)fsSource.Length;
                int numBytesWereRead = 0;
                string name = Path.GetFileName(path);
                byte[] pocketSend;
                byte[] pocketReceive;
                pocketSend = Encoding.Unicode.GetBytes(name);
                udpReceiver.Client.ReceiveTimeout = 5000;
                udpSender.Send(pocketSend, pocketSend.Length, sendEndpoint);
                pocketReceive = udpReceiver.Receive(ref receiveEndpoint);
                int parts = (int)fsSource.Length / pocketSize;
                if ((int)fsSource.Length % pocketSize != 0) parts++;
                pocketSend = BitConverter.GetBytes(parts);
                udpSender.Send(pocketSend, pocketSend.Length, sendEndpoint);
                pocketReceive = udpReceiver.Receive(ref receiveEndpoint);
                int n = 0;
                pocketSend = new byte[pocketSize];
                for(int i=0; i < parts - 1; i++)
                {
                    n = fsSource.Read(pocketSend, 0, pocketSize);
                    if (n == 0) break;
                    numBytesWereRead += n;
                    numBytesToRead -= n;
                    udpSender.Send(pocketSend, pocketSend.Length, sendEndpoint);
                    pocketReceive = udpReceiver.Receive(ref receiveEndpoint);
                }
                pocketSend = new byte[numBytesToRead];
                n = fsSource.Read(pocketSend, 0, numBytesToRead);
                udpSender.Send(pocketSend, pocketSend.Length, sendEndpoint);
                pocketReceive = udpReceiver.Receive(ref receiveEndpoint);
            }
            Console.WriteLine("File sent successfully");
        }

        static void Receiver()
        {
            IPEndPoint receiveEndPoint = null;
            try
            {
                udpReceiver = new UdpClient(sendPort);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                return;
            }
            byte[] pocketSend = new byte[1];
            byte[] pocketReceive;
            pocketSend[0] = 1;
            pocketReceive = udpReceiver.Receive(ref receiveEndPoint);
            IPEndPoint sendEndPoint = new IPEndPoint(receiveEndPoint.Address, receivePort);
            string name = Encoding.Unicode.GetString(pocketReceive);
            // Here we have to check is this parcel is our parcel with expected data
            //
            //----------------------------------------------------------------------
            udpSender.Send(pocketSend, pocketSend.Length, sendEndPoint);
            udpReceiver.Client.ReceiveTimeout = 5000;
            pocketReceive = udpReceiver.Receive(ref receiveEndPoint);
            int parts = BitConverter.ToInt32(pocketReceive, 0);
            // Here we have to check is this parcel is our parcel with expected data
            //
            //----------------------------------------------------------------------
            udpSender.Send(pocketSend, pocketSend.Length, sendEndPoint);
            using (FileStream fsDest = new FileStream(name, FileMode.Create, FileAccess.Write))
            {
                for(int i = 0; i< parts; i++)
                {
                    pocketReceive = udpReceiver.Receive(ref receiveEndPoint);
                    fsDest.Write(pocketReceive, 0, pocketReceive.Length);
                    udpSender.Send(pocketSend, pocketSend.Length, sendEndPoint);
                }
            }
            Console.WriteLine("File received");
        }

        static void Main(string[] args)
        {
            udpSender = new UdpClient();
            udpReceiver = new UdpClient();

            if (args.Length < 1)
            {
                Console.WriteLine(helpstring);

            }
            else if (args[0] == "-s")
            {
                if (args.Length < 3)
                {
                    Console.WriteLine("Not enough parameters");
                    Console.WriteLine(helpstring);
                }
                else
                {
                    try
                    {
                        Sender(args[1], args[2]);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.ToString());
                    }
                }
            }
            else if (args[0] == "-r")
            {
                try
                {
                    Receiver();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                }
            }
            else Console.WriteLine(helpstring);

            Console.Read();
            udpSender.Close();
            udpReceiver.Close();
        }
    }
}