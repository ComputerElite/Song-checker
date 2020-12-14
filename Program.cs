using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Compression;
using System.Collections;
using System.IO;
using SimpleJSON;
using System.Windows.Forms;

namespace Song_Checking
{
    class Program
    {
        static String exe = "";
        [STAThread]
        static void Main(string[] args)
        {
            
            exe = AppDomain.CurrentDomain.BaseDirectory;

            if (!Directory.Exists(exe + "\\finished")) Directory.CreateDirectory(exe + "\\finished");

            Console.WriteLine("Select your Song to check");
            OpenFileDialog ofd = new OpenFileDialog();
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                //Get the path of specified file
                if (!File.Exists(ofd.FileName))
                {
                    Console.WriteLine("The file doesn't exist.");
                    Console.ReadLine();
                    return;
                }

            }

            ZipFile.ExtractToDirectory(ofd.FileName, exe + "\\tmp\\correct");
            ArrayList found = new ArrayList();
            String entry = exe + "\\tmp\\correct";
            String dat = entry + "\\Info.dat";

            //get Info.dat

            MoveOutOfFolder(entry, found);
            if (!File.Exists(entry + "\\Info.dat") || !File.Exists(entry + "\\info.dat"))
            {
                if (Directory.GetDirectories(entry).Count() > 0)
                {
                    if (!File.Exists(entry + "\\Info.dat") || !File.Exists(entry + "\\info.dat"))
                    {
                        found.Add("Fatal: Info.dat missing");
                        sendfoundings(found, false);
                        return;
                    }
                }
                else
                {
                    found.Add("Fatal: Info.dat missing");
                    sendfoundings(found, false);
                    return;
                }
            }

            JSONNode info = JSON.Parse(File.ReadAllText(dat));
            File.Delete(dat);


            if (!File.Exists(entry + "\\" + info["_songFilename"]))
            {
                Boolean corrected = false;
                foreach (String file in Directory.GetFiles(entry))
                {
                    if (file.EndsWith(".ogg") || file.EndsWith(".egg") || file.EndsWith(".wav") || file.EndsWith(".bmp") || file.EndsWith(".exr") || file.EndsWith(".gif") || file.EndsWith(".hdr") || file.EndsWith(".iff") || file.EndsWith(".pict") || file.EndsWith(".psd") || file.EndsWith(".tga") || file.EndsWith(".tiff"))
                    {
                        info["_songFilename"] = System.IO.Path.GetFileName(file);
                        found.Add("Corrected: Wrong song in Info.dat");
                        corrected = true;
                        break;
                    }
                    if (info["_songFilename"].ToString().Replace("\"", "").StartsWith(System.IO.Path.GetFileNameWithoutExtension(file)))
                    {
                        info["_songFilename"] = System.IO.Path.GetFileName(file);
                        found.Add("Corrected: Wrong song extension in Info.dat");
                        corrected = true;
                        break;
                    }
                }
                if (!corrected)
                {
                    found.Add("Fatal: no valid song found");
                    sendfoundings(found, false);
                    return;
                }
            }

            if (!File.Exists(entry + "\\" + info["_coverImageFilename"]))
            {
                Boolean corrected = false;
                foreach (String file in Directory.GetFiles(entry))
                {
                    if (file.EndsWith(".png") || file.EndsWith(".jpg") || file.EndsWith("."))
                    {
                        info["_coverImageFilename"] = System.IO.Path.GetFileName(file);
                        found.Add("Corrected: Wrong cover name in Info.dat");
                        corrected = true;
                        break;
                    }
                    if (info["_coverImageFilename"].ToString().Replace("\"", "").StartsWith(System.IO.Path.GetFileNameWithoutExtension(file)))
                    {
                        info["_coverImageFilename"] = System.IO.Path.GetFileName(file);
                        found.Add("Corrected: Wrong cover extension in Info.dat");
                        corrected = true;
                        break;
                    }
                }
                if (!corrected)
                {
                    found.Add("Fatal: no valid cover found");
                    sendfoundings(found, false);
                    return;
                }
            }

            Boolean baddiff = false;
            foreach (JSONNode BeatmapSet in info["_difficultyBeatmapSets"])
            {
                foreach (JSONNode difficulty in BeatmapSet["_difficultyBeatmaps"])
                {
                    if (!File.Exists(entry + "\\" + difficulty["_beatmapFilename"]))
                    {
                        found.Add("Fatal: Difficulty file for difficulty " + difficulty["_difficulty"] + " in BeatMapSet " + BeatmapSet["_beatmapCharacteristicName"] + " not found");
                        baddiff = true;
                    }
                }
            }

            if (baddiff)
            {
                sendfoundings(found, false);
                return;
            }

            int index = 0;
            List<String> ka = new List<String>();
            foreach (KeyValuePair<string, JSONNode> c in (JSONObject)info)
            {
                if (c.Value.IsArray)
                {
                    index++;
                    continue;
                }
                if (c.Value.ToString().Replace("\"", "") == "unknown")
                {

                    ka.Add(c.Key);
                    found.Add("corrected: changed unknown of key " + c.Key + " to k. A. in Info.dat");
                }
                index++;
            }

            foreach (String c in ka)
            {
                info[c] = "k. A.";
            }

            if (found.Count == 0)
            {
                found.Add("All should be good. please check manually if you have any issues");
                sendfoundings(found, true, true);
                return;
            }

            File.WriteAllText(entry + "\\Info.dat", info.ToString());

            String Name = info["_songName"];
            Name = Name.Replace("/", "");
            Name = Name.Replace(":", "");
            Name = Name.Replace("*", "");
            Name = Name.Replace("?", "");
            Name = Name.Replace("\"", "");
            Name = Name.Replace("<", "");
            Name = Name.Replace(">", "");
            Name = Name.Replace("|", "");

            for (int f = 0; f < Name.Length; f++)
            {
                if (Name.Substring(f, 1).Equals("\\"))
                {
                    Name = Name.Substring(0, f - 1) + Name.Substring(f + 1, Name.Length - f - 1);
                }
            }

            if (File.Exists(exe + "\\finished\\" + Name + ".zip")) File.Delete(exe + "\\finished\\" + Name + ".zip");
            ZipFile.CreateFromDirectory(exe + "\\tmp\\correct", exe + "\\finished\\" + Name + ".zip");
            sendfoundings(found, true);
        }

        public static void MoveOutOfFolder(String FolderToMoveAll, ArrayList found)
        {
            foreach (String folder in Directory.GetDirectories(FolderToMoveAll))
            {
                if (found.Count == 0)
                {
                    found.Add("Corrected: Folder(s) in zip file");
                }

                MoveOutOfFolder(folder, found);
                foreach (String file in Directory.GetFiles(folder))
                {
                    File.Move(file, FolderToMoveAll + "\\" + Path.GetFileName(file));
                }
                Directory.Delete(folder);
            }
        }


        public static void sendfoundings(ArrayList found, bool sendZip, bool check = false)
        {
            Directory.Delete(exe + "\\tmp\\correct", true);
            File.Delete(exe + "\\tmp\\correct.zip");
            String tosend = "";
            foreach (String c in found)
            {
                tosend += "\n- " + c;
            }
            if (!sendZip)
            {
                Console.WriteLine("Founding:\n" + tosend + "\nNo correction possible");
            }
            else
            {
                if (check)
                {
                    Console.WriteLine("All good");
                }
                else
                {
                    Console.WriteLine("Foundings:" + tosend + " \nsong is in the finished folder");
                }

            }
            Console.ReadLine();
        }
    }
}
