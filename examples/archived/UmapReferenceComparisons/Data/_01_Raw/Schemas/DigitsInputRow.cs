using Flowthru.Core.Abstractions;

namespace UmapReferenceComparisons.Data._01_Raw.Schemas;

/// <summary>
/// Input row for the Digits dataset (8x8 grayscale images).
/// </summary>
/// <remarks>
/// The Digits dataset contains 1,797 samples of 8x8 pixel grayscale images
/// of handwritten digits (0-9). Each pixel value ranges from 0-16.
/// </remarks>
[FlowthruSchema]
public partial record DigitsInputRow
{
    /// <summary>
    /// Unique observation identifier (GUID).
    /// </summary>
    [SerializedLabel("id")]
    public string Id { get; init; } = null!;

    /// <summary>
    /// Class label (0-9 for digit classes).
    /// </summary>
    [SerializedLabel("label")]
    public int Label { get; init; }

    // ===============
    // PIXEL VALUES
    // ===============
    [SerializedLabel("pixel_0")]
    public double Pixel0 { get; init; }

    [SerializedLabel("pixel_1")]
    public double Pixel1 { get; init; }

    [SerializedLabel("pixel_2")]
    public double Pixel2 { get; init; }

    [SerializedLabel("pixel_3")]
    public double Pixel3 { get; init; }

    [SerializedLabel("pixel_4")]
    public double Pixel4 { get; init; }

    [SerializedLabel("pixel_5")]
    public double Pixel5 { get; init; }

    [SerializedLabel("pixel_6")]
    public double Pixel6 { get; init; }

    [SerializedLabel("pixel_7")]
    public double Pixel7 { get; init; }

    [SerializedLabel("pixel_8")]
    public double Pixel8 { get; init; }

    [SerializedLabel("pixel_9")]
    public double Pixel9 { get; init; }

    [SerializedLabel("pixel_10")]
    public double Pixel10 { get; init; }

    [SerializedLabel("pixel_11")]
    public double Pixel11 { get; init; }

    [SerializedLabel("pixel_12")]
    public double Pixel12 { get; init; }

    [SerializedLabel("pixel_13")]
    public double Pixel13 { get; init; }

    [SerializedLabel("pixel_14")]
    public double Pixel14 { get; init; }

    [SerializedLabel("pixel_15")]
    public double Pixel15 { get; init; }

    [SerializedLabel("pixel_16")]
    public double Pixel16 { get; init; }

    [SerializedLabel("pixel_17")]
    public double Pixel17 { get; init; }

    [SerializedLabel("pixel_18")]
    public double Pixel18 { get; init; }

    [SerializedLabel("pixel_19")]
    public double Pixel19 { get; init; }

    [SerializedLabel("pixel_20")]
    public double Pixel20 { get; init; }

    [SerializedLabel("pixel_21")]
    public double Pixel21 { get; init; }

    [SerializedLabel("pixel_22")]
    public double Pixel22 { get; init; }

    [SerializedLabel("pixel_23")]
    public double Pixel23 { get; init; }

    [SerializedLabel("pixel_24")]
    public double Pixel24 { get; init; }

    [SerializedLabel("pixel_25")]
    public double Pixel25 { get; init; }

    [SerializedLabel("pixel_26")]
    public double Pixel26 { get; init; }

    [SerializedLabel("pixel_27")]
    public double Pixel27 { get; init; }

    [SerializedLabel("pixel_28")]
    public double Pixel28 { get; init; }

    [SerializedLabel("pixel_29")]
    public double Pixel29 { get; init; }

    [SerializedLabel("pixel_30")]
    public double Pixel30 { get; init; }

    [SerializedLabel("pixel_31")]
    public double Pixel31 { get; init; }

    [SerializedLabel("pixel_32")]
    public double Pixel32 { get; init; }

    [SerializedLabel("pixel_33")]
    public double Pixel33 { get; init; }

    [SerializedLabel("pixel_34")]
    public double Pixel34 { get; init; }

    [SerializedLabel("pixel_35")]
    public double Pixel35 { get; init; }

    [SerializedLabel("pixel_36")]
    public double Pixel36 { get; init; }

    [SerializedLabel("pixel_37")]
    public double Pixel37 { get; init; }

    [SerializedLabel("pixel_38")]
    public double Pixel38 { get; init; }

    [SerializedLabel("pixel_39")]
    public double Pixel39 { get; init; }

    [SerializedLabel("pixel_40")]
    public double Pixel40 { get; init; }

    [SerializedLabel("pixel_41")]
    public double Pixel41 { get; init; }

    [SerializedLabel("pixel_42")]
    public double Pixel42 { get; init; }

    [SerializedLabel("pixel_43")]
    public double Pixel43 { get; init; }

    [SerializedLabel("pixel_44")]
    public double Pixel44 { get; init; }

    [SerializedLabel("pixel_45")]
    public double Pixel45 { get; init; }

    [SerializedLabel("pixel_46")]
    public double Pixel46 { get; init; }

    [SerializedLabel("pixel_47")]
    public double Pixel47 { get; init; }

    [SerializedLabel("pixel_48")]
    public double Pixel48 { get; init; }

    [SerializedLabel("pixel_49")]
    public double Pixel49 { get; init; }

    [SerializedLabel("pixel_50")]
    public double Pixel50 { get; init; }

    [SerializedLabel("pixel_51")]
    public double Pixel51 { get; init; }

    [SerializedLabel("pixel_52")]
    public double Pixel52 { get; init; }

    [SerializedLabel("pixel_53")]
    public double Pixel53 { get; init; }

    [SerializedLabel("pixel_54")]
    public double Pixel54 { get; init; }

    [SerializedLabel("pixel_55")]
    public double Pixel55 { get; init; }

    [SerializedLabel("pixel_56")]
    public double Pixel56 { get; init; }

    [SerializedLabel("pixel_57")]
    public double Pixel57 { get; init; }

    [SerializedLabel("pixel_58")]
    public double Pixel58 { get; init; }

    [SerializedLabel("pixel_59")]
    public double Pixel59 { get; init; }

    [SerializedLabel("pixel_60")]
    public double Pixel60 { get; init; }

    [SerializedLabel("pixel_61")]
    public double Pixel61 { get; init; }

    [SerializedLabel("pixel_62")]
    public double Pixel62 { get; init; }

    [SerializedLabel("pixel_63")]
    public double Pixel63 { get; init; }
}
