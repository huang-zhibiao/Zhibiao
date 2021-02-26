using System.Runtime.InteropServices;

namespace Zhibiao.UnrarNative
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public struct RARHeaderData
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
        public string ArcName;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
        public string FileName;

        public ArchiveFlags Flags;
        public uint PackSize;
        public uint UnpSize;
        public HostOS HostOS;
        public uint FileCRC;
        public uint FileTime;
        public uint UnpVer;
        public PackingMethod Method;
        public uint FileAttr;

        [MarshalAs(UnmanagedType.LPStr)]
        public string CmtBuf;

        public uint CmtBufSize;
        public uint CmtSize;
        public uint CmtState;

        //结构体不能有无参的构造方法
        public void Initialize()
        {
            this.CmtBuf = new string((char)0, 65536);
            this.CmtBufSize = 65536;
        }
    }
}