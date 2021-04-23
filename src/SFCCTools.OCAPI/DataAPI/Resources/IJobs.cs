using System.Threading.Tasks;
using Refit;
using SFCCTools.OCAPI.DataAPI.Types;

namespace SFCCTools.OCAPI.DataAPI.Resources
{
    public interface IJobs
    {
        [Post("/jobs/{JobId}/executions")]
        Task<JobExecution> Create(string jobId);
        
        [Post("/jobs/{JobId}/executions/{Id}")]
        Task<JobExecution> Get(string jobId, string id);
        
        [Post("/jobs/sfcc-site-archive-export/executions")]
        Task<JobExecution> SiteArchiveExport(SiteArchiveExportConfiguration configuration);
        
        [Post("/jobs/sfcc-site-archive-import/executions")]
        Task<JobExecution> SiteArchiveImport(SiteArchiveImportConfiguration configuration);
    }
}