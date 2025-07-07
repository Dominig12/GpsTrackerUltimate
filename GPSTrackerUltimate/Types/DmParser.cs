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
            var allObjects = new Dictionary<string, DmObject>();
            DmObject? current = null;

            foreach (var file in dmFilePaths)
            {
                var lines = File.ReadAllLines(path : file);

                foreach (var rawLine in lines)
                {
                    var line = rawLine.Trim();

                    if (string.IsNullOrWhiteSpace(value : line) || line.StartsWith(value : "//"))
                    {
                        continue;
                    }

                    if (line.StartsWith(value : "/"))
                    {
                        var tokens = line.Split(separator : new[] { ' ', '\t', '(', '=', ':', ';' }, options : StringSplitOptions.RemoveEmptyEntries);
                        if (tokens.Length == 0)
                        {
                            continue;
                        }

                        var potentialPath = tokens[0];

                        if (ObjectHeaderRegex.IsMatch(potentialPath))
                        {
                            var fullPath = potentialPath;
                            var parent = "/" + string.Join("/", fullPath.Split('/').Skip(1).SkipLast(1));

                            if (allObjects.TryGetValue(fullPath, out var existing))
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
                                allObjects[fullPath] = current;
                            }

                            continue;
                        }
                    }

                    if (current != null)
                    {
                        var match = VariableAssignmentRegex.Match(input : line);
                        if (match.Success)
                        {
                            var name = match.Groups[groupnum : 1].Value.Trim();
                            var rawValue = match.Groups[2].Value.Trim().TrimEnd(';');

// Удаляем комментарии, если они не внутри строки
                            int commentIndex = rawValue.IndexOf("//");
                            if (commentIndex >= 0)
                            {
                                // Убедимся, что // не находится внутри кавычек
                                bool insideQuotes = false;
                                for (int i = 0; i < commentIndex; i++)
                                {
                                    if (rawValue[i] == '"')
                                        insideQuotes = !insideQuotes;
                                }

                                if (!insideQuotes)
                                {
                                    rawValue = rawValue.Substring(0, commentIndex).TrimEnd();
                                }
                            }

                            current.Variables[name] = rawValue;
                        }
                    }
                }

                current = null;
            }

            // Построение дерева
            foreach (var obj in allObjects.Values)
            {
                if (allObjects.TryGetValue(key : obj.ParentPath, value : out var parent))
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
            if (allObjects.TryGetValue(key : typePath, value : out var obj))
            {
                var vars = obj.GetAllResolvedVariables(allObjects : allObjects);
                return (obj, vars);
            }

            return null;
        }
    }

}
