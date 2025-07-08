using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using GPSTrackerUltimate.Types.Byond;

namespace GPSTrackerUltimate.Types
{

    public static class DmeParser
    {
        private static readonly Regex IncludeRegex = new Regex(pattern : @"#include\s+""([^""]+\.dm)""", options : RegexOptions.Compiled);

        /// <summary>
        /// Возвращает список всех .dm файлов, указанных в .dme через #include "...dm"
        /// </summary>
        public static List<string> ParseIncludedDmFiles(string dmePath)
        {
            List<string>? result = new List<string>();
            string? dmeDir = Path.GetDirectoryName(path : dmePath)!;

            foreach (string? line in File.ReadLines(path : dmePath))
            {
                Match? match = IncludeRegex.Match(input : line);
                if (match.Success)
                {
                    string? relativePath = match.Groups[groupnum : 1].Value.Replace(oldChar : '\\', newChar : Path.DirectorySeparatorChar);
                    string? fullPath = Path.Combine(path1 : dmeDir, path2 : relativePath);
                    if (File.Exists(path : fullPath))
                    {
                        result.Add(item : fullPath);
                    }
                }
            }

            return result;
        }
    }

}
