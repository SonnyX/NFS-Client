namespace NFSLibrary
{
    using System;

    /// <summary>
    /// Event arguments for connection health status changes.
    /// </summary>
    public class ConnectionHealthChangedEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the previous health status.
        /// </summary>
        public ConnectionHealthStatus OldStatus { get; }

        /// <summary>
        /// Gets the new health status.
        /// </summary>
        public ConnectionHealthStatus NewStatus { get; }

        /// <summary>
        /// Creates new event arguments.
        /// </summary>
        /// <param name="oldStatus">The previous health status.</param>
        /// <param name="newStatus">The new health status.</param>
        public ConnectionHealthChangedEventArgs(ConnectionHealthStatus oldStatus, ConnectionHealthStatus newStatus)
        {
            OldStatus = oldStatus;
            NewStatus = newStatus;
        }
    }
}
