using Hyperion.Messages;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization;
using System.IO;
using System.Collections.Generic;

namespace Hyperion.Messaging
{
    public class MessageQueue : IMessageQueue
    {
        private IFileQueue fileQueue;

        public MessageQueue(IFileQueue fileQueue)
        {
            this.fileQueue = fileQueue;
        }

        public void Enqueue(string data)
        {
            var fileName = fileQueue.GetNewFileName();
            // Add file name to use to the queue
            fileQueue.Enqueue(fileName);
            // Write message to file
            using (var stream = fileQueue.GetWriter(fileName))
            {
                stream.Write(data);
            }
        }

        //public void Requeue(string data)
        //{
        //    var fileName = fileQueue.GetNewFileName();
        //    // Add file name to use to the queue
        //    fileQueue.Requeue(fileName);
        //    // Write message to file
        //    using (var stream = fileQueue.GetWriter(fileName))
        //    {
        //        stream.Write(data);
        //    }
        //}

        //public string Dequeue()
        //{
        //    var filePath = fileQueue.GetFilePath(fileQueue.Dequeue());
        //    var message = string.Empty;
        //    if (File.Exists(filePath))
        //    {
        //        using (var stream = fileQueue.GetReader(filePath))
        //        {
        //            message = stream.ReadToEnd();
        //        }
        //        File.Delete(filePath); // TODO check if it isn't better to do this after
        //    }

        //    return message;
        //}

        public FileMessage GetMessage(string fileName)
        {
            var filePath = fileQueue.GetFilePath(fileName);
            if (File.Exists(filePath))
            {
                using (var stream = fileQueue.GetReader(filePath))
                {
                    return new FileMessage(filePath, stream.ReadToEnd());
                }
            }

            return new FileMessage();
        }

        public void RequeueAllFileNames(IEnumerable<string> fileNames)
        {
            fileQueue.RequeueAll(fileNames);
        }

        public IEnumerable<string> DequeueAllFileNames()
        {
            return fileQueue.DequeueAll();
        }

        //public IEnumerable<FileMessage> Messages
        //{
        //    get
        //    {
        //        var fileNames = fileQueue.FileNames;
        //        foreach (var fileName in fileNames)
        //        {
        //            if (File.Exists(fileName))
        //            {
        //                using (var stream = fileQueue.GetReader(fileName))
        //                {
        //                    yield return new FileMessage(fileName, stream.ReadToEnd());
        //                }
        //            }
        //            // TODO fileName remove from queue
        //        }
        //    }
        //}

        public void Clear()
        {
            fileQueue.Clear();
        }
    }
}
