namespace ChuckDeviceController.Protos
{
    /// <summary>
    /// Generic gRPC client interface contract
    /// </summary>
    /// <typeparam name="TClient">gRPC client proto.</typeparam>
    /// <typeparam name="TRequest">gRPC client proto request.</typeparam>
    /// <typeparam name="TResponse">gRPC client proto response.</typeparam>
    public interface IGrpcClient<TClient, TRequest, TResponse>
    {
        /// <summary>
        /// Sends a request via gRPC.
        /// </summary>
        /// <param name="payload">gRPC client proto request.</param>
        /// <returns>gRPC client proto response.</returns>
        Task<TResponse?> SendAsync(TRequest payload);
    }
}