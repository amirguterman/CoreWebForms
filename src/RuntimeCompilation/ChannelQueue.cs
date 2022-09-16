// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Channels;

namespace Microsoft.AspNetCore.SystemWebAdapters.UI.RuntimeCompilation;

internal sealed class ChannelQueue : IQueue
{
    private readonly Channel<Func<CancellationToken, Task>> _queue;

    public ChannelQueue()
    {
        _queue = Channel.CreateUnbounded<Func<CancellationToken, Task>>();
    }

    public IAsyncEnumerable<Func<CancellationToken, Task>> GetItemsAsync(CancellationToken token) => _queue.Reader.ReadAllAsync(token);

    public void Add(Func<CancellationToken, Task> func)
    {
        while (!_queue.Writer.TryWrite(func))
        {
        }
    }
}
