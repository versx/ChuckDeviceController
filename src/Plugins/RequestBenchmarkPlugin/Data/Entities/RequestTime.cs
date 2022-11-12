namespace RequestBenchmarkPlugin.Data.Entities
{
    using System.ComponentModel;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    using Utilities;

    [Table("timing")]
    public class RequestTime
    {
        /// <summary>
        /// Gets or sets the route path of the request.
        /// </summary>
        [
            DisplayName("Route"),
            Column("route"),
            Key,
        ]
        public string Route { get; set; }

        /// <summary>
        /// Gets or sets the total number of requests made to the route.
        /// </summary>
        [
            DisplayName("Requests"),
            DisplayFormat(DataFormatString = "{0:N0}"),
            Column("requests"),
        ]
        public uint Requests { get; set; }

        /// <summary>
        /// Gets or sets the total number of milliseconds used for the request that was the slowest.
        /// </summary>
        [
            DisplayName("Slowest"),
            Column("slowest"),
        ]
        public decimal Slowest { get; set; }

        /// <summary>
        /// Gets or sets the total number of milliseconds used for the request that was
        /// the quickest.
        /// </summary>
        [
            DisplayName("Fastest"),
            Column("fastest"),
        ]
        public decimal Fastest { get; set; }

        /// <summary>
        /// Gets or sets the average number of milliseconds per request to the route.
        /// </summary>
        [
            DisplayName("Average"),
            Column("average"),
        ]
        public decimal Average { get; set; }

        /// <summary>
        /// Gets or sets the total number of request times to the route.
        /// </summary>
        [
            DisplayName("Total"),
            Column("total"),
        ]
        public decimal Total { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="benchmark"></param>
        /// <returns></returns>
        public static RequestTime FromRequestBenchmark(RequestBenchmark benchmark)
        {
            var model = new RequestTime
            {
                Route = benchmark.Route,
                Requests = benchmark.Requests,
                Fastest = benchmark.Fastest,
                Slowest = benchmark.Slowest,
                Average = benchmark.Average,
                Total = benchmark.Total,
            };
            return model;
        }
    }
}