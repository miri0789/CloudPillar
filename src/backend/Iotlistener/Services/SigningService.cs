﻿using Backend.Infra.Common;
using Backend.Iotlistener.Interfaces;
using Shared.Entities.Messages;
using Shared.Logger;

namespace Backend.Iotlistener.Services;


public class SigningService : ISigningService
{

    private readonly IHttpRequestorService _httpRequestorService;
    private readonly IEnvironmentsWrapper _environmentsWrapper;
    private readonly ILoggerHandler _logger;
    public SigningService(IHttpRequestorService httpRequestorService, IEnvironmentsWrapper environmentsWrapper, ILoggerHandler logger)
    { 
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _environmentsWrapper = environmentsWrapper ?? throw new ArgumentNullException(nameof(environmentsWrapper));
        _httpRequestorService = httpRequestorService ?? throw new ArgumentNullException(nameof(httpRequestorService));
    }

    public async Task CreateTwinKeySignature(string deviceId, SignEvent signEvent)
    {
        try
        {
            string requestUrl = $"{_environmentsWrapper.keyHolderUrl}Signing/createTwinKeySignature?deviceId={deviceId}";
            await _httpRequestorService.SendRequest(requestUrl, HttpMethod.Get);
        }
        catch (Exception ex)
        {
            _logger.Error($"SigningService CreateTwinKeySignature failed.", ex);
        }
    }

}
