﻿namespace Minx.zRPC.NET
{
    public class Invocation
    {
        public string TypeName { get; set; }

        public string MethodName { get; set; }

        public object[] Arguments { get; set; }
    }
}
