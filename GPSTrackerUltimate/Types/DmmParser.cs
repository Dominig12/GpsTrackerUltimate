using System.IO;
using System.Text.RegularExpressions;
using System.Windows.Media.Imaging;
using GPSTrackerUltimate.Types.Map;

namespace GPSTrackerUltimate.Types
{

    public class DmmParser
{
    private readonly Dictionary<string, List<(string path, Dictionary<string, string> overrides)>> templates = new();
    private readonly List<Tile> tiles = new();

    public List<Tile> Parse(string path)
    {
        string[] lines = File.ReadAllLines(path : path);
        ParseTemplates(lines : lines);
        ParseTileGrid(lines : lines);
        return tiles;
    }

    private void ParseTemplates(string[] lines)
    {
        string currentKey = null;
        List<(string path, Dictionary<string, string>)> currentList = null;

        Regex templateHeader = new Regex(pattern : "^\"(?<key>[a-zA-Z0-9_]+)\"\\s*=\\s*\\(");
        Regex simplePathLine = new Regex(pattern : @"^\s*(?<path>/[^\s,{]+)\s*,?");
        Regex objectWithVarsStart = new Regex(pattern : @"^\s*(?<path>/[^\s,{]+)\s*\{");
        Regex keyValueLine = new Regex(pattern : @"^\s*(?<key>[a-zA-Z0-9_]+)\s*=\s*""?(?<value>[^""}]+)""?,?");
        
        Dictionary<string, string> currentOverrides = null;
        string currentPath = null;
        bool insideOverrideBlock = false;

        foreach (string line in lines)
        {
            if (templateHeader.IsMatch(input : line))
            {
                if (currentKey != null)
                {
                    templates[key : currentKey] = currentList;
                }

                currentKey = templateHeader.Match(input : line).Groups[groupname : "key"].Value;
                currentList = new List<(string, Dictionary<string, string>)>();
                continue;
            }

            if (line.Trim() == ")" && currentKey != null)
            {
                templates[key : currentKey] = currentList;
                currentKey = null;
                continue;
            }

            if (currentKey != null)
            {
                if (insideOverrideBlock)
                {
                    if (line.Contains(value : "}"))
                    {
                        // конец блока
                        currentList.Add(item : (currentPath, currentOverrides));
                        insideOverrideBlock = false;
                        currentPath = null;
                        currentOverrides = null;
                    }
                    else
                    {
                        Match kvMatch = keyValueLine.Match(input : line);
                        if (kvMatch.Success)
                        {
                            string key = kvMatch.Groups[groupname : "key"].Value.Trim();
                            string value = kvMatch.Groups[groupname : "value"].Value.Trim();
                            currentOverrides[key : key] = value;
                        }
                    }
                }
                else
                {
                    Match objectWithVarsMatch = objectWithVarsStart.Match(input : line);
                    if (objectWithVarsMatch.Success)
                    {
                        currentPath = objectWithVarsMatch.Groups[groupname : "path"].Value;
                        currentOverrides = new Dictionary<string, string>();
                        insideOverrideBlock = true;
                    }
                    else
                    {
                        Match simpleMatch = simplePathLine.Match(input : line);
                        if (simpleMatch.Success)
                        {
                            string path = simpleMatch.Groups[groupname : "path"].Value;
                            currentList.Add(item : (path, new Dictionary<string, string>()));
                        }
                    }
                }
            }
        }
    }

    private void ParseTileGrid(string[] lines)
    {
        Regex tileBlockStart = new Regex(pattern : @"^\((\d+),(\d+),(\d+)\)\s*=\s*\{\s*""");
        int x = 0, y = 0, z = 0;
        bool readingBlock = false;
        List<string> currentBlock = new();

        foreach (string line in lines)
        {
            if (tileBlockStart.IsMatch(input : line))
            {
                Match match = tileBlockStart.Match(input : line);
                x = int.Parse(s : match.Groups[groupnum : 1].Value);
                y = int.Parse(s : match.Groups[groupnum : 2].Value);
                z = int.Parse(s : match.Groups[groupnum : 3].Value);
                readingBlock = true;
                currentBlock = new List<string>();
                continue;
            }
            if (readingBlock)
            {
                if (line.Trim() == "\"}")
                {
                    readingBlock = false;

                    for (int row = 0; row < currentBlock.Count; row++)
                    {
                        string key = currentBlock[index : row].Trim();
                        if (!templates.ContainsKey(key : key))
                        {
                            continue;
                        }

                        Tile tile = new Tile
                        {
                            X = x,
                            Y = row + 1,
                            Z = z
                        };

                        List<(string path, Dictionary<string, string> overrides)> pathList = templates[key : key];
                        for (int i = 0; i < pathList.Count; i++)
                        {
                            tile.PathContent[key : i] = pathList[index : i].path;

                            // Добавим переопределения
                            foreach (KeyValuePair<string, string> pair in pathList[index : i].Item2)
                            {
                                tile.PathOverrides[key : $"{i}.{pair.Key}"] = pair.Value;
                            }
                        }

                        tiles.Add(item : tile);
                    }

                    continue;
                }

                currentBlock.Add(item : line);
            }
        }
    }
}

}
