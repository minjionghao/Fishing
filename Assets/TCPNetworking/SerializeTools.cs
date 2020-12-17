
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;


    public class SerializeTools
    {
        private static object[] constructorParams = new object[0];
        public static Encoding UTFEncoding = Encoding.UTF8;



        public static byte[] GetObjectBytes(object obj, Type propType = null)
        {
            if (obj is int)
            {
                return BitConverter.GetBytes((int)obj);
            }
            else if (obj is uint)
            {
                return BitConverter.GetBytes((uint)obj);
            }
            else if (obj is ushort)
            {
                return BitConverter.GetBytes((ushort)obj);
            }
            else if (obj is short)
            {
                return BitConverter.GetBytes((short)obj);
            }
            else if (obj is byte)
            {
                return new byte[] { (byte)obj };
            }
            else if (obj is sbyte){
                return new byte[] { (byte)obj };
            }
            else if (obj is float)
            {
                return BitConverter.GetBytes((float)obj);
            }
            else if (obj is bool)
            {
                return BitConverter.GetBytes((bool)obj);
            }
            else if (obj is long)
            {
                return BitConverter.GetBytes((long)obj);
            }
            else if (obj is string)
            {
                var str = (string)obj;
                var temp = UTFEncoding.GetBytes(str);
                var ret = new byte[2 + temp.Length];
                Buffer.BlockCopy(BitConverter.GetBytes((ushort)temp.Length), 0, ret, 0, 2);
                Buffer.BlockCopy(temp, 0, ret, 2, temp.Length);
                return ret;
            }
            else if (propType != null && propType.IsAbstract)
            {
                var name = propType.FullName;
                var bytes = new List<byte>();
                bytes.AddRange(GetObjectBytes(obj.GetType().FullName));
                bytes.AddRange(GetObjectBytes(obj));
                return bytes.ToArray();
            }
            else if (obj is Array)
            {
                var arr = (Array)obj;
                var bytes = new List<byte>();
                bytes.AddRange(BitConverter.GetBytes((ushort)arr.Length));
                for (int i = 0; i < arr.Length; i++)
                {
                    bytes.AddRange(GetObjectBytes(arr.GetValue(i), Type.GetType(obj.GetType().FullName.Replace("[]", string.Empty))));
                }
                return bytes.ToArray();
            }
            else if (obj is System.Collections.IList)
            {
                var arr = (System.Collections.IList)obj;
                var bytes = new List<byte>();
                bytes.AddRange(BitConverter.GetBytes((ushort)arr.Count));
                for (int i = 0; i < arr.Count; i++)
                {
                    bytes.AddRange(GetObjectBytes(arr[i], obj.GetType().GetGenericArguments()[0]));
                }
                return bytes.ToArray();
            }
            else if (obj is UnityEngine.Vector2)
            {
                var v = (UnityEngine.Vector2)obj;
                var bytes = new List<byte>();
                bytes.AddRange(BitConverter.GetBytes(v.x));
                bytes.AddRange(BitConverter.GetBytes(v.y));
                return bytes.ToArray();
            }
            else if (obj is UnityEngine.Vector3)
            {
                var v = (UnityEngine.Vector3)obj;
                var bytes = new List<byte>();
                bytes.AddRange(BitConverter.GetBytes(v.x));
                bytes.AddRange(BitConverter.GetBytes(v.y));
                bytes.AddRange(BitConverter.GetBytes(v.z));
                return bytes.ToArray();
            }
            else if (obj is UnityEngine.Rect)
            {
                var rect = (UnityEngine.Rect)obj;
                var bytes = new List<byte>();
                bytes.AddRange(BitConverter.GetBytes(rect.x));
                bytes.AddRange(BitConverter.GetBytes(rect.y));
                bytes.AddRange(BitConverter.GetBytes(rect.width));
                bytes.AddRange(BitConverter.GetBytes(rect.height));
                return bytes.ToArray();
            }
            else if (obj is UnityEngine.RectInt)
            {
                var rect = (UnityEngine.RectInt)obj;
                var bytes = new List<byte>();
                bytes.AddRange(BitConverter.GetBytes(rect.x));
                bytes.AddRange(BitConverter.GetBytes(rect.y));
                bytes.AddRange(BitConverter.GetBytes(rect.width));
                bytes.AddRange(BitConverter.GetBytes(rect.height));
                return bytes.ToArray();
            }
            else
            {
                var props = obj.GetType().GetProperties();
                var bytes = new List<byte>() { };
                for (int i = 0; i < props.Length; i++)
                {
                    var prop = props[i].GetValue(obj);
                    bytes.AddRange(GetObjectBytes(prop, props[i].PropertyType));
                }
                return bytes.ToArray();
            }
        }

        public static object GetObject(Type type, ByteBuffer buffer)
        {
            if (type.IsEnum)
            {
                //TODO:因为暂时没有用到，所以暂时不管枚举类
                return 0;
            }
            if (Type.GetTypeCode(type) == TypeCode.Int32)
            {
                var tInt = buffer.ReadInt();
                return tInt;
            }
            else if (Type.GetTypeCode(type) == TypeCode.UInt32)
            {
                return buffer.ReadUInt();
            }
            else if (Type.GetTypeCode(type) == TypeCode.UInt16)
            {
                return buffer.ReadUShort();
            }
            else if (Type.GetTypeCode(type) == TypeCode.Int16)
            {
                return buffer.ReadShort();
            }
            else if (Type.GetTypeCode(type) == TypeCode.Byte)
            {
                return buffer.ReadByte();
            }
            else if (Type.GetTypeCode(type) == TypeCode.SByte){
                return (sbyte)buffer.ReadByte();
            }
            else if (Type.GetTypeCode(type) == TypeCode.Single)
            {
                return buffer.ReadFloat();
            }
            else if (Type.GetTypeCode(type) == TypeCode.String)
            {
                var str = buffer.ReadString();
                return str;
            }
            else if (Type.GetTypeCode(type) == TypeCode.Boolean)
            {
                return buffer.ReadBoolean();
            }
            else if (Type.GetTypeCode(type) == TypeCode.Int64)
            {
                return buffer.ReadLong();
            }
            else if (Type.GetTypeCode(type) == TypeCode.Object)
            {
                if (type.IsAbstract)
                {
                    var typeName = buffer.ReadString();
                    // UnityEngine.Debug.Log(typeName);
                    return GetObject(Type.GetType(typeName), buffer);
                }
                else if (type.IsGenericType)
                {
                    // UnityEngine.Debug.Log("arr:" + type.Name);
                    var len = buffer.ReadUShort();
                    //TODO:这里默认generic类型就是List类型!需要扩展的时候要进行修改！
                    var constructor = type.GetConstructors()[0];
                    var tType = type.GetGenericArguments()[0];
                    var list = constructor.Invoke(new object[0]) as System.Collections.IList;
                    for (int j = 0; j < len; j++)
                    {
                        list.Add(GetObject(tType, buffer));
                    }
                    return list;
                }
                else if (type.IsArray)
                {
                    // UnityEngine.Debug.Log("arr:" + type.Name);
                    var len = buffer.ReadUShort();
                    var constructor = type.GetConstructors()[0];
                    var arr = (Array)constructor.Invoke(new object[] { len });
                    if (len == 0) return arr;
                    var tType = Type.GetType(type.FullName.Replace("[]", string.Empty));
                    for (int j = 0; j < len; j++)
                    {
                        arr.SetValue(GetObject(tType, buffer), j);
                    }
                    return arr;
                }
                else if (type.IsValueType)
                {
                    // UnityEngine.Debug.Log("value type:" + type.FullName);
                    if (type.Name.EndsWith("Rect"))
                    {
                        return new UnityEngine.Rect(buffer.ReadFloat(), buffer.ReadFloat(), buffer.ReadFloat(), buffer.ReadFloat());
                    }
                    else if (type.Name.EndsWith("Vector2"))
                    {
                        return new UnityEngine.Vector2(buffer.ReadFloat(), buffer.ReadFloat());
                    }
                    else if (type.Name.EndsWith("Vector3"))
                    {
                        return new UnityEngine.Vector3(buffer.ReadFloat(), buffer.ReadFloat(), buffer.ReadFloat());
                    }
                    else if (type.Name.EndsWith("RectInt"))
                    {
                        return new UnityEngine.RectInt(buffer.ReadInt(), buffer.ReadInt(), buffer.ReadInt(), buffer.ReadInt());
                    }
                }
                else
                {
                    var data = type.GetConstructors()[0].Invoke(constructorParams);
                    var tProps = type.GetProperties();
                    FillObject(data, tProps, buffer);
                    return data;
                }
            }
            return null;
        }

        public static void FillObject(object ret, PropertyInfo[] props, ByteBuffer buffer, int indexOffest = 0)
        {
            for (int i = indexOffest; i < props.Length; i++)
            {
                UnityEngine.Debug.Log("slf " + props[i].Name);
                var prop = props[i];
                prop.SetValue(ret, GetObject(prop.PropertyType, buffer));
                if (prop.Name == "Code")
                    if ((short)prop.GetValue(ret) != 1)
                        break;
            }
        }
    }

