using System.Text;
using Microsoft.Azure.Devices.Client;
using Newtonsoft.Json.Linq;
using shared.Entities.Messages;

namespace CloudPillar.Agent.Interfaces;
public interface IC2DSubscriptionHandler
{
    Task Subscribe(CancellationToken cancellationToken);
    void Unsubscribe();

    bool CheckSubscribed();
}
