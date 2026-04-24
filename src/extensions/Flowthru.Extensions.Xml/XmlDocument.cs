namespace Flowthru.Core.Data;

/// <summary>
/// Wraps a deserialized XML document with its source file name.
/// </summary>
/// <typeparam name="T">The deserialized document type.</typeparam>
/// <remarks>
/// <para>
/// Used by <c>XmlDirectoryStorageAdapter&lt;T&gt;</c> so downstream pipeline steps can
/// identify which file each document originated from — useful when file names carry
/// semantic meaning (e.g., a test project name encoded as the file name).
/// </para>
/// </remarks>
/// <param name="FileName">The file name (without directory path) of the source XML file.</param>
/// <param name="Document">The deserialized document.</param>
public record XmlDocument<T>(string FileName, T Document);
