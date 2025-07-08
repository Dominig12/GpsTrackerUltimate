using System.IO;
using System.Text.RegularExpressions;
using GPSTrackerUltimate.Types.Byond;

namespace GPSTrackerUltimate.Types
{

    public static class DmParser
    {
        private static readonly Regex ObjectHeaderRegex = new(pattern : @"^/([a-zA-Z0-9_/]+)$", options : RegexOptions.Compiled);
        private static readonly Regex VariableAssignmentRegex = new(pattern : @"^([a-zA-Z0-9_/]+)\s*=\s*(.+)$", options : RegexOptions.Compiled);

        /// <summary>
        /// Парсит все объекты с построением дерева
        /// </summary>
        public static Dictionary<string, DmObject> ParseObjectsFromFiles(IEnumerable<string> dmFilePaths)
        {
            Dictionary<string, DmObject>? allObjects = new Dictionary<string, DmObject>();
            DmObject? current = null;

            foreach (string? file in dmFilePaths)
            {
                string[]? lines = File.ReadAllLines(path : file);

                foreach (string? rawLine in lines)
                {
                    string? line = rawLine.Trim();

                    if (string.IsNullOrWhiteSpace(value : line) || line.StartsWith(value : "//"))
                    {
                        continue;
                    }

                    if (line.StartsWith(value : "/"))
                    {
                        string[]? tokens = line.Split(separator : new[] { ' ', '\t', '(', '=', ':', ';' }, options : StringSplitOptions.RemoveEmptyEntries);
                        if (tokens.Length == 0)
                        {
                            continue;
                        }

                        string? potentialPath = tokens[0];

                        if (ObjectHeaderRegex.IsMatch(input : potentialPath))
                        {
                            string? fullPath = potentialPath;
                            string? parent = "/" + string.Join(separator : "/", values : fullPath.Split(separator : '/').Skip(count : 1).SkipLast(count : 1));

                            if (allObjects.TryGetValue(key : fullPath, value : out DmObject? existing))
                            {
                                // Добавляем или перезаписываем переменные
                                current = existing;
                                current.ParentPath = parent; // убедимся, что родитель правильный (вдруг другой файл точнее)
                            }
                            else
                            {
                                current = new DmObject
                                {
                                    Path = fullPath,
                                    ParentPath = parent
                                };
                                allObjects[key : fullPath] = current;
                            }

                            continue;
                        }
                    }

                    if (current != null)
                    {
                        Match? match = VariableAssignmentRegex.Match(input : line);
                        if (match.Success)
                        {
                            string? name = match.Groups[groupnum : 1].Value.Trim();
                            string? rawValue = match.Groups[groupnum : 2].Value.Trim().TrimEnd(trimChar : ';');

// Удаляем комментарии, если они не внутри строки
                            int commentIndex = rawValue.IndexOf(value : "//");
                            if (commentIndex >= 0)
                            {
                                // Убедимся, что // не находится внутри кавычек
                                bool insideQuotes = false;
                                for (int i = 0; i < commentIndex; i++)
                                {
                                    if (rawValue[index : i] == '"')
                                    {
                                        insideQuotes = !insideQuotes;
                                    }
                                }

                                if (!insideQuotes)
                                {
                                    rawValue = rawValue.Substring(startIndex : 0, length : commentIndex).TrimEnd();
                                }
                            }

                            current.Variables[key : name] = rawValue;
                        }
                    }
                }

                current = null;
            }

            // Построение дерева
            foreach (DmObject? obj in allObjects.Values)
            {
                if (allObjects.TryGetValue(key : obj.ParentPath, value : out DmObject? parent))
                {
                    parent.Children.Add(item : obj);
                }
            }

            return allObjects;
        }

        /// <summary>
        /// Находит объект по пути и возвращает его с унаследованными параметрами
        /// </summary>
        public static (DmObject Obj, Dictionary<string, string> ResolvedVars)? FindObjectWithResolvedVars(
            string typePath,
            Dictionary<string, DmObject> allObjects)
        {
            if (allObjects.TryGetValue(key : typePath, value : out DmObject? obj))
            {
                Dictionary<string, string>? vars = obj.GetAllResolvedVariables(allObjects : allObjects);
                return (obj, vars);
            }

            return null;
        }
    }

}
