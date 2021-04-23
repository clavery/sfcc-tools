using System.Threading.Tasks;

namespace SFCCTools.AccountManager
{
    public interface IAccessTokenAccessor
    {
        AccessToken Token { get; }
        bool ValidateAccessToken();
        Task RenewAccessTokenAsync();
    }

    public interface IBusinessManagerAccessTokenAccessor : IAccessTokenAccessor
    {
    }
    public interface IAccountManagerAccessTokenAccessor : IAccessTokenAccessor
    {
    }
}