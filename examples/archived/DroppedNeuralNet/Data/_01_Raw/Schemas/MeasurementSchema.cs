using Flowthru.Core.Abstractions;

namespace DroppedNeuralNet.Data._01_Raw.Schemas;

/// <summary>
/// One row of historical sensor data produced by the original neural network.
/// 48 float features, model prediction, and ground-truth label.
/// </summary>
[FlowthruSchema]
public partial record MeasurementSchema
{
    [SerializedLabel("measurement_0")]
    public float Measurement0 { get; init; }

    [SerializedLabel("measurement_1")]
    public float Measurement1 { get; init; }

    [SerializedLabel("measurement_2")]
    public float Measurement2 { get; init; }

    [SerializedLabel("measurement_3")]
    public float Measurement3 { get; init; }

    [SerializedLabel("measurement_4")]
    public float Measurement4 { get; init; }

    [SerializedLabel("measurement_5")]
    public float Measurement5 { get; init; }

    [SerializedLabel("measurement_6")]
    public float Measurement6 { get; init; }

    [SerializedLabel("measurement_7")]
    public float Measurement7 { get; init; }

    [SerializedLabel("measurement_8")]
    public float Measurement8 { get; init; }

    [SerializedLabel("measurement_9")]
    public float Measurement9 { get; init; }

    [SerializedLabel("measurement_10")]
    public float Measurement10 { get; init; }

    [SerializedLabel("measurement_11")]
    public float Measurement11 { get; init; }

    [SerializedLabel("measurement_12")]
    public float Measurement12 { get; init; }

    [SerializedLabel("measurement_13")]
    public float Measurement13 { get; init; }

    [SerializedLabel("measurement_14")]
    public float Measurement14 { get; init; }

    [SerializedLabel("measurement_15")]
    public float Measurement15 { get; init; }

    [SerializedLabel("measurement_16")]
    public float Measurement16 { get; init; }

    [SerializedLabel("measurement_17")]
    public float Measurement17 { get; init; }

    [SerializedLabel("measurement_18")]
    public float Measurement18 { get; init; }

    [SerializedLabel("measurement_19")]
    public float Measurement19 { get; init; }

    [SerializedLabel("measurement_20")]
    public float Measurement20 { get; init; }

    [SerializedLabel("measurement_21")]
    public float Measurement21 { get; init; }

    [SerializedLabel("measurement_22")]
    public float Measurement22 { get; init; }

    [SerializedLabel("measurement_23")]
    public float Measurement23 { get; init; }

    [SerializedLabel("measurement_24")]
    public float Measurement24 { get; init; }

    [SerializedLabel("measurement_25")]
    public float Measurement25 { get; init; }

    [SerializedLabel("measurement_26")]
    public float Measurement26 { get; init; }

    [SerializedLabel("measurement_27")]
    public float Measurement27 { get; init; }

    [SerializedLabel("measurement_28")]
    public float Measurement28 { get; init; }

    [SerializedLabel("measurement_29")]
    public float Measurement29 { get; init; }

    [SerializedLabel("measurement_30")]
    public float Measurement30 { get; init; }

    [SerializedLabel("measurement_31")]
    public float Measurement31 { get; init; }

    [SerializedLabel("measurement_32")]
    public float Measurement32 { get; init; }

    [SerializedLabel("measurement_33")]
    public float Measurement33 { get; init; }

    [SerializedLabel("measurement_34")]
    public float Measurement34 { get; init; }

    [SerializedLabel("measurement_35")]
    public float Measurement35 { get; init; }

    [SerializedLabel("measurement_36")]
    public float Measurement36 { get; init; }

    [SerializedLabel("measurement_37")]
    public float Measurement37 { get; init; }

    [SerializedLabel("measurement_38")]
    public float Measurement38 { get; init; }

    [SerializedLabel("measurement_39")]
    public float Measurement39 { get; init; }

    [SerializedLabel("measurement_40")]
    public float Measurement40 { get; init; }

    [SerializedLabel("measurement_41")]
    public float Measurement41 { get; init; }

    [SerializedLabel("measurement_42")]
    public float Measurement42 { get; init; }

    [SerializedLabel("measurement_43")]
    public float Measurement43 { get; init; }

    [SerializedLabel("measurement_44")]
    public float Measurement44 { get; init; }

    [SerializedLabel("measurement_45")]
    public float Measurement45 { get; init; }

    [SerializedLabel("measurement_46")]
    public float Measurement46 { get; init; }

    [SerializedLabel("measurement_47")]
    public float Measurement47 { get; init; }

    [SerializedLabel("pred")]
    public float Pred { get; init; }

    [SerializedLabel("true")]
    public float TrueValue { get; init; }
}
