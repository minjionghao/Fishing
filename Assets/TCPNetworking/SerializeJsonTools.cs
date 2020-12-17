using System;
using System.Collections.Generic;

using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;


    public class SerializeJsonTools
    {
        private static object[] constructorParams = new object[0];
        public static Encoding UTFEncoding = Encoding.UTF8;
        public static int TypeCodeList1 = 101; // 基本类型数组 e: int[] IntList;
        public static int TypeCodeList2 = 102; // 自定义类型数组 e: DataClass[] DataList;
        public static int TypeCodeList3 = 103; // 异构自定义类型数组，数组中每个item的类型都不一样 e:DropItems in RltStartGame(V1) 
        public static int TypeProtocolList = 104;
        
        
        private const string KeyName = "Name";
        private const string KeyType = "Type";
        private const string KeyTable = "Table";

        private const string ValueCodeStr = "code";


        public static byte[] GetObjectBytes(object data, ProtoDataType table, bool isJObject = true)
        {

            string name = table.Name;
            int type = table.Type;
            object obj = data;
            if (isJObject)
                obj = (data as JObject)[name].Value<object>();

            if (type == (int)TypeCode.Int32)
            {
                return BitConverter.GetBytes(Convert.ToInt32(obj));
            }
            else if (type == (int)TypeCode.UInt32)
            {
                return BitConverter.GetBytes(Convert.ToUInt32(obj));
            }
            else if (type == (int)TypeCode.UInt16)
            {
                return BitConverter.GetBytes(Convert.ToUInt16(obj));
            }
            else if (type == (int)TypeCode.Int16)
            {
                return BitConverter.GetBytes(Convert.ToInt16(obj));
            }
            else if (type == (int)TypeCode.Byte)
            {
                return new byte[]{Convert.ToByte(obj)};
//                return BitConverter.GetBytes(Convert.ToByte(obj));
            }
            else if(type == (int)TypeCode.SByte){
                return new byte[]{(byte)Convert.ToSByte(obj)};
            }
            else if (type == (int)TypeCode.Single)
            {
                return BitConverter.GetBytes(Convert.ToSingle(obj));
            }
            else if (type == (int)TypeCode.Boolean)
            {
                return BitConverter.GetBytes(Convert.ToBoolean(obj));
            }
            else if (type == (int)TypeCode.Int64)
            {
                return BitConverter.GetBytes(Convert.ToInt64(obj));
            }
            else if (type == (int)TypeCode.String)
            {
                var str = Convert.ToString(obj);
                var temp = UTFEncoding.GetBytes(str);
                var ret = new byte[2 + temp.Length];
                Buffer.BlockCopy(BitConverter.GetBytes((ushort)temp.Length), 0, ret, 0, 2);
                Buffer.BlockCopy(temp, 0, ret, 2, temp.Length);
                return ret;
            }
            else if (type == TypeCodeList1)
            {
                var arr = obj as JArray;
                var table1 = table.Table[0];
                var bytes = new List<byte>();
                // var table1 = table[KeyTable].Value<JArray>()[0].Value<JObject>();
                if (arr == null)
                {
                    bytes.AddRange(BitConverter.GetBytes((ushort)0));
                }
                else
                {
                    bytes.AddRange(BitConverter.GetBytes((ushort)arr.Count));
                    for (int i = 0; i < arr.Count; i++)
                    {
                        bytes.AddRange(GetObjectBytes(arr[i].Value<object>(), table1, false));
                    }
                }
                return bytes.ToArray();
            }
            else if (type == TypeCodeList2)
            {
                var arr = obj as JArray;
                // var table1 = table[KeyTable].Value<JArray>();
                var table1 = table.Table;
                var bytes = new List<byte>();
                if (arr == null)
                {
                    bytes.AddRange(BitConverter.GetBytes((ushort)0));
                }
                else
                {
                    bytes.AddRange(BitConverter.GetBytes((ushort)arr.Count));
                    for (int i = 0; i < arr.Count; i++)
                    {
                        for (int j = 0; j < table1.Length; j++)
                        {
                            bytes.AddRange(GetObjectBytes(arr[i].Value<JObject>(), table1[j]));
                        }
                    }
                }
                return bytes.ToArray();
            }
            else if (type == TypeCodeList3)
            {
                var arr = obj as JArray;
                var enumTable = table.Table[0];
                var bytes = new List<byte>();
                if (arr == null)
                {
                    bytes.AddRange(BitConverter.GetBytes((ushort)0));
                }
                else
                {
                    
                    bytes.AddRange(BitConverter.GetBytes((ushort)arr.Count));
                    for (int i = 0; i < arr.Count; i++)
                    {
                        var enumValue = arr[i][enumTable.Name].Value<int>();
                        var enumIndex = enumTable.EnumList.IndexOf(enumValue);
                        if (enumIndex == -1)
                        {
                            continue;
                        }
                        if (enumTable.Type == (int)TypeCode.Byte)
                        {
                            bytes.Add((byte)enumValue);
                        }
                        else if (enumTable.Type == (int)TypeCode.Int16)
                        {
                            bytes.AddRange(BitConverter.GetBytes((short)enumValue));
                        }

                        var table2 = table.Table[enumIndex + 1];
                        var item = arr[i][table2.Name].Value<JObject>();
                        for (int j = 0; j < table2.Table.Length; j++)
                        {
                            bytes.AddRange(GetObjectBytes(item, table2.Table[j]));
                        }
                    }
                }
                return bytes.ToArray();
            }
            else if (type == TypeProtocolList)
            {
                var arr = obj as JArray;
                var bytes = new List<byte>();
                bytes.AddRange(BitConverter.GetBytes((ushort)arr.Count));
                for (int i = 0; i < arr.Count; i++)
                {
                    JObject protoItem = arr[i] as JObject;
                    ReqLuaMessage msg = new ReqLuaMessage();
                    msg.ID = (ushort)protoItem["ID"];
                    msg.Data = (string)protoItem["Data"];
                    byte[] protoBytes = ProtocolFactory.Instance.GetBytes(msg);
                    bytes.AddRange(protoBytes);
                }
                return bytes.ToArray();
            }
            return null;
        }

        public static object GetObject(ProtoDataType table, ByteBuffer buffer)
        {

            int type = table.Type;

            if (type == (int)TypeCode.Int32)
            {
                var tInt = buffer.ReadInt();
                return tInt;
            }
            else if (type == (int)TypeCode.UInt32)
            {
                return buffer.ReadUInt();
            }
            else if (type == (int)TypeCode.UInt16)
            {
                return buffer.ReadUShort();
            }
            else if (type == (int)TypeCode.Int16)
            {
                return buffer.ReadShort();
            }
            else if (type == (int)TypeCode.Byte)
            {
                return buffer.ReadByte();
            }
            else if (type == (int)TypeCode.SByte){
                return (sbyte)buffer.ReadByte();
            }
            else if (type == (int)TypeCode.Single)
            {
                return buffer.ReadFloat();
            }
            else if (type == (int)TypeCode.String)
            {
                var str = buffer.ReadString();
                return str;
            }
            else if (type == (int)TypeCode.Boolean)
            {
                return buffer.ReadBoolean();
            }
            else if (type == (int)TypeCode.Int64)
            {
                return buffer.ReadLong();
            }
            else if (type == TypeCodeList1)
            {
                var len = buffer.ReadUShort();
                JArray arr = new JArray();
                if (len == 0) return arr;
                // var table1 = table[KeyTable].Value<JArray>()[0].Value<JObject>();
                var table1 = table.Table[0];

                for (int i = 0; i < len; i++)
                {
                    arr.Add(GetObject(table1, buffer));
                }
                return arr;
            }
            else if (type == TypeCodeList2)
            {
                var len = buffer.ReadUShort();
                JArray arr = new JArray();
                if (len == 0) return arr;
                // var table1 = table[KeyTable].Value<JArray>();
                var table1 = table.Table;
                for (int i = 0; i < len; i++)
                {
                    JObject obj = new JObject();
                    for (int j = 0; j < table1.Length; j++)
                    {
                        var tablej = table1[j];
                        string name = tablej.Name;
                        obj.Add(new JProperty(name, GetObject(tablej, buffer)));
                    }
                    arr.Add(obj);
                }
                return arr;
            }
            else if (type == TypeCodeList3)
            {
                var len = buffer.ReadUShort();
                JArray arr = new JArray();
                if (len == 0) return arr;
                for (int i = 0; i < len; i++)
                {
                    JObject obj = new JObject();
                    var enumTable = table.Table[0];
                    int enumIndex = -1;
                    if (enumTable.Type == (int)TypeCode.Byte)
                    {
                        var enumValue = buffer.ReadByte();
                        obj.Add(new JProperty(enumTable.Name, enumValue));
                        enumIndex = enumTable.EnumList.IndexOf(enumValue);
                    }
                    else if (enumTable.Type == (int)TypeCode.Int16)
                    {
                        var enumValue = buffer.ReadShort();
                        obj.Add(new JProperty(enumTable.Name, enumValue));
                        enumIndex = enumTable.EnumList.IndexOf(enumValue);
                    }

                    if (enumIndex >= 0)
                    {
                        var table1 = table.Table[enumIndex + 1];
                        JObject obj1 = new JObject();
                        for (int j = 0; j < table1.Table.Length; j++)
                        {
                            obj1.Add(new JProperty(table1.Table[j].Name, GetObject(table1.Table[j], buffer)));
                        }
                        obj.Add(table1.Name, obj1);
                    }
                    arr.Add(obj);
                }
                return arr;
            }
            else if (type == TypeProtocolList)
            {
                var len = buffer.ReadUShort();
                JArray arr = new JArray();
                if (len == 0) return arr;
                for (int i = 0; i < len; i++)
                {
                    JObject obj = new JObject();
                    int protoLen = buffer.ReadInt();
                    ushort protoID = buffer.ReadUShort();
                    obj.Add(new JProperty("ID", protoID));
                    JObject data = new JObject();
                    FillObject(data, ProtocolFactory.Instance.propsJsonDict[protoID], buffer, 1);                    
                    obj.Add(new JProperty("Data", JsonConvert.SerializeObject(data)));
                    arr.Add(obj);
                }
                return arr;
            }
            return null;
        }

        public static void FillObject(JObject ret, ProtoDataType table, ByteBuffer buffer, int indexOffest = 0)
        {
            for (int i = indexOffest; i < table.Table.Length; i++)
            {
                var prop = table.Table[i];
                string name = prop.Name;
                ret.Add(new JProperty(name, GetObject(prop, buffer)));
                if (name == ValueCodeStr && ret[name].ToObject<short>() != 1)
                    break;
            }
        }
    }

