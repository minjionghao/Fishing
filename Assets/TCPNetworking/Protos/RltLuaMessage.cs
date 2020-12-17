
using LuaInterface;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using UnityEngine;

public class RltLuaMessage : ITcpRltProtocol
    {
        public RltLuaMessage()
        {
            Data = new JObject();
        }
        public ushort ID { get; set; }
        public JObject Data { get; set; }
        public virtual void Deal(LuaFunction dealFunc)
        {
            try
            {
                if (dealFunc != null)
                {
                    GameMainLoopManager.Instance.AddActionDoInMainThread (() => {	
                        var luaState = LuaFileManager.Instance.luaState;
                        if (luaState != null)
                        {
                            ProtoType type = ProtocolFactory.CheckIsLuaProto(ID);
                            if (type == ProtoType.TcpLuaProto)
                            {
                                dealFunc.Call<int, string>(ID, Data.ToString());
                            }
                            else
                            {
                                dealFunc.Call<int, string>(ID, JsonConvert.SerializeObject(this));
                            }
                        }
                    });
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning(e.Message);
            }
        }
    }
