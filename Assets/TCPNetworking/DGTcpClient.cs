using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using LuaInterface;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
public enum DGSocketDisconnectReason
{
    HeartBeatTimeOut = 1,
    ErrorClose = 2,
    KcpTimeOut = 3,
    KcpError = 4,
    KcpClosed = 5,
    KcpHeartBeatTimeOut = 6
}

public class DGTcpClient
{
    public TcpClient TcpClient = null;
    private long _lastSendTime = 0;
    private float _lastReceiveGameTime;
    private long _lastReceiveTime = 0;
    private float _lastSendGameTime;
    private Task<bool> _connectTask = null;

    private int _tcpClientState = 0;
    private string _tag = "DGTcpClient";
    private LuaFunction _onConnectFunc;
    private LuaFunction _onMessageFunc;
    private LuaFunction _onDisconnectFunc;
    private LuaFunction _setServerTimeFun;
    private bool _needHeartBeat;
    private float _heartBeatInterval = 5f;
    private float _heartBeatTimeOut = 15f;
    private string _ip;
    private int _port;
    private ReqLuaMessage heartBeatProto;
    private ushort _rltHeartBeatProtoID;
    private bool _needLuaHeartbeatCallback = false;
    private int _threadSleepTime = 50;
    public DGTcpClient(string ip, int port, LuaFunction onConnectFunc, LuaFunction onMessageFunc, LuaFunction onDisconnectFunc, LuaFunction setServerTimeFunc, bool needHeartBeat = true, ushort heartBeatProtoID = 5, ushort rHeartBeatProtoID = 6, float interval = 5f, float timeout = 15f, bool needLuaHeartbeatCallback = false, int threadSleepTime = 50, string tag = "DGTcpClient")
    {
        _ip = ip;
        _port = port;
        _onConnectFunc = onConnectFunc;
        _onMessageFunc = onMessageFunc;
        _onDisconnectFunc = onDisconnectFunc;
        _setServerTimeFun = setServerTimeFunc;
        _needHeartBeat = needHeartBeat;
        _heartBeatInterval = interval;
        _heartBeatTimeOut = timeout;
        _rltHeartBeatProtoID = rHeartBeatProtoID;
        _tag = tag;
        heartBeatProto = new ReqLuaMessage();
        heartBeatProto.ID = heartBeatProtoID;
        heartBeatProto.Data = "{\"ID\":"+heartBeatProtoID+"}";
        _needLuaHeartbeatCallback = needLuaHeartbeatCallback;
        _threadSleepTime = threadSleepTime;
    }

    private string GetLogPrefix()
    {
        return "[" + _tag + "] ";
    }
    
    public async void Connect()
    {
        if(_connectTask?.IsCompleted == false)
        {
            return;
        }
        _tcpClientState = 1;        
        TcpClient?.CloseSocket();
        TcpClient = new TcpClient(_ip, _port);
        TcpClient.OnError = OnDisconnect;
        TcpClient.OnConnectFailed = OnConnectFailed;
        TcpClient.OnHeartBeat = OnHeartBeat;
        TcpClient.DealMessageFunc = _onMessageFunc;
        TcpClient.RltHeartBeatProtoID = _rltHeartBeatProtoID;
        TcpClient.NeedLuaHearbeatCallback = _needLuaHeartbeatCallback;
        TcpClient.ThreadSleepTime = _threadSleepTime;
        _connectTask = TcpClient.Connect();
        await _connectTask;
        
        _tcpClientState = 2;
        if (TcpClient.Connected)
        {
            _lastReceiveGameTime = UnityEngine.Time.time;
            _tcpClientState = 3;
            GameMainLoopManager.Instance.AddActionDoInMainThread (() => {
                #if ADDLOG
                DebugHelper.Log(GetLogPrefix() + "Notify lua Connected");
                #endif
                var luaState = LuaFileManager.Instance.luaState;
                if (luaState != null)
                {
                    _onConnectFunc.Call<bool>(true);
                }
            });
        }
        else
        {
            GameMainLoopManager.Instance.AddActionDoInMainThread (() => {
                #if ADDLOG
                DebugHelper.Log(GetLogPrefix() + "Notify lua Connect Faild");
                #endif
                var luaState = LuaFileManager.Instance.luaState;
                if (luaState != null)
                {
                    _onConnectFunc.Call<bool>(false);
                }
            });
        }
    }

    public void SetHeartBeatInterval(float interval)
    {
        _heartBeatInterval = interval;
    }

    public void SetHeartBeatTimeOut(float timeout)
    {
        _heartBeatTimeOut = timeout;
    }

    public void OnUpdate()
    {
        if (_needHeartBeat == false) return;
        if (_tcpClientState != 3) return;
        
        if (UnityEngine.Time.time - _lastSendGameTime > _heartBeatInterval)
        {
            SendHeartBeat();
        }
        
        if (UnityEngine.Time.time - _lastReceiveGameTime > _heartBeatTimeOut)
        {
            _lastReceiveGameTime += 1.0f;
            #if ADDLOG
            UnityEngine.Debug.Log(GetLogPrefix() + " HeartBeat Timeout CloseSocket");
            #endif
            OnHeartBeatTimeOut();
        }
    }

    
    public void OnClose()
    {
        #if ADDLOG
        DebugHelper.Log(GetLogPrefix() + " OnClose");
        #endif
        if (_tcpClientState == 1)
            return;
        _tcpClientState = 0;

        GameMainLoopManager.Instance.AddActionDoInMainThread (() => {
            if (TcpClient != null)
            {
                TcpClient.CloseSocket();
            }
        });
    }

    public void SendHeartBeat()
    {
        if (!CheckConnect())
        {
            OnClose();
            return;
        }
        TcpClient.SendMessage(heartBeatProto);
        _lastSendGameTime = UnityEngine.Time.time;
    }

    public void OnHeartBeat(long time)
    {
        _lastReceiveGameTime = UnityEngine.Time.time;
        _lastReceiveTime = time;
//        try
//        {
//            GameMainLoopManager.Instance.AddActionDoInMainThread (() => {	
//                var luaState = LuaFileManager.Instance.luaState;
//                if (luaState != null && _setServerTimeFun != null)
//                {
//                    _setServerTimeFun.Call<long>((long)(_lastReceiveTime / 1000));
//                }
//            });
//        }
//        catch (System.Exception e)
//        {
//            DebugHelper.LogWarning(e.Message);
//        }
    }
    
    
    public void SendLuaProto(ushort ID, string Data)
    {
        if (!CheckConnect())
            return;
        ReqLuaMessage msg = new ReqLuaMessage();
        msg.ID = ID;
        msg.Data = Data;
        TcpClient.SendMessage(msg);
    }

    public bool CheckConnect()
    {
        if (TcpClient != null && TcpClient.Connected)
        {
            return true;
        }
        return false;
    }

    public void SendMessage(IReqProto reqProto)
    {
        if (!CheckConnect())
        {
            return;
        }
        TcpClient.SendMessage(reqProto);
    }

    public void Destroy()
    {
        if (TcpClient != null)
        {
            TcpClient.CloseSocket();
        }
    }

    public void Disconnect()
    {
        if (TcpClient != null)
        {
            TcpClient.CloseSocket();
        }
    }

    private void OnHeartBeatTimeOut()
    {
        #if ADDLOG
        DebugHelper.LogError(GetLogPrefix() + " OnHeartBeatTimeOut");
#endif
        GameMainLoopManager.Instance.AddActionDoInMainThread (() => {	
            var luaState = LuaFileManager.Instance.luaState;
            if (luaState != null && _onDisconnectFunc != null)
            {
                _onDisconnectFunc.Call<DGSocketDisconnectReason>(DGSocketDisconnectReason.HeartBeatTimeOut);
            }
        });
        OnClose();
    }

    private void OnDisconnect(Exception e)
    {
        #if ADDLOG
        DebugHelper.LogError(GetLogPrefix() + " OnDisconnect：" + e.Message + " " + e.StackTrace);
#endif
        GameMainLoopManager.Instance.AddActionDoInMainThread (() => {	
            var luaState = LuaFileManager.Instance.luaState;
            if (luaState != null && _onDisconnectFunc != null)
            {
                _onDisconnectFunc.Call<DGSocketDisconnectReason>(DGSocketDisconnectReason.ErrorClose);
            }
        });
        OnClose();
    }

    private void OnConnectFailed(Exception e)
    {
        #if ADDLOG
        DebugHelper.LogError(GetLogPrefix() + "tcp连接失败：" + e.Message + " " + e.StackTrace);
        #endif
    }

    public int GetClientState()
    {
        return _tcpClientState;
    }
}
