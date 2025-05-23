using Grpc.Core;
using redflyDatabaseAdapters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace redflyDatabaseAdapters;

public class AppGrpcSession
{

    public static Metadata? Headers { get; set; }

    public static string GrpcUrl { get; set; } = "https://hosted-chakra-grpc-linux.azurewebsites.net/";

    public static SyncProfileViewModel? SyncProfile { get; set; }

    public static AddClientAndUserProfileViewModel? ClientAndUserProfileViewModel { get; set; }

}
