﻿using JR.Utils.GUI.Forms;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;

namespace AndroidSideloader.Utilities
{
    internal class Zip
    {
        public static void ExtractFile(string sourceArchive, string destination)
        {
            string args = $"x \"{sourceArchive}\" -y -o\"{destination}\" -bsp1";
            DoExtract(args);
        }

        public static void ExtractFile(string sourceArchive, string destination, string password)
        {
            string args = $"x \"{sourceArchive}\" -y -o\"{destination}\" -p\"{password}\" -bsp1";
            DoExtract(args);
        }

        private static void DoExtract(string args)
        {
            if (!File.Exists(Environment.CurrentDirectory + "\\7z.exe") || !File.Exists(Environment.CurrentDirectory + "\\7z.dll"))
            {
                _ = Logger.Log("Begin download 7-zip");
                WebClient client = new WebClient();
                client.DownloadFile("https://github.com/VRPirates/rookie/raw/master/7z.exe", "7z.exe");
                client.DownloadFile("https://github.com/VRPirates/rookie/raw/master/7z.dll", "7z.dll");
                _ = Logger.Log("Complete download 7-zip");
            }

            ProcessStartInfo pro = new ProcessStartInfo
            {
                WindowStyle = ProcessWindowStyle.Hidden,
                FileName = "7z.exe",
                Arguments = args,
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardInput = true,
                RedirectStandardError = true,
                RedirectStandardOutput = true
            };

            _ = Logger.Log($"Extract: 7z {string.Join(" ", args.Split(' ').Where(a => !a.StartsWith("-p")))}");

            using (Process x = new Process())
            {
                x.StartInfo = pro;

                if (MainForm.isInDownloadExtract && x != null)
                {
                    x.OutputDataReceived += (sender, e) =>
                    {
                        if (e.Data != null)
                        {
                            var match = Regex.Match(e.Data, @"(\d+)%");
                            if (match.Success)
                            {
                                int progress = int.Parse(match.Groups[1].Value);
                                MainForm mainForm = (MainForm)Application.OpenForms[0];
                                if (mainForm != null)
                                {
                                    mainForm.Invoke((Action)(() => mainForm.SetProgress(progress)));
                                }
                            }
                        }
                    };
                }
                x.ErrorDataReceived += (sender, e) => Console.WriteLine(e.Data);

                x.Start();

                x.BeginOutputReadLine();
                x.BeginErrorReadLine();
                x.WaitForExit();
                if (x.ExitCode != 0)
                {
                    string error = x.StandardError.ReadToEnd();

                    if (error.Contains("There is not enough space on the disk"))
                    {
                        _ = FlexibleMessageBox.Show(Program.form, $"Not enough space to extract archive.\r\nMake sure your {Path.GetPathRoot(Properties.Settings.Default.downloadDir)} drive has at least double the space of the game, then try again.",
                            "NOT ENOUGH SPACE",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error);
                    }
                    _ = Logger.Log(x.StandardOutput.ReadToEnd());
                    _ = Logger.Log(error, LogLevel.ERROR);
                    throw new ApplicationException($"Extracting failed, status code {x.ExitCode}");
                }
            }
        }
    }
}