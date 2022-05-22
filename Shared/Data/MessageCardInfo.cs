using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Draw.Rodeo.Shared.Data
{
    public enum MessageType
    {
        System,
        Self,
        Other
    }

    public class MessageCardInfo
    {
        public MessageType Type { get; set; }
        public MessageInfo MessageInfo { get; set; }

        public MessageCardInfo()
        {
            Type = MessageType.System;
            MessageInfo = new MessageInfo();
        }
    }
}
