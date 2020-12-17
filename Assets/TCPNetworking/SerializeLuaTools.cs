
using System;
using System.Collections.Generic;
using System.Text;
using LuaInterface;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;


    public class SerializeLuaTools
    {
        private static object[] constructorParams = new object[0];
        public static Encoding UTFEncoding = Encoding.UTF8;
        public static int TypeArray = 101;
        public static int TypeCustomArray = 102;


        public static byte[] GetObjectBytes(object data, LuaTable table)
        {

            string name = LuaOpreate.getString(table, "Name");
            int type = LuaOpreate.getInt(table, "Type");
            var obj = data;
            if (string.IsNullOrEmpty(name) == false)
                obj = (data as LuaTable)[name];

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
//                return BitConverter.GetBytes(Convert.ToByte(obj));
                return new byte[]{Convert.ToByte(obj)};
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
            else if (type == TypeArray)
            {
                var arr = (LuaTable)obj;
                var table1 = table["Table"] as LuaTable;
                var bytes = new List<byte>();
                bytes.AddRange(BitConverter.GetBytes((ushort)arr.Length));
                for (int i = 0; i < arr.Length; i++)
                {
                    bytes.AddRange(GetObjectBytes(arr[i + 1], table1[1] as LuaTable));
                }
                return bytes.ToArray();
            }
            else if (type == TypeCustomArray)
            {
                var arr = (LuaTable)obj;
                var table1 = table["Table"] as LuaTable;
                var bytes = new List<byte>();
                bytes.AddRange(BitConverter.GetBytes((ushort)arr.Length));
                for (int i = 0; i < arr.Length; i++)
                {
                    for (int j = 0; j < table1.Length; j++)
                    {
                        bytes.AddRange(GetObjectBytes(arr[i + 1], table1[j + 1] as LuaTable));
                    }
                }
                return bytes.ToArray();
            }
            return null;
        }

        public static object GetObject(LuaTable table, ByteBuffer buffer)
        {

            // string name = LuaOpreate.getString(table, "Name");
            int type = LuaOpreate.getInt(table, "Type");

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
            else if (type == TypeArray)
            {
                var len = buffer.ReadUShort();
                JArray arr = new JArray();
                if (len == 0) return arr;
                var tablei = table["Table"] as LuaTable;

                for (int i = 0; i < len; i++)
                {
                    arr.Add(GetObject(tablei[1] as LuaTable, buffer));
                }
                return arr;
            }
            else if (type == TypeCustomArray)
            {
                var len = buffer.ReadUShort();
                JArray arr = new JArray();
                if (len == 0) return arr;
                var tablei = table["Table"] as LuaTable;
                for (int i = 0; i < len; i++)
                {
                    JObject obj = new JObject();
                    for (int j = 0; j < tablei.Length; j++)
                    {
                        var tablej = tablei[j + 1] as LuaTable;
                        string name = LuaOpreate.getString(tablej, "Name");
                        obj.Add(new JProperty(name, GetObject(tablej, buffer)));
                    }
                    arr.Add(obj);
                }
                return arr;
            }
            return null;
        }

        public static void FillObject(JObject ret, LuaTable table, ByteBuffer buffer, int indexOffest = 0)
        {
            for (int i = indexOffest; i < table.Length; i++)
            {
                var prop = table[i + 1] as LuaTable;
                string name = LuaOpreate.getString(prop, "Name");
                ret.Add(new JProperty(name, GetObject(prop, buffer)));
                if (name == "Code" && ret[name].ToObject<short>() != 1)
                    break;
            }
        }
    }

