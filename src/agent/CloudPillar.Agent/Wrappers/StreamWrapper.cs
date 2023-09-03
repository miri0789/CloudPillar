public class StreamWrapper : Stream
{
    private readonly Stream _innerStream;
    private readonly CancellationToken _cancellationToken;
    private readonly IProgress<long>? _progress;

    public StreamWrapper(Stream innerStream, CancellationToken cancellationToken, IProgress<long>? progress = null)
    {
        ArgumentNullException.ThrowIfNull(innerStream);
        ArgumentNullException.ThrowIfNull(cancellationToken);

        _innerStream = innerStream;
        _cancellationToken = cancellationToken;
        _progress = progress;
    }

    public override bool CanRead => _innerStream.CanRead;

    public override bool CanSeek => _innerStream.CanSeek;

    public override bool CanWrite => _innerStream.CanWrite;

    public override long Length => _innerStream.Length;

    public override long Position
    {
        get => _innerStream.Position;
        set => _innerStream.Position = value;
    }

    public override void Flush()
    {
        _cancellationToken.ThrowIfCancellationRequested();
        _innerStream.Flush();
    }

    public override Task FlushAsync(CancellationToken _)
    {
        _cancellationToken.ThrowIfCancellationRequested();
        return _innerStream.FlushAsync(_cancellationToken);
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        _cancellationToken.ThrowIfCancellationRequested();
        int bytesRead = _innerStream.ReadAsync(buffer, offset, count, _cancellationToken).Result;
        _progress?.Report(bytesRead);
        return bytesRead;
    }

    public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken _)
    {
        _cancellationToken.ThrowIfCancellationRequested();
        int bytesRead = await _innerStream.ReadAsync(buffer, offset, count, _cancellationToken);
        _progress?.Report(bytesRead);
        return bytesRead;
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        _cancellationToken.ThrowIfCancellationRequested();
        return _innerStream.Seek(offset, origin);
    }

    public override void SetLength(long value)
    {
        _cancellationToken.ThrowIfCancellationRequested();
        _innerStream.SetLength(value);
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        _cancellationToken.ThrowIfCancellationRequested();
        _innerStream.WriteAsync(buffer, offset, count, _cancellationToken).Wait();
        _progress?.Report(count);
    }

    public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken _)
    {
        _cancellationToken.ThrowIfCancellationRequested();
        await _innerStream.WriteAsync(buffer, offset, count, _cancellationToken);
        _progress?.Report(count);
    }
}
