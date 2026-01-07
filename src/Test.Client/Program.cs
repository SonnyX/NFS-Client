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
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

        /*
         * See https://www.dummies.com/article/technology/computers/operating-systems/linux/how-to-share-files-with-nfs-on-linux-systems-255851/
         * https://github.com/SonnyX/NFS-Client
         * https://github.com/nekoni/nekodrive
         * https://github.com/DeCoRawr/NFSClient
         * https://code.google.com/archive/p/nekodrive/wikis/UseNFSDotNetLibrary.wiki
         * https://ubuntu.com/server/docs/network-file-system-nfs
         * https://www.hanewin.net/nfs-e.htm
         * https://serverfault.com/questions/240897/how-to-properly-set-permissions-for-nfs-folder-permission-denied-on-mounting-en
         * 
         */

        private static bool _RunForever = true;
        private static string _Hostname = "192.168.254.129";
        private static NfsVersion _Version = NfsVersion.V3;
        private static string _Share = "/srv";

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
            NfsClient client = new NfsClient(_Version);
            client.Connect(IPAddress.Parse(_Hostname));
            List<string> exports = client.GetExportedDevices();
            if (exports != null && exports.Count > 0)
            {
                foreach (var share in exports)
                {
                    Console.WriteLine(share);
                }
            }
            client.Disconnect();
        }

        private static async Task<List<string>> ListFilesInternal(string baseDir)
        {
            List<string> files = new List<string>();

            if (String.IsNullOrEmpty(baseDir)) baseDir = ".";
            else
            {
                if (!baseDir.EndsWith("/")) baseDir += "/";
                if (!baseDir.EndsWith(".")) baseDir += ".";
            }

            try
            {
                NfsClient client = new NfsClient(_Version);
                client.Connect(IPAddress.Parse(_Hostname));
                client.MountDevice(_Share);

                Console.WriteLine("Retrieving file list from base directory: " + baseDir);

                foreach (string item in client.GetItemList(baseDir))
                {
                    NFSAttributes attrib = client.GetItemAttributes(item);
                    if (attrib == null) continue;

                    bool isDirectory = client.IsDirectory(item);
                    if (isDirectory) continue;
                    files.Add(item);
                }

                client.UnMountDevice();
                client.Disconnect();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }

            return files;
        }

        private static async Task<List<string>> ListDirectoriesInternal(string baseDir)
        {
            List<string> directories = new List<string>();

            if (String.IsNullOrEmpty(baseDir)) baseDir = ".";
            else
            {
                if (!baseDir.EndsWith("/")) baseDir += "/";
                if (!baseDir.EndsWith(".")) baseDir += ".";
            }

            try
            {
                NfsClient client = new NfsClient(_Version);
                client.Connect(IPAddress.Parse(_Hostname));
                client.MountDevice(_Share);

                Console.WriteLine("Retrieving directory list from base directory: " + baseDir);

                foreach (string item in client.GetItemList(baseDir))
                {
                    NFSAttributes attrib = client.GetItemAttributes(item);
                    if (attrib == null) continue;

                    bool isDirectory = client.IsDirectory(item);
                    if (!isDirectory) continue;
                    directories.Add(item);
                }

                client.UnMountDevice();
                client.Disconnect();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }

            return directories;
        }

        private static async Task ListFiles()
        {
            try
            {
                string baseDir = Inputty.GetString("Base directory:", null, true);

                List<string> files = await ListFilesInternal(baseDir);
                List<string> directories = await ListDirectoriesInternal(baseDir);

                Console.WriteLine("Directories:");
                if (directories != null && directories.Count > 0)
                    foreach (string directory in directories) Console.WriteLine("  " + directory);
                else
                    Console.WriteLine("  (none)");

                Console.WriteLine("Files:");
                if (files != null && files.Count > 0)
                {
                    foreach (string file in files)
                        Console.WriteLine("  " + file);
                }
                else
                    Console.WriteLine("  (none)");
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        private static async Task WalkDirectory()
        {
            NfsClient client = new NfsClient(_Version);
            client.Connect(IPAddress.Parse(_Hostname));
            client.MountDevice(_Share);

            await WalkDirectoryInternal(client, ".", "", 0);

            client.UnMountDevice();
            client.Disconnect();
        }

        private static async Task WalkDirectoryInternal(NfsClient client, string item, string path, int spacing)
        {
            try
            {
                string spaces = "";
                for (int i = 0; i < spacing; i++) spaces += " ";
                Console.WriteLine(spaces + "| Walking directory " + item);

                string basePath = "";
                if (!String.IsNullOrEmpty(path)) basePath = path + "/";

                List<string> items = client.GetItemList(basePath + item, true);
                Console.WriteLine(spaces + "| Read " + items.Count + " items");

                foreach (string curr in items)
                {
                    bool isDir = client.IsDirectory(curr);

                    if (!isDir)
                    {
                        NFSAttributes attrib = client.GetItemAttributes(path + curr);
                        if (attrib == null)
                        {
                            Console.WriteLine(spaces + "  | Unable to get attributes for " + curr);
                            continue;
                        }

                        Console.WriteLine(spaces + "  | " + (path + curr) + " " + attrib.Size + " bytes");
                    }
                    else
                    {
                        Console.WriteLine(spaces + "  | " + curr + " (dir)");
                        // await WalkDirectoryInternal(client, (curr + "/."), (path + "/" + curr), spacing + 2);
                    }
                }

                Console.WriteLine("");
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        private static async Task ReadFile()
        {
            string file = Inputty.GetString("Filename:", null, true);
            if (String.IsNullOrEmpty(file)) return;

            NfsClient client = new NfsClient(_Version);
            client.Connect(IPAddress.Parse(_Hostname));
            client.MountDevice(_Share);

            Stream stream = new MemoryStream();
            client.Read(file, ref stream);

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

            client.UnMountDevice();

            stream.Close();
            stream.Dispose();
            stream = null;

            client.Disconnect();
        }

        private static async Task WriteFile()
        {
            string file = Inputty.GetString("Filename:", null, true);
            if (String.IsNullOrEmpty(file)) return;

            string data = Inputty.GetString("Data    :", null, true);
            if (String.IsNullOrEmpty(data)) return;

            NfsClient client = new NfsClient(_Version);
            client.Connect(IPAddress.Parse(_Hostname));
            client.MountDevice(_Share);

            using (MemoryStream stream = new MemoryStream())
            {
                await stream.WriteAsync(Encoding.UTF8.GetBytes(data));
                stream.Seek(0, SeekOrigin.Begin);
                client.Write(file, stream);
            }

            client.UnMountDevice();

            client.Disconnect();
        }

        private static async Task DeleteFile()
        {
            string file = Inputty.GetString("Filename:", null, true);
            if (String.IsNullOrEmpty(file)) return;

            NfsClient client = new NfsClient(_Version);
            client.Connect(IPAddress.Parse(_Hostname));
            client.MountDevice(_Share);
            client.DeleteFile(file);
            client.UnMountDevice();

            client.Disconnect();
        }

#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
    }
}
