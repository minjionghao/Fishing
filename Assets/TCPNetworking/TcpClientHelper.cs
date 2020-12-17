using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using LuaInterface;
using UnityEngine;
using Newtonsoft.Json;
public class TcpClientHelper
{
    public static string address = "";
    public static int port = -1;
    public static List<DGTcpClient> ClientArray = new List<DGTcpClient>();

    public static DGTcpClient CreateConnection(string ip, int port, LuaFunction onConnectFuncName, LuaFunction onMessageFuncName, LuaFunction onDisconnectFuncName, LuaFunction setServerTimeFuncName, bool needHeartBeat = true, ushort heartBeatProtoID = 5, ushort rHeartBeatProtoID = 6, float interval = 5f, float timeout = 15f, bool needLuaHBCallback = false, int tSleep = 50, string tag = "DGTcpClient")
    {
        DGTcpClient client = new DGTcpClient(ip, port, onConnectFuncName, onMessageFuncName, onDisconnectFuncName, setServerTimeFuncName, needHeartBeat, heartBeatProtoID, rHeartBeatProtoID, interval, timeout, needLuaHBCallback, tSleep, tag);
        client.Connect();
        ClientArray.Add(client);
        return client;
    }

    public static void CloseConnection(DGTcpClient client)
    {
        try
        {
            if (client == null) return;
            for (int i = ClientArray.Count - 1; i >= 0; i--)
            {
                var lClient = ClientArray[i];
                if (client == lClient)
                {
                    ClientArray.RemoveAt(i);
                    lClient.Disconnect();
                }
            }
        }
        catch (Exception e)
        {
            Debug.Log("TcpClientHelper CloseConnection Error: e = " + e.Message+ " trace = " + e.StackTrace);
        }
    }

    public static void CloseAllConnection()
    {
        try
        {
            for (int i = ClientArray.Count - 1; i >= 0; i--)
            {
                var lClient = ClientArray[i];
                ClientArray.RemoveAt(i);
                if(lClient != null)
                    lClient.Disconnect();
            }
            ClientArray.Clear();
        }
        catch (Exception e)
        {
            Debug.Log("TcpClientHelper CloseConnection Error: e = " + e.Message+ " trace = " + e.StackTrace);
        }
    }

    public static void OnUpdate()
    {
        try
        {
            for (int i = 0; i < ClientArray.Count; i++)
            {
                var lClient = ClientArray[i];
                if(lClient != null)
                    lClient.OnUpdate();
            }
        }
        catch (Exception e)
        {
            Debug.Log("TcpClientHelper OnUpdate Error: e = " + e.Message+ " trace = " + e.StackTrace);
        }
    }
}
