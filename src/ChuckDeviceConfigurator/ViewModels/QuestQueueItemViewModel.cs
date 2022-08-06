namespace ChuckDeviceConfigurator.ViewModels
{
    using System.ComponentModel;

    public class QuestQueueViewModel
    {
        public string? Name { get; set; }

        public List<QuestQueueItemViewModel> Queue { get; set; } = new();

        public bool AutoRefresh { get; set; }
    }

    public class QuestQueueItemViewModel
    {
        [DisplayName("Image")]
        public string? Image { get; set; }

        [DisplayName("ID")]
        public string Id { get; set; }

        [DisplayName("Name")]
        public string? Name { get; set; }

        [DisplayName("Is Alternative")]
        public bool IsAlternative { get; set; }

        [DisplayName("Latitude")]
        public double Latitude { get; set; }

        [DisplayName("Longitude")]
        public double Longitude { get; set; }
    }
}
