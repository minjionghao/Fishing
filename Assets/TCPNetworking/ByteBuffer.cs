using System;
using System.Text;
using System.Collections.Generic;
using UnityEngine;


    public class ByteBuffer
    {
        private byte[] bytes;
        private int rIndex, wIndex;
        // 缓冲区未读取大小
        private int length;
        private int size;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="size">需要用到的字节数，构造时会申请四倍的空间,后面一半的空间是前面一半的镜像，方面读取</param>
        public ByteBuffer(int size)
        {
            this.size = size * 2;
            bytes = new byte[size * 4];
            rIndex = wIndex = length = 0;
        }

        public void SetReadIndex(int index)
        {
            rIndex = index % bytes.Length;
        }

        public int GetReadIndex()
        {
            return rIndex;
        }

        public void SetWriteIndex(int index)
        {
            wIndex = index % bytes.Length;
        }

        public void WriteBytes(byte[] bytes, int count)
        {
            if (length + count > size)
            {
                UnityEngine.Debug.LogError("Buffer塞不下了!");
                return;
            }
            if (wIndex + count > size)
            {
                Buffer.BlockCopy(bytes, 0, this.bytes, wIndex, size - wIndex);
                Buffer.BlockCopy(bytes, 0, this.bytes, size + wIndex, size - wIndex);
                Buffer.BlockCopy(bytes, size - wIndex, this.bytes, 0, count - size + wIndex);
                Buffer.BlockCopy(bytes, size - wIndex, this.bytes, size, count - size + wIndex);
                wIndex = wIndex + count - size;
            }
            else
            {
                Buffer.BlockCopy(bytes, 0, this.bytes, wIndex, count);
                Buffer.BlockCopy(bytes, 0, this.bytes, size + wIndex, count);
                wIndex = wIndex + count;
            }
            length += count;
        }
        
        // public int Decompress(int len)
        // {
        //     var unzipData = CompressionHelper.ZipDecompress(bytes, rIndex, len);
        //     int leftCount = wIndex - rIndex - len;
        //     
        //     byte[] leftBytes = null;
        //     if (leftCount > 0)
        //     {
        //         leftBytes = new byte[leftCount];
        //         Buffer.BlockCopy(this.bytes, rIndex + len, leftBytes, 0, leftCount);
        //     }
        //
        //     wIndex = rIndex;
        //     length = length - len - leftCount;
        //     WriteBytes(unzipData, unzipData.Length);
        //     if (leftCount > 0 && leftBytes != null)
        //     {
        //         WriteBytes(leftBytes, leftBytes.Length);
        //     }
        //
        //     return unzipData.Length;
        // }

        public int ReadInt()
        {
            rIndex += 4;
            return BitConverter.ToInt32(bytes, rIndex - 4);
        }

        public short ReadShort()
        {
            rIndex += 2;
            return BitConverter.ToInt16(bytes, rIndex - 2);
        }

        public ushort ReadUShort()
        {
            rIndex += 2;
            return BitConverter.ToUInt16(bytes, rIndex - 2);
        }

        public uint ReadUInt()
        {
            rIndex += 4;
            return BitConverter.ToUInt32(bytes, rIndex - 4);
        }

        public float ReadFloat()
        {
            rIndex += 4;
            return BitConverter.ToSingle(bytes, rIndex - 4);
        }

        public string ReadString()
        {
            var len = BitConverter.ToUInt16(bytes, rIndex);
            rIndex += len + 2;
            if (len == 0) return "";
            return SerializeTools.UTFEncoding.GetString(bytes, rIndex - len, len);
        }

        public byte ReadByte()
        {
            rIndex++;
            return bytes[rIndex - 1];
        }

        public bool ReadBoolean()
        {
            rIndex++;
            return bytes[rIndex - 1] > 0;
        }
        public long ReadLong()
        {
            rIndex += 8;
            return BitConverter.ToInt64(bytes, rIndex - 8);
        }

        public ByteBuffer ReadBuffer(int len)
        {
            rIndex += len;
            ByteBuffer buffer = new ByteBuffer(len);
            Buffer.BlockCopy(bytes, rIndex - len, buffer.bytes, 0, len);
            return buffer;
        }


        public IRltProto[] ReadProtocol(int bytesCount)
        {
            if (length < 6) return null; // 正常包头长度为  包长(4bit)+协议号(2bit)
            if (bytesCount < length)
                UnityEngine.Debug.LogWarning("出现分包");
            bytesCount = length;
            List<IRltProto> protocols = new List<IRltProto>();
            int curReadCount = 0;
            while (curReadCount < bytesCount) // 如果出现黏包，会有多个协议在缓冲区，需要多次读取
            {
                var startIndex = rIndex;
                var protocolLen = BitConverter.ToInt32(bytes, rIndex);
                curReadCount += 4;
                if (protocolLen < 2)
                {
                    UnityEngine.Debug.LogError("协议解析出错，协议长度小于2");
                    break;
                }
                if (bytesCount - curReadCount < protocolLen) // 当前缓冲区内数据不完整，可能出现分包
                {
                    #if ADDLOG
                    UnityEngine.Debug.LogError("协议解析出错，缓冲区长度小于协议长度 " + protocolLen.ToString());
                    #endif
                    break;
                }
                var id = BitConverter.ToUInt16(bytes, rIndex + 4);
                rIndex += 6;
                // 前置修改，否则带压缩的长度protocolLen会变
                curReadCount += protocolLen;
                var protocol = ProtocolFactory.Instance.CreateProto(id, this, ref protocolLen);
                protocols.Add(protocol);
                length -= protocolLen + 4;
                
                if (rIndex - startIndex != protocolLen + 4)
                {
                    Debug.LogError("ID: "+id+ "  协议读取不一致 已读:" + (rIndex - startIndex).ToString() + " protocolLen:" + (protocolLen + 4).ToString());
                    rIndex = startIndex + protocolLen + 4;
                }
// #if WEB_TEST
//                 byte[] bytexxx = ProtocolFactory.Instance.GetBytes(protocol);
//                 if (bytexxx.Length != protocolLen + 4)
//                     UnityEngine.Debug.LogError("解析出来的协议长度不一致" + id.ToString() + ":" + bytexxx.Length.ToString() + "--" + (protocolLen + 4).ToString());
// #endif
            }

            rIndex = rIndex % size;
            return protocols.ToArray();
        }
        

        public void Debug128(string tag)
        {
            Debug.Log("buffer " + rIndex.ToString() + " " + tag + " " + BitConverter.ToString(bytes, rIndex, 128));
        }
    }

