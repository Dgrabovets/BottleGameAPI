namespace BottleGame.Application.Common
{
    /// <summary>
    /// Result of an operation without a return value
    /// </summary>
    /// <remarks>
    /// Used for operations that return no data (for example, delete or update).
    /// Makes it possible to handle success and error in controllers in one way
    /// </remarks>
    public class Result
    {
        #region Properties

        /// <summary>
        /// Whether the operation succeeded
        /// </summary>
        public bool IsSuccess { get; protected set; }

        /// <summary>
        /// Whether the operation failed
        /// </summary>
        public bool IsFailure => !IsSuccess;

        /// <summary>
        /// Error message (null on success)
        /// </summary>
        public string? Error { get; protected set; }

        /// <summary>
        /// HTTP status code of the response
        /// </summary>
        public int StatusCode { get; protected set; }

        #endregion Properties

        #region Constructors

        /// <summary>
        /// Protected constructor that creates a result
        /// </summary>
        /// <param name="isSuccess">Whether the operation succeeded</param>
        /// <param name="error">Error message</param>
        /// <param name="statusCode">HTTP status code</param>
        protected Result(bool isSuccess, string? error, int statusCode)
        {
            IsSuccess = isSuccess;
            Error = error;
            StatusCode = statusCode;
        }

        #endregion Constructors

        #region Factory Methods

        /// <summary>
        /// Creates a successful result
        /// </summary>
        /// <returns>Successful result with code 200</returns>
        public static Result Success() => new(true, null, 200);

        /// <summary>
        /// Creates a successful result with the given status code
        /// </summary>
        /// <param name="statusCode">HTTP status code</param>
        /// <returns>Successful result</returns>
        public static Result Success(int statusCode) => new(true, null, statusCode);

        /// <summary>
        /// Creates a result with an error
        /// </summary>
        /// <param name="error">Error message</param>
        /// <param name="statusCode">HTTP status code (400 by default)</param>
        /// <returns>Result with an error</returns>
        public static Result Failure(string error, int statusCode = 400) => new(false, error, statusCode);

        /// <summary>
        /// Creates a "Not found" result (404)
        /// </summary>
        /// <param name="error">Error message</param>
        /// <returns>Result with code 404</returns>
        public static Result NotFound(string error) => new(false, error, 404);

        /// <summary>
        /// Creates an "Access denied" result (403)
        /// </summary>
        /// <param name="error">Error message</param>
        /// <returns>Result with code 403</returns>
        public static Result Forbidden(string error) => new(false, error, 403);

        /// <summary>
        /// Creates an "Unauthorized" result (401)
        /// </summary>
        /// <param name="error">Error message</param>
        /// <returns>Result with code 401</returns>
        public static Result Unauthorized(string error = "Unauthorized") => new(false, error, 401);

        /// <summary>
        /// Creates a "Conflict" result (409)
        /// </summary>
        /// <param name="error">Error message</param>
        /// <returns>Result with code 409</returns>
        public static Result Conflict(string error) => new(false, error, 409);

        /// <summary>
        /// Creates an "Internal server error" result (500)
        /// </summary>
        /// <param name="error">Error message</param>
        /// <returns>Result with code 500</returns>
        public static Result InternalError(string error = "Internal server error") => new(false, error, 500);

        #endregion Factory Methods
    }

    /// <summary>
    /// Result of an operation with a return value
    /// </summary>
    /// <typeparam name="T">Type of the returned value</typeparam>
    /// <remarks>
    /// Used for operations that return data (for example, get or create).
    /// Makes it possible to handle success and error in controllers in one way
    /// </remarks>
    public class Result<T> : Result
    {
        #region Properties

        /// <summary>
        /// Returned value (null on error)
        /// </summary>
        public T? Value { get; private set; }

        #endregion Properties

        #region Constructors

        /// <summary>
        /// Protected constructor that creates a result with a value
        /// </summary>
        /// <param name="isSuccess">Whether the operation succeeded</param>
        /// <param name="value">Returned value</param>
        /// <param name="error">Error message</param>
        /// <param name="statusCode">HTTP status code</param>
        protected Result(bool isSuccess, T? value, string? error, int statusCode)
            : base(isSuccess, error, statusCode)
        {
            Value = value;
        }

        #endregion Constructors

        #region Factory Methods

        /// <summary>
        /// Creates a successful result with data
        /// </summary>
        /// <param name="value">Returned value</param>
        /// <returns>Successful result with code 200</returns>
        public static Result<T> Success(T value) => new(true, value, null, 200);

        /// <summary>
        /// Creates a successful result with data and the given status code
        /// </summary>
        /// <param name="value">Returned value</param>
        /// <param name="statusCode">HTTP status code</param>
        /// <returns>Successful result</returns>
        public static Result<T> Success(T value, int statusCode) => new(true, value, null, statusCode);

        /// <summary>
        /// Creates a result with an error
        /// </summary>
        /// <param name="error">Error message</param>
        /// <param name="statusCode">HTTP status code (400 by default)</param>
        /// <returns>Result with an error</returns>
        public new static Result<T> Failure(string error, int statusCode = 400) => new(false, default, error, statusCode);

        /// <summary>
        /// Creates a "Not found" result (404)
        /// </summary>
        /// <param name="error">Error message</param>
        /// <returns>Result with code 404</returns>
        public new static Result<T> NotFound(string error) => new(false, default, error, 404);

        /// <summary>
        /// Creates an "Access denied" result (403)
        /// </summary>
        /// <param name="error">Error message</param>
        /// <returns>Result with code 403</returns>
        public new static Result<T> Forbidden(string error) => new(false, default, error, 403);

        /// <summary>
        /// Creates an "Unauthorized" result (401)
        /// </summary>
        /// <param name="error">Error message</param>
        /// <returns>Result with code 401</returns>
        public new static Result<T> Unauthorized(string error = "Unauthorized") => new(false, default, error, 401);

        /// <summary>
        /// Creates a "Conflict" result (409)
        /// </summary>
        /// <param name="error">Error message</param>
        /// <returns>Result with code 409</returns>
        public new static Result<T> Conflict(string error) => new(false, default, error, 409);

        /// <summary>
        /// Creates an "Internal server error" result (500)
        /// </summary>
        /// <param name="error">Error message</param>
        /// <returns>Result with code 500</returns>
        public new static Result<T> InternalError(string error = "Internal server error") => new(false, default, error, 500);

        #endregion Factory Methods

        #region Implicit Operators

        /// <summary>
        /// Implicit conversion of a value to a successful result
        /// </summary>
        /// <param name="value">Value to wrap</param>
        public static implicit operator Result<T>(T value) => Success(value);

        #endregion Implicit Operators
    }
}


