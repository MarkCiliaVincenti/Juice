using Newtonsoft.Json;

namespace Juice
{
    public interface IOperationResult
    {
        public string? Message { get; }
        public bool Succeeded { get; }
        [JsonIgnore]
        public Exception? Exception { get; }
        public void ThrowIfNotSucceeded();
    }
    public interface IOperationResult<T> : IOperationResult
    {
        public T? Data { get; set; }
    }

    public class OperationResult
    {
        private static readonly OperationResultInternal _success = new OperationResultInternal { Succeeded = true };

        /// <summary>
        /// Return a succeeded <see cref="IOperationResult"/>
        /// </summary>
        public static IOperationResult Success => _success;

        #region OperationResult

        /// <summary>
        /// Create a succeeded <see cref="IOperationResult"/> with a message
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public static IOperationResult Succeeded(string? message)
            => new OperationResultInternal()
            {
                Succeeded = true,
                Message = message
            };

        /// <summary>
        /// Create a failed <see cref="IOperationResult"/> with an <see cref="System.Exception"/>
        /// </summary>
        /// <param name="ex"></param>
        /// <returns></returns>
        public static IOperationResult Failed(Exception ex)
            => new OperationResultInternal()
            {
                Succeeded = false,
                Exception = ex
            };

        /// <summary>
        /// Create a failed <see cref="IOperationResult"/> with an <see cref="System.Exception"/> and message
        /// </summary>
        /// <param name="ex"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        public static IOperationResult Failed(Exception ex, string? message)
            => new OperationResultInternal()
            {
                Succeeded = false,
                Message = message,
                Exception = ex
            };

        /// <summary>
        /// Create a failed <see cref="IOperationResult"/> with message
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public static IOperationResult Failed(string? message)
            => new OperationResultInternal()
            {
                Succeeded = false,
                Message = message
            };

        #endregion

        #region OperationResult<T>
        /// <summary>
        /// Create a failed <see cref="IOperationResult{T}"/> with an <see cref="System.Exception"/>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="ex"></param>
        /// <returns></returns>
        public static IOperationResult<T> Failed<T>(Exception ex)
            => new OperationResultInternal<T>()
            { Succeeded = false, Exception = ex };

        /// <summary>
        /// Create a failed <see cref="IOperationResult{T}"/> with an <see cref="System.Exception"/> and message
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="ex"></param>
        /// <param name="message"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public static IOperationResult<T> Failed<T>(Exception ex, string? message, T? data = default)
            => new OperationResultInternal<T>()
            { Succeeded = false, Exception = ex, Message = message, Data = data };

        /// <summary>
        /// Create a failed <see cref="IOperationResult{T}"/> with message
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="message"></param>
        /// <returns></returns>
        public static IOperationResult<T> Failed<T>(string? message)
            => new OperationResultInternal<T>()
            { Succeeded = false, Message = message };

        /// <summary>
        /// Create a succeeded <see cref="IOperationResult{T}"/> with data and a message
        /// </summary>
        /// <param name="data"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        public static IOperationResult<T> Result<T>(T? data, string? message = null)
            => new OperationResultInternal<T>()
            { Succeeded = true, Data = data, Message = message };

        /// <summary>
        /// Create a succeeded <see cref="IOperationResult{T}"/> without data and a message
        /// </summary>
        /// <param name="message"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static IOperationResult<T> Succeeded<T>(string? message = null)
            => new OperationResultInternal<T>()
            { Succeeded = true, Message = message };


        public static IOperationResult<T>? FromJson<T>(string json) => JsonConvert.DeserializeObject<OperationResultInternal<T>>(json);

        #endregion

    }

    internal class OperationResultInternal : IOperationResult
    {
        protected string? _message;
        public string? Message
        {
            get { return _message ?? Exception?.InnerException?.Message ?? Exception?.Message; }
            set
            {
                _message = value;
            }
        }
        public bool Succeeded { get; init; }

        [JsonIgnore]
        public Exception? Exception { get; init; }

        public void ThrowIfNotSucceeded()
        {
            if (!Succeeded)
            {
                var ex = Exception ?? new Exception(Message);
                System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(ex).Throw();
            }
        }
        
        public override string? ToString()
            => Message ?? (Succeeded ? "Operation Succeeded" : "Operation Failed");

    }

    internal class OperationResultInternal<T> : OperationResultInternal, IOperationResult<T>
    {
        public T? Data { get; set; }

    }
}
