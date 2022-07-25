namespace ChuckDeviceConfigurator.Services.Rpc
{
    using Grpc.Core;

    using ChuckDeviceConfigurator.Services.Jobs;
    using ChuckDeviceController.Protos;

    public class TrainerInfoServerService : Leveling.LevelingBase
    {
        #region Variables

        private readonly ILogger<TrainerInfoServerService> _logger;
        private readonly IJobControllerService _jobControllerService;

        #endregion

        #region Constructor

        public TrainerInfoServerService(
            ILogger<TrainerInfoServerService> logger,
            IJobControllerService jobControllerService)
        {
            _logger = logger;
            _jobControllerService = jobControllerService;
        }

        #endregion

        public override async Task<TrainerInfoResponse> ReceivedTrainerInfo(TrainerInfoRequest request, ServerCallContext context)
        {
            _logger.LogInformation($"Received {request.Username} request for trainer info");

            var username = request.Username;
            var result = _jobControllerService.GetTrainerLevelingStatus(username);
            var response = new TrainerInfoResponse
            {
                Status = TrainerInfoStatus.Ok,
                Username = username,
                StoreLevelingData = result.StoreLevelingData,
                IsLeveling = result.IsTrainerLeveling,
            };
            return await Task.FromResult(response);
        }
    }
}