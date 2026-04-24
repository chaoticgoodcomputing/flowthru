using System.Xml.Serialization;
using Flowthru.Core.Abstractions;

namespace FlowthruCoverage.Data._01_Raw.Schemas;

/// <summary>Root element of a Cobertura XML coverage report.</summary>
[XmlRoot("coverage")]
public class CoberturaReport : IStructuredSerializable
{
  [XmlAttribute("line-rate")]
  public double LineRate { get; set; }

  [XmlAttribute("branch-rate")]
  public double BranchRate { get; set; }

  [XmlAttribute("lines-covered")]
  public int LinesCovered { get; set; }

  [XmlAttribute("lines-valid")]
  public int LinesValid { get; set; }

  [XmlAttribute("branches-covered")]
  public int BranchesCovered { get; set; }

  [XmlAttribute("branches-valid")]
  public int BranchesValid { get; set; }

  [XmlArray("packages")]
  [XmlArrayItem("package")]
  public List<CoberturaPackage> Packages { get; set; } = [];
}

/// <summary>A named package (assembly) within a Cobertura report.</summary>
public class CoberturaPackage
{
  [XmlAttribute("name")]
  public string Name { get; set; } = string.Empty;

  [XmlAttribute("line-rate")]
  public double LineRate { get; set; }

  [XmlAttribute("branch-rate")]
  public double BranchRate { get; set; }

  [XmlArray("classes")]
  [XmlArrayItem("class")]
  public List<CoberturaClass> Classes { get; set; } = [];
}

/// <summary>A named class within a package.</summary>
public class CoberturaClass
{
  [XmlAttribute("name")]
  public string Name { get; set; } = string.Empty;

  [XmlAttribute("filename")]
  public string Filename { get; set; } = string.Empty;

  [XmlAttribute("line-rate")]
  public double LineRate { get; set; }

  [XmlAttribute("branch-rate")]
  public double BranchRate { get; set; }

  [XmlArray("methods")]
  [XmlArrayItem("method")]
  public List<CoberturaMethod> Methods { get; set; } = [];
}

/// <summary>A named method within a class.</summary>
public class CoberturaMethod
{
  [XmlAttribute("name")]
  public string Name { get; set; } = string.Empty;

  [XmlAttribute("signature")]
  public string Signature { get; set; } = string.Empty;

  [XmlAttribute("line-rate")]
  public double LineRate { get; set; }

  [XmlAttribute("branch-rate")]
  public double BranchRate { get; set; }

  [XmlArray("lines")]
  [XmlArrayItem("line")]
  public List<CoberturaLine> Lines { get; set; } = [];
}

/// <summary>A single instrumented line within a method.</summary>
public class CoberturaLine
{
  [XmlAttribute("number")]
  public int Number { get; set; }

  [XmlAttribute("hits")]
  public int Hits { get; set; }
}
