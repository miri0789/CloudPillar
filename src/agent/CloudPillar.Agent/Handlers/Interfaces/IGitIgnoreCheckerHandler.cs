using CloudPillar.Agent.Entities;
using Shared.Entities.Twin;

namespace CloudPillar.Agent.Handlers;

public interface IGitIgnoreCheckerHandler
{
    bool IsIgnored(string path);
}