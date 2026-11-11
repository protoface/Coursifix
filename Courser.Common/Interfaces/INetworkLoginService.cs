using Courser.Common.Models;

namespace Courser.Common.Interfaces;

public interface INetworkLoginService
{
    public Task<Result> Login();
}
