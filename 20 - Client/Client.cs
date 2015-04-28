using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using ServerData;
using Microsoft.Kinect;
using System.Windows.Media.Media3D;

namespace _20__Client
{
    class Client
    {
        public static string ip = "128.243.19.128";
        public static Socket master;
        public static string inputState = "z";
        public static string CLIENT_ID = "CLIENT1";

        public Client()
        {
            master = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            IPEndPoint ipe = new IPEndPoint(IPAddress.Parse(ip), 4242);

            try
            {
                master.Connect( ipe );
            }
            catch
            {
                Console.WriteLine("COULD NOT CONNECT TO SERVER HOST!");
                Thread.Sleep(1000);
            }

            Thread thread = new Thread(DataIN);
            thread.Start();
        }

        static void DataIN()
        {
            byte[] buffer;
            int readBytes;

            for (; ; )
            {
                try
                {
                    buffer = new Byte[ master.SendBufferSize];
                    readBytes = master.Receive(buffer);

                    if (readBytes > 0)
                    {
                        DataManager(new Packet(buffer));
                    }
                }
                catch (SocketException ex)
                {
                    Console.WriteLine("Disconnected form Server");
                    Environment.Exit(0);
                }
            }
        }

        static void DataManager(Packet p)
        {
            // MANAGE RECEIVED DATA

            switch (p.packetType)
            {
                case PacketType.InputCode:
                    Console.WriteLine( "RECEIVED INPUT CODE: " + p.clientCode );
                    if (p.clientCode == "exit")
                        Environment.Exit(0);
                    inputState = p.clientCode;
                    break;
            }
        }

        public void SendReferenceData( double[] referenceData )
        {
            Packet referenceDataPacket = new Packet( PacketType.RegisterClient, CLIENT_ID );
            referenceDataPacket.SetReferenceData( referenceData );

            SendPacket( referenceDataPacket );
        }

        public void SendInputCode( string inputCode )
        {
            Packet inputCodePacket = new Packet(PacketType.InputCode, CLIENT_ID);
            inputCodePacket.clientCode = inputCode;

            SendPacket( inputCodePacket );
        }

        Packet depthDataPacket = null;
        public void AddDepthData( ushort[] inputData )
        {
            if( depthDataPacket == null )
                depthDataPacket = new Packet( PacketType.DepthData, CLIENT_ID );
            depthDataPacket.depthData = inputData;
        }

        public void SendDepthData()
        {
            if (this.depthDataPacket != null)
                SendPacket( depthDataPacket );
            this.depthDataPacket = null;
        }

        Packet bodyDataPacket = null;
        public void AddBodyData( double personID, Dictionary<JointType, Point3D> bodyDictIn, List<ColorSpacePoint> csPointsIn )
        {
            if (bodyDataPacket == null)
                this.bodyDataPacket = new Packet( PacketType.Transfer, CLIENT_ID );
            
            this.bodyDataPacket.personList.Add( new Person( personID, csPointsIn, bodyDictIn ) );
        }

        public void SendBodyData()
        {
            if( this.bodyDataPacket != null )
                SendPacket( this.bodyDataPacket );
            this.bodyDataPacket = null;
        }

        public void SendPacket( Packet packet )
        {
            master.Send( packet.ToBytes() );
        }
    }
}