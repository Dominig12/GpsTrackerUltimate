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
        var lines = File.ReadAllLines(path : path);
        ParseTemplates(lines : lines);
        ParseTileGrid(lines : lines);
        return tiles;
    }

    private void ParseTemplates(string[] lines)
    {
        string currentKey = null;
        List<(string path, Dictionary<string, string>)> currentList = null;

        var templateHeader = new Regex("^\"(?<key>[a-zA-Z0-9_]+)\"\\s*=\\s*\\(");
        var simplePathLine = new Regex(@"^\s*(?<path>/[^\s,{]+)\s*,?");
        var objectWithVarsStart = new Regex(@"^\s*(?<path>/[^\s,{]+)\s*\{");
        var keyValueLine = new Regex(pattern : @"^\s*(?<key>[a-zA-Z0-9_]+)\s*=\s*""?(?<value>[^""}]+)""?,?");
        
        Dictionary<string, string> currentOverrides = null;
        string currentPath = null;
        bool insideOverrideBlock = false;

        foreach (var line in lines)
        {
            if (templateHeader.IsMatch(line))
            {
                if (currentKey != null)
                {
                    templates[currentKey] = currentList;
                }

                currentKey = templateHeader.Match(line).Groups["key"].Value;
                currentList = new List<(string, Dictionary<string, string>)>();
                continue;
            }

            if (line.Trim() == ")" && currentKey != null)
            {
                templates[currentKey] = currentList;
                currentKey = null;
                continue;
            }

            if (currentKey != null)
            {
                if (insideOverrideBlock)
                {
                    if (line.Contains("}"))
                    {
                        // конец блока
                        currentList.Add((currentPath, currentOverrides));
                        insideOverrideBlock = false;
                        currentPath = null;
                        currentOverrides = null;
                    }
                    else
                    {
                        var kvMatch = keyValueLine.Match(line);
                        if (kvMatch.Success)
                        {
                            string key = kvMatch.Groups["key"].Value.Trim();
                            string value = kvMatch.Groups["value"].Value.Trim();
                            currentOverrides[key] = value;
                        }
                    }
                }
                else
                {
                    var objectWithVarsMatch = objectWithVarsStart.Match(line);
                    if (objectWithVarsMatch.Success)
                    {
                        currentPath = objectWithVarsMatch.Groups["path"].Value;
                        currentOverrides = new Dictionary<string, string>();
                        insideOverrideBlock = true;
                    }
                    else
                    {
                        var simpleMatch = simplePathLine.Match(line);
                        if (simpleMatch.Success)
                        {
                            string path = simpleMatch.Groups["path"].Value;
                            currentList.Add((path, new Dictionary<string, string>()));
                        }
                    }
                }
            }
        }
    }

    private void ParseTileGrid(string[] lines)
    {
        var tileBlockStart = new Regex(pattern : @"^\((\d+),(\d+),(\d+)\)\s*=\s*\{\s*""");
        int x = 0, y = 0, z = 0;
        bool readingBlock = false;
        List<string> currentBlock = new();

        foreach (var line in lines)
        {
            if (tileBlockStart.IsMatch(input : line))
            {
                var match = tileBlockStart.Match(input : line);
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
                        string key = currentBlock[row].Trim();
                        if (!templates.ContainsKey(key))
                            continue;

                        var tile = new Tile
                        {
                            X = x,
                            Y = row + 1,
                            Z = z
                        };

                        var pathList = templates[key];
                        for (int i = 0; i < pathList.Count; i++)
                        {
                            tile.PathContent[i] = pathList[i].path;

                            // Добавим переопределения
                            foreach (var pair in pathList[i].Item2)
                            {
                                tile.PathOverrides[$"{i}.{pair.Key}"] = pair.Value;
                            }
                        }

                        tiles.Add(tile);
                    }

                    continue;
                }

                currentBlock.Add(line);
            }
        }
    }
}

}
