using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.ServiceModel;
using System.Threading.Tasks;

namespace PortiLog
{
    /// <summary>
    /// Defines the client of the move service.
    /// </summary>
    public static class ServiceClient
    {
        public static IService CreateChannel(string url)
        {
            BasicHttpBinding binding = new BasicHttpBinding();
            binding.MaxReceivedMessageSize = 2147483647;
            binding.MaxBufferSize = 2147483647;

            EndpointAddress endpoint = new EndpointAddress(url);

            ChannelFactory<IService> factory = new ChannelFactory<IService>(binding, endpoint);

            // Create a channel.
            IService channel = factory.CreateChannel(endpoint);
            return channel;
        }
    }
}