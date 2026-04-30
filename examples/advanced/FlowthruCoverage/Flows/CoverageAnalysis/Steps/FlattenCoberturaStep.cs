using Flowthru.Core.Data;
using Flowthru.Core.Steps;
using FlowthruCoverage.Data._01_Raw.Schemas;
using FlowthruCoverage.Data._02_Intermediate.Schemas;
#if FUNIT_ENABLED
using Flowthru.FUnit;
#endif

namespace FlowthruCoverage.Flows.Coverage.Steps;

/// <summary>
/// Flattens a directory of Cobertura XML documents into individual line-level coverage rows.
/// The directory key (full file path) drives the TestProject column; the deserialized
/// <see cref="CoberturaReport"/> supplies the package/class/method/line hierarchy.
/// </summary>
[FlowthruStep]
public static class FlattenCoberturaStep
{
  public static Func<
    Directory<CoberturaReport>,
    IEnumerable<LineCoverageRow>
  > Create()
  {
    return documents =>
      documents.SelectMany(entry =>
      {
        // Key is the full file path (e.g. ".../Flowthru.Core.Tests.xml") — strip path and
        // extension to recover the test project name.
        var testProject = Path.GetFileNameWithoutExtension(entry.Key);
        var report = entry.Value;

        return report.Packages.SelectMany(pkg =>
          pkg.Classes.SelectMany(cls =>
            cls.Methods.SelectMany(method =>
              method.Lines.Select(line => new LineCoverageRow
              {
                TestProject = testProject,
                SrcPackage = pkg.Name,
                SourceFile = cls.Filename,
                ClassName = cls.Name,
                MethodName = method.Name,
                MethodSignature = method.Signature,
                LineNumber = line.Number,
                Hits = line.Hits,
              })
            )
          )
        );
      });
  }

#if FUNIT_ENABLED
  /// <summary>FUnit tests for <see cref="FlattenCoberturaStep"/>.</summary>
  public class Tests : FUnitContext
  {
    private static KeyValuePair<string, CoberturaReport> Entry(string fileName, params CoberturaPackage[] packages) =>
      new(fileName, new CoberturaReport { Packages = packages.ToList() });

    private static Directory<CoberturaReport> Dir(params KeyValuePair<string, CoberturaReport>[] entries) =>
      new(entries);

    private static CoberturaPackage Package(string name, params CoberturaClass[] classes) =>
      new() { Name = name, Classes = classes.ToList() };

    private static CoberturaClass Class(string name, string filename, params CoberturaMethod[] methods) =>
      new()
      {
        Name = name,
        Filename = filename,
        Methods = methods.ToList(),
      };

    private static CoberturaMethod Method(string name, string signature, params (int Number, int Hits)[] lines) =>
      new()
      {
        Name = name,
        Signature = signature,
        Lines = lines.Select(l => new CoberturaLine { Number = l.Number, Hits = l.Hits }).ToList(),
      };

    /// <summary>Empty directory yields no rows — no spurious entries materialized.</summary>
    [StepTest(typeof(FlattenCoberturaStep))]
    public void EmptyInput_YieldsEmptyOutput()
    {
      var result = Invoke(FlattenCoberturaStep.Create(), Directory<CoberturaReport>.Empty);

      Assert.That(result, Is.Empty);
    }

    /// <summary>
    /// The TestProject column is derived from the file path key, with the .xml extension
    /// stripped. This is the only signal tying coverage data back to which test run produced it.
    /// </summary>
    [StepTest(typeof(FlattenCoberturaStep))]
    public void TestProjectName_IsDerivedFromFileNameWithoutExtension()
    {
      var dir = Dir(Entry(
        "Flowthru.Core.Tests.xml",
        Package("Flowthru.Core",
          Class("Flowthru.Core.Foo", "Foo.cs",
            Method("Bar", "()", (10, 1))))
      ));

      var result = Invoke(FlattenCoberturaStep.Create(), dir).ToList();

      Assert.That(result, Has.Count.EqualTo(1));
      Assert.That(result[0].TestProject, Is.EqualTo("Flowthru.Core.Tests"));
    }

    /// <summary>
    /// One Cobertura line becomes one row carrying the full package/class/method context plus
    /// the line's number and hit count — verifying the four-level fan-out preserves all fields.
    /// </summary>
    [StepTest(typeof(FlattenCoberturaStep))]
    public void NestedHierarchy_FlattensToOneRowPerLine()
    {
      var dir = Dir(Entry(
        "TestRun.xml",
        Package("Flowthru.Core",
          Class("Flowthru.Core.Foo", "src/Foo.cs",
            Method("Bar", "(int)", (10, 5), (11, 0))))
      ));

      var result = Invoke(FlattenCoberturaStep.Create(), dir).ToList();

      Assert.That(result, Has.Count.EqualTo(2));
      Assert.That(result[0].SrcPackage, Is.EqualTo("Flowthru.Core"));
      Assert.That(result[0].ClassName, Is.EqualTo("Flowthru.Core.Foo"));
      Assert.That(result[0].SourceFile, Is.EqualTo("src/Foo.cs"));
      Assert.That(result[0].MethodName, Is.EqualTo("Bar"));
      Assert.That(result[0].MethodSignature, Is.EqualTo("(int)"));
      Assert.That(result[0].LineNumber, Is.EqualTo(10));
      Assert.That(result[0].Hits, Is.EqualTo(5));
      Assert.That(result[1].LineNumber, Is.EqualTo(11));
      Assert.That(result[1].Hits, Is.EqualTo(0));
    }

    /// <summary>
    /// Multiple files flatten with each carrying its own TestProject derived from its key.
    /// Confirms no cross-contamination between source XMLs.
    /// </summary>
    [StepTest(typeof(FlattenCoberturaStep))]
    public void MultipleDocuments_PreserveTestProjectPerDocument()
    {
      var dir = Dir(
        Entry("A.Tests.xml", Package("PkgA", Class("PkgA.Foo", "Foo.cs", Method("M", "()", (1, 1))))),
        Entry("B.Tests.xml", Package("PkgB", Class("PkgB.Bar", "Bar.cs", Method("N", "()", (1, 0)))))
      );

      var result = Invoke(FlattenCoberturaStep.Create(), dir).ToList();

      Assert.That(result, Has.Count.EqualTo(2));
      // Directory iteration order follows ordinal-string key order, which matches insertion
      // order here ("A.Tests.xml" < "B.Tests.xml") — but assert by content rather than order.
      var byTestProject = result.ToDictionary(r => r.TestProject);
      Assert.That(byTestProject["A.Tests"].SrcPackage, Is.EqualTo("PkgA"));
      Assert.That(byTestProject["B.Tests"].SrcPackage, Is.EqualTo("PkgB"));
    }
  }
#endif
}
