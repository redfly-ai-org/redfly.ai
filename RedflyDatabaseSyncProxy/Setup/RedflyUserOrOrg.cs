using Grpc.Core;
using Grpc.Net.Client;
using RedflyCoreFramework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedflyDatabaseSyncProxy.Setup;

internal class RedflyUserOrOrg
{

    internal static async Task<bool> Setup(GrpcChannel channel, Metadata headers)
    {
        CancellationTokenSource cts;
        Task progressTask;

        var userSetupApiClient = new UserSetupApi.UserSetupApiClient(channel);
        ServiceResponse? getUserSetupDataResponse = null;

        cts = new CancellationTokenSource();
        progressTask = RedflyConsole.ShowWaitAnimation(cts.Token);

        try
        {
            getUserSetupDataResponse = await GetUserSetupData(userSetupApiClient, headers);
        }
        finally
        {
            cts.Cancel();
            await progressTask;
        }

        if (IsSetupRequired(getUserSetupDataResponse))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(getUserSetupDataResponse.Message);
            Console.ResetColor();

            ServiceValueResponse? addOrUpdateClientAndUserProfileResponse = null;

            cts = new CancellationTokenSource();
            progressTask = RedflyConsole.ShowWaitAnimation(cts.Token);

            try
            {
                addOrUpdateClientAndUserProfileResponse = await PromptUserToSetup(userSetupApiClient, headers);
            }
            finally
            {
                cts.Cancel();
                await progressTask;
            }

            if (!addOrUpdateClientAndUserProfileResponse.Success)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(addOrUpdateClientAndUserProfileResponse.Message);
                Console.WriteLine("User Account and Organization setup could NOT be completed successfully. Please try again later");
                Console.ResetColor();
                return false;
            }

            if (addOrUpdateClientAndUserProfileResponse.Success)
            {
                Console.WriteLine(addOrUpdateClientAndUserProfileResponse.Message);
                Console.WriteLine("User Account and Organization setup completed successfully.");

                //Reload data, so it can be used.
                getUserSetupDataResponse = await GetUserSetupData(userSetupApiClient, headers);
            }
        }

        if (getUserSetupDataResponse.Result != null)
        {
            AppSession.ClientAndUserProfileViewModel = getUserSetupDataResponse.Result;
        }

        return true;
    }


    private static async Task<ServiceResponse> GetUserSetupData(
        UserSetupApi.UserSetupApiClient userSetupApiClient, 
        Metadata headers)
    {
        const int maxRetryAttempts = 5;
        const int delayMilliseconds = 1000;

        for (int attempt = 1; attempt <= maxRetryAttempts; attempt++)
        {
            try
            {
                Console.WriteLine($"\rAttempt {attempt}: Getting User Setup data from the server...");
                var getUserSetupDataResponse = await userSetupApiClient
                                                    .GetUserSetupDataAsync(new UserIdRequest
                                                    {
                                                        UserId = Guid.NewGuid().ToString()
                                                    }, headers);

                Console.WriteLine("\rSuccessfully retrieved User Setup data.");
                return getUserSetupDataResponse;
            }
            catch (RpcException ex) when (attempt < maxRetryAttempts)
            {
                //Console.ForegroundColor = ConsoleColor.Red;
                //Console.WriteLine($"Attempt {attempt} failed: {ex.Message}. Retrying in {(delayMilliseconds * attempt)/1000} secs...");
                //Console.ResetColor();

                await Task.Delay(delayMilliseconds * attempt);
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"An unexpected error occurred: {ex.Message}");
                Console.ResetColor();
                throw;
            }
        }

        throw new Exception("Failed to fetch user setup data after multiple attempts.");
    }



    private static bool IsSetupRequired(ServiceResponse getUserSetupDataResponse)
    {
        return (!getUserSetupDataResponse.Success ||
                getUserSetupDataResponse.Result == null ||
                getUserSetupDataResponse.Result.IsFreshNewUser ||
                string.IsNullOrEmpty(getUserSetupDataResponse.Result.UserFirstName) ||
                string.IsNullOrEmpty(getUserSetupDataResponse.Result.UserLastName) ||
                string.IsNullOrEmpty(getUserSetupDataResponse.Result.ClientName));
    }

    private static async Task<ServiceValueResponse> PromptUserToSetup(UserSetupApi.UserSetupApiClient userSetupApiClient, Metadata headers)
    {
        var viewModel = new AddClientAndUserProfileViewModel();

        //The user account and organization have to be setup first.
        Console.WriteLine("Please setup your User Account and Organization to proceed further.");

        do
        {
            Console.WriteLine("Please enter your First Name:");
            viewModel.UserFirstName = Console.ReadLine();
        }
        while (string.IsNullOrWhiteSpace(viewModel.UserFirstName));

        do
        {
            Console.WriteLine("Please enter your Last Name:");
            viewModel.UserLastName = Console.ReadLine();
        }
        while (string.IsNullOrWhiteSpace(viewModel.UserLastName));

        do
        {
            Console.WriteLine("Please enter your Organization Name:");
            viewModel.ClientName = Console.ReadLine();
        }
        while (string.IsNullOrWhiteSpace(viewModel.ClientName));

        var addOrUpdateClientAndUserProfileResponse = await userSetupApiClient.AddOrUpdateClientAndUserProfileAsync(new AddOrUpdateClientAndUserProfileRequest
        {
            Model = viewModel
        }, headers);
        return addOrUpdateClientAndUserProfileResponse;
    }


}
