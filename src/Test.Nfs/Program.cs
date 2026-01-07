namespace Test.Nfs
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Net;
    using System.Text;
    using NFSLibrary;
    using NFSLibrary.Protocols.Commons;

    public static class Program
    {
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.

        private static string _Hostname = "192.168.254.129";
        private static NfsVersion _Version = NfsVersion.V3;
        private static string _Share = "/srv";

        /*

        /
        |-- root.txt
        |-- dir1
            |-- 1.txt
            |-- dir2
                |-- 2.txt
                |-- dir3
                    |-- 3.txt
                  
         */

        public static void Main(string[] args)
        {
            NfsClient client = null;
            Stream stream = null;
            string file = "";
            string folder = "";

            try
            {
                #region Initialize-and-Mount

                Console.WriteLine("");
                Console.WriteLine("Initializing client");
                client = new NfsClient(_Version);
                client.Connect(IPAddress.Parse(_Hostname));
                Console.WriteLine("Mounting device");
                client.MountDevice(_Share);

                #endregion

                #region List-Shares

                Console.WriteLine("");
                Console.WriteLine("Listing shares");
                List<string> exports = client.GetExportedDevices();
                if (exports != null && exports.Count > 0)
                    foreach (var share in exports)
                        Console.WriteLine("| " + share);

                #endregion

                #region Enumerate-Root

                Console.WriteLine("");
                Console.WriteLine("Listing root directory");
                foreach (string item in client.GetItemList("."))
                {
                    NFSAttributes attrib = client.GetItemAttributes(item);
                    if (attrib == null) continue;

                    bool isDirectory = client.IsDirectory(item);
                    Console.WriteLine("| " + item + " " + (isDirectory ? "(dir)" : null) + " " + attrib.Size + " bytes");
                }

                #endregion

                #region Read /root.txt

                Console.WriteLine("");
                // file = client.Combine("root.txt", null);
                // file = ".\\root.txt";
                file = NormalizePath("/root.txt");
                Console.WriteLine("Reading file root.txt: " + file);
                stream = new MemoryStream();
                client.Read("root.txt", ref stream);
                if (stream != null)
                {
                    stream.Seek(0, SeekOrigin.Begin);

                    int read = 0;
                    byte[] buf = new byte[4096];

                    while (true)
                    {
                        read = stream.Read(buf, 0, buf.Length);
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

                #endregion

                #region Enumerate /dir1

                Console.WriteLine("");
                // folder = client.Combine("dir1", ".");
                // folder = ".\\dir1";
                folder = NormalizePath("/dir1/");
                Console.WriteLine("Listing dir1 directory: " + folder);
                foreach (string item in client.GetItemList(folder))
                {
                    NFSAttributes attrib = client.GetItemAttributes(client.Combine(item, "dir1"));
                    if (attrib == null) continue;

                    bool isDirectory = client.IsDirectory(client.Combine(item, "dir1"));
                    Console.WriteLine("| " + item + " " + (isDirectory ? "(dir)" : null) + " " + attrib.Size + " bytes");
                }

                #endregion

                #region Read /dir1/1.txt

                Console.WriteLine("");
                // file = client.Combine("1.txt", "dir1");
                // file = ".\\dir1\\1.txt";
                file = NormalizePath("/dir1/1.txt");
                Console.WriteLine("Reading file 1.txt: " + file);
                stream = new MemoryStream();
                client.Read(file, ref stream);
                if (stream != null)
                {
                    stream.Seek(0, SeekOrigin.Begin);

                    int read = 0;
                    byte[] buf = new byte[4096];

                    while (true)
                    {
                        read = stream.Read(buf, 0, buf.Length);
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

                #endregion

                #region Enumerate /dir2

                Console.WriteLine("");
                // folder = client.Combine("dir2", "dir1");
                // folder = ".\\dir1\\dir2";
                folder = NormalizePath("/dir1/dir2/");
                Console.WriteLine("Listing dir2 directory: " + folder);
                foreach (string item in client.GetItemList(folder))
                {
                    Console.WriteLine("Processing item " + item);
                    NFSAttributes attrib = client.GetItemAttributes(client.Combine(item, "dir1"));
                    if (attrib == null) continue;

                    bool isDirectory = client.IsDirectory(client.Combine(item, "dir1"));
                    Console.WriteLine("| " + item + " " + (isDirectory ? "(dir)" : null) + " " + attrib.Size + " bytes");
                }

                #endregion

                #region Read /dir1/dir2/2.txt

                Console.WriteLine("");
                // file = client.Combine("2.txt", "dir1\\dir2");
                // file = ".\\dir1\\dir2\\2.txt";
                file = NormalizePath("/dir1/dir2/2.txt");
                Console.WriteLine("Reading file 2.txt: " + file);
                stream = new MemoryStream();
                client.Read(file, ref stream);
                if (stream != null)
                {
                    stream.Seek(0, SeekOrigin.Begin);

                    int read = 0;
                    byte[] buf = new byte[4096];

                    while (true)
                    {
                        read = stream.Read(buf, 0, buf.Length);
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

                #endregion

                #region Enumerate /dir3

                Console.WriteLine("");
                // folder = client.Combine("dir3", "dir1\\dir2");
                // folder = ".\\dir1\\dir2\\dir3";
                file = NormalizePath("/dir1/dir2/dir3");
                Console.WriteLine("Listing dir3 directory: " + folder);
                foreach (string item in client.GetItemList(folder))
                {
                    Console.WriteLine("Processing item " + item);
                    NFSAttributes attrib = client.GetItemAttributes(folder);
                    if (attrib == null) continue;

                    bool isDirectory = client.IsDirectory(client.Combine(item, "dir1\\dir2\\dir3"));
                    Console.WriteLine("| " + item + " " + (isDirectory ? "(dir)" : null) + " " + attrib.Size + " bytes");
                }

                #endregion

                #region Read /dir1/dir2/dir3/3.txt

                Console.WriteLine("");
                // file = client.Combine("3.txt", "dir1\\dir2\\dir3");
                // file = ".\\dir1\\dir2\\dir3\\3.txt";
                file = NormalizePath("/dir1/dir2/dir3/3.txt");
                Console.WriteLine("Reading file 3.txt: " + file);
                stream = new MemoryStream();
                client.Read(file, ref stream);
                if (stream != null)
                {
                    stream.Seek(0, SeekOrigin.Begin);

                    int read = 0;
                    byte[] buf = new byte[4096];

                    while (true)
                    {
                        read = stream.Read(buf, 0, buf.Length);
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

                #endregion

                #region Fully-Enumerate

                Console.WriteLine("");
                Console.WriteLine("Fully enumerating");
                List<string> fullEnumeration = FullyEnumerate(client, ".");
                Console.WriteLine("Results:");
                foreach (string curr in fullEnumeration)
                    Console.WriteLine("| " + curr);

                #endregion

                #region Normalization-Test

                Console.WriteLine("");
                Console.WriteLine("Normalizing 10 times: /dir1/dir2/dir3/3.txt");
                string str = "/dir1/dir2/dir3/3.txt";
                for (int i = 0; i < 10; i++)
                    str = NormalizePath(str);
                Console.WriteLine(str);

                #endregion

                Console.WriteLine("");
                Console.WriteLine("Finished");
                Console.WriteLine("");
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
            finally
            {
                if (client != null)
                {
                    if (client.IsMounted)
                    {
                        Console.WriteLine("Unmounting device");
                        client.UnMountDevice();
                    }

                    Console.WriteLine("Disconnecting client");
                    client.Disconnect();
                }
            }
        }

        private static List<string> FullyEnumerate(NfsClient client, string rootDirectory, int spaceCount = 0)
        {
            string spaces = "";
            if (spaceCount > 0) for (int i = 0; i < spaceCount; i++) spaces += " ";

            List<string> ret = new List<string>();

            Console.WriteLine(spaces + "Processing " + rootDirectory);

            foreach (string item in client.GetItemList(rootDirectory))
            {
                string itemKey = NormalizePath(rootDirectory + "/" + item);
                Console.WriteLine(spaces + "| Retrieved item " + itemKey);

                NFSAttributes attrib = client.GetItemAttributes(itemKey);
                if (attrib == null) continue;

                bool isDirectory = client.IsDirectory(itemKey);
                if (isDirectory)
                {
                    ret.Add(itemKey + " (dir)");
                    Console.WriteLine(spaces + "| Recursing into " + itemKey);
                    List<string> children = FullyEnumerate(client, itemKey, spaceCount + 1);
                    if (children != null && children.Count > 0)
                        ret.AddRange(children);
                }
                else
                {
                    ret.Add(itemKey);
                }
            }

            return ret;
        }

        private static string NormalizePath(string path)
        {
            if (String.IsNullOrEmpty(path)) return ".";
            path = path.Replace("/", "\\");
            while (path.EndsWith("\\")) path = path.Substring(0, path.Length - 1);
            while (path.StartsWith(".")) path = path.Substring(1);
            while (path.StartsWith("\\")) path = path.Substring(1);
            string[] parts = path.Split("\\");
            return ".\\" + string.Join("\\", parts);
        }

#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.
    }
}