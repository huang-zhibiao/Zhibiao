using System.Collections.Generic;

namespace Zhibiao.FastDFS.Client
{
    public class FastDFSOptions
    {
        public FastDFSOptions()
        {
            Trackers = new List<Tracker>();
        }

        public bool IsEnabled { get; set; }
        public int ConnectTimeout { get; set; } = 5;
        public int NetworkTimeout { get; set; } = 30;
        public string Charset { get; set; } = "ISO8859-1";
        public int TrackerHttpPort { get; set; } = 80;
        public bool AntiStealToken { get; set; }
        public string SecretKey { get; set; }
        public string WebUrl { get; set; }

        public List<Tracker> Trackers { get; set; }

        public class Tracker
        {
            public string IP { get; set; }
            public int Port { get; set; }
        }
    }
}
