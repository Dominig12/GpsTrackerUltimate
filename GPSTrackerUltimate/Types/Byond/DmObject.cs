namespace GPSTrackerUltimate.Types.Byond
{
    public class DmObject
    {
        public string Path { get; set; } = "";
        public string? ParentPath { get; set; }
        public Dictionary<string, string> Variables { get; set; } = new();
        public Dictionary<string, string> ResolvedVariables { get; set; } = new();
        public Dictionary<string, DmObject> Children { get; set; } = new();
    }

}
