using System;
using System.Net.Sockets;
using System.Net;
using System.Threading;
//using Unity.Tasks;
using System.Threading.Tasks;
using LuaInterface;
using UnityEngine;

//using System.Threading.Tasks;
//using UnityEngine.Networking.NetworkSystem;


    public enum NetworkState
    {
        CONNECTED,
        DISCONNECTED,
        CONNECTING
    }

    public class TcpClient
    {
        private System.Net.Sockets.TcpClient tcpClient = null;
        private string ip;
        private int port;
        private NetworkStream stream = null;
        private ByteBuffer byteBuffer;
        private object lockKey = new object();
        private byte[] buffer = new byte[65536];
        private Thread receiveThread = null;

        public Action<Exception> OnConnectFailed;
        public Action<Exception> OnError;
        public Action<long> OnHeartBeat;
        public bool stopThread = false;
        public LuaFunction DealMessageFunc;
        public ushort RltHeartBeatProtoID;
        public bool NeedLuaHearbeatCallback;
        public int ThreadSleepTime = 50;
        public NetworkState NetwrokState { get; private set; } = NetworkState.DISCONNECTED;
        public bool Connected
        {
            get
            {
                return NetwrokState == NetworkState.CONNECTED;
            }
        }

        public TcpClient(string address, int port)
        {
            Debug.Log("[jyk] NewTcpClient");
            NetwrokState = NetworkState.DISCONNECTED;
            this.ip = address;
            this.port = port;
            byteBuffer = new ByteBuffer(65536);
        }

        public Task<bool> Connect()
        {
            try
            {
                var addresses = Dns.GetHostAddresses(ip);
                for (int i = 0; i < addresses.Length; i++)
                {
                    if (addresses[i].AddressFamily == AddressFamily.InterNetwork ||
                        addresses[i].AddressFamily == AddressFamily.InterNetworkV6)
                    {
                        var client = new System.Net.Sockets.TcpClient(addresses[i].AddressFamily);
                        var address = addresses[i];
                        NetwrokState = NetworkState.CONNECTING;
                        return Task.Run(async () =>
                        {
                            try
                            {
                                CloseSocket();
                                stopThread = false;
                                tcpClient = client;
                                await tcpClient.ConnectAsync(address, port);
                                if (tcpClient.Connected == true)
                                {
                                    Debug.Log("[jyk] Connected:" + stopThread);
                                    stream = tcpClient.GetStream();
                                    NetwrokState = NetworkState.CONNECTED;
                                    this.receiveThread = new Thread(new ThreadStart(ReceiveMessage));
                                    this.receiveThread.IsBackground = true;
                                    this.receiveThread.Start();
                                }
                                else
                                {
                                    Debug.Log("[jyk] DisConnected:" + stopThread);
                                    NetwrokState = NetworkState.DISCONNECTED;
                                }

                                return tcpClient.Connected;
                            }
                            catch (Exception e)
                            {
                                if (OnConnectFailed != null)
                                {
                                    OnConnectFailed(e);
                                }

                                return false;
                            }
                        });
                    }
                }
                return Task.FromResult(false);
            }
            catch (Exception e)
            {
                return Task.FromResult(false);
            }
        }

        public void CloseSocket()
        {
            Debug.Log("[jyk] CloseSocket");
            if (tcpClient == null)
                return;
            if (stream != null)
                stream.Close();
//            if (receiveThread != null)
//                receiveThread.Abort();
            stopThread = true;
            tcpClient.Close();
            tcpClient = null;
            stream = null;
            receiveThread = null;
            NetwrokState = NetworkState.DISCONNECTED;
        }

        public void ReceiveMessage()
        {
            try
            {
                while (stopThread == false)
                {
                    if (ThreadSleepTime > 0)
                    {
                        Thread.Sleep(ThreadSleepTime);
                    }

                    if (tcpClient == null || tcpClient.Connected == false)
                        continue;
                    if (stream.CanRead && stream.DataAvailable)
                    {
                        int bytes = stream.Read(buffer, 0, buffer.Length);
                        if (bytes > 0)
                        {
                            byteBuffer.WriteBytes(buffer, bytes);
                            var protos = byteBuffer.ReadProtocol(bytes);
                            if (protos != null && protos.Length > 0)
                            {
                                GameMainLoopManager.Instance.AddActionDoInMainThread(() =>
                                {
                                    for (int i = 0; i < protos.Length; i++)
                                    {
                                        bool isHeartbeat = protos[i].ID == RltHeartBeatProtoID;
                                        if (isHeartbeat)
                                        {
                                            
                                            RltLuaMessage msg = protos[i] as RltLuaMessage;
                                            if (msg != null)
                                            {
                                                OnHeartBeat((long)msg.Data["current_time"]);
                                            }
                                        }

                                        if (isHeartbeat == false || NeedLuaHearbeatCallback)
                                        {
                                            protos[i].Deal(DealMessageFunc);
                                        }
                                    }
                                });
                            }
                        }
                    }

                }
            }
            catch (ThreadAbortException)
            {

            }
            catch (Exception e)
            {
                if (OnError != null)
                {
                    OnError(e);
                }
            }
        }

        public void SendMessage(IReqProto message)
        {
            try
            {
                if (Connected == false || message == null || tcpClient == null || tcpClient.Connected == false)
                {
                    return;
                }
#if ENABLE_LOG
                if (message is Proto.ReqLuaMessage)
                    DebugHelper.Log("send msg: " + (message as Proto.ReqLuaMessage).Data);
                else
                    DebugHelper.Log("send msg: " + Newtonsoft.Json.JsonConvert.SerializeObject(message));
#endif
                var bytes = ProtocolFactory.Instance.GetBytes(message);
                if (stream != null)
                {
                    stream.Write(bytes, 0, bytes.Length);
                    stream.Flush();
                }
                else
                {
                    Debug.LogError("stream is null!");
                }
            }
            catch(Exception e)
            {
                if (OnError != null)
                {
                    OnError(e);
                }
            }
        }
    }

