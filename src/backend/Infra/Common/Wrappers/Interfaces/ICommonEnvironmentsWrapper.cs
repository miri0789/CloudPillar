﻿namespace Backend.Infra.Common.Wrappers.Interfaces;
public interface ICommonEnvironmentsWrapper
{
    int retryPolicyBaseDelay { get; }
    int retryPolicyExponent { get; }
    string iothubConnectionString { get; }
    string keyHolderUrl { get; }
    string blobStreamerUrl { get; }

}
