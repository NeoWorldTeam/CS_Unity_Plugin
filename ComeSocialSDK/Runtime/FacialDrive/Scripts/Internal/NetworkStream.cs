using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;
using UnityEditor;
using UnityEditor.VersionControl;
using UnityEngine;

namespace ComeSocial.Face.Drive
{
    /// <inheritdoc cref="IStreamSource" />
    /// <summary>
    /// 一个基于网络的Stream Source 在给定的端口上设置一个监听服务器，让客户端连接到该服务器。
    /// </summary>
    public class NetworkStream : MonoBehaviour, IStreamSource
    {
        /// <summary>
        /// 最大的缓冲区队列大小，超过这个大小的旧帧将被丢弃
        /// </summary>
        const int k_MaxBufferQueue = 3;

        /// <summary>
        /// Socket.Listen中 "backlog "参数的值
        /// </summary>
        const int k_MaxConnections = 3;

#pragma warning disable 649
        [SerializeField]
        [Tooltip("缓冲区BS列表和ARKIT BS列表的映射信息，用于解释来自连接设备的数据流。")]
        StreamSettings m_StreamSettings;

        [SerializeField]
        [Tooltip("端口")]
        int m_Port = 9000;

        [SerializeField]
        [Tooltip("在尝试跳过帧之前，存储的帧数的阈值。")]
        int m_CatchUpThreshold = 5;

        [SerializeField]
        [Tooltip("如果编辑器在处理设备流的过程中落后，应该一次处理多少个帧。在一个活动的记录中，这些帧仍然被捕获，即使它们在编辑器中被跳过。")]
        int m_CatchUpSize = 3;

#pragma warning restore 649




        bool m_Running;

        //监听端口
        readonly List<Socket> m_ListenSockets = new List<Socket>();
        //处理线程
        readonly List<Thread> m_ActiveThreads = new List<Thread>();

        //实际传输数据的端口
        Socket m_TransferSocket;

        //内存管理
        CycleBufferManager cycleBufferManager = new CycleBufferManager();


        //implement IStreamSource
        public IStreamReader streamReader { private get; set; }

        public bool active
        {
            get { return m_TransferSocket != null && m_TransferSocket.Connected; }
        }

        public IStreamSettings streamSettings { get { return m_StreamSettings; } }


        private void Awake()
        {
            if (m_StreamSettings == null)
            {
                m_StreamSettings = Resources.Load<StreamSettings>("StreamSourceSettings/Come Social Stream Settings");
            }
        }

        void Start()
        {
            Debug.Log("NetworkStream Start");
            //检查配置
            if (m_StreamSettings == null)
            {
                Debug.LogErrorFormat("No Stream Setting set on {0}. Unable to run Server.", this);
                enabled = false;
                return;
            }

            //监听端口
            Debug.Log("Possible IP addresses:");
            IPAddress[] addresses;
            try
            {
                addresses = Dns.GetHostEntry(Dns.GetHostName()).AddressList;
            }
            catch (Exception)
            {
                Debug.LogWarning("DNS-based method failed, using network interfaces to find local IP");
                var addressList = new List<IPAddress>();
                foreach (var networkInterface in NetworkInterface.GetAllNetworkInterfaces())
                {
                    if (networkInterface.NetworkInterfaceType == NetworkInterfaceType.Loopback)
                        continue;

                    switch (networkInterface.OperationalStatus)
                    {
                        case OperationalStatus.Up:
                        case OperationalStatus.Unknown:
                            foreach (var ip in networkInterface.GetIPProperties().UnicastAddresses)
                            {
                                addressList.Add(ip.Address);
                            }

                            break;
                    }
                }

                addresses = addressList.ToArray();
            }

            foreach (var address in addresses)
            {
                if (address.AddressFamily != AddressFamily.InterNetwork)
                    continue;

                if (IPAddress.IsLoopback(address))
                    continue;

                var connectionAddress = address;
                Debug.Log(connectionAddress + ":" + m_Port);

                Socket listenSocket;
                try
                {
                    var endPoint = new IPEndPoint(IPAddress.Any, m_Port);
                    listenSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    listenSocket.Bind(endPoint);
                    listenSocket.Listen(k_MaxConnections);
                    m_ListenSockets.Add(listenSocket);

                    m_Running = true;
                }
                catch (Exception e)
                {
                    Debug.LogErrorFormat("Error creating listen socket on address {0} : {1}", connectionAddress, e);
                    continue;
                }

                var newtworkThread = new Thread(() =>
                {
                    // Block until timeout or successful connection
                    var socket = listenSocket.Accept();


                    // If another socket has already accepted a connection, exit the thread
                    if (m_TransferSocket != null)
                        return;

                    m_TransferSocket = socket;
                    Debug.Log(string.Format("Client connected on {0}", connectionAddress));


                    //协议头的缓存区
                    byte[] messageHeaderBuffer = new byte[15];
                    int readHeaderSize = 0;
                    MessageHeader messageHeader = new MessageHeader();

                    while (m_Running)
                    {
                        if (streamReader == null)
                            continue;

                        var source = streamReader.streamSource;
                        if (socket.Connected && source != null && source.Equals(this))
                        {
                            try
                            {
                                //读取协议头
                                if (readHeaderSize < CycleBuffer.HeaderLength)
                                {
                                    int needReadSize = CycleBuffer.HeaderLength - readHeaderSize;
                                    readHeaderSize += socket.Receive(messageHeaderBuffer, readHeaderSize, needReadSize, SocketFlags.None);

                                    if (readHeaderSize == CycleBuffer.HeaderLength)
                                    {
                                        //解析文件头
                                        GCHandle pinnedPacket = GCHandle.Alloc(messageHeaderBuffer.Reverse().ToArray(), GCHandleType.Pinned);
                                        messageHeader = Marshal.PtrToStructure<MessageHeader>(pinnedPacket.AddrOfPinnedObject());
                                        pinnedPacket.Free();

                                        int messageContentSize = messageHeader.messageSize - CycleBuffer.HeaderLength;

                                        MessageHandler messageHander = cycleBufferManager.getMessageHander(messageHeader.messageType, messageHeader.messageSize);
                                        CycleBuffer cycleBuffer = messageHander.getWriteCycleBuffer();
                                        //清除之前读取的数据
                                        cycleBuffer.readSize = 0;
                                        cycleBuffer.len = messageContentSize;
                                        messageHander.checkCode = messageHeader.checkCode;



                                        if (messageHeader.messageSize < 1)
                                        {
                                            Debug.LogError("数据出错");
                                            break;
                                        }
                                    }
                                }
                                    
                                if (readHeaderSize >= CycleBuffer.HeaderLength)
                                {
                                    //根据类型获取 MessageHander
                                    MessageHandler messageHandler = cycleBufferManager.getMessageHander(messageHeader.messageType, messageHeader.messageSize);
                                    CycleBuffer cycleBuffer = messageHandler.getWriteCycleBuffer();
                                    messageHandler.ReadMessageContent(socket);

                                    if (messageHandler.isReadFinish(cycleBuffer))
                                    {
                                        //清除协议头
                                        readHeaderSize = 0;
                                    }
                                }
                                    
                            }
                            catch (Exception e)
                            {
                                // Expect an exception on the last frame when OnDestroy closes the socket
                                if (m_Running)
                                    Debug.LogError(e.Message + "\n" + e.StackTrace);
                            }
                        }


                        Thread.Sleep(1);
                    }

                    socket.Disconnect(false);
                });
                newtworkThread.Start();
                m_ActiveThreads.Add(newtworkThread);
            }
        }



        void UpdateCurrentFrameBuffer()
        {
            MessageHandler messageHander = cycleBufferManager.getMessageHander(1,0);
            if (messageHander != null && messageHander.hasReadBuffer())
            {
                var messageContentBuffer = messageHander.getReadCycleBuffer();
                if (streamReader.streamSource.Equals(this))
                    streamReader.UpdateStreamData(messageContentBuffer.buffer, 0, messageContentBuffer.len);
            }


            messageHander = cycleBufferManager.getMessageHander(2, 0);
            if (messageHander != null && messageHander.hasReadBuffer())
            {
                var messageContentBuffer = messageHander.getReadCycleBuffer();
                if (streamReader.streamSource.Equals(this))
                    streamReader.UpdateHeadPoseStreamData(messageContentBuffer.buffer, 0, messageContentBuffer.len);
            }

            messageHander = cycleBufferManager.getMessageHander(3, 0);
            if (messageHander != null && messageHander.hasReadBuffer())
            {
                var messageContentBuffer = messageHander.getReadCycleBuffer();
                if (streamReader.streamSource.Equals(this))
                    streamReader.UpdateEyePoseStreamData(messageContentBuffer.buffer, 0, messageContentBuffer.len);
            }
        }




        public void StreamSourceUpdate()
        {
            var source = streamReader.streamSource;
            var notSource = source == null || !source.Equals(this);

            if (notSource || !active)
                return;

            UpdateCurrentFrameBuffer();
        }





        void OnDestroy()
        {
            Debug.Log("NetworkStraem OnDestroy");
            m_Running = false;

            foreach (var thread in m_ActiveThreads)
            {
                thread.Abort();
            }

            foreach (var socket in m_ListenSockets)
            {
                if (socket.Connected)
                    socket.Disconnect(false);

                socket.Close();
            }
        }
    }


    
}
