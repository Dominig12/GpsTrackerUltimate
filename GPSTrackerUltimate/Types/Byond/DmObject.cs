namespace GPSTrackerUltimate.Types.Byond
{

    public class DmObject
    {
        public string Path { get; set; } = "";
        public string ParentPath { get; set; } = "";
        public Dictionary<string, string> Variables { get; set; } = new();
        public List<DmObject> Children { get; set; } = new();

        /// <summary>
        /// Рекурсивно получает все переменные, включая унаследованные от родителей
        /// </summary>
        public Dictionary<string, string> GetAllResolvedVariables(Dictionary<string, DmObject> allObjects)
        {
            var resolved = new Dictionary<string, string>();

            if (allObjects.TryGetValue(key : ParentPath, value : out var parent))
            {
                foreach (var kv in parent.GetAllResolvedVariables(allObjects : allObjects))
                {
                    resolved[key : kv.Key] = kv.Value;
                }
            }

            // Переопределение
            foreach (var kv in Variables)
            {
                resolved[key : kv.Key] = kv.Value;
            }

            return resolved;
        }
    }

}
