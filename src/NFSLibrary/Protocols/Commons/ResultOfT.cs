namespace NFSLibrary.Protocols.Commons
{
    using System;
#if !NETSTANDARD2_1
    using System.Diagnostics.CodeAnalysis;
#endif
    using NFSLibrary.Protocols.Commons.Exceptions;
    /// <summary>
    /// Represents the result of an operation that can either succeed with a value or fail with an error.
    /// This provides a functional approach to error handling without exceptions for expected failures.
    /// </summary>
    /// <typeparam name="T">The type of the success value.</typeparam>
    public readonly struct Result<T>
    {
        private readonly T? _Value;
        private readonly NFSStats _Status;
        private readonly string? _ErrorMessage;

        /// <summary>
        /// Gets whether the operation was successful.
        /// </summary>
#if !NETSTANDARD2_1
        [MemberNotNullWhen(true, nameof(Value))]
#endif
        public bool IsSuccess => _Status == NFSStats.NFS_OK;

        /// <summary>
        /// Gets whether the operation failed.
        /// </summary>
        public bool IsFailure => !IsSuccess;

        /// <summary>
        /// Gets the success value. Only valid when <see cref="IsSuccess"/> is true.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when accessing Value on a failed result.</exception>
        public T Value => IsSuccess ? _Value! : throw new InvalidOperationException($"Cannot access Value on a failed result. Status: {_Status}, Error: {_ErrorMessage}");

        /// <summary>
        /// Gets the NFS status code.
        /// </summary>
        public NFSStats Status => _Status;

        /// <summary>
        /// Gets the error message for failed results.
        /// </summary>
        public string? ErrorMessage => _ErrorMessage;

        private Result(T value)
        {
            _Value = value;
            _Status = NFSStats.NFS_OK;
            _ErrorMessage = null;
        }

        private Result(NFSStats status, string? errorMessage = null)
        {
            _Value = default;
            _Status = status;
            _ErrorMessage = errorMessage ?? GetDefaultErrorMessage(status);
        }

        /// <summary>
        /// Creates a successful result with the specified value.
        /// </summary>
        /// <param name="value">The success value.</param>
        /// <returns>A successful result.</returns>
        public static Result<T> Success(T value) => new Result<T>(value);

        /// <summary>
        /// Creates a failed result with the specified status.
        /// </summary>
        /// <param name="status">The NFS status code.</param>
        /// <param name="errorMessage">Optional custom error message.</param>
        /// <returns>A failed result.</returns>
        public static Result<T> Failure(NFSStats status, string? errorMessage = null) =>
            new Result<T>(status, errorMessage);

        /// <summary>
        /// Gets the value if successful, or the specified default value if failed.
        /// </summary>
        /// <param name="defaultValue">The default value to return on failure.</param>
        /// <returns>The success value or the default value.</returns>
        public T GetValueOrDefault(T defaultValue) => IsSuccess ? _Value! : defaultValue;

        /// <summary>
        /// Gets the value if successful, or throws the appropriate NFS exception if failed.
        /// </summary>
        /// <returns>The success value.</returns>
        /// <exception cref="NFSGeneralException">Thrown when the result represents a failure.</exception>
        public T GetValueOrThrow()
        {
            if (IsSuccess)
                return _Value!;

            ExceptionHelpers.ThrowException(_Status);
            throw new NFSGeneralException(_ErrorMessage ?? GetDefaultErrorMessage(_Status));
        }

        /// <summary>
        /// Transforms the success value using the specified function.
        /// </summary>
        /// <typeparam name="TResult">The type of the transformed value.</typeparam>
        /// <param name="mapper">The transformation function.</param>
        /// <returns>A new result with the transformed value, or the original failure.</returns>
        public Result<TResult> Map<TResult>(Func<T, TResult> mapper)
        {
            return IsSuccess
                ? Result<TResult>.Success(mapper(_Value!))
                : Result<TResult>.Failure(_Status, _ErrorMessage);
        }

        /// <summary>
        /// Chains another operation that returns a Result.
        /// </summary>
        /// <typeparam name="TResult">The type of the next result's value.</typeparam>
        /// <param name="binder">The function that produces the next result.</param>
        /// <returns>The result of the bound function, or the original failure.</returns>
        public Result<TResult> Bind<TResult>(Func<T, Result<TResult>> binder)
        {
            return IsSuccess
                ? binder(_Value!)
                : Result<TResult>.Failure(_Status, _ErrorMessage);
        }

        /// <summary>
        /// Executes an action on the success value if successful.
        /// </summary>
        /// <param name="action">The action to execute.</param>
        /// <returns>This result unchanged.</returns>
        public Result<T> OnSuccess(Action<T> action)
        {
            if (IsSuccess)
                action(_Value!);
            return this;
        }

        /// <summary>
        /// Executes an action if the result is a failure.
        /// </summary>
        /// <param name="action">The action to execute with the status and error message.</param>
        /// <returns>This result unchanged.</returns>
        public Result<T> OnFailure(Action<NFSStats, string?> action)
        {
            if (IsFailure)
                action(_Status, _ErrorMessage);
            return this;
        }

        /// <summary>
        /// Pattern matches on the result, executing the appropriate function.
        /// </summary>
        /// <typeparam name="TResult">The return type.</typeparam>
        /// <param name="onSuccess">Function to execute on success.</param>
        /// <param name="onFailure">Function to execute on failure.</param>
        /// <returns>The result of the executed function.</returns>
        public TResult Match<TResult>(Func<T, TResult> onSuccess, Func<NFSStats, string?, TResult> onFailure)
        {
            return IsSuccess ? onSuccess(_Value!) : onFailure(_Status, _ErrorMessage);
        }

        /// <summary>
        /// Implicit conversion from value to successful result.
        /// </summary>
        public static implicit operator Result<T>(T value) => Success(value);

        private static string GetDefaultErrorMessage(NFSStats status)
        {
            return status switch
            {
                NFSStats.NFS_OK => "Success",
                NFSStats.NFSERR_PERM => "Operation not permitted",
                NFSStats.NFSERR_NOENT => "No such file or directory",
                NFSStats.NFSERR_IO => "I/O error",
                NFSStats.NFSERR_NXIO => "No such device or address",
                NFSStats.NFSERR_ACCES => "Permission denied",
                NFSStats.NFSERR_EXIST => "File exists",
                NFSStats.NFSERR_XDEV => "Cross-device link",
                NFSStats.NFSERR_NODEV => "No such device",
                NFSStats.NFSERR_NOTDIR => "Not a directory",
                NFSStats.NFSERR_ISDIR => "Is a directory",
                NFSStats.NFSERR_INVAL => "Invalid argument",
                NFSStats.NFSERR_FBIG => "File too large",
                NFSStats.NFSERR_NOSPC => "No space left on device",
                NFSStats.NFSERR_ROFS => "Read-only file system",
                NFSStats.NFSERR_MLINK => "Too many links",
                NFSStats.NFSERR_NAMETOOLONG => "File name too long",
                NFSStats.NFSERR_NOTEMPTY => "Directory not empty",
                NFSStats.NFSERR_DQUOT => "Disk quota exceeded",
                NFSStats.NFSERR_STALE => "Stale file handle",
                NFSStats.NFSERR_REMOTE => "Object is remote",
                NFSStats.NFSERR_BADHANDLE => "Bad file handle",
                NFSStats.NFSERR_NOT_SYNC => "Synchronization mismatch",
                NFSStats.NFSERR_BAD_COOKIE => "Invalid cookie",
                NFSStats.NFSERR_NOTSUPP => "Operation not supported",
                NFSStats.NFSERR_TOOSMALL => "Buffer too small",
                NFSStats.NFSERR_SERVERFAULT => "Server fault",
                NFSStats.NFSERR_BADTYPE => "Bad type",
                NFSStats.NFSERR_JUKEBOX => "Resource temporarily unavailable",
                _ => $"NFS error: {status}"
            };
        }
    }
}
