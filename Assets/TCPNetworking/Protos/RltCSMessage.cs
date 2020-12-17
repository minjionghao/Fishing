
using LuaInterface;
using Newtonsoft.Json;
using UnityEngine;

public class RltCSMessage : ITcpRltProtocol
    {
        public ushort ID { get; set; }
        public short code { get; set; }
        
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
                            dealFunc.Call<int, string>(ID, JsonConvert.SerializeObject(this));
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