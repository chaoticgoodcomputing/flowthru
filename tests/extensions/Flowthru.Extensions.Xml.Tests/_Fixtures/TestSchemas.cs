using System.Xml.Serialization;
using Flowthru.Core.Abstractions;

namespace Flowthru.Extensions.Xml.Tests.Fixtures;

/// <summary>Simple XML-serializable type for use in XML storage adapter tests.</summary>
[XmlRoot("TestItem")]
public class XmlTestItem : IStructuredSerializable
{
  public string Name { get; set; } = "";
  public int Count { get; set; }
}
