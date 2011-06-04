using System;
using Hyperion.Messages;

namespace Hyperion.Samples.Common
{
    [Serializable]
    public class Message : IMessage
    {
        public string From { get; set; }
        public string Text { get; set; }
    }
}