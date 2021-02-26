using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace Zhibiao.UnrarNative
{
    #region Event Deleegate Definitions

    /// <summary>
    /// Represents the method that will handle data available events
    /// </summary>
    public delegate void DataAvailableHandler(object sender, DataAvailableEventArgs e);
    /// <summary>
    /// Represents the method that will handle extraction progress events
    /// </summary>
    public delegate void ExtractionProgressHandler(object sender, ExtractionProgressEventArgs e);
    /// <summary>
    /// Represents the method that will handle missing archive volume events
    /// </summary>
    public delegate void MissingVolumeHandler(object sender, MissingVolumeEventArgs e);
    /// <summary>
    /// Represents the method that will handle new volume events
    /// </summary>
    public delegate void NewVolumeHandler(object sender, NewVolumeEventArgs e);
    /// <summary>
    /// Represents the method that will handle new file notifications
    /// </summary>
    public delegate void NewFileHandler(object sender, NewFileEventArgs e);
    /// <summary>
    /// Represents the method that will handle password required events
    /// </summary>
    public delegate void PasswordRequiredHandler(object sender, PasswordRequiredEventArgs e);

    #endregion

    public partial class Unrar : IDisposable
    {
        #region Private fields

        private string archivePathName = string.Empty;
        private IntPtr archiveHandle = new IntPtr(0);
        private bool retrieveComment = true;
        private string password = string.Empty;
        private string comment = string.Empty;
        private ArchiveFlags archiveFlags = 0;
        private RARHeaderDataEx header = new RARHeaderDataEx();
        private string destinationPath = string.Empty;
        private RARFileInfo currentFile = null;
        private UNRARCallback callback = null;

        #endregion

        #region Public event declarations

        /// <summary>
        /// Event that is raised when a new chunk of data has been extracted
        /// </summary>
        public event DataAvailableHandler DataAvailable;
        /// <summary>
        /// Event that is raised to indicate extraction progress
        /// </summary>
        public event ExtractionProgressHandler ExtractionProgress;
        /// <summary>
        /// Event that is raised when a required archive volume is missing
        /// </summary>
        public event MissingVolumeHandler MissingVolume;
        /// <summary>
        /// Event that is raised when a new file is encountered during processing
        /// </summary>
        public event NewFileHandler NewFile;
        /// <summary>
        /// Event that is raised when a new archive volume is opened for processing
        /// </summary>
        public event NewVolumeHandler NewVolume;
        /// <summary>
        /// Event that is raised when a password is required before continuing
        /// </summary>
        public event PasswordRequiredHandler PasswordRequired;

        #endregion

        public Unrar()
        {
            this.callback = new UNRARCallback(RARCallback);
        }

        public Unrar(string archivePathName) : this()
        {
            this.archivePathName = archivePathName;
        }

        ~Unrar()
        {
            if (this.archiveHandle != IntPtr.Zero)
            {
                Unrar.RARCloseArchive(this.archiveHandle);
                this.archiveHandle = IntPtr.Zero;
            }
        }

        public void Dispose()
        {
            if (this.archiveHandle != IntPtr.Zero)
            {
                Unrar.RARCloseArchive(this.archiveHandle);
                this.archiveHandle = IntPtr.Zero;
            }
        }

        #region Public Properties

        /// <summary>
        /// Path and name of RAR archive to open
        /// </summary>
        public string ArchivePathName
        {
            get
            {
                return this.archivePathName;
            }
            set
            {
                this.archivePathName = value;
            }
        }

        /// <summary>
        /// Archive comment 
        /// </summary>
        public string Comment
        {
            get
            {
                return (this.comment);
            }
        }

        /// <summary>
        /// Current file being processed
        /// </summary>
        public RARFileInfo CurrentFile
        {
            get
            {
                return (this.currentFile);
            }
        }

        /// <summary>
        /// Default destination path for extraction
        /// </summary>
        public string DestinationPath
        {
            get
            {
                return this.destinationPath;
            }
            set
            {
                this.destinationPath = value;
            }
        }

        /// <summary>
        /// Password for opening encrypted archive
        /// </summary>
        public string Password
        {
            get
            {
                return (this.password);
            }
            set
            {
                this.password = value;
                if (this.archiveHandle != IntPtr.Zero)
                    RARSetPassword(this.archiveHandle, value);
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Close the currently open archive
        /// </summary>
        /// <returns></returns>
        public void Close()
        {
            // Exit without exception if no archive is open
            if (this.archiveHandle == IntPtr.Zero)
                return;

            // Close archive
            var result = Unrar.RARCloseArchive(this.archiveHandle);

            // Check result
            if (result != 0)
            {
                ProcessFileError((RarError)result);
            }
            else
            {
                this.archiveHandle = IntPtr.Zero;
            }
        }

        /// <summary>
        /// Opens archive specified by the ArchivePathName property for testing or extraction
        /// </summary>
        public void Open()
        {
            if (this.ArchivePathName.Length == 0)
                throw new IOException("Archive name has not been set.");

            this.Open(this.ArchivePathName, OpenMode.Extract);
        }

        /// <summary>
        /// Opens archive specified by the ArchivePathName property with a specified mode
        /// </summary>
        /// <param name="openMode">Mode in which archive should be opened</param>
        public void Open(OpenMode openMode)
        {
            if (this.ArchivePathName.Length == 0)
                throw new IOException("Archive name has not been set.");
            this.Open(this.ArchivePathName, openMode);
        }

        /// <summary>
        /// Opens specified archive using the specified mode.  
        /// </summary>
        /// <param name="archivePathName">Path of archive to open</param>
        /// <param name="openMode">Mode in which to open archive</param>
        public void Open(string archivePathName, OpenMode openMode)
        {
            // Close any previously open archives
            if (this.archiveHandle != IntPtr.Zero)
                this.Close();

            // Prepare extended open archive struct
            this.ArchivePathName = archivePathName;
            RAROpenArchiveDataEx openStruct = new RAROpenArchiveDataEx();
            openStruct.Initialize();
            openStruct.ArcName = this.archivePathName + "\0";
            openStruct.ArcNameW = this.archivePathName + "\0";
            openStruct.OpenMode = openMode;
            if (this.retrieveComment)
            {
                openStruct.CmtBuf = new string((char)0, 65536);
                openStruct.CmtBufSize = 65536;
            }
            else
            {
                openStruct.CmtBuf = null;
                openStruct.CmtBufSize = 0;
            }

            // Open archive
            IntPtr handle = RAROpenArchiveEx(ref openStruct);

            // Check for success
            if (openStruct.OpenResult != 0)
            {
                switch (openStruct.OpenResult)
                {
                    case RarError.InsufficientMemory:
                        throw new OutOfMemoryException("Insufficient memory to perform operation.");

                    case RarError.BadData:
                        throw new IOException("Archive header broken");

                    case RarError.BadArchive:
                        throw new IOException("File is not a valid archive.");

                    case RarError.OpenError:
                        throw new IOException("File could not be opened.");
                }
            }

            // Save handle and flags
            this.archiveHandle = handle;
            this.archiveFlags = openStruct.Flags;

            // Set callback
            Unrar.RARSetCallback(this.archiveHandle, this.callback, this.GetHashCode());

            // If comment retrieved, save it
            if (openStruct.CmtState == 1)
                this.comment = openStruct.CmtBuf.ToString();

            // If password supplied, set it
            if (this.password.Length != 0)
                Unrar.RARSetPassword(this.archiveHandle, this.password);

            // Fire NewVolume event for first volume
            this.OnNewVolume(this.archivePathName);
        }

        /// <summary>
        /// Reads the next archive header and populates CurrentFile property data
        /// </summary>
        /// <returns></returns>
        public bool ReadHeader()
        {
            // Throw exception if archive not open
            if (this.archiveHandle == IntPtr.Zero)
                throw new IOException("Archive is not open.");

            // Initialize header struct
            this.header = new RARHeaderDataEx();
            header.Initialize();

            // Read next entry
            currentFile = null;
            var result = Unrar.RARReadHeaderEx(this.archiveHandle, ref this.header);

            // Check for error or end of archive
            if ((RarError)result == RarError.EndOfArchive)
                return false;
            else if ((RarError)result == RarError.BadData)
                throw new IOException("Archive data is corrupt.");

            // Determine if new file
            if ((((uint)header.Flags & 0x01) != 0) && currentFile != null)
                currentFile.ContinuedFromPrevious = true;
            else
            {
                // New file, prepare header
                currentFile = new RARFileInfo
                {
                    FileName = header.FileNameW.ToString()
                };
                if (((uint)header.Flags & 0x02) != 0)
                    currentFile.ContinuedOnNext = true;
                if (header.PackSizeHigh != 0)
                    currentFile.PackedSize = (header.PackSizeHigh * 0x100000000) + header.PackSize;
                else
                    currentFile.PackedSize = header.PackSize;
                if (header.UnpSizeHigh != 0)
                    currentFile.UnpackedSize = (header.UnpSizeHigh * 0x100000000) + header.UnpSize;
                else
                    currentFile.UnpackedSize = header.UnpSize;
                currentFile.HostOS = (int)header.HostOS;
                currentFile.FileCRC = header.FileCRC;
                currentFile.FileTime = FromMSDOSTime(header.FileTime);
                currentFile.VersionToUnpack = (int)header.UnpVer;
                currentFile.Method = (int)header.Method;
                currentFile.FileAttributes = (int)header.FileAttr;
                currentFile.BytesExtracted = 0;
                if ((header.Flags & FileFlags.Directory) == FileFlags.Directory)
                    currentFile.IsDirectory = true;
                this.OnNewFile();
            }

            // Return success
            return true;
        }

        /// <summary>
        /// Returns array of file names contained in archive
        /// </summary>
        /// <returns></returns>
        public string[] ListFiles()
        {
            ArrayList fileNames = new ArrayList();
            while (this.ReadHeader())
            {
                if (!currentFile.IsDirectory)
                    fileNames.Add(currentFile.FileName);
                this.Skip();
            }
            string[] files = new string[fileNames.Count];
            fileNames.CopyTo(files);
            return files;
        }

        /// <summary>
        /// Moves the current archive position to the next available header
        /// </summary>
        /// <returns></returns>
        public void Skip()
        {
            var result = Unrar.RARProcessFile(this.archiveHandle, (int)Operation.Skip, string.Empty, string.Empty);

            // Check result
            if (result != RarError.Success)
            {
                ProcessFileError(result);
            }
        }

        /// <summary>
        /// Tests the ability to extract the current file without saving extracted data to disk
        /// </summary>
        /// <returns></returns>
        public void Test()
        {
            var result = RARProcessFile(this.archiveHandle, Operation.Test, string.Empty, string.Empty);

            // Check result
            if (result != RarError.Success)
            {
                ProcessFileError(result);
            }
        }

        /// <summary>
        /// Extracts the current file to the default destination path
        /// </summary>
        /// <returns></returns>
        public void Extract()
        {
            this.Extract(this.destinationPath, string.Empty);
        }

        /// <summary>
        /// Extracts the current file to a specified destination path and filename
        /// </summary>
        /// <param name="destinationName">Path and name of extracted file</param>
        /// <returns></returns>
        public void Extract(string destinationName)
        {
            this.Extract(string.Empty, destinationName);
        }

        /// <summary>
        /// Extracts the current file to a specified directory without renaming file
        /// </summary>
        /// <param name="destinationPath"></param>
        /// <returns></returns>
        public void ExtractToDirectory(string destinationPath)
        {
            this.Extract(destinationPath, string.Empty);
        }

        #endregion

        #region Protected Virtual (Overridable) Methods

        protected virtual void OnNewFile()
        {
            if (this.NewFile != null)
            {
                var e = new NewFileEventArgs(this.currentFile);
                this.NewFile(this, e);
            }
        }

        protected virtual int OnPasswordRequired(IntPtr p1, int p2)
        {
            int result = -1;
            if (this.PasswordRequired != null)
            {
                PasswordRequiredEventArgs e = new PasswordRequiredEventArgs();
                this.PasswordRequired(this, e);
                if (e.ContinueOperation && e.Password.Length > 0)
                {
                    for (int i = 0; (i < e.Password.Length) && (i < p2); i++)
                        Marshal.WriteByte(p1, i, (byte)e.Password[i]);
                    Marshal.WriteByte(p1, e.Password.Length, (byte)0);
                    result = 1;
                }
            }
            else
            {
                throw new IOException("Password is required for extraction.");
            }
            return result;
        }

        protected virtual int OnDataAvailable(IntPtr p1, int p2)
        {
            int result = 1;
            if (this.currentFile != null)
                this.currentFile.BytesExtracted += p2;
            if (this.DataAvailable != null)
            {
                byte[] data = new byte[p2];
                Marshal.Copy(p1, data, 0, p2);
                DataAvailableEventArgs e = new DataAvailableEventArgs(data);
                this.DataAvailable(this, e);
                if (!e.ContinueOperation)
                    result = -1;
            }
            if ((this.ExtractionProgress != null) && (this.currentFile != null))
            {
                ExtractionProgressEventArgs e = new ExtractionProgressEventArgs
                {
                    FileName = this.currentFile.FileName,
                    FileSize = this.currentFile.UnpackedSize,
                    BytesExtracted = this.currentFile.BytesExtracted,
                    PercentComplete = this.currentFile.PercentComplete
                };
                this.ExtractionProgress(this, e);
                if (!e.ContinueOperation)
                    result = -1;
            }
            return result;
        }

        protected virtual int OnNewVolume(string volume)
        {
            int result = 1;
            if (this.NewVolume != null)
            {
                NewVolumeEventArgs e = new NewVolumeEventArgs(volume);
                this.NewVolume(this, e);
                if (!e.ContinueOperation)
                    result = -1;
            }
            return result;
        }

        protected virtual string OnMissingVolume(string volume)
        {
            string result = string.Empty;
            if (this.MissingVolume != null)
            {
                MissingVolumeEventArgs e = new MissingVolumeEventArgs(volume);
                this.MissingVolume(this, e);
                if (e.ContinueOperation)
                    result = e.VolumeName;
            }
            return result;
        }

        #endregion

        #region Private Methods

        private void Extract(string destinationPath, string destinationName)
        {
            var result = RARProcessFile(archiveHandle, Operation.Extract, destinationPath, destinationName);

            // Check result
            if (result != RarError.Success)
            {
                ProcessFileError(result);
            }
        }

        private DateTime FromMSDOSTime(uint dosTime)
        {
            ushort hiWord = (ushort)((dosTime & 0xFFFF0000) >> 16);
            ushort loWord = (ushort)(dosTime & 0xFFFF);
            int year = ((hiWord & 0xFE00) >> 9) + 1980;
            int month = (hiWord & 0x01E0) >> 5;
            int day = hiWord & 0x1F;
            int hour = (loWord & 0xF800) >> 11;
            int minute = (loWord & 0x07E0) >> 5;
            int second = (loWord & 0x1F) << 1;

            return new DateTime(year, month, day, hour, minute, second);
        }

        private void ProcessFileError(RarError result)
        {
            switch (result)
            {
                case RarError.UnknownFormat:
                    throw new OutOfMemoryException("Unknown archive format.");

                case RarError.BadData:
                    throw new IOException("File CRC Error");

                case RarError.BadArchive:
                    throw new IOException("File is not a valid archive.");

                case RarError.OpenError:
                    throw new IOException("File could not be opened.");

                case RarError.CreateError:
                    throw new IOException("File could not be created.");

                case RarError.CloseError:
                    throw new IOException("File close error.");

                case RarError.ReadError:
                    throw new IOException("File read error.");

                case RarError.WriteError:
                    throw new IOException("File write error.");
            }
        }

        private int RARCallback(uint msg, int UserData, IntPtr p1, int p2)
        {
            int result = -1;

            switch ((CallbackMessages)msg)
            {
                case CallbackMessages.ProcessData:
                    result = OnDataAvailable(p1, p2);
                    break;

                case CallbackMessages.NeedPassword:
                    result = OnPasswordRequired(p1, p2);
                    break;

                default:
                    string volume = Marshal.PtrToStringAnsi(p1);
                    if ((VolumeMessage)p2 == VolumeMessage.Notify)
                        result = OnNewVolume(volume);
                    else if ((VolumeMessage)p2 == VolumeMessage.Ask)
                    {
                        string newVolume = OnMissingVolume(volume);
                        if (newVolume.Length == 0)
                            result = -1;
                        else
                        {
                            if (newVolume != volume)
                            {
                                for (int i = 0; i < newVolume.Length; i++)
                                {
                                    Marshal.WriteByte(p1, i, (byte)newVolume[i]);
                                }
                                Marshal.WriteByte(p1, newVolume.Length, 0);
                            }
                            result = 1;
                        }
                    }
                    break;
            }
            return result;
        }

        #endregion
    }
}
