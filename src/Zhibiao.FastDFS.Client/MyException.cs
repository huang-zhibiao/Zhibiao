using System;

namespace Zhibiao.FastDFS.Client
{
    /// <summary>
    /// author zhouyh
    /// version 1.0
    /// </summary>
    public class MyException : Exception
    {
        public MyException()
        {
        }

        public MyException(string message)
            : base(message)
        {

        }
    }
}
