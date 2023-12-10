using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CloudPillar.Agent.Wrappers
{
    public interface IWindowsServiceWrapper
    {
        void InstallWindowsService();
    }
}