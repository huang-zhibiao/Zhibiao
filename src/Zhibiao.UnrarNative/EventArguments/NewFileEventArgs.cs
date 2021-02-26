namespace Zhibiao.UnrarNative
{
    public class NewFileEventArgs
	{
		public RARFileInfo fileInfo;
		public NewFileEventArgs(RARFileInfo fileInfo)
		{
			this.fileInfo = fileInfo;
		}
	}
}
