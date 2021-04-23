using System.Threading.Tasks;
using Refit;
using SFCCTools.OCAPI.ShopAPI.Types;

namespace SFCCTools.OCAPI.ShopAPI.Resources
{
    public interface IOrders
    {
        [Get("/orders/{orderNo}")]
        Task<Order> GetOrder(string orderNo);
        
        [Post("/orders/{orderNo}/notes")]
        Task<Order> AddNote(string orderNo, [Body] Note orderNote);
    }

    public class Note
    {
        public string Subject;
        public string Text;
    }
}