using System.Text.RegularExpressions;
using System.IO;
using System;

using Unknown6656.Common;

using Renci.SshNet;

namespace Unknown6656.IO;

public static class DatastreamExtensions
{
    private static readonly Regex SSH_PROTOCOL_REGEX = new(@"^(sftp|ssh|s?scp):\/\/(?<uname>[^:]+)(:(?<passw>[^@]+))?@(?<host>[^:\/]+|\[[0-9a-f\:]+\])(:(?<port>[0-9]{1,6}))?(\/|\\)(?<path>.+)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);


    public static DataStream FromSSH(string uri)
    {
        if (uri.Match(SSH_PROTOCOL_REGEX, out ReadOnlyIndexer<string, string>? g))
        {
            string host = g["host"];
            string uname = g["uname"];
            string passw = g["passw"];
            string rpath = '/' + g["path"];

            if (!int.TryParse(g["port"], out int port))
                port = 22;

            using (SftpClient sftp = new(host, port, uname, passw))
            using (MemoryStream ms = new())
            {
                sftp.Connect();
                sftp.DownloadFile(rpath, ms);
                sftp.Disconnect();

                return DataStream.FromStream(ms);
            }
        }
        else
            throw new ArgumentException($"Invalid SSH URI: The URI should have the format '<protocol>://<user>:<password>@<host>:<port>/<path>'.", nameof(uri));
    }

}