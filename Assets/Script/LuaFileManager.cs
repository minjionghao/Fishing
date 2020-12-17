using LuaInterface;
using UnityEngine;
public class LuaFileManager
{

    private static LuaFileManager _instance;

    /**
     * 赋值了吗？？？？？？？
     */
    public LuaState luaState;
    public static LuaFileManager Instance
    {
        get
        {
            if (_instance == null) _instance = new LuaFileManager();
            return _instance;
        }
    }
    
    
    
}
