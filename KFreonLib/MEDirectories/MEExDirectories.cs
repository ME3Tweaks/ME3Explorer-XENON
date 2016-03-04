﻿using KFreonLib.Debugging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;


namespace KFreonLib.MEDirectories
{
    public class MEDirectories
    {
        public List<string> BIOGames = new List<string>() { "", "", "" };
        public int WhichGame { get; set; }
        public string pathCooked
        {
            get
            {
                if (WhichGame == 0)
                    return null;
                return GetDifferentPathCooked(WhichGame);
            }
        }

        public string PathBIOGame
        {
            get
            {
                if (WhichGame == 0)
                    return null;
                return GetDifferentPathBIOGame(WhichGame);
            }
        }

        public string DLCPath
        {
            get
            {
                if (WhichGame == 0)
                    return null;

                return GetDifferentDLCPath(WhichGame);
            }
        }

         string execf = null;
        public string ExecFolder
        {
            get
            {
                if (execf == null)
                    execf = Path.GetDirectoryName(Application.ExecutablePath) + "\\Exec\\";

                return execf;
            }
        }

        string Thumpath = null;
        public  string ThumbnailPath
        {
            get
            {
                if (WhichGame == 0)
                    return null;

                return ExecFolder + "ThumbnailCaches\\ME" + WhichGame + "ThumbnailCache";
            }
        }

        public MEDirectories(int game)
        {
            WhichGame = game;
            Properties.Settings.Default.Upgrade();
        }

        public MEDirectories()
        {
            Properties.Settings.Default.Upgrade();
        }

        public  string GetDifferentPathCooked(int game)
        {
            return Path.Combine(GetDifferentPathBIOGame(game), game == 3 ? "CookedPCConsole" : "CookedPC");
        }

        public  string GetDifferentPathBIOGame(int game)
        {
            return BIOGames[game - 1];
        }

        public  string GetDifferentDLCPath(int game)
        {
            string dlc = null;
            string tempBIO = GetDifferentPathBIOGame(game);
            switch (game)
            {
                case 1:
                    dlc = Path.Combine(Path.GetDirectoryName(tempBIO), "DLC");
                    break;
                case 2:
                case 3:
                    dlc = Path.Combine(tempBIO, "DLC");
                    break;
            }
            return dlc;
        }



        public  List<string> SetupPathing(bool AskIfNotFound)
        {
            DebugOutput.PrintLn("Using ResIL dll at: " + Path.Combine(Path.GetDirectoryName(ExecFolder), "ResIL.dll"));
            List<string> Messages = new List<string>();
            for (int i = 1; i <= 3; i++)
            {
                // KFreon: Try to get pathing from MEDirectories
                int status = SetupPaths(i);
                if (status == 0)
                    Messages.Add("Found game path in settings. Using cooked directory: " + GetDifferentPathCooked(i));
                else if (status == 1)
                    Messages.Add("Found installation path from registry. Using cooked directory: " + GetDifferentPathCooked(i));
                else if (status == -1 && AskIfNotFound)
                {
                    // KFreon: Game not found, so ask where it is
                    string path = KFreonLib.Misc.Methods.SelectGameLoc(i);
                    if (path != null)
                    {
                        KFreonLib.Misc.Methods.SetGamePath(i, path);
                        Messages.Add("Gamepath set by user as:  " + path);

                        // KFreon: Save in settings?
                        SetupPaths(i);
                        continue;
                    }
                    else
                        Messages.Add("ME" + i + " game files not found and user didn't chose any.");
                }
            }
            SaveSettings();
            return Messages;
        }

        public  int SetupPaths(int whichgame)
        {
            string tempgamepath = "";
            string PropertiesPathString = "";
            //WhichGame = whichgame;
            int status = -1;

            switch (whichgame)
            {
                case 1:
                    tempgamepath = ME1Directory.GamePath();
                    PropertiesPathString = Properties.Settings.Default.ME1Directory;
                    break;
                case 2:
                    tempgamepath = ME2Directory.GamePath();
                    PropertiesPathString = Properties.Settings.Default.ME2Directory;
                    break;
                case 3:
                    tempgamepath = ME3Directory.GamePath();
                    PropertiesPathString = Properties.Settings.Default.ME3Directory;
                    break;
            }

            if (!String.IsNullOrEmpty(PropertiesPathString))
            {
                BIOGames[whichgame - 1] = PropertiesPathString;
                status = 0;
            }
            else if (tempgamepath != null)
            {
                status = 1;
                BIOGames[whichgame - 1] = Path.Combine(tempgamepath, "BIOGame");
            }
            else
                DebugOutput.PrintLn("ME" + whichgame + " game files not found.");

            return status;
        }


        public  void SaveSettings()
        {
            if (!String.IsNullOrEmpty(BIOGames[0]))
                Properties.Settings.Default.ME1Directory = BIOGames[0];

            if (!String.IsNullOrEmpty(BIOGames[1]))
                Properties.Settings.Default.ME2Directory = BIOGames[1];

            if (!String.IsNullOrEmpty(BIOGames[2]))
                Properties.Settings.Default.ME3Directory = BIOGames[2];

            Properties.Settings.Default.Save();
        }

        public  void SetPaths(string ME1Path = null, string ME2Path = null, string ME3Path = null)
        {
            if (!String.IsNullOrEmpty(ME1Path))
                Properties.Settings.Default.ME1Directory = ME1Path;

            if (!String.IsNullOrEmpty(ME2Path))
                Properties.Settings.Default.ME2Directory = ME2Path;

            if (!String.IsNullOrEmpty(ME3Path))
                Properties.Settings.Default.ME3Directory = ME3Path;

            SetupPathing(false);
        }
    }
}
