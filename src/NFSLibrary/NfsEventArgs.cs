namespace NFSLibrary
{
    using System;

    /// <summary>
    /// Event arguments for NFS data transfer events.
    /// </summary>
    public class NfsEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NfsEventArgs"/> class.
        /// </summary>
        /// <param name="bytes">The number of bytes transferred.</param>
        public NfsEventArgs(int bytes)
        {
            Bytes = bytes;
        }

        /// <summary>
        /// Gets the number of bytes transferred.
        /// </summary>
        public int Bytes { get; }
    }
}
