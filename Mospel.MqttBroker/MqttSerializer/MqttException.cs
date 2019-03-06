using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mospel.MqttSerializer
{
    public class MqttProtocolViolationException : Exception
    {
        public MqttProtocolViolationException(string message)
            : base(message)
        {
        }
    }
}
