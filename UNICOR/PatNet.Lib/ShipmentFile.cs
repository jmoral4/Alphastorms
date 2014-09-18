namespace PatNet.Lib
{
    public class ShipmentFile
    {
        public string Name { get; set; } //patent name
        public string FileName { get; set; }
        public int PageCount { get; set; } //optional - not in shipA
        public int HeaderClaims { get; set; } // optional - not in shipA
        public bool IsPresent { get; set; }
        public bool HasImage { get; set; }
        //other uses .. for tracking during processing
        // WasPresent, Successful, etc
    }
}