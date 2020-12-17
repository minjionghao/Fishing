using LuaInterface;
using UnityEngine;
public class LuaOpreate
    {
        public static string getString(LuaTable table, string name)
        {
            return table.GetTable<string>(name);
        }
        
        public static int getInt(LuaTable table, string type)
        {
            return table.GetTable<int>(type);
        }
    }