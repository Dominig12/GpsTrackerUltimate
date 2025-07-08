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
            Dictionary<string, string>? resolved = new Dictionary<string, string>();

            if (allObjects.TryGetValue(key : ParentPath, value : out DmObject? parent))
            {
                foreach (KeyValuePair<string, string> kv in parent.GetAllResolvedVariables(allObjects : allObjects))
                {
                    resolved[key : kv.Key] = kv.Value;
                }
            }

            // Переопределение
            foreach (KeyValuePair<string, string> kv in Variables)
            {
                resolved[key : kv.Key] = kv.Value;
            }

            return resolved;
        }
    }

}
