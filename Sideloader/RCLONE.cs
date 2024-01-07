using AndroidSideloader.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;

namespace AndroidSideloader
{
    internal class rcloneFolder
    {
        public string Path { get; set; }
        public string Name { get; set; }
        public string Size { get; set; }
        public string ModTime { get; set; }
    }

    internal class SideloaderRCLONE
    {
        public static List<string> RemotesList = new List<string>();

        public static string RcloneGamesFolder = "Quest Games";

        //This shit sucks but i'll switch to programatically adding indexes from the gamelist txt sometimes maybe

        public static int GameNameIndex = 0;
        public static int ReleaseNameIndex = 1;
        public static int PackageNameIndex = 2;
        public static int VersionCodeIndex = 3;
        public static int ReleaseAPKPathIndex = 4;
        public static int VersionNameIndex = 5;

        public static List<string> gameProperties = new List<string>();
        /* Game Name
         * Release Name
         * Release APK Path
         * Package Name
         * Version Code
         * Version Name
         */
        public static List<string[]> games = new List<string[]>();

        public static string Nouns = Environment.CurrentDirectory + "\\nouns";
        public static string ThumbnailsFolder = Environment.CurrentDirectory + "\\thumbnails";
        public static string NotesFolder = Environment.CurrentDirectory + "\\notes";

        // Downloader
        public static WebClient Webclient = new WebClient();
        public static void UpdateNouns(string remote)
        {
            _ = Logger.Log($"Updating Nouns");
            _ = RCLONE.runRcloneCommand_DownloadConfig($"sync \"{remote}:{RcloneGamesFolder}/.meta/nouns\" \"{Nouns}\"");
        }

        public static void UpdateGamePhotos(string remote)
        {
            _ = Logger.Log($"Updating Thumbnails");
            _ = RCLONE.runRcloneCommand_DownloadConfig($"sync \"{remote}:{RcloneGamesFolder}/.meta/thumbnails\" \"{ThumbnailsFolder}\"");
        }

        public static void UpdateGameNotes(string remote)
        {
            _ = Logger.Log($"Updating Game Notes");
            _ = RCLONE.runRcloneCommand_DownloadConfig($"sync \"{remote}:{RcloneGamesFolder}/.meta/notes\" \"{NotesFolder}\"");
        }

        public static void UpdateMetadataFromPublic()
        {
            _ = Logger.Log($"Downloading Metadata");
            string rclonecommand =
                $"sync \":http:/meta.7z\" \"{Environment.CurrentDirectory}\"";
            _ = RCLONE.runRcloneCommand_PublicConfig(rclonecommand);
        }

        public static void ProcessMetadataFromPublic()
        {
            try
            {
                _ = Logger.Log($"Extracting Metadata");
                Zip.ExtractFile($"{Environment.CurrentDirectory}\\meta.7z", $"{Environment.CurrentDirectory}\\meta",
                    MainForm.PublicConfigFile.Password);

                _ = Logger.Log($"Updating Metadata");

                if (Directory.Exists(Nouns))
                {
                    Directory.Delete(Nouns, true);
                }

                if (Directory.Exists(ThumbnailsFolder))
                {
                    Directory.Delete(ThumbnailsFolder, true);
                }

                if (Directory.Exists(NotesFolder))
                {
                    Directory.Delete(NotesFolder, true);
                }

                Directory.Move($"{Environment.CurrentDirectory}\\meta\\.meta\\nouns", Nouns);
                Directory.Move($"{Environment.CurrentDirectory}\\meta\\.meta\\thumbnails", ThumbnailsFolder);
                Directory.Move($"{Environment.CurrentDirectory}\\meta\\.meta\\notes", NotesFolder);

                _ = Logger.Log($"Initializing Games List");
                string gameList = File.ReadAllText($"{Environment.CurrentDirectory}\\meta\\VRP-GameList.txt");

                string[] splitList = gameList.Split('\n');
                splitList = splitList.Skip(1).ToArray();
                foreach (string game in splitList)
                {
                    if (game.Length > 1)
                    {
                        string[] splitGame = game.Split(';');
                        games.Add(splitGame);
                    }
                }

                Directory.Delete($"{Environment.CurrentDirectory}\\meta", true);
            }
            catch (Exception e)
            {
                _ = Logger.Log(e.Message);
                _ = Logger.Log(e.StackTrace);
            }
        }

        public static void RefreshRemotes()
        {
            _ = Logger.Log($"Refresh / List Remotes");
            RemotesList.Clear();
            string[] remotes = RCLONE.runRcloneCommand_DownloadConfig("listremotes").Output.Split('\n');

            _ = Logger.Log("Loaded following remotes: ");
            foreach (string r in remotes)
            {
                if (r.Length > 1)
                {
                    string remote = r.Remove(r.Length - 1);
                    if (remote.Contains("mirror"))
                    {
                        _ = Logger.Log(remote);
                        RemotesList.Add(remote);
                    }
                }
            }
        }

        public static void initGames(string remote)
        {
            _ = Logger.Log($"Initializing Games List");

            gameProperties.Clear();
            games.Clear();
            string tempGameList = RCLONE.runRcloneCommand_DownloadConfig($"cat \"{remote}:{RcloneGamesFolder}/VRP-GameList.txt\"").Output;
            if (MainForm.debugMode)
            {
                File.WriteAllText("VRP-GamesList.txt", tempGameList);
            }
            if (!tempGameList.Equals(""))
            {
                string[] gameListSplited = tempGameList.Split(new[] { '\n' });
                gameListSplited = gameListSplited.Skip(1).ToArray();
                foreach (string game in gameListSplited)
                {
                    if (game.Length > 1)
                    {
                        string[] splitGame = game.Split(';');
                        games.Add(splitGame);
                    }
                }
            }
        }

        public static void updateDownloadConfig()
        {
            _ = Logger.Log($"Attempting to Update Download Config");
            try
            {
                string configUrl = "https://vrpirates.wiki/downloads/vrp.download.config";
                string saveLocation = Path.Combine(Environment.CurrentDirectory, "rclone", "vrp.download.config");
                
                //Delete file if it already exists. The original code downloaded it, checked if it matched, and threw it away if it did;
                //I'm just going to delete it and download fresh, there's no point in doing the comparison, it's more work.
                if (File.Exists(saveLocation))
                {
                    File.Delete(saveLocation);
                }
                File.WriteAllText(saveLocation, Webclient.DownloadString(configUrl));
                _ = Logger.Log($"Retrieved updated config from: {configUrl}");
            }
            //Catch
            catch (Exception e)
            {
                _ = Logger.Log($"Failed to update Download config: {e.Message}", LogLevel.ERROR);
            }
        }

        public static void updateUploadConfig()
        {
            _ = Logger.Log($"Attempting to Update Upload Config");
            try
            {
                string configUrl = "https://vrpirates.wiki/downloads/vrp.upload.config";
                string saveLocation = Path.Combine(Environment.CurrentDirectory, "rclone", "vrp.upload.config");
                if(File.Exists(saveLocation))
                {
                    File.Delete(saveLocation);
                }
                File.WriteAllText(saveLocation, Webclient.DownloadString(configUrl));
                _ = Logger.Log($"Retrieved updated config from: {configUrl}");
            }
            catch (Exception e)
            {
                _ = Logger.Log($"Failed to update Upload config: {e.Message}", LogLevel.ERROR);
            }
        }

        public static void updatePublicConfig()
        {
            _ = Logger.Log($"Attempting to Update Public Config");
            try
            {
                string configUrl = "https://vrpirates.wiki/downloads/vrp-public.json";
                string saveLocation = Path.Combine(Environment.CurrentDirectory, "vrp-public.json");
                File.WriteAllText(saveLocation, Webclient.DownloadString(configUrl));
                _ = Logger.Log($"Retrieved and updated public config from: {configUrl}");
            }
            catch (Exception e)
            {
                _ = Logger.Log($"Failed to update Public config: {e.Message}", LogLevel.ERROR);
            }
        }

        private static string CalculateMD5(string filename)
        {
            using (MD5 md5 = MD5.Create())
            {
                using (FileStream stream = File.OpenRead(filename))
                {
                    byte[] hash = md5.ComputeHash(stream);
                    return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                }
            }
        }
    }
}