using BencodeNET;
using BencodeNET.Objects;
using ByteSizeLib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
//using System.Security.Cryptography;
using System.Text;

namespace TorrentChecker
{
    class Program
    {
        static void Main(string[] args)
        {
            int TotalLines = 0, Invalid = 0, OK = 0, Skipped = 0;
            long TotalBytes = 0;
            bool Rename = false;
            string checksumfile = new FileInfo(System.Reflection.Assembly.GetEntryAssembly().Location).Directory.FullName;
            string output = checksumfile + "\\TorrentCheck_" + DateTime.Now.ToString("yyyy-MM-dd_HH：mm") + ".txt";
            List<FileStruct> lists = new List<FileStruct>();
            List<string> TorrentList = GetFiles(checksumfile, "*.torrent");
            Console.WriteLine("Rename torrent after the check ? (y/n) : ");
            try
            {
                string response = Console.ReadLine().ToLowerInvariant();
                if (response.Length == 1)
                {
                    if (response == "y")
                        Rename = true;
                    else
                        Rename = false;
                }
            }
            catch (Exception)
            {
                Rename = false;
            }
            //if (!File.Exists(output))
            //{
            using (FileStream file = File.Create(output))
            { }
            //}
            Console.WriteLine("No. of torrents : " + TorrentList.Count);
            //if (args.Length == 1)
            //{
            //    if (args[0].ToString().ToLowerInvariant().Contains(".torrent"))
            //    {
            //        StartingPoint = TorrentList.FindIndex(a => a.ToLowerInvariant() == args[0].ToString().ToLowerInvariant());
            //    }
            //    else
            //    {
            //        StartingPoint = TorrentList.FindIndex(a => a.ToLowerInvariant() == args[0].ToString().ToLowerInvariant() + ".torrent");
            //    }
            //}
            //for (int i = StartingPoint; i < TorrentList.Count; i++)
            //{
            foreach (string Torrent in TorrentList)
            {
                //Console.WriteLine("Torrent found : " + Path.GetFileName(Torrent));
                if (!string.IsNullOrEmpty(Torrent.Trim()))
                {
                    TotalLines++;
                    FileStruct fs = new FileStruct();
                    fs.infohash = Path.GetFileNameWithoutExtension(Torrent);
                    fs.filepath = Torrent;
                    lists.Add(fs);
                }
            }
            int i = 0;
            if (lists.Any())
            {
                foreach (FileStruct fss in lists)
                {
                    //using (System.IO.StreamWriter file = File.AppendText(output))
                    //{
                    //    file.WriteLine(Convert.ToString(i + 1) + "/" + TorrentList.Count + "\t" + TorrentList[i].Split('\'')[TorrentList[i].Split('\'').Length - 1] + " checking...");
                    //}
                    TorrentFile torrent = Bencode.DecodeTorrentFile(fss.filepath);
                    //if (fss.infohash.ToLowerInvariant() == torrent.CalculateInfoHash().ToLowerInvariant())
                    //{
                    //}
                    //else
                    //{
                    //    using (StreamWriter file = File.AppendText(output))
                    //    {
                    //        file.WriteLine("Skipped \t" + Path.GetFileName(fss.filepath));
                    //    }
                    //    Skipped++;
                    //}
                        BString CorrentName = (BString)torrent.Info["name"];

                        // Calculate info hash (e.g. "B415C913643E5FF49FE37D304BBB5E6E11AD5101")
                        //string infoHash = torrent.CalculateInfoHash();

                        // Get name and size of each file in 'files' list of 'info' dictionary ("multi-file mode")
                        BList files = (BList)torrent.Info["files"];
                        long SubSum = 0;
                        if (files != null) //multi-files
                        {
                            foreach (BDictionary file in files)
                            {
                                // File size in bytes (BNumber has implicit conversion to int and long)
                                SubSum += (BNumber)file["length"];

                                // List of all parts of the file path. 'dir1/dir2/file.ext' => dir1, dir2 and file.ext
                                BList path = (BList)file["path"];

                                // Last element is the file name
                                BString fileName = (BString)path.Last();

                                // Converts fileName (BString = bytes) to a string
                                //string fileNameString = fileName.ToString(Encoding.UTF8);
                            }
                        }
                        else //single files
                        {
                            SubSum += (BNumber)torrent.Info["length"];
                        }
                        Console.WriteLine("Space needed : " + ByteSize.FromBytes(SubSum).ToString());
                        TotalBytes += SubSum;
                        string NewName = "";
                        try
                        {
                            if (Rename)
                            {
                                NewName = fss.filepath.Replace(Path.GetFileNameWithoutExtension(fss.filepath), CorrentName.ToString(Encoding.UTF8));
                                File.Move(fss.filepath, NewName);
                                Console.WriteLine("Rename to " + Path.GetFileName(NewName));
                                using (StreamWriter file = File.AppendText(output))
                                {
                                    file.WriteLine("Rename OK \t" + Path.GetFileName(NewName) + "\t" + ByteSize.FromBytes(SubSum).ToString() + "\t" + Path.GetFileName(fss.filepath));
                                }
                            }
                            else
                            {
                                using (StreamWriter file = File.AppendText(output))
                                {
                                    file.WriteLine("OK \t" + Path.GetFileName(fss.filepath) + "\t" + ByteSize.FromBytes(SubSum).ToString());
                                }
                            }
                            OK++;
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("Rename to " + Path.GetFileName(NewName) + " has failed.");
                            Console.WriteLine(ex.ToString());
                            using (StreamWriter file = File.AppendText(output))
                            {
                                file.WriteLine("Rename NG \t" + Path.GetFileName(NewName) + "\t" + Path.GetFileName(fss.filepath));
                            }
                            Invalid++;
                        }
                    Console.WriteLine(Convert.ToString(i + 1) + "/" + TorrentList.Count + "\t" + TorrentList[i].Split('\'')[TorrentList[i].Split('\'').Length - 1] + " Checked.");
                    TaskbarProgress.SetValue(Process.GetCurrentProcess().MainWindowHandle, ((i + 1) * 200 + lists.Count) / (lists.Count * 2), 100);
                    TaskbarProgress.SetState(Process.GetCurrentProcess().MainWindowHandle, TaskbarProgress.TaskbarStates.Normal);
                    i++;
                }
                using (StreamWriter file = File.AppendText(output))
                {
                    file.WriteLine("Check Completed");
                    file.WriteLine("----------------------");
                }
                lists.Clear();
            }
            //}
            using (StreamWriter file = File.AppendText(output))
            {
                file.WriteLine("Total space needed : " + ByteSize.FromBytes(TotalBytes).ToString());
                file.WriteLine("Total files checked : " + TotalLines);
                file.WriteLine("Good : " + OK + ", Invalid : " + Invalid + ", Skipped : " + Skipped);
                //file.WriteLine("Completed  @ " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            }
            Console.WriteLine("Total space needed : " + ByteSize.FromBytes(TotalBytes).ToString());
            Console.WriteLine("Total files checked : " + TotalLines);
            Console.WriteLine("Good : " + OK + ", Invalid : " + Invalid + ", Skipped : " + Skipped);
            //Console.WriteLine("Completed  @ " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            Console.ReadKey();
        }
        static private List<string> GetFiles(string path, string pattern)
        {
            var files = new List<string>();

            try
            {
                if (!path.Contains("$RECYCLE.BIN"))
                {
                    files.AddRange(Directory.GetFiles(path, pattern, SearchOption.TopDirectoryOnly));
                    foreach (var directory in Directory.GetDirectories(path))
                        files.AddRange(GetFiles(directory, pattern));
                }
            }
            catch (UnauthorizedAccessException) { }

            return files;
        }
        //static string computeMD5(string path)
        //{
        //    using (var md5 = MD5.Create())
        //    {
        //        using (var stream = File.OpenRead(path))
        //        {
        //            return BitConverter.ToString(md5.ComputeHash(stream)).Replace("-", "").ToLower();
        //        }
        //    }
        //}
    }
}
