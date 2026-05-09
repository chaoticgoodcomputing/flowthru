using System.Xml.Serialization;
using Flowthru.Data.Schema;

namespace FlowthruCoverage.Data._01_Raw.Schemas;

/// <summary>Root element of a Cobertura XML coverage report.</summary>
[XmlRoot("coverage")]
public class CoberturaReport : IStructuredSerializable
{
  /// <summary>
  /// Overall line coverage percentage for the report, as reported by Cobertura.
  /// </summary>
  [XmlAttribute("line-rate")]
  public double LineRate { get; set; }

  /// <summary>
  /// Overall branch coverage percentage for the report, as reported by Cobertura.
  /// </summary>
  [XmlAttribute("branch-rate")]
  public double BranchRate { get; set; }

  /// <summary>
  /// Total number of lines covered (executed at least once) across all packages, as reported by Cobertura.
  /// </summary>
  [XmlAttribute("lines-covered")]
  public int LinesCovered { get; set; }

  /// <summary>
  /// Total number of instrumented lines (covered or uncovered) across all packages, as reported by Cobertura.
  /// Used as the denominator for the overall line coverage percentage.
  /// Note that Cobertura may report some lines as "not valid" (e.g. non-code lines), which are excluded from this count.
  /// </summary>
  [XmlAttribute("lines-valid")]
  public int LinesValid { get; set; }

  /// <summary>
  /// Total number of branches covered (executed at least once) across all packages, as reported by Cobertura.
  /// </summary>
  [XmlAttribute("branches-covered")]
  public int BranchesCovered { get; set; }

  /// <summary>
  /// Total number of instrumented branches (covered or uncovered) across all packages, as reported by Cobertura.
  /// Used as the denominator for the overall branch coverage percentage.
  /// Note that Cobertura may report some branches as "not valid" (e.g. non-code branches), which are excluded from this count.
  /// </summary>
  [XmlAttribute("branches-valid")]
  public int BranchesValid { get; set; }

  /// <summary>
  /// Collection of packages (assemblies) included in this coverage report, each with its own line and branch coverage breakdowns.
  /// The <c>name</c> attribute of each package corresponds to the Cobertura-reported assembly name (e.g. "Flowthru.Core").
  /// Each package contains its own collection of classes, which in turn contain methods and lines with hit counts.
  /// This hierarchical structure allows for detailed analysis of coverage at multiple levels of granularity.
  /// Note that Cobertura may report some packages as "not valid" (e.g. generated code), which are excluded from this collection.
  /// </summary>
  [XmlArray("packages")]
  [XmlArrayItem("package")]
  public List<CoberturaPackage> Packages { get; set; } = [];
}

/// <summary>A named package (assembly) within a Cobertura report.</summary>
public class CoberturaPackage
{
  /// <summary>
  /// The assembly/package name as reported by Cobertura (e.g. "Flowthru.Core").
  /// </summary>
  [XmlAttribute("name")]
  public string Name { get; set; } = string.Empty;

  /// <summary>
  /// Line coverage percentage for this package, as reported by Cobertura. Calculated as (lines-covered / lines-valid) * 100.
  /// </summary>
  [XmlAttribute("line-rate")]
  public double LineRate { get; set; }

  /// <summary>
  /// Branch coverage percentage for this package, as reported by Cobertura. Calculated as (branches-covered / branches-valid) * 100.
  /// </summary>
  [XmlAttribute("branch-rate")]
  public double BranchRate { get; set; }

  /// <summary>
  /// Collection of classes within this package, each with its own line and branch coverage breakdowns.
  /// The <c>name</c> attribute of each class corresponds to the Cobertura-reported class name (e.g. "Flowthru.Core.SomeClass").
  /// Each class contains its own collection of methods, which in turn contain lines with hit counts.
  /// </summary>
  [XmlArray("classes")]
  [XmlArrayItem("class")]
  public List<CoberturaClass> Classes { get; set; } = [];
}

/// <summary>A named class within a package.</summary>
public class CoberturaClass
{
  /// <summary>
  /// The fully-qualified class name as reported by Cobertura (e.g. "Flowthru.Core.SomeClass").
  /// Note that Cobertura may report some classes as "not valid".
  [XmlAttribute("name")]
  public string Name { get; set; } = string.Empty;

  /// <summary>
  /// The source file path as recorded in the Cobertura XML for this class (e.g. "src/Flowthru/Core/SomeClass.cs").
  /// </summary>
  [XmlAttribute("filename")]
  public string Filename { get; set; } = string.Empty;

  /// <summary>
  /// Line coverage percentage for this class, as reported by Cobertura.
  /// </summary>
  [XmlAttribute("line-rate")]
  public double LineRate { get; set; }

  /// <summary>
  /// Branch coverage percentage for this class, as reported by Cobertura.
  /// </summary>
  [XmlAttribute("branch-rate")]
  public double BranchRate { get; set; }

  /// <summary>
  /// Collection of methods within this class, each with its own line and branch coverage breakdowns.
  /// The <c>name</c> attribute of each method corresponds to the Cobertura-reported method name (e.g. "SomeMethod"), and the <c>signature</c> attribute corresponds to the method signature (e.g. "(int,string)").
  /// </summary>
  [XmlArray("methods")]
  [XmlArrayItem("method")]
  public List<CoberturaMethod> Methods { get; set; } = [];
}

/// <summary>A named method within a class.</summary>
public class CoberturaMethod
{
  /// <summary>
  /// The method name as reported by Cobertura (e.g. "SomeMethod").
  /// </summary>
  [XmlAttribute("name")]
  public string Name { get; set; } = string.Empty;

  /// <summary>
  /// The method signature as reported by Cobertura (e.g. "(int,string)").
  /// </summary>
  [XmlAttribute("signature")]
  public string Signature { get; set; } = string.Empty;

  /// <summary>
  /// Line coverage percentage for this method, as reported by Cobertura.
  /// </summary>
  [XmlAttribute("line-rate")]
  public double LineRate { get; set; }

  /// <summary>
  /// Branch coverage percentage for this method, as reported by Cobertura.
  /// </summary>
  [XmlAttribute("branch-rate")]
  public double BranchRate { get; set; }

  /// <summary>
  /// Collection of lines within this method, each with its own hit count.
  /// </summary>
  [XmlArray("lines")]
  [XmlArrayItem("line")]
  public List<CoberturaLine> Lines { get; set; } = [];
}

/// <summary>A single instrumented line within a method.</summary>
public class CoberturaLine
{
  /// <summary>
  /// The line number within the source file.
  /// </summary>
  [XmlAttribute("number")]
  public int Number { get; set; }

  /// <summary>
  /// The number of times this line was executed.
  /// </summary>
  [XmlAttribute("hits")]
  public int Hits { get; set; }
}
