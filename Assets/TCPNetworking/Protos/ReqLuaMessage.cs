
using LuaInterface;

    public class ReqLuaMessage : ITcpReqProtocol
    {
        public ushort ID { get; set; }
        public string Data { get; set; }
        public LuaTable Table { get; set; }
    }
