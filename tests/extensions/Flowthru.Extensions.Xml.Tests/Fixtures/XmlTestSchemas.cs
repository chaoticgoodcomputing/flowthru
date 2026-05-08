using System.Xml.Serialization;
using Flowthru.Data.Schema;

namespace Flowthru.Extensions.Xml.Tests.Fixtures;

/// <summary>
/// Simple XML-serializable type used by the singleton + directory
/// adapter tests. Decorated with the <see cref="XmlRootAttribute"/>
/// the framework's <see cref="XmlSerializer"/> requires.
/// </summary>
[XmlRoot("TestItem")]
public class XmlTestItem : IStructuredSerializable
{
  public string Name { get; set; } = "";
  public int Count { get; set; }
}
