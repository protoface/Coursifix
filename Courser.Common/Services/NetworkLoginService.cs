using Courser.Common.Interfaces;
using Courser.Common.Models;
using Microsoft.Extensions.Options;

namespace Courser.Common.Services;

internal sealed class NetworkLoginService(IOptions<CourseConfig> courseConfig, HttpClient client) : INetworkLoginService
{
    private CourseConfig Config => courseConfig.Value;

    public async Task<Result> Login()
    {
        try
        {
            if (await client.GetAsync(Config.LoginUri) is { IsSuccessStatusCode: false } getResponse)
                return new Error($"Fetch of captive portal failed: {getResponse}");

            return await client.PostAsync(Config.LoginUri, new FormUrlEncodedContent([
                new("buttonClicked", "4"),
                new("redirect_url", Config.LoginRedirectUri.ToString()),
                new("err_flag", "0"),
                new("username", Config.WlanUserName),
                new("password", Config.WlanPassword)
            ])) is { IsSuccessStatusCode: false } postResponse
                ? new Error($"Login failed: {postResponse}")
                : new Ok();
        }
        catch (Exception ex)
        {
            return (Error)ex;
        }
    }
}
