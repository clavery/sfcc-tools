using System.Threading.Tasks;
using Refit;
using SFCCTools.OCAPI.DataAPI.Types;

namespace SFCCTools.OCAPI.DataAPI.Resources
{
    public static class CodeVersionsExtensions
    {
        public static async Task<bool> ActivateCodeVersion(this ICodeVersions codeVersionsClient, string id)
        {
            try
            {
                var result = await codeVersionsClient.Update(id, new CodeVersion()
                {
                    Active = true,
                    // only the active field is appropriate for this call
                    LastModificationTime = null,
                    ActivationTime = null
                });
                return result.Active;
            }
            catch (ApiException ex)
            {
                return false;
            }
        }
    }
    public interface ICodeVersions
    {
        [Get("/code_versions")]
        Task<DataCollection<CodeVersion>> GetAll();
        
        [Patch("/code_versions/{id}")]
        Task<CodeVersion> Update(string id, [Body] CodeVersion codeVersion);
        
        [Delete("/code_versions/{id}")]
        Task Delete(string id);
    }
}