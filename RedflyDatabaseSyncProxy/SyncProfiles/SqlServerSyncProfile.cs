using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedflyDatabaseSyncProxy.SyncProfiles;
internal class SqlServerSyncProfile
{

    internal static bool Exists(GetSyncProfilesResponse getSyncProfilesResponse)
    {
        return (getSyncProfilesResponse.Success &&
                getSyncProfilesResponse.Profiles != null &&
                getSyncProfilesResponse.Profiles.Count > 0);
    }

}
