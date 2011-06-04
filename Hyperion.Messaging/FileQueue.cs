using System.IO;
using System;
using System.Collections.Generic;

namespace Hyperion.Messaging
{
    /// <summary>
    /// TODO make this more generic
    /// </summary>
    public class FileQueue : IFileQueue
    {
        private const string QueueDirectory = "A45BB124-16AF-4CB7-ACAD-6E9347AB8A87";
        private const string QueueFileName = "queue";
        private readonly object sync;
        private readonly string queuePath;

        public FileQueue()
        {
            sync = new object();
            queuePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, QueueDirectory);
        }

        public FileQueue(string subDirectory)
            : this()
        {
            queuePath = Path.Combine(queuePath, subDirectory);
        }

        private string QueuePath
        {
            get
            {
                if (!Directory.Exists(queuePath))
                {
                    Directory.CreateDirectory(queuePath).Attributes = 
                        FileAttributes.Directory | FileAttributes.Hidden;
                }

                return queuePath;
            }
        }

        public string GetNewFileName()
        {
            return Guid.NewGuid().ToString(); // Path.GetRandomFileName()
        }

        public string GetFilePath(string fileName)
        {
            return Path.Combine(queuePath, fileName);
        }

        public StreamWriter GetWriter(string fileName)
        {
            var newFilePath = Path.Combine(queuePath, fileName);
            return File.CreateText(newFilePath);
        }

        public StreamReader GetReader(string fileName)
        {
            var newFilePath = Path.Combine(queuePath, fileName);
            return File.OpenText(newFilePath);
        }

        public void Enqueue(string fileName)
        {
            var queueFilePath = Path.Combine(QueuePath, QueueFileName);
            lock (sync)
            {
                using (var streamWriter = File.Exists(queueFilePath) ? 
                    File.AppendText(queueFilePath) : 
                    File.CreateText(queueFilePath))
                {
                    streamWriter.WriteLine(fileName);
                }
            }
        }

        public void Requeue(string fileName)
        {
            var tempFilePath = Path.Combine(queuePath, GetNewFileName());
            var queueFilePath = Path.Combine(QueuePath, QueueFileName);
            lock (sync)
            {
                if (File.Exists(queueFilePath))
                {
                    using (var streamWriter = File.CreateText(tempFilePath))
                    {
                        streamWriter.WriteLine(fileName);
                        using (var streamReader = File.OpenText(queueFilePath))
                        {
                            while (!streamReader.EndOfStream)
                            {
                                streamWriter.WriteLine(streamReader.ReadLine());
                            }
                        }
                    }
                    File.Delete(queueFilePath);
                    File.Move(tempFilePath, queueFilePath);
                }
                else
                {
                    using (var streamWriter = File.CreateText(queueFilePath))
                    {
                        streamWriter.WriteLine(fileName);
                    }                   
                }
            }
        }

        public void RequeueAll(IEnumerable<string> fileNames)
        {
            var tempFilePath = Path.Combine(queuePath, GetNewFileName());
            var queueFilePath = Path.Combine(QueuePath, QueueFileName);
            lock (sync)
            {
                if (File.Exists(queueFilePath))
                {
                    using (var streamWriter = File.CreateText(tempFilePath))
                    {
                        foreach (var fileName in fileNames)
                        {
                            streamWriter.WriteLine(fileName);
                        }
                        using (var streamReader = File.OpenText(queueFilePath))
                        {
                            while (!streamReader.EndOfStream)
                            {
                                streamWriter.WriteLine(streamReader.ReadLine());
                            }
                        }
                    }
                    File.Delete(queueFilePath);
                    File.Move(tempFilePath, queueFilePath);
                }
                else
                {
                    using (var streamWriter = File.CreateText(queueFilePath))
                    {
                        foreach (var fileName in fileNames)
                        {
                            streamWriter.WriteLine(fileName);
                        }
                    }
                }
            }
        }

        public string Dequeue()
        {
            var fileName = string.Empty;
            var queueFilePath = Path.Combine(QueuePath, QueueFileName);
            if (File.Exists(queueFilePath))
            {
                var tempFilePath = Path.Combine(queuePath, GetNewFileName());
                lock (sync)
                {
                    using (var streamReader = File.OpenText(queueFilePath))
                    {
                        fileName = streamReader.ReadLine();
                        using (var streamWriter = File.CreateText(tempFilePath))
                        {
                            while (!streamReader.EndOfStream)
                            {
                                streamWriter.WriteLine(streamReader.ReadLine());
                            }
                        }
                    }
                }
                File.Delete(queueFilePath);
                File.Move(tempFilePath, queueFilePath);
            }

            return fileName;
        }

        public IEnumerable<string> DequeueAll()
        {
            var queueFilePath = Path.Combine(QueuePath, QueueFileName);
            var fileNames = default(IEnumerable<string>);
            if (File.Exists(queueFilePath))
            {
                lock (sync)
                {
                    fileNames = File.ReadAllLines(queueFilePath);
                    //using (var streamReader = File.OpenText(queueFilePath))
                    //{
                    //    while (!streamReader.EndOfStream)
                    //    {
                    //        fileNames.Add(streamReader.ReadLine());
                    //    }
                    //}
                    File.Delete(queueFilePath);
                }
            }

            return fileNames ?? new List<string>();
        }

        public void Clear()
        {
            var filePaths = Directory.GetFiles(queuePath);
            foreach (var filePath in filePaths)
            {
                File.Delete(filePath);
            }
        }
    }
}
