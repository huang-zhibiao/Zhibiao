﻿using System;
using System.IO;
using System.Net.Sockets;
using System.Text;

namespace Zhibiao.FastDFS.Client
{
    /// <summary>
    /// author zhouyh
    /// version 1.0
    /// </summary>
    public class TrackerClient
    {
        protected TrackerGroup tracker_group;
        protected byte errno;

        public TrackerClient()
        {
            this.tracker_group = ClientGlobal.g_tracker_group;
        }

        public TrackerClient(TrackerGroup tracker_group)
        {
            this.tracker_group = tracker_group;
        }

        public byte ErrorCode
        {
            get
            {
                return this.errno;
            }
        }
        /// <summary>
        /// get a connection to tracker server
        /// </summary>
        /// <returns>tracker server Socket object, return null if fail</returns>
        public TrackerServer getConnection()
        {
            return this.tracker_group.getConnection();
        }
        /// <summary>
        /// query storage server to upload file
        /// </summary>
        /// <param name="trackerServer">the tracker server</param>
        /// <returns>storage server Socket object, return null if fail</returns>
        public StorageServer getStoreStorage(TrackerServer trackerServer)
        {
            String groupName = null;
            return this.getStoreStorage(trackerServer, groupName);
        }
        /// <summary>
        /// query storage server to upload file
        /// </summary>
        /// <param name="trackerServer">the tracker server</param>
        /// <param name="groupName">the group name to upload file to, can be empty</param>
        /// <returns>storage server object, return null if fail</returns>
        public StorageServer getStoreStorage(TrackerServer trackerServer, string groupName)
        {
            byte[] header;
            string ip_addr;
            int port;
            byte cmd;
            int out_len;
            bool bNewConnection;
            byte store_path;
            TcpClient trackerSocket;

            if (trackerServer == null)
            {
                trackerServer = getConnection();
                if (trackerServer == null)
                {
                    return null;
                }
                bNewConnection = true;
            }
            else
            {
                bNewConnection = false;
            }

            trackerSocket = trackerServer.getSocket();
            Stream output = trackerSocket.GetStream();

            try
            {
                if (groupName == null || groupName.Length == 0)
                {
                    cmd = ProtoCommon.TRACKER_PROTO_CMD_SERVICE_QUERY_STORE_WITHOUT_GROUP_ONE;
                    out_len = 0;
                }
                else
                {
                    cmd = ProtoCommon.TRACKER_PROTO_CMD_SERVICE_QUERY_STORE_WITH_GROUP_ONE;
                    out_len = ProtoCommon.FDFS_GROUP_NAME_MAX_LEN;
                }
                header = ProtoCommon.packHeader(cmd, out_len, (byte)0);
                output.Write(header, 0, header.Length);

                if (groupName != null && groupName.Length > 0)
                {
                    byte[] bGroupName;
                    byte[] bs;
                    int group_len;

                    bs = Encoding.GetEncoding(ClientGlobal.g_charset).GetBytes(groupName);
                    bGroupName = new byte[ProtoCommon.FDFS_GROUP_NAME_MAX_LEN];

                    if (bs.Length <= ProtoCommon.FDFS_GROUP_NAME_MAX_LEN)
                    {
                        group_len = bs.Length;
                    }
                    else
                    {
                        group_len = ProtoCommon.FDFS_GROUP_NAME_MAX_LEN;
                    }
                    for (int i = 0; i < bGroupName.Length; i++)
                        bGroupName[i] = (byte)0;
                    Array.Copy(bs, 0, bGroupName, 0, group_len);
                    output.Write(bGroupName, 0, bGroupName.Length);
                }

                ProtoCommon.RecvPackageInfo pkgInfo = ProtoCommon.recvPackage(trackerSocket.GetStream(),
                                             ProtoCommon.TRACKER_PROTO_CMD_RESP,
                                             ProtoCommon.TRACKER_QUERY_STORAGE_STORE_BODY_LEN);
                this.errno = pkgInfo.errno;
                if (pkgInfo.errno != 0)
                {
                    return null;
                }

                ip_addr = Encoding.GetEncoding(ClientGlobal.g_charset).GetString(pkgInfo.body, ProtoCommon.FDFS_GROUP_NAME_MAX_LEN, ProtoCommon.FDFS_IPADDR_SIZE - 1).Replace("\0", "").Trim();

                port = (int)ProtoCommon.buff2long(pkgInfo.body, ProtoCommon.FDFS_GROUP_NAME_MAX_LEN
                                + ProtoCommon.FDFS_IPADDR_SIZE - 1);
                store_path = pkgInfo.body[ProtoCommon.TRACKER_QUERY_STORAGE_STORE_BODY_LEN - 1];

                return new StorageServer(ip_addr, port, store_path);
            }
            catch (IOException ex)
            {
                if (!bNewConnection)
                {
                    try
                    {
                        trackerServer.close();
                    }
                    catch (IOException)
                    {

                    }
                }

                throw ex;
            }
            finally
            {
                if (bNewConnection)
                {
                    try
                    {
                        trackerServer.close();
                    }
                    catch (IOException ex1)
                    {
 
                    }
                }
            }
        }
        /// <summary>
        /// query storage servers to upload file
        /// </summary>
        /// <param name="trackerServer">the tracker server</param>
        /// <param name="groupName">the group name to upload file to, can be empty</param>
        /// <returns>storage servers, return null if fail</returns>
        public StorageServer[] getStoreStorages(TrackerServer trackerServer, string groupName)
        {
            byte[] header;
            string ip_addr;
            int port;
            byte cmd;
            int out_len;
            bool bNewConnection;
            TcpClient trackerSocket;

            if (trackerServer == null)
            {
                trackerServer = getConnection();
                if (trackerServer == null)
                {
                    return null;
                }
                bNewConnection = true;
            }
            else
            {
                bNewConnection = false;
            }

            trackerSocket = trackerServer.getSocket();
            Stream output = trackerSocket.GetStream();

            try
            {
                if (groupName == null || groupName.Length == 0)
                {
                    cmd = ProtoCommon.TRACKER_PROTO_CMD_SERVICE_QUERY_STORE_WITHOUT_GROUP_ALL;
                    out_len = 0;
                }
                else
                {
                    cmd = ProtoCommon.TRACKER_PROTO_CMD_SERVICE_QUERY_STORE_WITH_GROUP_ALL;
                    out_len = ProtoCommon.FDFS_GROUP_NAME_MAX_LEN;
                }
                header = ProtoCommon.packHeader(cmd, out_len, (byte)0);
                output.Write(header, 0, header.Length);

                if (groupName != null && groupName.Length > 0)
                {
                    byte[] bGroupName;
                    byte[] bs;
                    int group_len;

                    bs = Encoding.GetEncoding(ClientGlobal.g_charset).GetBytes(groupName);
                    bGroupName = new byte[ProtoCommon.FDFS_GROUP_NAME_MAX_LEN];

                    if (bs.Length <= ProtoCommon.FDFS_GROUP_NAME_MAX_LEN)
                    {
                        group_len = bs.Length;
                    }
                    else
                    {
                        group_len = ProtoCommon.FDFS_GROUP_NAME_MAX_LEN;
                    }
                    for (int i = 0; i < bGroupName.Length; i++)
                        bGroupName[i] = (byte)0;
                    Array.Copy(bs, 0, bGroupName, 0, group_len);
                    output.Write(bGroupName, 0, bGroupName.Length);
                }

                ProtoCommon.RecvPackageInfo pkgInfo = ProtoCommon.recvPackage(trackerSocket.GetStream(),
                                             ProtoCommon.TRACKER_PROTO_CMD_RESP, -1);
                this.errno = pkgInfo.errno;
                if (pkgInfo.errno != 0)
                {
                    return null;
                }

                if (pkgInfo.body.Length < ProtoCommon.TRACKER_QUERY_STORAGE_STORE_BODY_LEN)
                {
                    this.errno = ProtoCommon.ERR_NO_EINVAL;
                    return null;
                }

                int ipPortLen = pkgInfo.body.Length - (ProtoCommon.FDFS_GROUP_NAME_MAX_LEN + 1);
                int recordLength = ProtoCommon.FDFS_IPADDR_SIZE - 1 + ProtoCommon.FDFS_PROTO_PKG_LEN_SIZE;

                if (ipPortLen % recordLength != 0)
                {
                    this.errno = ProtoCommon.ERR_NO_EINVAL;
                    return null;
                }

                int serverCount = ipPortLen / recordLength;
                if (serverCount > 16)
                {
                    this.errno = ProtoCommon.ERR_NO_ENOSPC;
                    return null;
                }

                StorageServer[] results = new StorageServer[serverCount];
                byte store_path = pkgInfo.body[pkgInfo.body.Length - 1];
                int offset = ProtoCommon.FDFS_GROUP_NAME_MAX_LEN;

                for (int i = 0; i < serverCount; i++)
                {
                    ip_addr = Encoding.GetEncoding(ClientGlobal.g_charset).GetString(pkgInfo.body, offset, ProtoCommon.FDFS_IPADDR_SIZE - 1).Replace("\0", "").Replace("\0", "").Trim();
                    offset += ProtoCommon.FDFS_IPADDR_SIZE - 1;

                    port = (int)ProtoCommon.buff2long(pkgInfo.body, offset);
                    offset += ProtoCommon.FDFS_PROTO_PKG_LEN_SIZE;

                    results[i] = new StorageServer(ip_addr, port, store_path);
                }

                return results;
            }
            catch (IOException ex)
            {
                if (!bNewConnection)
                {
                    try
                    {
                        trackerServer.close();
                    }
                    catch (IOException ex1)
                    {

                    }
                }

                throw ex;
            }
            finally
            {
                if (bNewConnection)
                {
                    try
                    {
                        trackerServer.close();
                    }
                    catch (IOException ex1)
                    {

                    }
                }
            }
        }
        /// <summary>
        /// query storage server to download file
        /// </summary>
        /// <param name="trackerServer">the tracker server</param>
        /// <param name="groupName">the group name of storage server</param>
        /// <param name="filename">filename on storage server</param>
        /// <returns>storage server Socket object, return null if fail</returns>
        public StorageServer getFetchStorage(TrackerServer trackerServer, string groupName, string filename)
        {
            ServerInfo[] servers = this.getStorages(trackerServer, ProtoCommon.TRACKER_PROTO_CMD_SERVICE_QUERY_FETCH_ONE, groupName, filename);
            if (servers == null)
            {
                return null;
            }
            else
            {
                return new StorageServer(servers[0].Ip_Addr, servers[0].Port, 0);
            }
        }
        /// <summary>
        /// query storage server to update file (delete file or set meta data)
        /// </summary>
        /// <param name="trackerServer">the tracker server</param>
        /// <param name="groupName">the group name of storage server</param>
        /// <param name="filename">filename on storage server</param>
        /// <returns>storage server Socket object, return null if fail</returns>
        public StorageServer getUpdateStorage(TrackerServer trackerServer, string groupName, string filename)
        {
            ServerInfo[] servers = this.getStorages(trackerServer, ProtoCommon.TRACKER_PROTO_CMD_SERVICE_QUERY_UPDATE,
                    groupName, filename);
            if (servers == null)
            {
                return null;
            }
            else
            {
                return new StorageServer(servers[0].Ip_Addr, servers[0].Port, 0);
            }
        }
        /// <summary>
        /// get storage servers to download file
        /// </summary>
        /// <param name="trackerServer">the tracker server</param>
        /// <param name="groupName">the group name of storage server</param>
        /// <param name="filename">filename on storage server</param>
        /// <returns>storage servers, return null if fail</returns>
        public ServerInfo[] getFetchStorages(TrackerServer trackerServer, string groupName, string filename)
        {
            return this.getStorages(trackerServer, ProtoCommon.TRACKER_PROTO_CMD_SERVICE_QUERY_FETCH_ALL,
                    groupName, filename);
        }
        /// <summary>
        /// query storage server to download file
        /// </summary>
        /// <param name="trackerServer">the tracker server</param>
        /// <param name="cmd">command code, ProtoCommon.TRACKER_PROTO_CMD_SERVICE_QUERY_FETCH_ONE or ProtoCommon.TRACKER_PROTO_CMD_SERVICE_QUERY_UPDATE</param>
        /// <param name="groupName">the group name of storage server</param>
        /// <param name="filename">filename on storage server</param>
        /// <returns>storage server Socket object, return null if fail</returns>
        protected ServerInfo[] getStorages(TrackerServer trackerServer, byte cmd, string groupName, string filename)
        {
            byte[] header;
            byte[] bFileName;
            byte[] bGroupName;
            byte[] bs;
            int len;
            string ip_addr;
            int port;
            bool bNewConnection;
            TcpClient trackerSocket;

            if (trackerServer == null)
            {
                trackerServer = getConnection();
                if (trackerServer == null)
                {
                    return null;
                }
                bNewConnection = true;
            }
            else
            {
                bNewConnection = false;
            }
            trackerSocket = trackerServer.getSocket();
            Stream output = trackerSocket.GetStream();

            try
            {
                bs = Encoding.GetEncoding(ClientGlobal.g_charset).GetBytes(groupName);
                bGroupName = new byte[ProtoCommon.FDFS_GROUP_NAME_MAX_LEN];
                bFileName = Encoding.GetEncoding(ClientGlobal.g_charset).GetBytes(filename);

                if (bs.Length <= ProtoCommon.FDFS_GROUP_NAME_MAX_LEN)
                {
                    len = bs.Length;
                }
                else
                {
                    len = ProtoCommon.FDFS_GROUP_NAME_MAX_LEN;
                }
                for (int i = 0; i < bGroupName.Length; i++)
                    bGroupName[i] = (byte)0;
                Array.Copy(bs, 0, bGroupName, 0, len);

                header = ProtoCommon.packHeader(cmd, ProtoCommon.FDFS_GROUP_NAME_MAX_LEN + bFileName.Length, (byte)0);
                byte[] wholePkg = new byte[header.Length + bGroupName.Length + bFileName.Length];
                Array.Copy(header, 0, wholePkg, 0, header.Length);
                Array.Copy(bGroupName, 0, wholePkg, header.Length, bGroupName.Length);
                Array.Copy(bFileName, 0, wholePkg, header.Length + bGroupName.Length, bFileName.Length);
                output.Write(wholePkg, 0, wholePkg.Length);

                ProtoCommon.RecvPackageInfo pkgInfo = ProtoCommon.recvPackage(trackerSocket.GetStream(),
                                             ProtoCommon.TRACKER_PROTO_CMD_RESP, -1);
                this.errno = pkgInfo.errno;
                if (pkgInfo.errno != 0)
                {
                    return null;
                }

                if (pkgInfo.body.Length < ProtoCommon.TRACKER_QUERY_STORAGE_FETCH_BODY_LEN)
                {
                    throw new IOException("Invalid body length: " + pkgInfo.body.Length);
                }

                if ((pkgInfo.body.Length - ProtoCommon.TRACKER_QUERY_STORAGE_FETCH_BODY_LEN) % (ProtoCommon.FDFS_IPADDR_SIZE - 1) != 0)
                {
                    throw new IOException("Invalid body length: " + pkgInfo.body.Length);
                }

                int server_count = 1 + (pkgInfo.body.Length - ProtoCommon.TRACKER_QUERY_STORAGE_FETCH_BODY_LEN) / (ProtoCommon.FDFS_IPADDR_SIZE - 1);

                ip_addr = Encoding.GetEncoding(ClientGlobal.g_charset).GetString(pkgInfo.body, ProtoCommon.FDFS_GROUP_NAME_MAX_LEN, ProtoCommon.FDFS_IPADDR_SIZE - 1).Replace("\0", "").Trim();
                int offset = ProtoCommon.FDFS_GROUP_NAME_MAX_LEN + ProtoCommon.FDFS_IPADDR_SIZE - 1;

                port = (int)ProtoCommon.buff2long(pkgInfo.body, offset);
                offset += ProtoCommon.FDFS_PROTO_PKG_LEN_SIZE;

                ServerInfo[] servers = new ServerInfo[server_count];
                servers[0] = new ServerInfo(ip_addr, port);
                for (int i = 1; i < server_count; i++)
                {
                    servers[i] = new ServerInfo(Encoding.GetEncoding(ClientGlobal.g_charset).GetString(pkgInfo.body, offset, ProtoCommon.FDFS_IPADDR_SIZE - 1).Replace("\0", "").Trim(), port);
                    offset += ProtoCommon.FDFS_IPADDR_SIZE - 1;
                }

                return servers;
            }
            catch (IOException ex)
            {
                if (!bNewConnection)
                {
                    try
                    {
                        trackerServer.close();
                    }
                    catch (IOException ex1)
                    {
        
                    }
                }

                throw ex;
            }
            finally
            {
                if (bNewConnection)
                {
                    try
                    {
                        trackerServer.close();
                    }
                    catch (IOException ex1)
                    {
                
                    }
                }
            }
        }
        /// <summary>
        /// query storage server to download file
        /// </summary>
        /// <param name="trackerServer">the tracker server</param>
        /// <param name="file_id">the file id(including group name and filename)</param>
        /// <returns>storage server Socket object, return null if fail</returns>
        public StorageServer getFetchStorage1(TrackerServer trackerServer, string file_id)
        {
            string[] parts = new string[2];
            this.errno = StorageClientEx.split_file_id(file_id, parts);
            if (this.errno != 0)
            {
                return null;
            }

            return this.getFetchStorage(trackerServer, parts[0], parts[1]);
        }
        /// <summary>
        /// get storage servers to download file
        /// </summary>
        /// <param name="trackerServer">the tracker server</param>
        /// <param name="file_id">the file id(including group name and filename)</param>
        /// <returns>storage servers, return null if fail</returns>
        public ServerInfo[] getFetchStorages1(TrackerServer trackerServer, string file_id)
        {
            string[] parts = new string[2];
            this.errno = StorageClientEx.split_file_id(file_id, parts);
            if (this.errno != 0)
            {
                return null;
            }

            return this.getFetchStorages(trackerServer, parts[0], parts[1]);
        }
        /// <summary>
        /// list groups
        /// </summary>
        /// <param name="trackerServer">the tracker server</param>
        /// <returns>group stat array, return null if fail</returns>
        public StructGroupStat[] listGroups(TrackerServer trackerServer)
        {
            byte[] header;
            string ip_addr;
            int port;
            byte cmd;
            int out_len;
            bool bNewConnection;
            byte store_path;
            TcpClient trackerSocket;

            if (trackerServer == null)
            {
                trackerServer = getConnection();
                if (trackerServer == null)
                {
                    return null;
                }
                bNewConnection = true;
            }
            else
            {
                bNewConnection = false;
            }

            trackerSocket = trackerServer.getSocket();
            Stream output = trackerSocket.GetStream();

            try
            {
                header = ProtoCommon.packHeader(ProtoCommon.TRACKER_PROTO_CMD_SERVER_LIST_GROUP, 0, (byte)0);
                output.Write(header, 0, header.Length);

                ProtoCommon.RecvPackageInfo pkgInfo = ProtoCommon.recvPackage(trackerSocket.GetStream(),
                                             ProtoCommon.TRACKER_PROTO_CMD_RESP, -1);
                this.errno = pkgInfo.errno;
                if (pkgInfo.errno != 0)
                {
                    return null;
                }

                ProtoStructDecoder decoder = new ProtoStructDecoder();
                return decoder.decode<StructGroupStat>(pkgInfo.body, StructGroupStat.getFieldsTotalSize());
            }
            catch (IOException ex)
            {
                if (!bNewConnection)
                {
                    try
                    {
                        trackerServer.close();
                    }
                    catch (IOException ex1)
                    {
        
                    }
                }

                throw ex;
            }
            catch (Exception ex)
            {
                this.errno = ProtoCommon.ERR_NO_EINVAL;
                return null;
            }
            finally
            {
                if (bNewConnection)
                {
                    try
                    {
                        trackerServer.close();
                    }
                    catch (IOException ex1)
                    {
                
                    }
                }
            }
        }
        /// <summary>
        /// query storage server stat info of the group
        /// </summary>
        /// <param name="trackerServer">the tracker server</param>
        /// <param name="groupName">the group name of storage server</param>
        /// <returns>storage server stat array, return null if fail</returns>
        public StructStorageStat[] listStorages(TrackerServer trackerServer, string groupName)
        {
            string storageIpAddr = null;
            return this.listStorages(trackerServer, groupName, storageIpAddr);
        }
        /// <summary>
        /// query storage server stat info of the group
        /// </summary>
        /// <param name="trackerServer">the tracker server</param>
        /// <param name="groupName">the group name of storage server</param>
        /// <param name="storageIpAddr">the storage server ip address, can be null or empty</param>
        /// <returns>storage server stat array, return null if fail</returns>
        public StructStorageStat[] listStorages(TrackerServer trackerServer, string groupName, string storageIpAddr)
        {
            byte[] header;
            byte[] bGroupName;
            byte[] bs;
            int len;
            bool bNewConnection;
            TcpClient trackerSocket;

            if (trackerServer == null)
            {
                trackerServer = getConnection();
                if (trackerServer == null)
                {
                    return null;
                }
                bNewConnection = true;
            }
            else
            {
                bNewConnection = false;
            }
            trackerSocket = trackerServer.getSocket();
            Stream output = trackerSocket.GetStream();

            try
            {
                bs = Encoding.GetEncoding(ClientGlobal.g_charset).GetBytes(groupName);
                bGroupName = new byte[ProtoCommon.FDFS_GROUP_NAME_MAX_LEN];

                if (bs.Length <= ProtoCommon.FDFS_GROUP_NAME_MAX_LEN)
                {
                    len = bs.Length;
                }
                else
                {
                    len = ProtoCommon.FDFS_GROUP_NAME_MAX_LEN;
                }
                for (int i = 0; i < bGroupName.Length; i++)
                    bGroupName[i] = (byte)0;
                Array.Copy(bs, 0, bGroupName, 0, len);

                int ipAddrLen;
                byte[] bIpAddr;
                if (storageIpAddr != null && storageIpAddr.Length > 0)
                {
                    bIpAddr = Encoding.GetEncoding(ClientGlobal.g_charset).GetBytes(storageIpAddr);
                    if (bIpAddr.Length < ProtoCommon.FDFS_IPADDR_SIZE)
                    {
                        ipAddrLen = bIpAddr.Length;
                    }
                    else
                    {
                        ipAddrLen = ProtoCommon.FDFS_IPADDR_SIZE - 1;
                    }
                }
                else
                {
                    bIpAddr = null;
                    ipAddrLen = 0;
                }

                header = ProtoCommon.packHeader(ProtoCommon.TRACKER_PROTO_CMD_SERVER_LIST_STORAGE, ProtoCommon.FDFS_GROUP_NAME_MAX_LEN + ipAddrLen, (byte)0);
                byte[] wholePkg = new byte[header.Length + bGroupName.Length + ipAddrLen];
                Array.Copy(header, 0, wholePkg, 0, header.Length);
                Array.Copy(bGroupName, 0, wholePkg, header.Length, bGroupName.Length);
                if (ipAddrLen > 0)
                {
                    Array.Copy(bIpAddr, 0, wholePkg, header.Length + bGroupName.Length, ipAddrLen);
                }
                output.Write(wholePkg, 0, wholePkg.Length);

                ProtoCommon.RecvPackageInfo pkgInfo = ProtoCommon.recvPackage(trackerSocket.GetStream(),
                                             ProtoCommon.TRACKER_PROTO_CMD_RESP, -1);
                this.errno = pkgInfo.errno;
                if (pkgInfo.errno != 0)
                {
                    return null;
                }

                ProtoStructDecoder decoder = new ProtoStructDecoder();
                return decoder.decode<StructStorageStat>(pkgInfo.body, StructStorageStat.getFieldsTotalSize());
            }
            catch (IOException ex)
            {
                if (!bNewConnection)
                {
                    try
                    {
                        trackerServer.close();
                    }
                    catch (IOException ex1)
                    {

                    }
                }

                throw ex;
            }
            catch (Exception ex)
            {
                this.errno = ProtoCommon.ERR_NO_EINVAL;
                return null;
            }
            finally
            {
                if (bNewConnection)
                {
                    try
                    {
                        trackerServer.close();
                    }
                    catch (IOException ex1)
                    {

                    }
                }
            }
        }
        /// <summary>
        /// delete a storage server from the tracker server
        /// </summary>
        /// <param name="trackerServer">the connected tracker server</param>
        /// <param name="groupName">the group name of storage server</param>
        /// <param name="storageIpAddr">the storage server ip address</param>
        /// <returns>true for success, false for fail</returns>
        private bool deleteStorage(TrackerServer trackerServer, string groupName, string storageIpAddr)
        {
            byte[] header;
            byte[] bGroupName;
            byte[] bs;
            int len;
            TcpClient trackerSocket;

            trackerSocket = trackerServer.getSocket();
            Stream output = trackerSocket.GetStream();

            bs = Encoding.GetEncoding(ClientGlobal.g_charset).GetBytes(groupName);
            bGroupName = new byte[ProtoCommon.FDFS_GROUP_NAME_MAX_LEN];

            if (bs.Length <= ProtoCommon.FDFS_GROUP_NAME_MAX_LEN)
            {
                len = bs.Length;
            }
            else
            {
                len = ProtoCommon.FDFS_GROUP_NAME_MAX_LEN;
            }
            for (int i = 0; i < bGroupName.Length; i++)
                bGroupName[i] = (byte)0;
            Array.Copy(bs, 0, bGroupName, 0, len);

            int ipAddrLen;
            byte[] bIpAddr = Encoding.GetEncoding(ClientGlobal.g_charset).GetBytes(storageIpAddr);
            if (bIpAddr.Length < ProtoCommon.FDFS_IPADDR_SIZE)
            {
                ipAddrLen = bIpAddr.Length;
            }
            else
            {
                ipAddrLen = ProtoCommon.FDFS_IPADDR_SIZE - 1;
            }

            header = ProtoCommon.packHeader(ProtoCommon.TRACKER_PROTO_CMD_SERVER_DELETE_STORAGE, ProtoCommon.FDFS_GROUP_NAME_MAX_LEN + ipAddrLen, (byte)0);
            byte[] wholePkg = new byte[header.Length + bGroupName.Length + ipAddrLen];
            Array.Copy(header, 0, wholePkg, 0, header.Length);
            Array.Copy(bGroupName, 0, wholePkg, header.Length, bGroupName.Length);
            Array.Copy(bIpAddr, 0, wholePkg, header.Length + bGroupName.Length, ipAddrLen);
            output.Write(wholePkg, 0, wholePkg.Length);

            ProtoCommon.RecvPackageInfo pkgInfo = ProtoCommon.recvPackage(trackerSocket.GetStream(),
                                         ProtoCommon.TRACKER_PROTO_CMD_RESP, 0);
            this.errno = pkgInfo.errno;
            return pkgInfo.errno == 0;
        }
        /// <summary>
        /// delete a storage server from the global FastDFS cluster
        /// </summary>
        /// <param name="groupName">the group name of storage server</param>
        /// <param name="storageIpAddr">the storage server ip address</param>
        /// <returns>true for success, false for fail</returns>
        public bool deleteStorage(String groupName, string storageIpAddr)
        {
            return this.deleteStorage(ClientGlobal.g_tracker_group, groupName, storageIpAddr);
        }
        /// <summary>
        /// delete a storage server from the FastDFS cluster
        /// </summary>
        /// <param name="trackerGroup">the tracker server group</param>
        /// <param name="groupName">the group name of storage server</param>
        /// <param name="storageIpAddr">the storage server ip address</param>
        /// <returns>true for success, false for fail</returns>
        public bool deleteStorage(TrackerGroup trackerGroup, string groupName, string storageIpAddr)
        {
            int serverIndex;
            int notFoundCount;
            TrackerServer trackerServer;

            notFoundCount = 0;
            for (serverIndex = 0; serverIndex < trackerGroup.tracker_servers.Length; serverIndex++)
            {
                try
                {
                    trackerServer = trackerGroup.getConnection(serverIndex);
                }
                catch (IOException ex)
                {
                    this.errno = ProtoCommon.ECONNREFUSED;
                    return false;
                }

                try
                {
                    StructStorageStat[] storageStats = listStorages(trackerServer, groupName, storageIpAddr);
                    if (storageStats == null)
                    {
                        if (this.errno == ProtoCommon.ERR_NO_ENOENT)
                        {
                            notFoundCount++;
                        }
                        else
                        {
                            return false;
                        }
                    }
                    else if (storageStats.Length == 0)
                    {
                        notFoundCount++;
                    }
                    else if (storageStats[0].Status == ProtoCommon.FDFS_STORAGE_STATUS_ONLINE ||
                             storageStats[0].Status == ProtoCommon.FDFS_STORAGE_STATUS_ACTIVE)
                    {
                        this.errno = ProtoCommon.ERR_NO_EBUSY;
                        return false;
                    }
                }
                finally
                {
                    try
                    {
                        trackerServer.close();
                    }
                    catch (IOException ex1)
                    {
        
                    }
                }
            }

            if (notFoundCount == trackerGroup.tracker_servers.Length)
            {
                this.errno = ProtoCommon.ERR_NO_ENOENT;
                return false;
            }

            notFoundCount = 0;
            for (serverIndex = 0; serverIndex < trackerGroup.tracker_servers.Length; serverIndex++)
            {
                try
                {
                    trackerServer = trackerGroup.getConnection(serverIndex);
                }
                catch (IOException ex)
                {
                    Console.WriteLine("connect to server " + trackerGroup.tracker_servers[serverIndex].Address + ":" + trackerGroup.tracker_servers[serverIndex].Port + " fail");
                    this.errno = ProtoCommon.ECONNREFUSED;
                    return false;
                }

                try
                {
                    if (!this.deleteStorage(trackerServer, groupName, storageIpAddr))
                    {
                        if (this.errno != 0)
                        {
                            if (this.errno == ProtoCommon.ERR_NO_ENOENT)
                            {
                                notFoundCount++;
                            }
                            else if (this.errno != ProtoCommon.ERR_NO_EALREADY)
                            {
                                return false;
                            }
                        }
                    }
                }
                finally
                {
                    try
                    {
                        trackerServer.close();
                    }
                    catch (IOException ex1)
                    {
        
                    }
                }
            }

            if (notFoundCount == trackerGroup.tracker_servers.Length)
            {
                this.errno = ProtoCommon.ERR_NO_ENOENT;
                return false;
            }

            if (this.errno == ProtoCommon.ERR_NO_ENOENT)
            {
                this.errno = 0;
            }

            return this.errno == 0;
        }
    }
}
