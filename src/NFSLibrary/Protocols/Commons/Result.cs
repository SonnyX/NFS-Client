namespace NFSLibrary.Protocols.Commons
{
    using System;
    using NFSLibrary.Protocols.Commons.Exceptions;

    /// <summary>
    /// Non-generic Result for operations that don't return a value.
    /// </summary>
    public readonly struct Result
    {
        private readonly NFSStats _Status;
        private readonly string? _ErrorMessage;

        /// <summary>
        /// Gets whether the operation was successful.
        /// </summary>
        public bool IsSuccess => _Status == NFSStats.NFS_OK;

        /// <summary>
        /// Gets whether the operation failed.
        /// </summary>
        public bool IsFailure => !IsSuccess;

        /// <summary>
        /// Gets the NFS status code.
        /// </summary>
        public NFSStats Status => _Status;

        /// <summary>
        /// Gets the error message for failed results.
        /// </summary>
        public string? ErrorMessage => _ErrorMessage;

        private Result(NFSStats status, string? errorMessage)
        {
            _Status = status;
            _ErrorMessage = errorMessage;
        }

        /// <summary>
        /// Creates a successful result.
        /// </summary>
        /// <returns>A successful result.</returns>
        public static Result Success() => new Result(NFSStats.NFS_OK, null);

        /// <summary>
        /// Creates a failed result with the specified status.
        /// </summary>
        /// <param name="status">The NFS status code.</param>
        /// <param name="errorMessage">Optional custom error message.</param>
        /// <returns>A failed result.</returns>
        public static Result Failure(NFSStats status, string? errorMessage = null) =>
            new Result(status, errorMessage ?? GetDefaultErrorMessage(status));

        /// <summary>
        /// Throws if the result is a failure.
        /// </summary>
        /// <exception cref="NFSGeneralException">Thrown when the result represents a failure.</exception>
        public void ThrowIfFailure()
        {
            if (IsFailure)
            {
                ExceptionHelpers.ThrowException(_Status);
                throw new NFSGeneralException(_ErrorMessage ?? GetDefaultErrorMessage(_Status));
            }
        }

        /// <summary>
        /// Executes an action if the result is successful.
        /// </summary>
        /// <param name="action">The action to execute.</param>
        /// <returns>This result unchanged.</returns>
        public Result OnSuccess(Action action)
        {
            if (IsSuccess)
                action();
            return this;
        }

        /// <summary>
        /// Executes an action if the result is a failure.
        /// </summary>
        /// <param name="action">The action to execute with the status and error message.</param>
        /// <returns>This result unchanged.</returns>
        public Result OnFailure(Action<NFSStats, string?> action)
        {
            if (IsFailure)
                action(_Status, _ErrorMessage);
            return this;
        }

        /// <summary>
        /// Pattern matches on the result.
        /// </summary>
        /// <typeparam name="TResult">The return type.</typeparam>
        /// <param name="onSuccess">Function to execute on success.</param>
        /// <param name="onFailure">Function to execute on failure.</param>
        /// <returns>The result of the executed function.</returns>
        public TResult Match<TResult>(Func<TResult> onSuccess, Func<NFSStats, string?, TResult> onFailure)
        {
            return IsSuccess ? onSuccess() : onFailure(_Status, _ErrorMessage);
        }

        private static string GetDefaultErrorMessage(NFSStats status)
        {
            return status switch
            {
                NFSStats.NFS_OK => "Success",
                NFSStats.NFSERR_PERM => "Operation not permitted",
                NFSStats.NFSERR_NOENT => "No such file or directory",
                NFSStats.NFSERR_IO => "I/O error",
                NFSStats.NFSERR_ACCES => "Permission denied",
                NFSStats.NFSERR_EXIST => "File exists",
                NFSStats.NFSERR_NOTDIR => "Not a directory",
                NFSStats.NFSERR_ISDIR => "Is a directory",
                NFSStats.NFSERR_NOSPC => "No space left on device",
                NFSStats.NFSERR_ROFS => "Read-only file system",
                NFSStats.NFSERR_NOTEMPTY => "Directory not empty",
                NFSStats.NFSERR_STALE => "Stale file handle",
                _ => $"NFS error: {status}"
            };
        }
    }
}
