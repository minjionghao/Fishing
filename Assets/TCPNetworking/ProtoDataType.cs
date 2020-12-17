using System.Collections.Generic;
public class ProtoDataType
{
    public string Name { get; set; }
    public int Type { get; set; }

    public bool NeedCompress = false;
    public List<int> EnumList { get; set; }
    public ProtoDataType[] Table { get; set; }
}