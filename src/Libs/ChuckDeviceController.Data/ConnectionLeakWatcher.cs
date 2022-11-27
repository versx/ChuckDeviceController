﻿namespace ChuckDeviceController.Data
{
    using System.Data;
    using System.Data.Common;

    using MySqlConnector;

    /// <summary>
    ///     This class can help identify db connection leaks (connections that are not closed after use).
    /// Usage:
    ///     connection = new SqlConnection(..);
    ///     connection.Open()
    /// #if DEBUG
    ///     new ConnectionLeakWatcher(connection);
    /// #endif
    ///     That's it. Don't store a reference to the watcher. It will make itself available for garbage collection
    ///     once it has fulfilled its purpose. Watch the visual studio debug output for details on potentially leaked connections.
    ///     Note that a connection could possibly just be taking its time and may eventually be closed properly despite being flagged by this class.
    ///     So take the output with a pinch of salt.
    /// </summary>
    /// <credits>https://stackoverflow.com/a/15002420</credits>
    public class ConnectionLeakWatcher : IDisposable
    {
        //private static readonly ILogger<ConnectionLeakWatcher> _logger =
        //    new Logger<ConnectionLeakWatcher>(LoggerFactory.Create(x => x.AddConsole()));

        private const uint DefaultConnectionTimeoutS = 30;

        private static int _idCounter = 0;
        private readonly int _connectionId = ++_idCounter;
        private readonly Timer? _timer;
        //Store reference to connection so we can unsubscribe from state change events
        private DbConnection? _connection;
        private readonly uint _connectionTimeoutS;

        public string StackTrace { get; set; }

        public ConnectionLeakWatcher(MySqlConnection connection, uint connectionTimeoutS = DefaultConnectionTimeoutS)
        {
            _connection = connection;
            _connection.StateChange += ConnectionOnStateChange;
            _connectionTimeoutS = connectionTimeoutS;
            StackTrace = Environment.StackTrace;

            //_logger.LogDebug($"Connection opened {_connectionId}");
            Console.WriteLine($"Connection opened {_connectionId}");

            _timer = new Timer(_ =>
            {
                // The timeout expired without the connection being closed. Write to debug output the stack trace of the connection creation to
                // assist in pinpointing the problem
                //_logger.LogWarning($"Suspected connection leak with origin:\n{StackTrace}\nConnection id: {_connectionId}");
                Console.WriteLine($"Suspected connection leak with origin:\n{StackTrace}\nConnection id: {_connectionId}");
                // That's it - we're done. Clean up by calling Dispose.
                Dispose();
            }, null, _connectionTimeoutS * 1000, Timeout.Infinite);
        }

        private void ConnectionOnStateChange(object sender, StateChangeEventArgs stateChangeEventArgs)
        {
            // Connection state changed. Was it closed?
            if (stateChangeEventArgs.CurrentState == ConnectionState.Closed)
            {
                // The connection was closed within the timeout
                //_logger.LogWarning($"Connection closed {_connectionId}");
                Console.WriteLine($"Connection closed {_connectionId}");
                // That's it - we're done. Clean up by calling Dispose.
                Dispose();
            }
        }

        #region Dispose

        private bool _isDisposed = false;
        public void Dispose()
        {
            if (_isDisposed) return;

            _timer?.Dispose();

            if (_connection != null)
            {
                _connection.StateChange -= ConnectionOnStateChange;
                _connection = null;
            }

            _isDisposed = true;

            GC.SuppressFinalize(this);
        }

        ~ConnectionLeakWatcher()
        {
            Dispose();
        }

        #endregion
    }
}