namespace ChuckDeviceController.Common
{
    using System.ComponentModel;
    using System.Text.Json.Serialization;

    public class DataEntityTime : IComparable<DataEntityTime>
    {
        [JsonPropertyName("id")]
        public Guid Id { get; }

        [JsonPropertyName("count")]
        [DisplayName("Count")]
        public ulong Count { get; }

        [JsonPropertyName("time_s")]
        [DisplayName("Time (sec)")]
        public double TimeS { get; }

        public DataEntityTime()
        {
            Id = Guid.NewGuid();
        }

        public DataEntityTime(ulong count, double timeS)
            : this()
        {
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