using System;
using System.Reflection;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

public enum ProtoType
    {
        TcpLuaProto = 1,
        TcpCsProto = 2,
        KcpLuaProto = 3,
    }
    
    public class ProtocolFactory
    {

        private static ProtocolFactory _instance;
        public static ProtocolFactory Instance
        {
            get
            {
                if (_instance == null) _instance = new ProtocolFactory();
                return _instance;
            }
        }
        private Dictionary<ushort, ConstructorInfo> constructorsDict;
        private Dictionary<ushort, PropertyInfo[]> propsDict;
        public Dictionary<ushort, ProtoDataType> propsJsonDict;
        private object[] constructorParams;

        private ProtocolFactory()
        {
            constructorParams = new object[0];
            constructorsDict = new Dictionary<ushort, ConstructorInfo>();
            propsDict = new Dictionary<ushort, PropertyInfo[]>();
            propsJsonDict = new Dictionary<ushort, ProtoDataType>();
        }

        private void AddProto(ushort id, Type protoType)
        {
            constructorsDict.Add(id, protoType.GetConstructor(BindingFlags.Instance | BindingFlags.Public, null, new Type[0], null));
            propsDict.Add(id, protoType.GetProperties());
        }

        public void AddLuaProto(ushort id, string typeJson)
        {
            var protocol = JsonConvert.DeserializeObject<ProtoDataType>(typeJson);
            if(!propsJsonDict.ContainsKey(id))
                propsJsonDict.Add(id, protocol);
        }

        public IRltProto CreateProto(ushort id, ByteBuffer buffer, ref int protocolLen)
        {
            var type = CheckIsLuaProto(id);
            switch (type)
            {
                case ProtoType.TcpLuaProto:
                    return CreateLuaProto(id, buffer, ref protocolLen);
                case ProtoType.KcpLuaProto:
                    return CreateLuaProto(id, buffer, ref protocolLen);
                case ProtoType.TcpCsProto:
                    return CreateCSProto(id, buffer);
                default:
                    return CreateLuaProto(id, buffer, ref protocolLen);
            }
        }

        public IRltProto CreateCSProto(ushort id, ByteBuffer buffer)
        {
            var ret = constructorsDict[id].Invoke(constructorParams) as IRltProto;
            ret.ID = id;
            var props = propsDict[id];
            SerializeTools.FillObject(ret, propsDict[id], buffer, 1);
            return ret;
        }

        public IRltProto CreateLuaProto(ushort id, ByteBuffer buffer, ref int protocolLen)
        {
            if (propsJsonDict.ContainsKey(id))
            {
                var props = propsJsonDict[id];
                /**
                 * 暂时不用吧
                 */
                // if (props.NeedCompress)
                // {
                //     int decompressLen = buffer.Decompress(protocolLen - 2);
                //     protocolLen = decompressLen + 2;
                // }
                var ret = new RltLuaMessage();
                ret.ID = id;
                SerializeJsonTools.FillObject(ret.Data, props, buffer, 1);
                return ret;    
            }
            else
            {
                Debug.LogError("协议 id "+id+"未定义");
                return null;
            }
        }

        private byte[] GetProtocolBytes(IProtocol protocol)
        {
            var bytes = new List<byte>();
            var props = propsDict[protocol.ID];
            for (int i = 1; i < props.Length; i++)
            {
                var prop = props[i].GetValue(protocol);
                bytes.AddRange(SerializeTools.GetObjectBytes(prop));
            }
            return bytes.ToArray();
        }

        public byte[] GetLuaProtocolBytes(ReqLuaMessage protocol)
        {
            try
            {
                var bytes = new List<byte>();
                var props = propsJsonDict[protocol.ID];
                var data = JObject.Parse(protocol.Data);
                for (int i = 1; i < props.Table.Length; i++)
                {
                    bytes.AddRange(SerializeJsonTools.GetObjectBytes(data, props.Table[i]));
                }
                var result = bytes.ToArray();
                /**
                 * 暂时先不用吧
                 */
                // if (props.NeedCompress)
                // {
                //     result = CompressionHelper.ZipCompress(result);
                // }
                return result;
            }
            catch(Exception e)
            {
                UnityEngine.Debug.Log("slf GetLuaProtocolBytes ID:" + protocol.ID + " e:" + e.ToString());
                return null;
            }
        }

        public void ClearLoadedProtocol()
        {
            if (propsJsonDict != null)
            {
                propsJsonDict.Clear();
            }
        }
        
        // private byte[] GetKcpLuaProtocolBytes(Proto.ReqKcpLuaMessage protocol)
        // {
        //     try
        //     {
        //         var bytes = new List<byte>();
        //         var props = propsJsonDict[protocol.ID];
        //         var data = JObject.Parse(protocol.Data);
        //         for (int i = 1; i < props.Table.Length; i++)
        //         {
        //             bytes.AddRange(SerializeJsonTools.GetObjectBytes(data, props.Table[i]));
        //         }
        //         return bytes.ToArray();
        //     }
        //     catch(Exception e)
        //     {
        //         Debug.Log("GetKcpLuaProtocolBytes e:" + e.ToString());
        //         return null;
        //     }
        // }

        public byte[] GetBytes(IProtocol protocol)
        {
            var bID = BitConverter.GetBytes(protocol.ID);
            byte[] bytes;
            ProtoType type = CheckIsLuaProto(protocol.ID);
            switch (type)
            {
                case ProtoType.TcpLuaProto:
                    bytes = GetLuaProtocolBytes((ReqLuaMessage)protocol);
                    break;
                case ProtoType.KcpLuaProto:
                    bytes = GetLuaProtocolBytes((ReqLuaMessage)protocol);
                    break;
                case ProtoType.TcpCsProto:
                    bytes = GetProtocolBytes(protocol);
                    break;
                default:
                    bytes = GetLuaProtocolBytes((ReqLuaMessage)protocol);
                    break;
            }            
            
            var bLen = BitConverter.GetBytes((int)(bytes.Length + 2));
            var ret = new byte[bytes.Length + 6];
            Buffer.BlockCopy(bLen, 0, ret, 0, 4);
            Buffer.BlockCopy(bID, 0, ret, 4, 2);
            Buffer.BlockCopy(bytes, 0, ret, 6, bytes.Length);
            return ret;
        }

        private static ushort TCP_LUA_PROTO_MAX = 9999;
        private static ushort KCP_LUA_PROTO_MAX = 29999;
        public static ProtoType CheckIsLuaProto(ushort ID)
        {
            if (ID <= TCP_LUA_PROTO_MAX)
                return ProtoType.TcpLuaProto;
            else if (ID <= KCP_LUA_PROTO_MAX)
                return ProtoType.KcpLuaProto;
            else
                return ProtoType.TcpCsProto;
        }
    }

