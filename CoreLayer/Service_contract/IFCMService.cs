using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreLayer.Service_contract
{
    public interface IFCMService
    {
        Task<bool> SendNotificationAsync(string deviceToken, string title, string body, Dictionary<string, string> data = null);
        Task<bool> SendNotificationToMultipleAsync(List<string> deviceTokens, string title, string body, Dictionary<string, string> data = null);
    }
}
