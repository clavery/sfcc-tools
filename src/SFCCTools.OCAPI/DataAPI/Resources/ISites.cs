using System.Collections.Generic;
using System.Threading.Tasks;
using Refit;
using SFCCTools.OCAPI.DataAPI.Types;

namespace SFCCTools.OCAPI.DataAPI.Resources
{
    public interface ISites
    {
        [Get("/sites?select=(**)")]
        Task<DataCollection<Site>> GetAll();
    }
}