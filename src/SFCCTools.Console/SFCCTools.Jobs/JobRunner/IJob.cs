using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace SFCCTools.Jobs
{
    public interface IJob
    {
        Task Run(CancellationToken cancellationToken);
    }
}