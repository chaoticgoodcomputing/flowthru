using Flowthru.Data.Storage;

namespace Flowthru.Core.Tests.Storage;

/// <summary>
/// Tests for <see cref="SeekableSpill"/> (#121): already-seekable sources pass
/// through un-owned; forward-only sources spill to a seekable temp file whose
/// content is preserved and which is deleted on dispose.
/// </summary>
[TestFixture]
public class SeekableSpillTests
{
  [Test]
  public async Task SeekableSource_PassesThrough_AndIsLeftOpen()
  {
    var source = new MemoryStream(new byte[] { 1, 2, 3 }) { Position = 2 };

    await using (var spill = await SeekableSpill.CreateAsync(source))
    {
      Assert.That(spill.Stream, Is.SameAs(source), "An already-seekable source is returned as-is.");
      Assert.That(spill.Stream.Position, Is.EqualTo(0), "It is rewound to the start.");
    }

    Assert.That(source.CanRead, Is.True, "The caller-owned stream is not disposed.");
  }

  [Test]
  public async Task ForwardOnlySource_SpillsToSeekable_ContentPreserved_ThenDeleted()
  {
    var content = new byte[] { 10, 20, 30, 40, 50 };
    var forwardOnly = new ForwardOnlyStream(new MemoryStream(content));

    Stream spilled;
    await using (var spill = await SeekableSpill.CreateAsync(forwardOnly))
    {
      spilled = spill.Stream;
      Assert.That(spill.Stream.CanSeek, Is.True);

      var first = new byte[content.Length];
      await spill.Stream.ReadExactlyAsync(first);
      Assert.That(first, Is.EqualTo(content));

      // Genuinely seekable — rewind and re-read.
      spill.Stream.Position = 0;
      var second = new byte[content.Length];
      await spill.Stream.ReadExactlyAsync(second);
      Assert.That(second, Is.EqualTo(content));
    }

    Assert.That(spilled.CanRead, Is.False, "The spilled temp stream is disposed (and DeleteOnClose removes the file).");
  }

  private sealed class ForwardOnlyStream : Stream
  {
    private readonly Stream _inner;

    public ForwardOnlyStream(Stream inner) => _inner = inner;

    public override bool CanRead => true;
    public override bool CanSeek => false;
    public override bool CanWrite => false;
    public override long Length => throw new NotSupportedException();
    public override long Position
    {
      get => throw new NotSupportedException();
      set => throw new NotSupportedException();
    }

    public override int Read(byte[] buffer, int offset, int count) => _inner.Read(buffer, offset, count);
    public override void Flush() { }
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
    public override void SetLength(long value) => throw new NotSupportedException();
    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
  }
}
