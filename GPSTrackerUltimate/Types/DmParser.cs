using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using GPSTrackerUltimate.Types.Byond;

public static class DmParser
{
    private static readonly Regex CommentsInLine = new(@"//(?=(?:[^""]*""[^""]*"")*[^""]*$).*", RegexOptions.Compiled);
    private static readonly Regex CommentsMultiline = new(@"/\*[\s\S]*?\*/", RegexOptions.Compiled);
    private static readonly Regex FullBlock = new Regex(
        @"^(/[^()\r\n]+)[\r\n]+((?:^[ \t]+.*[\r\n]*)*)",
        RegexOptions.Multiline | RegexOptions.Compiled);

    private static readonly Regex VariableRegex = new Regex(
        @"^[ \t]*([a-zA-Z0-9_/]+)\s*=\s*(.+)$",
        RegexOptions.Multiline | RegexOptions.Compiled);
    public static Dictionary<string, DmObject> ParseObjectsFromFiles(IEnumerable<string> filePaths)
    {
        var allObjects = new Dictionary<string, DmObject>();

        foreach (var file in filePaths)
        {
            var fileContent = File.ReadAllText(file);

            fileContent = CommentsMultiline.Replace(fileContent, "");
            fileContent = CommentsInLine.Replace(fileContent, "");

            var matches = FullBlock.Matches(fileContent);

            foreach (Match match in matches)
            {
                string objectPath = match.Groups[1].Value.Trim();
                string body = match.Groups[2].Value;

                // Определяем родителя
                string parentPath = "/";
                var parts = objectPath.Split('/');
                if (parts.Length > 2)
                    parentPath = "/" + string.Join("/", parts.Skip(1).Take(parts.Length - 2));

                if (!allObjects.TryGetValue(objectPath, out var obj))
                {
                    obj = new DmObject
                    {
                        Path = objectPath,
                        ParentPath = parentPath
                    };
                    allObjects[objectPath] = obj;
                }
                else
                {
                    obj.ParentPath = parentPath;
                }

                var varMatches = VariableRegex.Matches(body);
                foreach (Match vm in varMatches)
                {
                    string varName = vm.Groups[1].Value.Trim();
                    if (varName.StartsWith("var/"))
                        varName = varName.Substring(4);

                    string varValue = vm.Groups[2].Value.Trim().TrimEnd(';').Trim();
                    varValue = RemoveCommentOutsideQuotes(varValue);

                    if (Regex.IsMatch(varValue, @"^\w+\s*\(.*\)$"))
                        continue;

                    obj.Variables[varName] = varValue;
                }
            }
        }
        
        // --- Новый блок для создания промежуточных объектов ---

        var allPaths = allObjects.Keys.ToList();
        foreach (var path in allPaths)
        {
            var parts = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
            for (int depth = 1; depth < parts.Length; depth++)
            {
                string parentPath = "/" + string.Join("/", parts.Take(depth));
                string childPath = "/" + string.Join("/", parts.Take(depth + 1));

                if (!allObjects.ContainsKey(parentPath))
                {
                    // Создаем промежуточный объект с пустыми переменными и ParentPath
                    string grandParentPath = "/";
                    if (depth > 1)
                        grandParentPath = "/" + string.Join("/", parts.Take(depth - 1));

                    var intermediate = new DmObject
                    {
                        Path = parentPath,
                        ParentPath = grandParentPath
                    };
                    allObjects[parentPath] = intermediate;
                }
            }
        }
        
        // --- Построение дерева детей ---
        foreach (var obj in allObjects.Values)
        {
            if (!string.IsNullOrEmpty(obj.ParentPath) && allObjects.TryGetValue(obj.ParentPath, out var parent))
                parent.Children[obj.Path] = obj;
        }

        ResolveAllVariablesIteratively(allObjects);
        return allObjects;
    }

    private static string RemoveCommentOutsideQuotes(string line)
    {
        int commentIndex = line.IndexOf("//");
        while (commentIndex >= 0)
        {
            if (!IsInsideQuotes(line, commentIndex))
            {
                line = line.Substring(0, commentIndex).TrimEnd();
                break;
            }
            commentIndex = line.IndexOf("//", commentIndex + 2);
        }
        return line;
    }

    private static void ResolveAllVariablesIteratively(Dictionary<string, DmObject> allObjects)
    {
        var resolved = new HashSet<string>();
        var queue = new Queue<DmObject>();

        // Находим все корни (без родителя или с несуществующим родителем)
        foreach (var obj in allObjects.Values)
        {
            if (string.IsNullOrEmpty(obj.ParentPath) || !allObjects.ContainsKey(obj.ParentPath))
                queue.Enqueue(obj);
        }

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();

            if (!string.IsNullOrEmpty(current.ParentPath) &&
                allObjects.TryGetValue(current.ParentPath, out var parent) &&
                !resolved.Contains(parent.Path))
            {
                queue.Enqueue(current);
                continue;
            }

            var resolvedVars = new Dictionary<string, string>();

            if (!string.IsNullOrEmpty(current.ParentPath) &&
                allObjects.TryGetValue(current.ParentPath, out parent))
            {
                foreach (var kv in parent.ResolvedVariables)
                    resolvedVars[kv.Key] = kv.Value;
            }

            foreach (var kv in current.Variables)
                resolvedVars[kv.Key] = kv.Value;

            current.ResolvedVariables = resolvedVars;
            resolved.Add(current.Path);

            foreach (var child in current.Children.Values)
                queue.Enqueue(child);
        }
    }

    private static bool IsInsideQuotes(string text, int index)
    {
        bool inside = false;
        for (int i = 0; i < index; i++)
        {
            if (text[i] == '"')
                inside = !inside;
        }
        return inside;
    }
}
