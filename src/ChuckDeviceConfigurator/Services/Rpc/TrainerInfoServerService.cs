namespace ChuckDeviceConfigurator.Services.Rpc
{
    using Grpc.Core;

    using ChuckDeviceConfigurator.Attributes;
    using ChuckDeviceConfigurator.Services.Jobs;
    using ChuckDeviceController.Protos;

    [JwtAuthorize]
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
            _logger.LogDebug($"Received {request.Username} request for trainer info");

            var username = request.Username;
            if (string.IsNullOrEmpty(username))
            {
                _logger.LogWarning($"Trainer username was null, unable to fetch trainer info.");
                return await Task.FromResult(new TrainerInfoResponse
                {
                    Status = TrainerInfoStatus.Error,
                });
            }

            var result = _jobControllerService.GetTrainerLevelingStatus(username);
            if (result == null)
            {
                _logger.LogWarning($"Trainer leveling status for username '{username}' from job controller service returned null, unable to fetch trainer info.");
                return await Task.FromResult(new TrainerInfoResponse
                {
                    Status = TrainerInfoStatus.Error,
                    Username = username,
                });
            }

            return await Task.FromResult(new TrainerInfoResponse
            {
                Status = TrainerInfoStatus.Ok,
                Username = username,
                StoreLevelingData = result.StoreLevelingData,
                IsLeveling = result.IsTrainerLeveling,
            });
        }
    }
}