using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace JabbR.Services
{
    public interface IJabbrConfiguration
    {
        bool RequireHttps { get; }
        bool MigrateDatabase { get; }

        string DeploymentSha { get; }
        string DeploymentBranch { get; }
        string DeploymentTime { get; }
    }
}