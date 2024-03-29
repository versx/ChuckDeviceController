﻿namespace ChuckDeviceConfigurator.Services.Rpc;

using Grpc.Core;

using ChuckDeviceConfigurator.Services.Jobs;
using ChuckDeviceController.Authorization.Jwt.Attributes;
using ChuckDeviceController.Protos;

[JwtAuthorize(Strings.Identifier)]
public class TrainerInfoServerService : Leveling.LevelingBase
{
    private readonly ILogger<TrainerInfoServerService> _logger;
    private readonly IJobControllerService _jobControllerService;

    public TrainerInfoServerService(
        ILogger<TrainerInfoServerService> logger,
        IJobControllerService jobControllerService)
    {
        _logger = logger;
        _jobControllerService = jobControllerService;
    }

    public override async Task<TrainerInfoResponse> HandleTrainerInfo(TrainerInfoRequest request, ServerCallContext context)
    {
        _logger.LogDebug($"Received {request.Username} request for trainer info");
        var username = request?.Username;
        var errorResponse = Task.FromResult(new TrainerInfoResponse
        {
            Status = TrainerInfoStatus.Error,
            Username = username,
            StoreLevelingData = true,
            IsLeveling = false,
        });

        if (string.IsNullOrEmpty(username))
        {
            _logger.LogWarning($"Trainer username was null, unable to fetch trainer info.");
            return await errorResponse;
        }

        try
        {
            var result = _jobControllerService.GetTrainerLevelingStatus(username);
            if (result == null)
            {
                _logger.LogWarning($"Trainer leveling status for username '{username}' from job controller service returned null, unable to fetch trainer info.");
                return await errorResponse;
            }

            return await Task.FromResult(new TrainerInfoResponse
            {
                Status = TrainerInfoStatus.Ok,
                Username = username,
                StoreLevelingData = result.StoreLevelingData,
                IsLeveling = result.IsTrainerLeveling,
            });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error: {ex}");
        }
        return await errorResponse;
    }
}