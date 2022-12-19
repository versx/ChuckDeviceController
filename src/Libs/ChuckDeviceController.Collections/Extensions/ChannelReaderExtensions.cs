namespace ChuckDeviceController.Collections.Extensions;

using System.Threading.Channels;

public static class ChannelReaderExtensions
{
	public static async Task<List<T>> ReadMultipleAsync<T>(
		this ChannelReader<T> reader,
		int maxBatchSize = 1024,
		CancellationToken cancellationToken = default)
	{
		await reader.WaitToReadAsync(cancellationToken);

		var batch = new List<T>();
		while (batch.Count < maxBatchSize && reader.TryRead(out T? message))
		{
			batch.Add(message);
		}
		return batch;
	}
}