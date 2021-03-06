﻿using System;
using System.Windows.Forms;

namespace Elmanager
{
    static class Constants
    {
        internal const string AllLevs = "*" + LevExtension;
        internal const Keys Decrease = Keys.OemMinus;
        internal const Keys DecreaseBig = Keys.PageDown;
        internal const double DegToRad = Math.PI / 180;
        internal const double HeadDiameter = 0.476;
        internal const double HeadRadius = HeadDiameter / 2;
        internal const Keys Increase = Keys.Oemplus;
        internal const Keys IncreaseBig = Keys.PageUp;
        internal const string LevExtension = ".lev";
        internal const string LebExtension = ".leb";
        internal static readonly string[] LevLebExtensions = {LevExtension, LebExtension};
        internal static readonly string[] ImportableExtensions = { LevExtension, LebExtension, ".bmp", ".png", ".gif", ".tiff", ".exif", ".svg", ".svgz" };

        internal const string LevOrRecDirNotFound =
            "Replay or level directory are not specified or they doesn\'t exist!";

        internal const double RadToDeg = 180 / Math.PI;
        internal const string LevDirNotFound = "Level directory is not specified or it doesn\'t exist!";
        internal const string RecDirNotFound = "Replay directory is not specified or it doesn\'t exist!";
        internal const string RecExtension = ".rec";
        internal const double SpeedConst = 10000.0 / 23.0;

        internal const string VersionUri = "https://api.github.com/repos/Smibu/elmanager/releases/latest";
        internal const string ChangelogUri = "https://github.com/Smibu/elmanager/blob/master/Elmanager/changelog.md";
        public const double Tolerance = 0.00000001;
    }
}