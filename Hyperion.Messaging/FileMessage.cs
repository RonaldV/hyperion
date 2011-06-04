using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Hyperion.Messaging
{
    public class FileMessage : IDisposable
    {
        private readonly string fileName;
        private readonly string message;

        public FileMessage()
        {
            IsLoaded = false;
        }

        public FileMessage(string fileName, string message)
        {
            this.fileName = fileName;
            this.message = message;
            IsLoaded = true;
        }

        public string Message { get { return message; } }
        public bool IsLoaded { get; private set; }

        #region IDisposable Members

        private bool disposed = false;

        public void Dispose()
        {
            Dispose(true);
            // This object will be cleaned up by the Dispose method.
            // Therefore, you should call GC.SupressFinalize to
            // take this object off the finalization queue
            // and prevent finalization code for this object
            // from executing a second time.
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            // Check to see if Dispose has already been called.
            if (!this.disposed)
            {
                // If disposing equals true, dispose all managed
                // and unmanaged resources.
                if (disposing)
                {
                    if (IsLoaded)
                    {
                        File.Delete(fileName);
                    }
                }

                // Call the appropriate methods to clean up
                // unmanaged resources here.
                // If disposing is false,
                // only the following code is executed.

                disposed = true;
            }
        }

        #endregion
    }
}
