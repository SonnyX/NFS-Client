namespace Test.Client
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Net;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using GetSomeInput;
    using NFSLibrary;
    using NFSLibrary.Protocols.Commons;

    public static class Program
    {
        /*
         * See https://www.dummies.com/article/technology/computers/operating-systems/linux/how-to-share-files-with-nfs-on-linux-systems-255851/
         * https://github.com/SonnyX/NFS-Client
         * https://github.com/nekoni/nekodrive
         * https://code.google.com/archive/p/nekodrive/wikis/UseNFSDotNetLibrary.wiki
         * https://ubuntu.com/server/docs/network-file-system-nfs
         * https://www.hanewin.net/nfs-e.htm
         * https://serverfault.com/questions/240897/how-to-properly-set-permissions-for-nfs-folder-permission-denied-on-mounting-en
         * 
         */

        private static bool _RunForever = true;
        private static string _Hostname = "127.0.0.1";
        private static NfsClient.NfsVersion _Version = NfsClient.NfsVersion.V3;
        private static string _Share = "/c/users/joelc/downloads/test";

        public static void Main(string[] args)
        {
            while (_RunForever)
            {
                string userInput = Inputty.GetString("Command [?/help]:", null, false);

                switch (userInput)
                {
                    case "?":
                        Menu();
                        break;
                    case "q":
                        _RunForever = false;
                        break;
                    case "cls":
                        Console.Clear();
                        break;

                    case "host":
                        _Hostname = Inputty.GetString("Host IP:", _Hostname, false);
                        break;
                    case "share":
                        _Share = Inputty.GetString("Share:", _Share, false);
                        break;

                    case "shares":
                        ListShares().Wait();
                        break;
                    case "enum":
                        ListFiles().Wait();
                        break;
                    case "walk":
                        WalkDirectory().Wait();
                        break;
                    case "read":
                        ReadFile().Wait();
                        break;
                    case "write":
                        WriteFile().Wait();
                        break;
                    case "delete":
                        DeleteFile().Wait();
                        break;
                }
            }
        }

        private static void Menu()
        {
            Console.WriteLine("");
            Console.WriteLine("Available commands:");
            Console.WriteLine("  ?           Help, this menu");
            Console.WriteLine("  q           Quit");
            Console.WriteLine("  cls         Clear screen");
            Console.WriteLine("  host        Set host IP");
            Console.WriteLine("  share       Set share name");
            Console.WriteLine("");
            Console.WriteLine("  shares      List shares");
            Console.WriteLine("  enum        Enumerate a share");
            Console.WriteLine("  walk        Walk the directory tree");
            Console.WriteLine("  read        Read a file");
            Console.WriteLine("  write       Write a file");
            Console.WriteLine("  delete      Delete a file");
            Console.WriteLine("");
        }

        private static async Task ListShares()
        {
            NfsClient nfs = new NfsClient(_Version);
            nfs.Connect(IPAddress.Parse(_Hostname));
            List<string> exports = nfs.GetExportedDevices();
            if (exports != null && exports.Count > 0)
            {
                foreach (var share in exports)
                {
                    Console.WriteLine(share);
                }
            }
            nfs.Disconnect();
        }

        private static async Task ListFiles()
        {
            string baseDir = Inputty.GetString("Base directory:", null, true);

            NfsClient nfs = new NfsClient(_Version);
            nfs.Connect(IPAddress.Parse(_Hostname));
            nfs.MountDevice(_Share);

            if (String.IsNullOrEmpty(baseDir))
            {
                foreach (string item in nfs.GetItemList("."))
                {
                    NFSAttributes attrib = nfs.GetItemAttributes(item);
                    if (attrib == null)
                    {
                        Console.WriteLine("Attributes not found for " + item);
                        continue;
                    }
                    Console.WriteLine(item + " " + attrib.CreateDateTime.ToString() + " " + attrib.Size);
                }
            }
            else
            {
                foreach (string item in nfs.GetItemList(baseDir))
                {
                    string path = baseDir;
                    if (!path.StartsWith("/")) path += "/";
                    if (!path.EndsWith("/")) path += "/";

                    NFSAttributes attrib = nfs.GetItemAttributes(path + item);
                    if (attrib == null)
                    {
                        Console.WriteLine("Attributes not found for " + item);
                        continue;
                    }
                    Console.WriteLine(item + " " + attrib.CreateDateTime.ToString() + " " + attrib.Size);
                }
            }

            nfs.UnMountDevice();
            nfs.Disconnect();
        }

        private static async Task WalkDirectory()
        {
            NfsClient nfs = new NfsClient(_Version);
            nfs.Connect(IPAddress.Parse(_Hostname));
            nfs.MountDevice(_Share);

            await WalkDirectoryInternal(nfs, ".", "", 0);

            nfs.UnMountDevice();
            nfs.Disconnect();
        }

        private static async Task WalkDirectoryInternal(NfsClient client, string item, string path, int spacing)
        {
            string spaces = "";
            for (int i = 0; i < spacing; i++) spaces += " ";
            Console.WriteLine(spaces + "| Walking directory " + item);

            List<string> items = client.GetItemList(item, true);
            Console.WriteLine(spaces + "  | Read " + items.Count + " items");

            foreach (string curr in items)
            {
                NFSAttributes attrib = client.GetItemAttributes(path + curr);
                if (attrib == null)
                {
                    Console.WriteLine(spaces + "    | Unable to get attributes for " + curr);
                    continue;
                }

                if (client.IsDirectory(curr))
                {
                    Console.WriteLine(spaces + "    | " + curr + " (dir)");
                    await WalkDirectoryInternal(client, curr, (path + "/" + curr + "/"), spacing + 2);
                }
                else
                {
                    Console.WriteLine(spaces + "    | " + (path + curr) + " " + attrib.Size + " bytes");
                }
            }

            Console.WriteLine("");
        }

        private static async Task ReadFile()
        {
            string file = Inputty.GetString("Filename:", null, true);
            if (String.IsNullOrEmpty(file)) return;

            NfsClient nfs = new NfsClient(_Version);
            nfs.Connect(IPAddress.Parse(_Hostname));
            nfs.MountDevice(_Share);

            Stream stream = new MemoryStream();
            nfs.Read(file, ref stream);

            if (stream != null)
            {
                stream.Seek(0, SeekOrigin.Begin);

                int read = 0;
                byte[] buf = new byte[4096];

                while (true)
                {
                    read = await stream.ReadAsync(buf, 0, buf.Length);
                    if (read > 0)
                    {
                        byte[] segment = new byte[read];
                        Buffer.BlockCopy(buf, 0, segment, 0, read);
                        Console.WriteLine(Encoding.UTF8.GetString(segment));
                    }
                    else
                    {
                        break;
                    }
                }
            }

            nfs.UnMountDevice();

            stream.Close();
            stream.Dispose();
            stream = null;

            nfs.Disconnect();
        }

        private static async Task WriteFile()
        {
            string file = Inputty.GetString("Filename:", null, true);
            if (String.IsNullOrEmpty(file)) return;

            string data = Inputty.GetString("Data    :", null, true);
            if (String.IsNullOrEmpty(data)) return;

            NfsClient nfs = new NfsClient(_Version);
            nfs.Connect(IPAddress.Parse(_Hostname));
            nfs.MountDevice(_Share);

            using (MemoryStream stream = new MemoryStream())
            {
                await stream.WriteAsync(Encoding.UTF8.GetBytes(data));
                stream.Seek(0, SeekOrigin.Begin);
                nfs.Write(file, stream);
            }

            nfs.UnMountDevice();

            nfs.Disconnect();
        }

        private static async Task DeleteFile()
        {
            string file = Inputty.GetString("Filename:", null, true);
            if (String.IsNullOrEmpty(file)) return;

            NfsClient nfs = new NfsClient(_Version);
            nfs.Connect(IPAddress.Parse(_Hostname));
            nfs.MountDevice(_Share);
            nfs.DeleteFile(file);
            nfs.UnMountDevice();

            nfs.Disconnect();
        }
    }
}