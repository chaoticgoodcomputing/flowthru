using System.Text.Json.Nodes;
using Flowthru.Data.Schema;
using Flowthru.Data.Storage;
using Flowthru.Prelude;
using Flowthru.Step.Python.Internal;
using Flowthru.Validation.PreFlight;
using Flowthru.Validation.PreFlight.Python;
using Flowthru.Validation.Runtime;
using Flowthru.Validation.Runtime.Python;

namespace Flowthru.Extensions.Python.Tests;

/// <summary>
/// Direct exercises of the pure-C# helpers inside
/// <see cref="SubprocessPythonExecutor"/> — type classification,
/// encode/decode for non-subprocess kinds, error classification,
/// and the JSON envelope translators for inspector results. No
/// Python subprocess is spawned by any test in this fixture.
/// </summary>
/// <remarks>
/// <para>
/// These helpers sit at the boundary between Core's typed error
/// surface and the over-the-wire string payloads exchanged with the
/// Python worker. They are pure functions of their inputs, so they
/// can be tested directly without IPC. Subprocess-level integration
/// is covered in a sibling fixture; this one is the fast feedback
/// loop on the helper logic.
/// </para>
/// </remarks>
[TestFixture]
[Category("Python")]
public class SubprocessPythonExecutorHelperTests
{
  // ── Probe schemas ─────────────────────────────────────────────────

  [FlowthruSchema]
  public partial record MyRecord
  {
    public required int Id { get; init; }
    public required string Name { get; init; }
    public required double Score { get; init; }
  }

  // ── ClassifyType ──────────────────────────────────────────────────

  [Test]
  public void ClassifyType_Int_IsScalar()
  {
    Assert.That(SubprocessPythonExecutor.ClassifyType(typeof(int)), Is.EqualTo("scalar"));
  }

  [Test]
  public void ClassifyType_String_IsScalar()
  {
    // Strings implement IEnumerable<char>; the helper must NOT
    // pick that up as 'tabular'. The exclusion is the whole point
    // of the string-vs-IEnumerable<char> guard inside
    // IsEnumerableSchema.
    Assert.That(SubprocessPythonExecutor.ClassifyType(typeof(string)), Is.EqualTo("scalar"));
  }

  [Test]
  public void ClassifyType_Guid_IsScalar()
  {
    Assert.That(SubprocessPythonExecutor.ClassifyType(typeof(Guid)), Is.EqualTo("scalar"));
  }

  [Test]
  public void ClassifyType_Record_IsScalar()
  {
    Assert.That(SubprocessPythonExecutor.ClassifyType(typeof(MyRecord)), Is.EqualTo("scalar"));
  }

  [Test]
  public void ClassifyType_ByteArray_IsBytes()
  {
    // byte[] short-circuits BEFORE the IEnumerable check; classification
    // must yield 'bytes', not 'tabular'.
    Assert.That(SubprocessPythonExecutor.ClassifyType(typeof(byte[])), Is.EqualTo("bytes"));
  }

  [Test]
  public void ClassifyType_IEnumerableOfRecord_IsTabular()
  {
    Assert.That(
      SubprocessPythonExecutor.ClassifyType(typeof(IEnumerable<MyRecord>)),
      Is.EqualTo("tabular")
    );
  }

  [Test]
  public void ClassifyType_ListOfRecord_IsTabular()
  {
    Assert.That(
      SubprocessPythonExecutor.ClassifyType(typeof(List<MyRecord>)),
      Is.EqualTo("tabular")
    );
  }

  [Test]
  public void ClassifyType_PairValueTuple_IsMulti()
  {
    Assert.That(
      SubprocessPythonExecutor.ClassifyType(typeof((int, string))),
      Is.EqualTo("multi")
    );
  }

  [Test]
  public void ClassifyType_TripleValueTuple_IsMulti()
  {
    Assert.That(
      SubprocessPythonExecutor.ClassifyType(typeof(ValueTuple<int, string, double>)),
      Is.EqualTo("multi")
    );
  }

  [Test]
  public void ClassifyType_DirectoryOfRecord_IsDirectory()
  {
    Assert.That(
      SubprocessPythonExecutor.ClassifyType(typeof(DirectoryOf<MyRecord>)),
      Is.EqualTo("directory")
    );
  }

  // ── IsValueTuple / IsDirectoryType / IsEnumerableSchema ───────────

  [Test]
  public void IsValueTuple_TrueForTuple()
  {
    Assert.That(SubprocessPythonExecutor.IsValueTuple(typeof((int, string))), Is.True);
  }

  [Test]
  public void IsValueTuple_FalseForRecord()
  {
    Assert.That(SubprocessPythonExecutor.IsValueTuple(typeof(MyRecord)), Is.False);
  }

  [Test]
  public void IsValueTuple_FalseForList()
  {
    Assert.That(SubprocessPythonExecutor.IsValueTuple(typeof(List<int>)), Is.False);
  }

  [Test]
  public void IsDirectoryType_TrueForDirectoryOf()
  {
    Assert.That(SubprocessPythonExecutor.IsDirectoryType(typeof(DirectoryOf<int>)), Is.True);
  }

  [Test]
  public void IsDirectoryType_FalseForDictionary()
  {
    // Dictionary<string,T> looks shape-similar but the helper is
    // specifically scoped to Flowthru's DirectoryOf<T>.
    Assert.That(
      SubprocessPythonExecutor.IsDirectoryType(typeof(Dictionary<string, int>)),
      Is.False
    );
  }

  [Test]
  public void IsEnumerableSchema_TrueForListOfRecord()
  {
    Assert.That(SubprocessPythonExecutor.IsEnumerableSchema(typeof(List<MyRecord>)), Is.True);
  }

  [Test]
  public void IsEnumerableSchema_FalseForString()
  {
    Assert.That(SubprocessPythonExecutor.IsEnumerableSchema(typeof(string)), Is.False);
  }

  [Test]
  public void IsEnumerableSchema_FalseForByteArray()
  {
    Assert.That(SubprocessPythonExecutor.IsEnumerableSchema(typeof(byte[])), Is.False);
  }

  // ── LooksLikeMarshalling ──────────────────────────────────────────

  [Test]
  public void LooksLikeMarshalling_Marshal_True()
  {
    Assert.That(SubprocessPythonExecutor.LooksLikeMarshalling("failed to marshal value"), Is.True);
  }

  [Test]
  public void LooksLikeMarshalling_Dtype_True()
  {
    Assert.That(SubprocessPythonExecutor.LooksLikeMarshalling("dtype mismatch"), Is.True);
  }

  [Test]
  public void LooksLikeMarshalling_Arrow_True()
  {
    Assert.That(SubprocessPythonExecutor.LooksLikeMarshalling("Arrow IPC buffer too short"), Is.True);
  }

  [Test]
  public void LooksLikeMarshalling_Base64_True()
  {
    Assert.That(SubprocessPythonExecutor.LooksLikeMarshalling("invalid base64 payload"), Is.True);
  }

  [Test]
  public void LooksLikeMarshalling_NotSupportedForArrow_True()
  {
    Assert.That(
      SubprocessPythonExecutor.LooksLikeMarshalling("Decimal not supported for Arrow"),
      Is.True
    );
  }

  [Test]
  public void LooksLikeMarshalling_CaseInsensitive()
  {
    Assert.That(SubprocessPythonExecutor.LooksLikeMarshalling("MARSHAL FAILED"), Is.True);
    Assert.That(SubprocessPythonExecutor.LooksLikeMarshalling("DType issue"), Is.True);
  }

  [Test]
  public void LooksLikeMarshalling_ModuleNotFound_False()
  {
    Assert.That(SubprocessPythonExecutor.LooksLikeMarshalling("module not found"), Is.False);
  }

  [Test]
  public void LooksLikeMarshalling_SyntaxError_False()
  {
    Assert.That(SubprocessPythonExecutor.LooksLikeMarshalling("syntax error"), Is.False);
  }

  // ── ClassifyInvokeFailure ─────────────────────────────────────────

  [Test]
  public void ClassifyInvokeFailure_MarshalLikeMessage_MapsToMarshallingFailed()
  {
    var err = SubprocessPythonExecutor.ClassifyInvokeFailure(
      "module", "func", "Failed to marshal dtype int32"
    );

    Assert.That(err, Is.InstanceOf<PythonRuntimeError.MarshallingFailed>());
    var mf = (PythonRuntimeError.MarshallingFailed)err;
    Assert.That(mf.Source, Is.EqualTo("module.func"));
    Assert.That(mf.Detail, Is.EqualTo("Failed to marshal dtype int32"));
  }

  [Test]
  public void ClassifyInvokeFailure_GenericMessage_MapsToWorkerError()
  {
    var err = SubprocessPythonExecutor.ClassifyInvokeFailure(
      "module", "func", "KeyError: 'x'"
    );

    Assert.That(err, Is.InstanceOf<PythonRuntimeError.WorkerError>());
    var we = (PythonRuntimeError.WorkerError)err;
    Assert.That(we.Module, Is.EqualTo("module"));
    Assert.That(we.Function, Is.EqualTo("func"));
    Assert.That(we.PythonMessage, Is.EqualTo("KeyError: 'x'"));
  }

  // ── ClassifyValidateFailure ───────────────────────────────────────

  [Test]
  public void ClassifyValidateFailure_ImportError_MapsToModuleNotFound()
  {
    var err = SubprocessPythonExecutor.ClassifyValidateFailure(
      "demo", "step", "ImportError: no module named X"
    );

    Assert.That(err, Is.InstanceOf<PythonRuntimeError.ModuleNotFound>());
  }

  [Test]
  public void ClassifyValidateFailure_ModuleNotFoundError_MapsToModuleNotFound()
  {
    var err = SubprocessPythonExecutor.ClassifyValidateFailure(
      "demo", "step", "ModuleNotFoundError: ..."
    );

    Assert.That(err, Is.InstanceOf<PythonRuntimeError.ModuleNotFound>());
  }

  [Test]
  public void ClassifyValidateFailure_CouldNotBeImported_MapsToModuleNotFound()
  {
    var err = SubprocessPythonExecutor.ClassifyValidateFailure(
      "demo", "step", "demo could not be imported"
    );

    Assert.That(err, Is.InstanceOf<PythonRuntimeError.ModuleNotFound>());
  }

  [Test]
  public void ClassifyValidateFailure_AttributeError_MapsToFunctionMissing()
  {
    var err = SubprocessPythonExecutor.ClassifyValidateFailure(
      "demo", "step", "AttributeError: module has no attribute 'step'"
    );

    Assert.That(err, Is.InstanceOf<PythonRuntimeError.FunctionMissing>());
  }

  [Test]
  public void ClassifyValidateFailure_NotFoundInModule_MapsToFunctionMissing()
  {
    var err = SubprocessPythonExecutor.ClassifyValidateFailure(
      "demo", "step", "function 'step' not found in module 'demo'"
    );

    Assert.That(err, Is.InstanceOf<PythonRuntimeError.FunctionMissing>());
  }

  [Test]
  public void ClassifyValidateFailure_HasNoAttribute_MapsToFunctionMissing()
  {
    var err = SubprocessPythonExecutor.ClassifyValidateFailure(
      "demo", "step", "module 'demo' has no attribute 'step'"
    );

    Assert.That(err, Is.InstanceOf<PythonRuntimeError.FunctionMissing>());
  }

  [Test]
  public void ClassifyValidateFailure_StepDecoratorMissing_MapsToDecoratorAbsent()
  {
    var err = SubprocessPythonExecutor.ClassifyValidateFailure(
      "demo", "step", "@step decorator missing"
    );

    Assert.That(err, Is.InstanceOf<PythonRuntimeError.DecoratorAbsent>());
  }

  [Test]
  public void ClassifyValidateFailure_FlowthruInputsMissing_MapsToDecoratorAbsent()
  {
    var err = SubprocessPythonExecutor.ClassifyValidateFailure(
      "demo", "step", "__flowthru_inputs__ missing"
    );

    Assert.That(err, Is.InstanceOf<PythonRuntimeError.DecoratorAbsent>());
  }

  [Test]
  public void ClassifyValidateFailure_DecoratorNotFound_MapsToDecoratorAbsent()
  {
    var err = SubprocessPythonExecutor.ClassifyValidateFailure(
      "demo", "step", "decorator not found"
    );

    Assert.That(err, Is.InstanceOf<PythonRuntimeError.DecoratorAbsent>());
  }

  [Test]
  public void ClassifyValidateFailure_UnrelatedMessage_MapsToWorkerError()
  {
    var err = SubprocessPythonExecutor.ClassifyValidateFailure(
      "demo", "step", "unrelated message"
    );

    Assert.That(err, Is.InstanceOf<PythonRuntimeError.WorkerError>());
  }

  // ── TranslateInspectorResult ──────────────────────────────────────

  [Test]
  public void TranslateInspectorResult_Success_ReturnsValid()
  {
    var result = new JsonObject { ["success"] = true };

    var validated = SubprocessPythonExecutor.TranslateInspectorResult(result, "svc.path");

    Assert.That(validated, Is.InstanceOf<Validated<PreFlightError, FlowUnit>.Valid>());
  }

  [Test]
  public void TranslateInspectorResult_FailureWithAllFields_ReturnsFailWithFormattedDetail()
  {
    var result = new JsonObject
    {
      ["success"] = false,
      ["source"] = "svc.from.python",
      ["error_type"] = "TypeError",
      ["message"] = "expected str, got int",
    };

    var validated = SubprocessPythonExecutor.TranslateInspectorResult(result, "svc.fallback");

    Assert.That(validated, Is.InstanceOf<Validated<PreFlightError, FlowUnit>.Invalid>());
    var invalid = (Validated<PreFlightError, FlowUnit>.Invalid)validated;
    Assert.That(invalid.Errors, Has.Count.EqualTo(1));
    Assert.That(invalid.Errors[0], Is.InstanceOf<PreFlightError.External>());
    var external = (PreFlightError.External)invalid.Errors[0];
    Assert.That(external.Cause, Is.InstanceOf<PythonPreFlightError.ServiceInspectionFailed>());
    var failed = (PythonPreFlightError.ServiceInspectionFailed)external.Cause;
    Assert.That(failed.ServiceClassPath, Is.EqualTo("svc.from.python"));
    Assert.That(failed.Detail, Is.EqualTo("[TypeError] expected str, got int"));
  }

  [Test]
  public void TranslateInspectorResult_FailureMissingSource_FallsBackToParam()
  {
    var result = new JsonObject
    {
      ["success"] = false,
      ["error_type"] = "ValueError",
      ["message"] = "bad input",
    };

    var validated = SubprocessPythonExecutor.TranslateInspectorResult(result, "svc.fallback");

    var invalid = (Validated<PreFlightError, FlowUnit>.Invalid)validated;
    var failed = (PythonPreFlightError.ServiceInspectionFailed)
      ((PreFlightError.External)invalid.Errors[0]).Cause;
    Assert.That(failed.ServiceClassPath, Is.EqualTo("svc.fallback"));
  }

  [Test]
  public void TranslateInspectorResult_FailureMissingMessage_UsesNoMessagePlaceholder()
  {
    var result = new JsonObject
    {
      ["success"] = false,
      ["source"] = "svc.x",
      ["error_type"] = "RuntimeError",
    };

    var validated = SubprocessPythonExecutor.TranslateInspectorResult(result, "svc.fallback");

    var invalid = (Validated<PreFlightError, FlowUnit>.Invalid)validated;
    var failed = (PythonPreFlightError.ServiceInspectionFailed)
      ((PreFlightError.External)invalid.Errors[0]).Cause;
    Assert.That(failed.Detail, Is.EqualTo("[RuntimeError] (no message)"));
  }

  [Test]
  public void TranslateInspectorResult_FailureWhitespaceErrorType_DetailHasNoBrackets()
  {
    var result = new JsonObject
    {
      ["success"] = false,
      ["source"] = "svc.x",
      ["error_type"] = "   ",
      ["message"] = "raw message",
    };

    var validated = SubprocessPythonExecutor.TranslateInspectorResult(result, "svc.fallback");

    var invalid = (Validated<PreFlightError, FlowUnit>.Invalid)validated;
    var failed = (PythonPreFlightError.ServiceInspectionFailed)
      ((PreFlightError.External)invalid.Errors[0]).Cause;
    Assert.That(failed.Detail, Is.EqualTo("raw message"));
  }

  // ── ExtractStringList ─────────────────────────────────────────────

  [Test]
  public void ExtractStringList_ArrayOfStrings_ReturnsValues()
  {
    var root = new JsonObject
    {
      ["inputs"] = new JsonArray("A", "B"),
    };

    var list = SubprocessPythonExecutor.ExtractStringList(root, "inputs");

    Assert.That(list, Is.EqualTo(new[] { "A", "B" }));
  }

  [Test]
  public void ExtractStringList_EmptyArray_ReturnsEmpty()
  {
    var root = new JsonObject { ["inputs"] = new JsonArray() };

    var list = SubprocessPythonExecutor.ExtractStringList(root, "inputs");

    Assert.That(list, Is.Empty);
  }

  [Test]
  public void ExtractStringList_MissingKey_ReturnsEmpty()
  {
    var root = new JsonObject();

    var list = SubprocessPythonExecutor.ExtractStringList(root, "inputs");

    Assert.That(list, Is.Empty);
  }

  [Test]
  public void ExtractStringList_NonArrayValue_ReturnsEmpty()
  {
    var root = new JsonObject { ["inputs"] = "not-an-array" };

    var list = SubprocessPythonExecutor.ExtractStringList(root, "inputs");

    Assert.That(list, Is.Empty);
  }

  [Test]
  public void ExtractStringList_FiltersNullAndEmptyEntries()
  {
    var root = new JsonObject
    {
      ["inputs"] = new JsonArray("A", null, ""),
    };

    var list = SubprocessPythonExecutor.ExtractStringList(root, "inputs");

    Assert.That(list, Is.EqualTo(new[] { "A" }));
  }

  // ── FormatInnerExceptionDetail ────────────────────────────────────

  [Test]
  public void FormatInnerExceptionDetail_SingleLevel_RendersTypeAndMessage()
  {
    var ex = new InvalidOperationException("boom");

    var detail = SubprocessPythonExecutor.FormatInnerExceptionDetail(ex);

    Assert.That(detail, Is.EqualTo("InvalidOperationException: boom"));
  }

  [Test]
  public void FormatInnerExceptionDetail_NestedChain_JoinsWithArrow()
  {
    var inner = new ArgumentException("arg-fail");
    var middle = new NotSupportedException("ns-fail", inner);
    var outer = new InvalidOperationException("io-fail", middle);

    var detail = SubprocessPythonExecutor.FormatInnerExceptionDetail(outer);

    Assert.That(detail, Is.EqualTo(
      "InvalidOperationException: io-fail → NotSupportedException: ns-fail → ArgumentException: arg-fail"
    ));
  }

  // ── BuildDtypeSpecJson ────────────────────────────────────────────

  [Test]
  public void BuildDtypeSpecJson_OnIEnumerableOfRecord_EmitsColumnDtypeMap()
  {
    var node = SubprocessPythonExecutor.BuildDtypeSpecJson(typeof(IEnumerable<MyRecord>));

    var obj = node.AsObject();
    // Column names come from ArrowSchemaMapper's field-name policy
    // (property name unless [SerializedLabel] overrides). Verify the
    // three properties are present with the right dtype mapping; we
    // don't assume any particular casing, just that pandas dtype
    // strings carry through correctly.
    var keys = obj.Select(kv => kv.Key).ToList();
    Assert.That(keys, Has.Count.EqualTo(3),
      "Spec should have exactly one entry per property on MyRecord.");

    // Find each column by case-insensitive match on the property name.
    string FindKey(string propName) =>
      keys.First(k => string.Equals(k, propName, StringComparison.OrdinalIgnoreCase));

    Assert.That(obj[FindKey("Id")]!.GetValue<string>(), Is.EqualTo("int32"));
    Assert.That(obj[FindKey("Name")]!.GetValue<string>(), Is.EqualTo("object"));
    Assert.That(obj[FindKey("Score")]!.GetValue<string>(), Is.EqualTo("float64"));
  }

  // ── BuildDirectorySpecJson ────────────────────────────────────────

  [Test]
  public void BuildDirectorySpecJson_TabularInner_IncludesDtypeSpec()
  {
    // DirectoryOf<IEnumerable<MyRecord>> — inner is tabular.
    var spec = SubprocessPythonExecutor.BuildDirectorySpecJson(
      typeof(DirectoryOf<IEnumerable<MyRecord>>)
    );

    Assert.That(spec["inner_kind"]!.GetValue<string>(), Is.EqualTo("tabular"));
    Assert.That(spec.ContainsKey("dtype_spec"), Is.True,
      "Tabular inner must carry a dtype_spec for the worker to coerce types.");
    Assert.That(spec["dtype_spec"], Is.InstanceOf<JsonObject>());
  }

  [Test]
  public void BuildDirectorySpecJson_ScalarInner_OmitsDtypeSpec()
  {
    var spec = SubprocessPythonExecutor.BuildDirectorySpecJson(typeof(DirectoryOf<int>));

    Assert.That(spec["inner_kind"]!.GetValue<string>(), Is.EqualTo("scalar"));
    Assert.That(spec.ContainsKey("dtype_spec"), Is.False,
      "Scalar inner needs no dtype coercion — the key should be absent.");
  }

  // ── BuildMultiElementSpecs ────────────────────────────────────────

  [Test]
  public void BuildMultiElementSpecs_MixedTuple_ProducesPerElementKindsAndDtypeWhereTabular()
  {
    var arr = SubprocessPythonExecutor.BuildMultiElementSpecs(
      typeof(ValueTuple<int, IEnumerable<MyRecord>, byte[]>)
    );

    Assert.That(arr.Count, Is.EqualTo(3));
    Assert.That(arr[0]!["kind"]!.GetValue<string>(), Is.EqualTo("scalar"));
    Assert.That(arr[1]!["kind"]!.GetValue<string>(), Is.EqualTo("tabular"));
    Assert.That(arr[2]!["kind"]!.GetValue<string>(), Is.EqualTo("bytes"));

    // Only the tabular element carries a dtype_spec — scalar and
    // bytes don't need one.
    Assert.That(arr[0]!.AsObject().ContainsKey("dtype_spec"), Is.False);
    Assert.That(arr[1]!.AsObject().ContainsKey("dtype_spec"), Is.True);
    Assert.That(arr[2]!.AsObject().ContainsKey("dtype_spec"), Is.False);
  }

  // ── EncodeValue / DecodeValue: scalar ─────────────────────────────

  [Test]
  public void EncodeDecode_ScalarInt_RoundTrip()
  {
    var encoded = SubprocessPythonExecutor.EncodeValue(42, typeof(int), "scalar");
    Assert.That(encoded, Is.EqualTo("42"));

    var decoded = SubprocessPythonExecutor.DecodeValue<int>(encoded, typeof(int), "scalar");
    Assert.That(decoded, Is.EqualTo(42));
  }

  [Test]
  public void EncodeDecode_ScalarString_RoundTrip()
  {
    var encoded = SubprocessPythonExecutor.EncodeValue("hello", typeof(string), "scalar");
    // JSON-serialised string is quoted.
    Assert.That(encoded, Is.EqualTo("\"hello\""));

    var decoded = SubprocessPythonExecutor.DecodeValue<string>(encoded, typeof(string), "scalar");
    Assert.That(decoded, Is.EqualTo("hello"));
  }

  // ── EncodeValue / DecodeValue: bytes ──────────────────────────────

  [Test]
  public void EncodeDecode_Bytes_RoundTrip()
  {
    var input = new byte[] { 0x00, 0x01, 0x10, 0x7F, 0xFF };

    var encoded = SubprocessPythonExecutor.EncodeValue(input, typeof(byte[]), "bytes");
    // Base64-encoded payload must be the canonical encoding of the
    // input bytes — this is the wire format the worker expects.
    Assert.That(encoded, Is.EqualTo(Convert.ToBase64String(input)));

    var decoded = SubprocessPythonExecutor.DecodeValue<byte[]>(encoded, typeof(byte[]), "bytes");
    Assert.That(decoded, Is.EqualTo(input));
  }

  // ── EncodeValue / DecodeValue: multi ──────────────────────────────

  [Test]
  public void EncodeDecode_MultiTuple_IntString_RoundTrip()
  {
    var tuple = (7, "seven");
    var tupleType = typeof((int, string));

    var encoded = SubprocessPythonExecutor.EncodeValue(tuple, tupleType, "multi");

    // The envelope is a JSON array of {kind, value} entries.
    var arr = JsonNode.Parse(encoded)!.AsArray();
    Assert.That(arr.Count, Is.EqualTo(2));
    Assert.That(arr[0]!["kind"]!.GetValue<string>(), Is.EqualTo("scalar"));
    Assert.That(arr[1]!["kind"]!.GetValue<string>(), Is.EqualTo("scalar"));

    var decoded = SubprocessPythonExecutor.DecodeValue<(int, string)>(encoded, tupleType, "multi");
    Assert.That(decoded.Item1, Is.EqualTo(7));
    Assert.That(decoded.Item2, Is.EqualTo("seven"));
  }

  // ── EncodeValue / DecodeValue: directory ──────────────────────────

  [Test]
  public void EncodeDecode_DirectoryOfInt_RoundTrip()
  {
    var dict = new Dictionary<string, int>
    {
      ["a.txt"] = 1,
      ["b.txt"] = 2,
    };
    var dir = new DirectoryOf<int>(dict);
    var dirType = typeof(DirectoryOf<int>);

    var encoded = SubprocessPythonExecutor.EncodeValue(dir, dirType, "directory");

    var envelope = JsonNode.Parse(encoded)!.AsObject();
    Assert.That(envelope["inner_kind"]!.GetValue<string>(), Is.EqualTo("scalar"));
    Assert.That(envelope["entries"], Is.InstanceOf<JsonObject>());
    var entries = envelope["entries"]!.AsObject();
    Assert.That(entries.ContainsKey("a.txt"), Is.True);
    Assert.That(entries.ContainsKey("b.txt"), Is.True);

    var decoded = SubprocessPythonExecutor.DecodeValue<DirectoryOf<int>>(encoded, dirType, "directory");
    Assert.That(decoded.Count, Is.EqualTo(2));
    Assert.That(decoded["a.txt"], Is.EqualTo(1));
    Assert.That(decoded["b.txt"], Is.EqualTo(2));
  }

  // ── EncodeTabular + DecodeTabular ─────────────────────────────────

  [Test]
  public void EncodeDecode_Tabular_RoundTripThroughArrow()
  {
    // The tabular path goes through ArrowMarshaller — encode produces a
    // base64-wrapped Arrow IPC buffer; decode parses it back. This is
    // the only kind that involves the marshaller, so we exercise it as
    // one integrated round-trip.
    var rows = new[]
    {
      new MyRecord { Id = 1, Name = "alpha", Score = 1.5 },
      new MyRecord { Id = 2, Name = "beta",  Score = 2.5 },
    };
    var collectionType = typeof(IEnumerable<MyRecord>);

    var encoded = SubprocessPythonExecutor.EncodeTabular(rows, collectionType);

    // Encoding must be valid base64 — anything else means the wire
    // contract with the worker is broken.
    Assert.That(() => Convert.FromBase64String(encoded), Throws.Nothing);

    var decoded = SubprocessPythonExecutor.DecodeTabular<IEnumerable<MyRecord>>(
      encoded, collectionType
    );
    var list = decoded.ToList();
    Assert.That(list, Has.Count.EqualTo(2));
    Assert.That(list[0].Id, Is.EqualTo(1));
    Assert.That(list[0].Name, Is.EqualTo("alpha"));
    Assert.That(list[0].Score, Is.EqualTo(1.5));
    Assert.That(list[1].Id, Is.EqualTo(2));
    Assert.That(list[1].Name, Is.EqualTo("beta"));
    Assert.That(list[1].Score, Is.EqualTo(2.5));
  }
}
