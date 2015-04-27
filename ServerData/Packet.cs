using Microsoft.Kinect;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Runtime.Serialization.Formatters.Binary;
using System.Windows.Media.Media3D;

namespace ServerData
{
    [Serializable]
    public class Packet
    {
        public string clientID;
        public string clientCode;
        public double[] referenceFrameData;
        public List<Person> personList;
        public PacketType packetType;

        public Packet(PacketType type, string senderID)
        {
            this.referenceFrameData = new double[10];
            this.personList = new List<Person>();
            this.clientID = senderID;
            this.packetType = type;
        }

        public Packet(byte[] packetBytes)
        {
            using (MemoryStream mStream = new MemoryStream(packetBytes))
            {
                Packet p;
                try
                {
                    mStream.Position = 0;
                    BinaryFormatter bFormatter = new BinaryFormatter();
                    p = (Packet)bFormatter.Deserialize(mStream);
                }
                catch (Exception exc)
                {
                    Console.WriteLine("DESERIALIZATION FAILED. Reason: " + exc.Message);
                    throw;
                }
                finally
                {
                    mStream.Close();
                }

                this.clientID = p.clientID;
                this.clientCode = p.clientCode;
                this.referenceFrameData = p.referenceFrameData;
                this.personList = p.personList;
                this.packetType = p.packetType;
            }
        }

        public byte[] ToBytes()
        {
            using (MemoryStream mStream = new MemoryStream())
            {
                BinaryFormatter bFormatter = new BinaryFormatter();
                byte[] bytes;

                try
                {
                    bFormatter.Serialize(mStream, this);
                    bytes = mStream.ToArray();
                    mStream.Flush();
                }
                catch (Exception exc)
                {
                    Console.WriteLine("SERIALIZATION FAILED. Reason: " + exc.Message);
                    throw;
                }
                finally
                {
                    mStream.Close();
                }

                return bytes;
            }
        }

        public static string GetIPforAddress()
        {
            IPAddress[] ips = Dns.GetHostAddresses(Dns.GetHostName());

            foreach (IPAddress i in ips)
            {
                if (i.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                    return i.ToString();
            }

            return "127.0.0.1";
        }

        public void SetReferenceData(double[] referenceData)
        {
            this.referenceFrameData = referenceData;
        }

        public void AddBodyData(Person person)
        {
            if (person != null)
                this.personList.Add(person);
            else return;
        }
    }

    public enum PacketType
    {
        RegisterClient,
        InputCode,
        Transfer
    }
}