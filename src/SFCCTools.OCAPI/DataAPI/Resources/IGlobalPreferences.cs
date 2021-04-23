using System.Threading.Tasks;
using Refit;
using SFCCTools.OCAPI.DataAPI.Types;

namespace SFCCTools.OCAPI.DataAPI.Resources
{
    public interface IGlobalPreferences
    {
        [Get("/global_preferences/preference_groups/{groupId}/{instanceType}")]
        Task<OrganizationPreferences> Get(string groupId, string instanceType);
        
        [Patch("/global_preferences/preference_groups/{groupId}/{instanceType}")]
        Task<OrganizationPreferences> Update(string groupId, string instanceType, [Body] OrganizationPreferences organizationPreferences);
    }
}