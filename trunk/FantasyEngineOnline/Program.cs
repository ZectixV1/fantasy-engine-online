using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;

namespace FantasyEngineOnline
{
    class Program
    {

        static void Main(string[] args)
        {
            SocketManager a = new SocketManager();
            Console.ReadLine();
        }
    }

    class SocketManager
    {
        const int SERVER_PORT = 7845;
        const int MAX_CONNECTIONS = 5;

        private Socket m_mainSocket;
        private Socket[] m_workerSocket = new Socket[MAX_CONNECTIONS];
        private bool[] m_beingUsed = new bool[MAX_CONNECTIONS];

        private AsyncCallback m_workerCallback;

        public SocketManager()
        {
            m_workerCallback = new AsyncCallback(OnDataReceived);

            m_mainSocket = new Socket(AddressFamily.InterNetwork,
                                          SocketType.Stream,
                                          ProtocolType.Tcp);

            IPEndPoint ipLocal = new IPEndPoint(IPAddress.Any, SERVER_PORT);

            m_mainSocket.Bind(ipLocal);
            m_mainSocket.Listen(4);
            m_mainSocket.BeginAccept(new AsyncCallback(OnClientConnect), null);
        }

        void OnClientConnect(IAsyncResult asyn)
        {
            Socket thisSocket = null;
            int thisSocketIndex = 0;

            //Find a free socket
            for (int i = 0; i < MAX_CONNECTIONS; i++)
            {
                if (m_beingUsed[i] == false)
                {
                    m_beingUsed[i] = true;

                    m_workerSocket[i] = m_mainSocket.EndAccept(asyn);
                    thisSocket = m_workerSocket[i];
                    thisSocketIndex = i;

                    break;
                }
            }

            Console.WriteLine("Assigned:: " + thisSocketIndex);

            if (thisSocket != null)
            {
                WaitForData(thisSocket, thisSocketIndex);
                m_mainSocket.BeginAccept(new AsyncCallback(OnClientConnect), null);
            }
        }

        void WaitForData(Socket theSocket, int socketIndex)
        {
            SocketPacket thePacket = new SocketPacket();

            thePacket.m_currentSocket = theSocket;
            thePacket.SocketIndex = socketIndex;
            
            theSocket.BeginReceive(thePacket.dataBuffer, 0,
                                   thePacket.dataBuffer.Length,
                                   SocketFlags.None,
                                   m_workerCallback,
                                   thePacket);
        }

        void OnDataReceived(IAsyncResult async)
        {
            SocketPacket socketData = (SocketPacket)async.AsyncState;
            int iRx = 0;

            try
            {
                iRx = socketData.m_currentSocket.EndReceive(async);
                if (iRx == 0)
                {
                    socketData.m_currentSocket.Close();
                    m_beingUsed[socketData.SocketIndex] = false;

                    Console.WriteLine("Freed:: " + socketData.SocketIndex);

                }
                else
                {

                    char[] chars = new char[iRx + 1];
                    System.Text.Decoder d = System.Text.Encoding.UTF8.GetDecoder();
                    int charLen = d.GetChars(socketData.dataBuffer,
                                             0, iRx, chars, 0);

                    String szData = new String(chars);

                    WaitForData(socketData.m_currentSocket, socketData.SocketIndex);

                    Console.Write(szData);
                }
            }
            catch (SocketException err)
            {
                Console.WriteLine(err.Message);
            }
        }
    }

    class SocketPacket
    {
        public Socket m_currentSocket;
        public byte[] dataBuffer = new byte[1024];
        public int SocketIndex = 0;
    }
}
