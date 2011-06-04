using System.IO;
using System.Collections.Generic;

namespace Hyperion.Messaging
{
    public interface IFileQueue
    {
        string GetNewFileName();
        string GetFilePath(string fileName);
        StreamWriter GetWriter(string fileName);
        StreamReader GetReader(string fileName);

        void Enqueue(string data);
        void Requeue(string data);
        void RequeueAll(IEnumerable<string> datas);
        string Dequeue();
        IEnumerable<string> DequeueAll();
        void Clear();
    }
}
