using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedflyCoreFramework;
public static class ClientSessionId
{

    public static string Generate()
    {
        // Combine the machine name and a GUID to ensure uniqueness
        string machineName = Environment.MachineName;
        string guid = "9052b6a0-03bf-4f36-b811-e7038ef1b692";

        // Hash the combination for a consistent length (optional)
        using (var sha256 = System.Security.Cryptography.SHA256.Create())
        {
            var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes($"{machineName}-{guid}"));
            return Convert.ToBase64String(hashBytes).Substring(0, 32); // Truncate for readability
        }
    }

}
