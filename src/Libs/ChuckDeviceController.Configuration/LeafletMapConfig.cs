namespace ChuckDeviceController.Configuration
{
    public class LeafletMapConfig
    {
        public double StartLatitude { get; set; }

        public double StartLongitude { get; set; }

        public ushort StartZoom { get; set; }

        public ushort MinimumZoom { get; set; }

        public ushort MaximumZoom { get; set; }

        public string TileserverUrl { get; set; }

        public LeafletMapConfig()
        {
            StartLatitude = 0;
            StartLongitude = 0;
            StartZoom = 13;
            MinimumZoom = 4;
            MaximumZoom = 18;
            TileserverUrl = "https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png";
        }
    }
}
