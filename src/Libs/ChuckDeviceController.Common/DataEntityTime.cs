namespace ChuckDeviceController.Common
{
    using System.ComponentModel;
    using System.ComponentModel.DataAnnotations;
    using System.Text.Json.Serialization;

    public class DataEntityTime : IComparable<DataEntityTime>
    {
        [JsonPropertyName("id")]
        public Guid Id { get; set; }

        [JsonPropertyName("count")]
        [DisplayName("Count")]
        [DisplayFormat(DataFormatString = "{0:N0}")]
        public ulong Count { get; set; }

        [JsonPropertyName("time_s")]
        [DisplayName("Time (sec)")]
        public double TimeS { get; set; }

        public DataEntityTime(ulong count, double timeS)
        {
            Id = Guid.NewGuid();
            Count = count;
            TimeS = timeS;
        }

        public int CompareTo(DataEntityTime? other)
        {
            if (other == null)
                return -1;

            if (other.Id != Id)
                return -1;

            return 0;
        }
    }
}