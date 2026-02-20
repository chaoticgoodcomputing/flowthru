using Flowthru.Abstractions;

namespace UmapReferenceComparisons.Data._01_Raw.Schemas;

/// <summary>
/// Input row for the MNIST dataset (28x28 grayscale images).
///
/// I hate this.
/// </summary>
/// <remarks>
/// The MNIST dataset contains 70,000 samples of 28x28 pixel grayscale images
/// of handwritten digits (0-9). Each pixel value ranges from 0-255.
/// </remarks>
[FlowthruSchema]
public partial record MnistInputRow
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
  public long Pixel0 { get; init; }

  [SerializedLabel("pixel_1")]
  public long Pixel1 { get; init; }

  [SerializedLabel("pixel_2")]
  public long Pixel2 { get; init; }

  [SerializedLabel("pixel_3")]
  public long Pixel3 { get; init; }

  [SerializedLabel("pixel_4")]
  public long Pixel4 { get; init; }

  [SerializedLabel("pixel_5")]
  public long Pixel5 { get; init; }

  [SerializedLabel("pixel_6")]
  public long Pixel6 { get; init; }

  [SerializedLabel("pixel_7")]
  public long Pixel7 { get; init; }

  [SerializedLabel("pixel_8")]
  public long Pixel8 { get; init; }

  [SerializedLabel("pixel_9")]
  public long Pixel9 { get; init; }

  [SerializedLabel("pixel_10")]
  public long Pixel10 { get; init; }

  [SerializedLabel("pixel_11")]
  public long Pixel11 { get; init; }

  [SerializedLabel("pixel_12")]
  public long Pixel12 { get; init; }

  [SerializedLabel("pixel_13")]
  public long Pixel13 { get; init; }

  [SerializedLabel("pixel_14")]
  public long Pixel14 { get; init; }

  [SerializedLabel("pixel_15")]
  public long Pixel15 { get; init; }

  [SerializedLabel("pixel_16")]
  public long Pixel16 { get; init; }

  [SerializedLabel("pixel_17")]
  public long Pixel17 { get; init; }

  [SerializedLabel("pixel_18")]
  public long Pixel18 { get; init; }

  [SerializedLabel("pixel_19")]
  public long Pixel19 { get; init; }

  [SerializedLabel("pixel_20")]
  public long Pixel20 { get; init; }

  [SerializedLabel("pixel_21")]
  public long Pixel21 { get; init; }

  [SerializedLabel("pixel_22")]
  public long Pixel22 { get; init; }

  [SerializedLabel("pixel_23")]
  public long Pixel23 { get; init; }

  [SerializedLabel("pixel_24")]
  public long Pixel24 { get; init; }

  [SerializedLabel("pixel_25")]
  public long Pixel25 { get; init; }

  [SerializedLabel("pixel_26")]
  public long Pixel26 { get; init; }

  [SerializedLabel("pixel_27")]
  public long Pixel27 { get; init; }

  [SerializedLabel("pixel_28")]
  public long Pixel28 { get; init; }

  [SerializedLabel("pixel_29")]
  public long Pixel29 { get; init; }

  [SerializedLabel("pixel_30")]
  public long Pixel30 { get; init; }

  [SerializedLabel("pixel_31")]
  public long Pixel31 { get; init; }

  [SerializedLabel("pixel_32")]
  public long Pixel32 { get; init; }

  [SerializedLabel("pixel_33")]
  public long Pixel33 { get; init; }

  [SerializedLabel("pixel_34")]
  public long Pixel34 { get; init; }

  [SerializedLabel("pixel_35")]
  public long Pixel35 { get; init; }

  [SerializedLabel("pixel_36")]
  public long Pixel36 { get; init; }

  [SerializedLabel("pixel_37")]
  public long Pixel37 { get; init; }

  [SerializedLabel("pixel_38")]
  public long Pixel38 { get; init; }

  [SerializedLabel("pixel_39")]
  public long Pixel39 { get; init; }

  [SerializedLabel("pixel_40")]
  public long Pixel40 { get; init; }

  [SerializedLabel("pixel_41")]
  public long Pixel41 { get; init; }

  [SerializedLabel("pixel_42")]
  public long Pixel42 { get; init; }

  [SerializedLabel("pixel_43")]
  public long Pixel43 { get; init; }

  [SerializedLabel("pixel_44")]
  public long Pixel44 { get; init; }

  [SerializedLabel("pixel_45")]
  public long Pixel45 { get; init; }

  [SerializedLabel("pixel_46")]
  public long Pixel46 { get; init; }

  [SerializedLabel("pixel_47")]
  public long Pixel47 { get; init; }

  [SerializedLabel("pixel_48")]
  public long Pixel48 { get; init; }

  [SerializedLabel("pixel_49")]
  public long Pixel49 { get; init; }

  [SerializedLabel("pixel_50")]
  public long Pixel50 { get; init; }

  [SerializedLabel("pixel_51")]
  public long Pixel51 { get; init; }

  [SerializedLabel("pixel_52")]
  public long Pixel52 { get; init; }

  [SerializedLabel("pixel_53")]
  public long Pixel53 { get; init; }

  [SerializedLabel("pixel_54")]
  public long Pixel54 { get; init; }

  [SerializedLabel("pixel_55")]
  public long Pixel55 { get; init; }

  [SerializedLabel("pixel_56")]
  public long Pixel56 { get; init; }

  [SerializedLabel("pixel_57")]
  public long Pixel57 { get; init; }

  [SerializedLabel("pixel_58")]
  public long Pixel58 { get; init; }

  [SerializedLabel("pixel_59")]
  public long Pixel59 { get; init; }

  [SerializedLabel("pixel_60")]
  public long Pixel60 { get; init; }

  [SerializedLabel("pixel_61")]
  public long Pixel61 { get; init; }

  [SerializedLabel("pixel_62")]
  public long Pixel62 { get; init; }

  [SerializedLabel("pixel_63")]
  public long Pixel63 { get; init; }

  [SerializedLabel("pixel_64")]
  public long Pixel64 { get; init; }

  [SerializedLabel("pixel_65")]
  public long Pixel65 { get; init; }

  [SerializedLabel("pixel_66")]
  public long Pixel66 { get; init; }

  [SerializedLabel("pixel_67")]
  public long Pixel67 { get; init; }

  [SerializedLabel("pixel_68")]
  public long Pixel68 { get; init; }

  [SerializedLabel("pixel_69")]
  public long Pixel69 { get; init; }

  [SerializedLabel("pixel_70")]
  public long Pixel70 { get; init; }

  [SerializedLabel("pixel_71")]
  public long Pixel71 { get; init; }

  [SerializedLabel("pixel_72")]
  public long Pixel72 { get; init; }

  [SerializedLabel("pixel_73")]
  public long Pixel73 { get; init; }

  [SerializedLabel("pixel_74")]
  public long Pixel74 { get; init; }

  [SerializedLabel("pixel_75")]
  public long Pixel75 { get; init; }

  [SerializedLabel("pixel_76")]
  public long Pixel76 { get; init; }

  [SerializedLabel("pixel_77")]
  public long Pixel77 { get; init; }

  [SerializedLabel("pixel_78")]
  public long Pixel78 { get; init; }

  [SerializedLabel("pixel_79")]
  public long Pixel79 { get; init; }

  [SerializedLabel("pixel_80")]
  public long Pixel80 { get; init; }

  [SerializedLabel("pixel_81")]
  public long Pixel81 { get; init; }

  [SerializedLabel("pixel_82")]
  public long Pixel82 { get; init; }

  [SerializedLabel("pixel_83")]
  public long Pixel83 { get; init; }

  [SerializedLabel("pixel_84")]
  public long Pixel84 { get; init; }

  [SerializedLabel("pixel_85")]
  public long Pixel85 { get; init; }

  [SerializedLabel("pixel_86")]
  public long Pixel86 { get; init; }

  [SerializedLabel("pixel_87")]
  public long Pixel87 { get; init; }

  [SerializedLabel("pixel_88")]
  public long Pixel88 { get; init; }

  [SerializedLabel("pixel_89")]
  public long Pixel89 { get; init; }

  [SerializedLabel("pixel_90")]
  public long Pixel90 { get; init; }

  [SerializedLabel("pixel_91")]
  public long Pixel91 { get; init; }

  [SerializedLabel("pixel_92")]
  public long Pixel92 { get; init; }

  [SerializedLabel("pixel_93")]
  public long Pixel93 { get; init; }

  [SerializedLabel("pixel_94")]
  public long Pixel94 { get; init; }

  [SerializedLabel("pixel_95")]
  public long Pixel95 { get; init; }

  [SerializedLabel("pixel_96")]
  public long Pixel96 { get; init; }

  [SerializedLabel("pixel_97")]
  public long Pixel97 { get; init; }

  [SerializedLabel("pixel_98")]
  public long Pixel98 { get; init; }

  [SerializedLabel("pixel_99")]
  public long Pixel99 { get; init; }

  [SerializedLabel("pixel_100")]
  public long Pixel100 { get; init; }

  [SerializedLabel("pixel_101")]
  public long Pixel101 { get; init; }

  [SerializedLabel("pixel_102")]
  public long Pixel102 { get; init; }

  [SerializedLabel("pixel_103")]
  public long Pixel103 { get; init; }

  [SerializedLabel("pixel_104")]
  public long Pixel104 { get; init; }

  [SerializedLabel("pixel_105")]
  public long Pixel105 { get; init; }

  [SerializedLabel("pixel_106")]
  public long Pixel106 { get; init; }

  [SerializedLabel("pixel_107")]
  public long Pixel107 { get; init; }

  [SerializedLabel("pixel_108")]
  public long Pixel108 { get; init; }

  [SerializedLabel("pixel_109")]
  public long Pixel109 { get; init; }

  [SerializedLabel("pixel_110")]
  public long Pixel110 { get; init; }

  [SerializedLabel("pixel_111")]
  public long Pixel111 { get; init; }

  [SerializedLabel("pixel_112")]
  public long Pixel112 { get; init; }

  [SerializedLabel("pixel_113")]
  public long Pixel113 { get; init; }

  [SerializedLabel("pixel_114")]
  public long Pixel114 { get; init; }

  [SerializedLabel("pixel_115")]
  public long Pixel115 { get; init; }

  [SerializedLabel("pixel_116")]
  public long Pixel116 { get; init; }

  [SerializedLabel("pixel_117")]
  public long Pixel117 { get; init; }

  [SerializedLabel("pixel_118")]
  public long Pixel118 { get; init; }

  [SerializedLabel("pixel_119")]
  public long Pixel119 { get; init; }

  [SerializedLabel("pixel_120")]
  public long Pixel120 { get; init; }

  [SerializedLabel("pixel_121")]
  public long Pixel121 { get; init; }

  [SerializedLabel("pixel_122")]
  public long Pixel122 { get; init; }

  [SerializedLabel("pixel_123")]
  public long Pixel123 { get; init; }

  [SerializedLabel("pixel_124")]
  public long Pixel124 { get; init; }

  [SerializedLabel("pixel_125")]
  public long Pixel125 { get; init; }

  [SerializedLabel("pixel_126")]
  public long Pixel126 { get; init; }

  [SerializedLabel("pixel_127")]
  public long Pixel127 { get; init; }

  [SerializedLabel("pixel_128")]
  public long Pixel128 { get; init; }

  [SerializedLabel("pixel_129")]
  public long Pixel129 { get; init; }

  [SerializedLabel("pixel_130")]
  public long Pixel130 { get; init; }

  [SerializedLabel("pixel_131")]
  public long Pixel131 { get; init; }

  [SerializedLabel("pixel_132")]
  public long Pixel132 { get; init; }

  [SerializedLabel("pixel_133")]
  public long Pixel133 { get; init; }

  [SerializedLabel("pixel_134")]
  public long Pixel134 { get; init; }

  [SerializedLabel("pixel_135")]
  public long Pixel135 { get; init; }

  [SerializedLabel("pixel_136")]
  public long Pixel136 { get; init; }

  [SerializedLabel("pixel_137")]
  public long Pixel137 { get; init; }

  [SerializedLabel("pixel_138")]
  public long Pixel138 { get; init; }

  [SerializedLabel("pixel_139")]
  public long Pixel139 { get; init; }

  [SerializedLabel("pixel_140")]
  public long Pixel140 { get; init; }

  [SerializedLabel("pixel_141")]
  public long Pixel141 { get; init; }

  [SerializedLabel("pixel_142")]
  public long Pixel142 { get; init; }

  [SerializedLabel("pixel_143")]
  public long Pixel143 { get; init; }

  [SerializedLabel("pixel_144")]
  public long Pixel144 { get; init; }

  [SerializedLabel("pixel_145")]
  public long Pixel145 { get; init; }

  [SerializedLabel("pixel_146")]
  public long Pixel146 { get; init; }

  [SerializedLabel("pixel_147")]
  public long Pixel147 { get; init; }

  [SerializedLabel("pixel_148")]
  public long Pixel148 { get; init; }

  [SerializedLabel("pixel_149")]
  public long Pixel149 { get; init; }

  [SerializedLabel("pixel_150")]
  public long Pixel150 { get; init; }

  [SerializedLabel("pixel_151")]
  public long Pixel151 { get; init; }

  [SerializedLabel("pixel_152")]
  public long Pixel152 { get; init; }

  [SerializedLabel("pixel_153")]
  public long Pixel153 { get; init; }

  [SerializedLabel("pixel_154")]
  public long Pixel154 { get; init; }

  [SerializedLabel("pixel_155")]
  public long Pixel155 { get; init; }

  [SerializedLabel("pixel_156")]
  public long Pixel156 { get; init; }

  [SerializedLabel("pixel_157")]
  public long Pixel157 { get; init; }

  [SerializedLabel("pixel_158")]
  public long Pixel158 { get; init; }

  [SerializedLabel("pixel_159")]
  public long Pixel159 { get; init; }

  [SerializedLabel("pixel_160")]
  public long Pixel160 { get; init; }

  [SerializedLabel("pixel_161")]
  public long Pixel161 { get; init; }

  [SerializedLabel("pixel_162")]
  public long Pixel162 { get; init; }

  [SerializedLabel("pixel_163")]
  public long Pixel163 { get; init; }

  [SerializedLabel("pixel_164")]
  public long Pixel164 { get; init; }

  [SerializedLabel("pixel_165")]
  public long Pixel165 { get; init; }

  [SerializedLabel("pixel_166")]
  public long Pixel166 { get; init; }

  [SerializedLabel("pixel_167")]
  public long Pixel167 { get; init; }

  [SerializedLabel("pixel_168")]
  public long Pixel168 { get; init; }

  [SerializedLabel("pixel_169")]
  public long Pixel169 { get; init; }

  [SerializedLabel("pixel_170")]
  public long Pixel170 { get; init; }

  [SerializedLabel("pixel_171")]
  public long Pixel171 { get; init; }

  [SerializedLabel("pixel_172")]
  public long Pixel172 { get; init; }

  [SerializedLabel("pixel_173")]
  public long Pixel173 { get; init; }

  [SerializedLabel("pixel_174")]
  public long Pixel174 { get; init; }

  [SerializedLabel("pixel_175")]
  public long Pixel175 { get; init; }

  [SerializedLabel("pixel_176")]
  public long Pixel176 { get; init; }

  [SerializedLabel("pixel_177")]
  public long Pixel177 { get; init; }

  [SerializedLabel("pixel_178")]
  public long Pixel178 { get; init; }

  [SerializedLabel("pixel_179")]
  public long Pixel179 { get; init; }

  [SerializedLabel("pixel_180")]
  public long Pixel180 { get; init; }

  [SerializedLabel("pixel_181")]
  public long Pixel181 { get; init; }

  [SerializedLabel("pixel_182")]
  public long Pixel182 { get; init; }

  [SerializedLabel("pixel_183")]
  public long Pixel183 { get; init; }

  [SerializedLabel("pixel_184")]
  public long Pixel184 { get; init; }

  [SerializedLabel("pixel_185")]
  public long Pixel185 { get; init; }

  [SerializedLabel("pixel_186")]
  public long Pixel186 { get; init; }

  [SerializedLabel("pixel_187")]
  public long Pixel187 { get; init; }

  [SerializedLabel("pixel_188")]
  public long Pixel188 { get; init; }

  [SerializedLabel("pixel_189")]
  public long Pixel189 { get; init; }

  [SerializedLabel("pixel_190")]
  public long Pixel190 { get; init; }

  [SerializedLabel("pixel_191")]
  public long Pixel191 { get; init; }

  [SerializedLabel("pixel_192")]
  public long Pixel192 { get; init; }

  [SerializedLabel("pixel_193")]
  public long Pixel193 { get; init; }

  [SerializedLabel("pixel_194")]
  public long Pixel194 { get; init; }

  [SerializedLabel("pixel_195")]
  public long Pixel195 { get; init; }

  [SerializedLabel("pixel_196")]
  public long Pixel196 { get; init; }

  [SerializedLabel("pixel_197")]
  public long Pixel197 { get; init; }

  [SerializedLabel("pixel_198")]
  public long Pixel198 { get; init; }

  [SerializedLabel("pixel_199")]
  public long Pixel199 { get; init; }

  [SerializedLabel("pixel_200")]
  public long Pixel200 { get; init; }

  [SerializedLabel("pixel_201")]
  public long Pixel201 { get; init; }

  [SerializedLabel("pixel_202")]
  public long Pixel202 { get; init; }

  [SerializedLabel("pixel_203")]
  public long Pixel203 { get; init; }

  [SerializedLabel("pixel_204")]
  public long Pixel204 { get; init; }

  [SerializedLabel("pixel_205")]
  public long Pixel205 { get; init; }

  [SerializedLabel("pixel_206")]
  public long Pixel206 { get; init; }

  [SerializedLabel("pixel_207")]
  public long Pixel207 { get; init; }

  [SerializedLabel("pixel_208")]
  public long Pixel208 { get; init; }

  [SerializedLabel("pixel_209")]
  public long Pixel209 { get; init; }

  [SerializedLabel("pixel_210")]
  public long Pixel210 { get; init; }

  [SerializedLabel("pixel_211")]
  public long Pixel211 { get; init; }

  [SerializedLabel("pixel_212")]
  public long Pixel212 { get; init; }

  [SerializedLabel("pixel_213")]
  public long Pixel213 { get; init; }

  [SerializedLabel("pixel_214")]
  public long Pixel214 { get; init; }

  [SerializedLabel("pixel_215")]
  public long Pixel215 { get; init; }

  [SerializedLabel("pixel_216")]
  public long Pixel216 { get; init; }

  [SerializedLabel("pixel_217")]
  public long Pixel217 { get; init; }

  [SerializedLabel("pixel_218")]
  public long Pixel218 { get; init; }

  [SerializedLabel("pixel_219")]
  public long Pixel219 { get; init; }

  [SerializedLabel("pixel_220")]
  public long Pixel220 { get; init; }

  [SerializedLabel("pixel_221")]
  public long Pixel221 { get; init; }

  [SerializedLabel("pixel_222")]
  public long Pixel222 { get; init; }

  [SerializedLabel("pixel_223")]
  public long Pixel223 { get; init; }

  [SerializedLabel("pixel_224")]
  public long Pixel224 { get; init; }

  [SerializedLabel("pixel_225")]
  public long Pixel225 { get; init; }

  [SerializedLabel("pixel_226")]
  public long Pixel226 { get; init; }

  [SerializedLabel("pixel_227")]
  public long Pixel227 { get; init; }

  [SerializedLabel("pixel_228")]
  public long Pixel228 { get; init; }

  [SerializedLabel("pixel_229")]
  public long Pixel229 { get; init; }

  [SerializedLabel("pixel_230")]
  public long Pixel230 { get; init; }

  [SerializedLabel("pixel_231")]
  public long Pixel231 { get; init; }

  [SerializedLabel("pixel_232")]
  public long Pixel232 { get; init; }

  [SerializedLabel("pixel_233")]
  public long Pixel233 { get; init; }

  [SerializedLabel("pixel_234")]
  public long Pixel234 { get; init; }

  [SerializedLabel("pixel_235")]
  public long Pixel235 { get; init; }

  [SerializedLabel("pixel_236")]
  public long Pixel236 { get; init; }

  [SerializedLabel("pixel_237")]
  public long Pixel237 { get; init; }

  [SerializedLabel("pixel_238")]
  public long Pixel238 { get; init; }

  [SerializedLabel("pixel_239")]
  public long Pixel239 { get; init; }

  [SerializedLabel("pixel_240")]
  public long Pixel240 { get; init; }

  [SerializedLabel("pixel_241")]
  public long Pixel241 { get; init; }

  [SerializedLabel("pixel_242")]
  public long Pixel242 { get; init; }

  [SerializedLabel("pixel_243")]
  public long Pixel243 { get; init; }

  [SerializedLabel("pixel_244")]
  public long Pixel244 { get; init; }

  [SerializedLabel("pixel_245")]
  public long Pixel245 { get; init; }

  [SerializedLabel("pixel_246")]
  public long Pixel246 { get; init; }

  [SerializedLabel("pixel_247")]
  public long Pixel247 { get; init; }

  [SerializedLabel("pixel_248")]
  public long Pixel248 { get; init; }

  [SerializedLabel("pixel_249")]
  public long Pixel249 { get; init; }

  [SerializedLabel("pixel_250")]
  public long Pixel250 { get; init; }

  [SerializedLabel("pixel_251")]
  public long Pixel251 { get; init; }

  [SerializedLabel("pixel_252")]
  public long Pixel252 { get; init; }

  [SerializedLabel("pixel_253")]
  public long Pixel253 { get; init; }

  [SerializedLabel("pixel_254")]
  public long Pixel254 { get; init; }

  [SerializedLabel("pixel_255")]
  public long Pixel255 { get; init; }

  [SerializedLabel("pixel_256")]
  public long Pixel256 { get; init; }

  [SerializedLabel("pixel_257")]
  public long Pixel257 { get; init; }

  [SerializedLabel("pixel_258")]
  public long Pixel258 { get; init; }

  [SerializedLabel("pixel_259")]
  public long Pixel259 { get; init; }

  [SerializedLabel("pixel_260")]
  public long Pixel260 { get; init; }

  [SerializedLabel("pixel_261")]
  public long Pixel261 { get; init; }

  [SerializedLabel("pixel_262")]
  public long Pixel262 { get; init; }

  [SerializedLabel("pixel_263")]
  public long Pixel263 { get; init; }

  [SerializedLabel("pixel_264")]
  public long Pixel264 { get; init; }

  [SerializedLabel("pixel_265")]
  public long Pixel265 { get; init; }

  [SerializedLabel("pixel_266")]
  public long Pixel266 { get; init; }

  [SerializedLabel("pixel_267")]
  public long Pixel267 { get; init; }

  [SerializedLabel("pixel_268")]
  public long Pixel268 { get; init; }

  [SerializedLabel("pixel_269")]
  public long Pixel269 { get; init; }

  [SerializedLabel("pixel_270")]
  public long Pixel270 { get; init; }

  [SerializedLabel("pixel_271")]
  public long Pixel271 { get; init; }

  [SerializedLabel("pixel_272")]
  public long Pixel272 { get; init; }

  [SerializedLabel("pixel_273")]
  public long Pixel273 { get; init; }

  [SerializedLabel("pixel_274")]
  public long Pixel274 { get; init; }

  [SerializedLabel("pixel_275")]
  public long Pixel275 { get; init; }

  [SerializedLabel("pixel_276")]
  public long Pixel276 { get; init; }

  [SerializedLabel("pixel_277")]
  public long Pixel277 { get; init; }

  [SerializedLabel("pixel_278")]
  public long Pixel278 { get; init; }

  [SerializedLabel("pixel_279")]
  public long Pixel279 { get; init; }

  [SerializedLabel("pixel_280")]
  public long Pixel280 { get; init; }

  [SerializedLabel("pixel_281")]
  public long Pixel281 { get; init; }

  [SerializedLabel("pixel_282")]
  public long Pixel282 { get; init; }

  [SerializedLabel("pixel_283")]
  public long Pixel283 { get; init; }

  [SerializedLabel("pixel_284")]
  public long Pixel284 { get; init; }

  [SerializedLabel("pixel_285")]
  public long Pixel285 { get; init; }

  [SerializedLabel("pixel_286")]
  public long Pixel286 { get; init; }

  [SerializedLabel("pixel_287")]
  public long Pixel287 { get; init; }

  [SerializedLabel("pixel_288")]
  public long Pixel288 { get; init; }

  [SerializedLabel("pixel_289")]
  public long Pixel289 { get; init; }

  [SerializedLabel("pixel_290")]
  public long Pixel290 { get; init; }

  [SerializedLabel("pixel_291")]
  public long Pixel291 { get; init; }

  [SerializedLabel("pixel_292")]
  public long Pixel292 { get; init; }

  [SerializedLabel("pixel_293")]
  public long Pixel293 { get; init; }

  [SerializedLabel("pixel_294")]
  public long Pixel294 { get; init; }

  [SerializedLabel("pixel_295")]
  public long Pixel295 { get; init; }

  [SerializedLabel("pixel_296")]
  public long Pixel296 { get; init; }

  [SerializedLabel("pixel_297")]
  public long Pixel297 { get; init; }

  [SerializedLabel("pixel_298")]
  public long Pixel298 { get; init; }

  [SerializedLabel("pixel_299")]
  public long Pixel299 { get; init; }

  [SerializedLabel("pixel_300")]
  public long Pixel300 { get; init; }

  [SerializedLabel("pixel_301")]
  public long Pixel301 { get; init; }

  [SerializedLabel("pixel_302")]
  public long Pixel302 { get; init; }

  [SerializedLabel("pixel_303")]
  public long Pixel303 { get; init; }

  [SerializedLabel("pixel_304")]
  public long Pixel304 { get; init; }

  [SerializedLabel("pixel_305")]
  public long Pixel305 { get; init; }

  [SerializedLabel("pixel_306")]
  public long Pixel306 { get; init; }

  [SerializedLabel("pixel_307")]
  public long Pixel307 { get; init; }

  [SerializedLabel("pixel_308")]
  public long Pixel308 { get; init; }

  [SerializedLabel("pixel_309")]
  public long Pixel309 { get; init; }

  [SerializedLabel("pixel_310")]
  public long Pixel310 { get; init; }

  [SerializedLabel("pixel_311")]
  public long Pixel311 { get; init; }

  [SerializedLabel("pixel_312")]
  public long Pixel312 { get; init; }

  [SerializedLabel("pixel_313")]
  public long Pixel313 { get; init; }

  [SerializedLabel("pixel_314")]
  public long Pixel314 { get; init; }

  [SerializedLabel("pixel_315")]
  public long Pixel315 { get; init; }

  [SerializedLabel("pixel_316")]
  public long Pixel316 { get; init; }

  [SerializedLabel("pixel_317")]
  public long Pixel317 { get; init; }

  [SerializedLabel("pixel_318")]
  public long Pixel318 { get; init; }

  [SerializedLabel("pixel_319")]
  public long Pixel319 { get; init; }

  [SerializedLabel("pixel_320")]
  public long Pixel320 { get; init; }

  [SerializedLabel("pixel_321")]
  public long Pixel321 { get; init; }

  [SerializedLabel("pixel_322")]
  public long Pixel322 { get; init; }

  [SerializedLabel("pixel_323")]
  public long Pixel323 { get; init; }

  [SerializedLabel("pixel_324")]
  public long Pixel324 { get; init; }

  [SerializedLabel("pixel_325")]
  public long Pixel325 { get; init; }

  [SerializedLabel("pixel_326")]
  public long Pixel326 { get; init; }

  [SerializedLabel("pixel_327")]
  public long Pixel327 { get; init; }

  [SerializedLabel("pixel_328")]
  public long Pixel328 { get; init; }

  [SerializedLabel("pixel_329")]
  public long Pixel329 { get; init; }

  [SerializedLabel("pixel_330")]
  public long Pixel330 { get; init; }

  [SerializedLabel("pixel_331")]
  public long Pixel331 { get; init; }

  [SerializedLabel("pixel_332")]
  public long Pixel332 { get; init; }

  [SerializedLabel("pixel_333")]
  public long Pixel333 { get; init; }

  [SerializedLabel("pixel_334")]
  public long Pixel334 { get; init; }

  [SerializedLabel("pixel_335")]
  public long Pixel335 { get; init; }

  [SerializedLabel("pixel_336")]
  public long Pixel336 { get; init; }

  [SerializedLabel("pixel_337")]
  public long Pixel337 { get; init; }

  [SerializedLabel("pixel_338")]
  public long Pixel338 { get; init; }

  [SerializedLabel("pixel_339")]
  public long Pixel339 { get; init; }

  [SerializedLabel("pixel_340")]
  public long Pixel340 { get; init; }

  [SerializedLabel("pixel_341")]
  public long Pixel341 { get; init; }

  [SerializedLabel("pixel_342")]
  public long Pixel342 { get; init; }

  [SerializedLabel("pixel_343")]
  public long Pixel343 { get; init; }

  [SerializedLabel("pixel_344")]
  public long Pixel344 { get; init; }

  [SerializedLabel("pixel_345")]
  public long Pixel345 { get; init; }

  [SerializedLabel("pixel_346")]
  public long Pixel346 { get; init; }

  [SerializedLabel("pixel_347")]
  public long Pixel347 { get; init; }

  [SerializedLabel("pixel_348")]
  public long Pixel348 { get; init; }

  [SerializedLabel("pixel_349")]
  public long Pixel349 { get; init; }

  [SerializedLabel("pixel_350")]
  public long Pixel350 { get; init; }

  [SerializedLabel("pixel_351")]
  public long Pixel351 { get; init; }

  [SerializedLabel("pixel_352")]
  public long Pixel352 { get; init; }

  [SerializedLabel("pixel_353")]
  public long Pixel353 { get; init; }

  [SerializedLabel("pixel_354")]
  public long Pixel354 { get; init; }

  [SerializedLabel("pixel_355")]
  public long Pixel355 { get; init; }

  [SerializedLabel("pixel_356")]
  public long Pixel356 { get; init; }

  [SerializedLabel("pixel_357")]
  public long Pixel357 { get; init; }

  [SerializedLabel("pixel_358")]
  public long Pixel358 { get; init; }

  [SerializedLabel("pixel_359")]
  public long Pixel359 { get; init; }

  [SerializedLabel("pixel_360")]
  public long Pixel360 { get; init; }

  [SerializedLabel("pixel_361")]
  public long Pixel361 { get; init; }

  [SerializedLabel("pixel_362")]
  public long Pixel362 { get; init; }

  [SerializedLabel("pixel_363")]
  public long Pixel363 { get; init; }

  [SerializedLabel("pixel_364")]
  public long Pixel364 { get; init; }

  [SerializedLabel("pixel_365")]
  public long Pixel365 { get; init; }

  [SerializedLabel("pixel_366")]
  public long Pixel366 { get; init; }

  [SerializedLabel("pixel_367")]
  public long Pixel367 { get; init; }

  [SerializedLabel("pixel_368")]
  public long Pixel368 { get; init; }

  [SerializedLabel("pixel_369")]
  public long Pixel369 { get; init; }

  [SerializedLabel("pixel_370")]
  public long Pixel370 { get; init; }

  [SerializedLabel("pixel_371")]
  public long Pixel371 { get; init; }

  [SerializedLabel("pixel_372")]
  public long Pixel372 { get; init; }

  [SerializedLabel("pixel_373")]
  public long Pixel373 { get; init; }

  [SerializedLabel("pixel_374")]
  public long Pixel374 { get; init; }

  [SerializedLabel("pixel_375")]
  public long Pixel375 { get; init; }

  [SerializedLabel("pixel_376")]
  public long Pixel376 { get; init; }

  [SerializedLabel("pixel_377")]
  public long Pixel377 { get; init; }

  [SerializedLabel("pixel_378")]
  public long Pixel378 { get; init; }

  [SerializedLabel("pixel_379")]
  public long Pixel379 { get; init; }

  [SerializedLabel("pixel_380")]
  public long Pixel380 { get; init; }

  [SerializedLabel("pixel_381")]
  public long Pixel381 { get; init; }

  [SerializedLabel("pixel_382")]
  public long Pixel382 { get; init; }

  [SerializedLabel("pixel_383")]
  public long Pixel383 { get; init; }

  [SerializedLabel("pixel_384")]
  public long Pixel384 { get; init; }

  [SerializedLabel("pixel_385")]
  public long Pixel385 { get; init; }

  [SerializedLabel("pixel_386")]
  public long Pixel386 { get; init; }

  [SerializedLabel("pixel_387")]
  public long Pixel387 { get; init; }

  [SerializedLabel("pixel_388")]
  public long Pixel388 { get; init; }

  [SerializedLabel("pixel_389")]
  public long Pixel389 { get; init; }

  [SerializedLabel("pixel_390")]
  public long Pixel390 { get; init; }

  [SerializedLabel("pixel_391")]
  public long Pixel391 { get; init; }

  [SerializedLabel("pixel_392")]
  public long Pixel392 { get; init; }

  [SerializedLabel("pixel_393")]
  public long Pixel393 { get; init; }

  [SerializedLabel("pixel_394")]
  public long Pixel394 { get; init; }

  [SerializedLabel("pixel_395")]
  public long Pixel395 { get; init; }

  [SerializedLabel("pixel_396")]
  public long Pixel396 { get; init; }

  [SerializedLabel("pixel_397")]
  public long Pixel397 { get; init; }

  [SerializedLabel("pixel_398")]
  public long Pixel398 { get; init; }

  [SerializedLabel("pixel_399")]
  public long Pixel399 { get; init; }

  [SerializedLabel("pixel_400")]
  public long Pixel400 { get; init; }

  [SerializedLabel("pixel_401")]
  public long Pixel401 { get; init; }

  [SerializedLabel("pixel_402")]
  public long Pixel402 { get; init; }

  [SerializedLabel("pixel_403")]
  public long Pixel403 { get; init; }

  [SerializedLabel("pixel_404")]
  public long Pixel404 { get; init; }

  [SerializedLabel("pixel_405")]
  public long Pixel405 { get; init; }

  [SerializedLabel("pixel_406")]
  public long Pixel406 { get; init; }

  [SerializedLabel("pixel_407")]
  public long Pixel407 { get; init; }

  [SerializedLabel("pixel_408")]
  public long Pixel408 { get; init; }

  [SerializedLabel("pixel_409")]
  public long Pixel409 { get; init; }

  [SerializedLabel("pixel_410")]
  public long Pixel410 { get; init; }

  [SerializedLabel("pixel_411")]
  public long Pixel411 { get; init; }

  [SerializedLabel("pixel_412")]
  public long Pixel412 { get; init; }

  [SerializedLabel("pixel_413")]
  public long Pixel413 { get; init; }

  [SerializedLabel("pixel_414")]
  public long Pixel414 { get; init; }

  [SerializedLabel("pixel_415")]
  public long Pixel415 { get; init; }

  [SerializedLabel("pixel_416")]
  public long Pixel416 { get; init; }

  [SerializedLabel("pixel_417")]
  public long Pixel417 { get; init; }

  [SerializedLabel("pixel_418")]
  public long Pixel418 { get; init; }

  [SerializedLabel("pixel_419")]
  public long Pixel419 { get; init; }

  [SerializedLabel("pixel_420")]
  public long Pixel420 { get; init; }

  [SerializedLabel("pixel_421")]
  public long Pixel421 { get; init; }

  [SerializedLabel("pixel_422")]
  public long Pixel422 { get; init; }

  [SerializedLabel("pixel_423")]
  public long Pixel423 { get; init; }

  [SerializedLabel("pixel_424")]
  public long Pixel424 { get; init; }

  [SerializedLabel("pixel_425")]
  public long Pixel425 { get; init; }

  [SerializedLabel("pixel_426")]
  public long Pixel426 { get; init; }

  [SerializedLabel("pixel_427")]
  public long Pixel427 { get; init; }

  [SerializedLabel("pixel_428")]
  public long Pixel428 { get; init; }

  [SerializedLabel("pixel_429")]
  public long Pixel429 { get; init; }

  [SerializedLabel("pixel_430")]
  public long Pixel430 { get; init; }

  [SerializedLabel("pixel_431")]
  public long Pixel431 { get; init; }

  [SerializedLabel("pixel_432")]
  public long Pixel432 { get; init; }

  [SerializedLabel("pixel_433")]
  public long Pixel433 { get; init; }

  [SerializedLabel("pixel_434")]
  public long Pixel434 { get; init; }

  [SerializedLabel("pixel_435")]
  public long Pixel435 { get; init; }

  [SerializedLabel("pixel_436")]
  public long Pixel436 { get; init; }

  [SerializedLabel("pixel_437")]
  public long Pixel437 { get; init; }

  [SerializedLabel("pixel_438")]
  public long Pixel438 { get; init; }

  [SerializedLabel("pixel_439")]
  public long Pixel439 { get; init; }

  [SerializedLabel("pixel_440")]
  public long Pixel440 { get; init; }

  [SerializedLabel("pixel_441")]
  public long Pixel441 { get; init; }

  [SerializedLabel("pixel_442")]
  public long Pixel442 { get; init; }

  [SerializedLabel("pixel_443")]
  public long Pixel443 { get; init; }

  [SerializedLabel("pixel_444")]
  public long Pixel444 { get; init; }

  [SerializedLabel("pixel_445")]
  public long Pixel445 { get; init; }

  [SerializedLabel("pixel_446")]
  public long Pixel446 { get; init; }

  [SerializedLabel("pixel_447")]
  public long Pixel447 { get; init; }

  [SerializedLabel("pixel_448")]
  public long Pixel448 { get; init; }

  [SerializedLabel("pixel_449")]
  public long Pixel449 { get; init; }

  [SerializedLabel("pixel_450")]
  public long Pixel450 { get; init; }

  [SerializedLabel("pixel_451")]
  public long Pixel451 { get; init; }

  [SerializedLabel("pixel_452")]
  public long Pixel452 { get; init; }

  [SerializedLabel("pixel_453")]
  public long Pixel453 { get; init; }

  [SerializedLabel("pixel_454")]
  public long Pixel454 { get; init; }

  [SerializedLabel("pixel_455")]
  public long Pixel455 { get; init; }

  [SerializedLabel("pixel_456")]
  public long Pixel456 { get; init; }

  [SerializedLabel("pixel_457")]
  public long Pixel457 { get; init; }

  [SerializedLabel("pixel_458")]
  public long Pixel458 { get; init; }

  [SerializedLabel("pixel_459")]
  public long Pixel459 { get; init; }

  [SerializedLabel("pixel_460")]
  public long Pixel460 { get; init; }

  [SerializedLabel("pixel_461")]
  public long Pixel461 { get; init; }

  [SerializedLabel("pixel_462")]
  public long Pixel462 { get; init; }

  [SerializedLabel("pixel_463")]
  public long Pixel463 { get; init; }

  [SerializedLabel("pixel_464")]
  public long Pixel464 { get; init; }

  [SerializedLabel("pixel_465")]
  public long Pixel465 { get; init; }

  [SerializedLabel("pixel_466")]
  public long Pixel466 { get; init; }

  [SerializedLabel("pixel_467")]
  public long Pixel467 { get; init; }

  [SerializedLabel("pixel_468")]
  public long Pixel468 { get; init; }

  [SerializedLabel("pixel_469")]
  public long Pixel469 { get; init; }

  [SerializedLabel("pixel_470")]
  public long Pixel470 { get; init; }

  [SerializedLabel("pixel_471")]
  public long Pixel471 { get; init; }

  [SerializedLabel("pixel_472")]
  public long Pixel472 { get; init; }

  [SerializedLabel("pixel_473")]
  public long Pixel473 { get; init; }

  [SerializedLabel("pixel_474")]
  public long Pixel474 { get; init; }

  [SerializedLabel("pixel_475")]
  public long Pixel475 { get; init; }

  [SerializedLabel("pixel_476")]
  public long Pixel476 { get; init; }

  [SerializedLabel("pixel_477")]
  public long Pixel477 { get; init; }

  [SerializedLabel("pixel_478")]
  public long Pixel478 { get; init; }

  [SerializedLabel("pixel_479")]
  public long Pixel479 { get; init; }

  [SerializedLabel("pixel_480")]
  public long Pixel480 { get; init; }

  [SerializedLabel("pixel_481")]
  public long Pixel481 { get; init; }

  [SerializedLabel("pixel_482")]
  public long Pixel482 { get; init; }

  [SerializedLabel("pixel_483")]
  public long Pixel483 { get; init; }

  [SerializedLabel("pixel_484")]
  public long Pixel484 { get; init; }

  [SerializedLabel("pixel_485")]
  public long Pixel485 { get; init; }

  [SerializedLabel("pixel_486")]
  public long Pixel486 { get; init; }

  [SerializedLabel("pixel_487")]
  public long Pixel487 { get; init; }

  [SerializedLabel("pixel_488")]
  public long Pixel488 { get; init; }

  [SerializedLabel("pixel_489")]
  public long Pixel489 { get; init; }

  [SerializedLabel("pixel_490")]
  public long Pixel490 { get; init; }

  [SerializedLabel("pixel_491")]
  public long Pixel491 { get; init; }

  [SerializedLabel("pixel_492")]
  public long Pixel492 { get; init; }

  [SerializedLabel("pixel_493")]
  public long Pixel493 { get; init; }

  [SerializedLabel("pixel_494")]
  public long Pixel494 { get; init; }

  [SerializedLabel("pixel_495")]
  public long Pixel495 { get; init; }

  [SerializedLabel("pixel_496")]
  public long Pixel496 { get; init; }

  [SerializedLabel("pixel_497")]
  public long Pixel497 { get; init; }

  [SerializedLabel("pixel_498")]
  public long Pixel498 { get; init; }

  [SerializedLabel("pixel_499")]
  public long Pixel499 { get; init; }

  [SerializedLabel("pixel_500")]
  public long Pixel500 { get; init; }

  [SerializedLabel("pixel_501")]
  public long Pixel501 { get; init; }

  [SerializedLabel("pixel_502")]
  public long Pixel502 { get; init; }

  [SerializedLabel("pixel_503")]
  public long Pixel503 { get; init; }

  [SerializedLabel("pixel_504")]
  public long Pixel504 { get; init; }

  [SerializedLabel("pixel_505")]
  public long Pixel505 { get; init; }

  [SerializedLabel("pixel_506")]
  public long Pixel506 { get; init; }

  [SerializedLabel("pixel_507")]
  public long Pixel507 { get; init; }

  [SerializedLabel("pixel_508")]
  public long Pixel508 { get; init; }

  [SerializedLabel("pixel_509")]
  public long Pixel509 { get; init; }

  [SerializedLabel("pixel_510")]
  public long Pixel510 { get; init; }

  [SerializedLabel("pixel_511")]
  public long Pixel511 { get; init; }

  [SerializedLabel("pixel_512")]
  public long Pixel512 { get; init; }

  [SerializedLabel("pixel_513")]
  public long Pixel513 { get; init; }

  [SerializedLabel("pixel_514")]
  public long Pixel514 { get; init; }

  [SerializedLabel("pixel_515")]
  public long Pixel515 { get; init; }

  [SerializedLabel("pixel_516")]
  public long Pixel516 { get; init; }

  [SerializedLabel("pixel_517")]
  public long Pixel517 { get; init; }

  [SerializedLabel("pixel_518")]
  public long Pixel518 { get; init; }

  [SerializedLabel("pixel_519")]
  public long Pixel519 { get; init; }

  [SerializedLabel("pixel_520")]
  public long Pixel520 { get; init; }

  [SerializedLabel("pixel_521")]
  public long Pixel521 { get; init; }

  [SerializedLabel("pixel_522")]
  public long Pixel522 { get; init; }

  [SerializedLabel("pixel_523")]
  public long Pixel523 { get; init; }

  [SerializedLabel("pixel_524")]
  public long Pixel524 { get; init; }

  [SerializedLabel("pixel_525")]
  public long Pixel525 { get; init; }

  [SerializedLabel("pixel_526")]
  public long Pixel526 { get; init; }

  [SerializedLabel("pixel_527")]
  public long Pixel527 { get; init; }

  [SerializedLabel("pixel_528")]
  public long Pixel528 { get; init; }

  [SerializedLabel("pixel_529")]
  public long Pixel529 { get; init; }

  [SerializedLabel("pixel_530")]
  public long Pixel530 { get; init; }

  [SerializedLabel("pixel_531")]
  public long Pixel531 { get; init; }

  [SerializedLabel("pixel_532")]
  public long Pixel532 { get; init; }

  [SerializedLabel("pixel_533")]
  public long Pixel533 { get; init; }

  [SerializedLabel("pixel_534")]
  public long Pixel534 { get; init; }

  [SerializedLabel("pixel_535")]
  public long Pixel535 { get; init; }

  [SerializedLabel("pixel_536")]
  public long Pixel536 { get; init; }

  [SerializedLabel("pixel_537")]
  public long Pixel537 { get; init; }

  [SerializedLabel("pixel_538")]
  public long Pixel538 { get; init; }

  [SerializedLabel("pixel_539")]
  public long Pixel539 { get; init; }

  [SerializedLabel("pixel_540")]
  public long Pixel540 { get; init; }

  [SerializedLabel("pixel_541")]
  public long Pixel541 { get; init; }

  [SerializedLabel("pixel_542")]
  public long Pixel542 { get; init; }

  [SerializedLabel("pixel_543")]
  public long Pixel543 { get; init; }

  [SerializedLabel("pixel_544")]
  public long Pixel544 { get; init; }

  [SerializedLabel("pixel_545")]
  public long Pixel545 { get; init; }

  [SerializedLabel("pixel_546")]
  public long Pixel546 { get; init; }

  [SerializedLabel("pixel_547")]
  public long Pixel547 { get; init; }

  [SerializedLabel("pixel_548")]
  public long Pixel548 { get; init; }

  [SerializedLabel("pixel_549")]
  public long Pixel549 { get; init; }

  [SerializedLabel("pixel_550")]
  public long Pixel550 { get; init; }

  [SerializedLabel("pixel_551")]
  public long Pixel551 { get; init; }

  [SerializedLabel("pixel_552")]
  public long Pixel552 { get; init; }

  [SerializedLabel("pixel_553")]
  public long Pixel553 { get; init; }

  [SerializedLabel("pixel_554")]
  public long Pixel554 { get; init; }

  [SerializedLabel("pixel_555")]
  public long Pixel555 { get; init; }

  [SerializedLabel("pixel_556")]
  public long Pixel556 { get; init; }

  [SerializedLabel("pixel_557")]
  public long Pixel557 { get; init; }

  [SerializedLabel("pixel_558")]
  public long Pixel558 { get; init; }

  [SerializedLabel("pixel_559")]
  public long Pixel559 { get; init; }

  [SerializedLabel("pixel_560")]
  public long Pixel560 { get; init; }

  [SerializedLabel("pixel_561")]
  public long Pixel561 { get; init; }

  [SerializedLabel("pixel_562")]
  public long Pixel562 { get; init; }

  [SerializedLabel("pixel_563")]
  public long Pixel563 { get; init; }

  [SerializedLabel("pixel_564")]
  public long Pixel564 { get; init; }

  [SerializedLabel("pixel_565")]
  public long Pixel565 { get; init; }

  [SerializedLabel("pixel_566")]
  public long Pixel566 { get; init; }

  [SerializedLabel("pixel_567")]
  public long Pixel567 { get; init; }

  [SerializedLabel("pixel_568")]
  public long Pixel568 { get; init; }

  [SerializedLabel("pixel_569")]
  public long Pixel569 { get; init; }

  [SerializedLabel("pixel_570")]
  public long Pixel570 { get; init; }

  [SerializedLabel("pixel_571")]
  public long Pixel571 { get; init; }

  [SerializedLabel("pixel_572")]
  public long Pixel572 { get; init; }

  [SerializedLabel("pixel_573")]
  public long Pixel573 { get; init; }

  [SerializedLabel("pixel_574")]
  public long Pixel574 { get; init; }

  [SerializedLabel("pixel_575")]
  public long Pixel575 { get; init; }

  [SerializedLabel("pixel_576")]
  public long Pixel576 { get; init; }

  [SerializedLabel("pixel_577")]
  public long Pixel577 { get; init; }

  [SerializedLabel("pixel_578")]
  public long Pixel578 { get; init; }

  [SerializedLabel("pixel_579")]
  public long Pixel579 { get; init; }

  [SerializedLabel("pixel_580")]
  public long Pixel580 { get; init; }

  [SerializedLabel("pixel_581")]
  public long Pixel581 { get; init; }

  [SerializedLabel("pixel_582")]
  public long Pixel582 { get; init; }

  [SerializedLabel("pixel_583")]
  public long Pixel583 { get; init; }

  [SerializedLabel("pixel_584")]
  public long Pixel584 { get; init; }

  [SerializedLabel("pixel_585")]
  public long Pixel585 { get; init; }

  [SerializedLabel("pixel_586")]
  public long Pixel586 { get; init; }

  [SerializedLabel("pixel_587")]
  public long Pixel587 { get; init; }

  [SerializedLabel("pixel_588")]
  public long Pixel588 { get; init; }

  [SerializedLabel("pixel_589")]
  public long Pixel589 { get; init; }

  [SerializedLabel("pixel_590")]
  public long Pixel590 { get; init; }

  [SerializedLabel("pixel_591")]
  public long Pixel591 { get; init; }

  [SerializedLabel("pixel_592")]
  public long Pixel592 { get; init; }

  [SerializedLabel("pixel_593")]
  public long Pixel593 { get; init; }

  [SerializedLabel("pixel_594")]
  public long Pixel594 { get; init; }

  [SerializedLabel("pixel_595")]
  public long Pixel595 { get; init; }

  [SerializedLabel("pixel_596")]
  public long Pixel596 { get; init; }

  [SerializedLabel("pixel_597")]
  public long Pixel597 { get; init; }

  [SerializedLabel("pixel_598")]
  public long Pixel598 { get; init; }

  [SerializedLabel("pixel_599")]
  public long Pixel599 { get; init; }

  [SerializedLabel("pixel_600")]
  public long Pixel600 { get; init; }

  [SerializedLabel("pixel_601")]
  public long Pixel601 { get; init; }

  [SerializedLabel("pixel_602")]
  public long Pixel602 { get; init; }

  [SerializedLabel("pixel_603")]
  public long Pixel603 { get; init; }

  [SerializedLabel("pixel_604")]
  public long Pixel604 { get; init; }

  [SerializedLabel("pixel_605")]
  public long Pixel605 { get; init; }

  [SerializedLabel("pixel_606")]
  public long Pixel606 { get; init; }

  [SerializedLabel("pixel_607")]
  public long Pixel607 { get; init; }

  [SerializedLabel("pixel_608")]
  public long Pixel608 { get; init; }

  [SerializedLabel("pixel_609")]
  public long Pixel609 { get; init; }

  [SerializedLabel("pixel_610")]
  public long Pixel610 { get; init; }

  [SerializedLabel("pixel_611")]
  public long Pixel611 { get; init; }

  [SerializedLabel("pixel_612")]
  public long Pixel612 { get; init; }

  [SerializedLabel("pixel_613")]
  public long Pixel613 { get; init; }

  [SerializedLabel("pixel_614")]
  public long Pixel614 { get; init; }

  [SerializedLabel("pixel_615")]
  public long Pixel615 { get; init; }

  [SerializedLabel("pixel_616")]
  public long Pixel616 { get; init; }

  [SerializedLabel("pixel_617")]
  public long Pixel617 { get; init; }

  [SerializedLabel("pixel_618")]
  public long Pixel618 { get; init; }

  [SerializedLabel("pixel_619")]
  public long Pixel619 { get; init; }

  [SerializedLabel("pixel_620")]
  public long Pixel620 { get; init; }

  [SerializedLabel("pixel_621")]
  public long Pixel621 { get; init; }

  [SerializedLabel("pixel_622")]
  public long Pixel622 { get; init; }

  [SerializedLabel("pixel_623")]
  public long Pixel623 { get; init; }

  [SerializedLabel("pixel_624")]
  public long Pixel624 { get; init; }

  [SerializedLabel("pixel_625")]
  public long Pixel625 { get; init; }

  [SerializedLabel("pixel_626")]
  public long Pixel626 { get; init; }

  [SerializedLabel("pixel_627")]
  public long Pixel627 { get; init; }

  [SerializedLabel("pixel_628")]
  public long Pixel628 { get; init; }

  [SerializedLabel("pixel_629")]
  public long Pixel629 { get; init; }

  [SerializedLabel("pixel_630")]
  public long Pixel630 { get; init; }

  [SerializedLabel("pixel_631")]
  public long Pixel631 { get; init; }

  [SerializedLabel("pixel_632")]
  public long Pixel632 { get; init; }

  [SerializedLabel("pixel_633")]
  public long Pixel633 { get; init; }

  [SerializedLabel("pixel_634")]
  public long Pixel634 { get; init; }

  [SerializedLabel("pixel_635")]
  public long Pixel635 { get; init; }

  [SerializedLabel("pixel_636")]
  public long Pixel636 { get; init; }

  [SerializedLabel("pixel_637")]
  public long Pixel637 { get; init; }

  [SerializedLabel("pixel_638")]
  public long Pixel638 { get; init; }

  [SerializedLabel("pixel_639")]
  public long Pixel639 { get; init; }

  [SerializedLabel("pixel_640")]
  public long Pixel640 { get; init; }

  [SerializedLabel("pixel_641")]
  public long Pixel641 { get; init; }

  [SerializedLabel("pixel_642")]
  public long Pixel642 { get; init; }

  [SerializedLabel("pixel_643")]
  public long Pixel643 { get; init; }

  [SerializedLabel("pixel_644")]
  public long Pixel644 { get; init; }

  [SerializedLabel("pixel_645")]
  public long Pixel645 { get; init; }

  [SerializedLabel("pixel_646")]
  public long Pixel646 { get; init; }

  [SerializedLabel("pixel_647")]
  public long Pixel647 { get; init; }

  [SerializedLabel("pixel_648")]
  public long Pixel648 { get; init; }

  [SerializedLabel("pixel_649")]
  public long Pixel649 { get; init; }

  [SerializedLabel("pixel_650")]
  public long Pixel650 { get; init; }

  [SerializedLabel("pixel_651")]
  public long Pixel651 { get; init; }

  [SerializedLabel("pixel_652")]
  public long Pixel652 { get; init; }

  [SerializedLabel("pixel_653")]
  public long Pixel653 { get; init; }

  [SerializedLabel("pixel_654")]
  public long Pixel654 { get; init; }

  [SerializedLabel("pixel_655")]
  public long Pixel655 { get; init; }

  [SerializedLabel("pixel_656")]
  public long Pixel656 { get; init; }

  [SerializedLabel("pixel_657")]
  public long Pixel657 { get; init; }

  [SerializedLabel("pixel_658")]
  public long Pixel658 { get; init; }

  [SerializedLabel("pixel_659")]
  public long Pixel659 { get; init; }

  [SerializedLabel("pixel_660")]
  public long Pixel660 { get; init; }

  [SerializedLabel("pixel_661")]
  public long Pixel661 { get; init; }

  [SerializedLabel("pixel_662")]
  public long Pixel662 { get; init; }

  [SerializedLabel("pixel_663")]
  public long Pixel663 { get; init; }

  [SerializedLabel("pixel_664")]
  public long Pixel664 { get; init; }

  [SerializedLabel("pixel_665")]
  public long Pixel665 { get; init; }

  [SerializedLabel("pixel_666")]
  public long Pixel666 { get; init; }

  [SerializedLabel("pixel_667")]
  public long Pixel667 { get; init; }

  [SerializedLabel("pixel_668")]
  public long Pixel668 { get; init; }

  [SerializedLabel("pixel_669")]
  public long Pixel669 { get; init; }

  [SerializedLabel("pixel_670")]
  public long Pixel670 { get; init; }

  [SerializedLabel("pixel_671")]
  public long Pixel671 { get; init; }

  [SerializedLabel("pixel_672")]
  public long Pixel672 { get; init; }

  [SerializedLabel("pixel_673")]
  public long Pixel673 { get; init; }

  [SerializedLabel("pixel_674")]
  public long Pixel674 { get; init; }

  [SerializedLabel("pixel_675")]
  public long Pixel675 { get; init; }

  [SerializedLabel("pixel_676")]
  public long Pixel676 { get; init; }

  [SerializedLabel("pixel_677")]
  public long Pixel677 { get; init; }

  [SerializedLabel("pixel_678")]
  public long Pixel678 { get; init; }

  [SerializedLabel("pixel_679")]
  public long Pixel679 { get; init; }

  [SerializedLabel("pixel_680")]
  public long Pixel680 { get; init; }

  [SerializedLabel("pixel_681")]
  public long Pixel681 { get; init; }

  [SerializedLabel("pixel_682")]
  public long Pixel682 { get; init; }

  [SerializedLabel("pixel_683")]
  public long Pixel683 { get; init; }

  [SerializedLabel("pixel_684")]
  public long Pixel684 { get; init; }

  [SerializedLabel("pixel_685")]
  public long Pixel685 { get; init; }

  [SerializedLabel("pixel_686")]
  public long Pixel686 { get; init; }

  [SerializedLabel("pixel_687")]
  public long Pixel687 { get; init; }

  [SerializedLabel("pixel_688")]
  public long Pixel688 { get; init; }

  [SerializedLabel("pixel_689")]
  public long Pixel689 { get; init; }

  [SerializedLabel("pixel_690")]
  public long Pixel690 { get; init; }

  [SerializedLabel("pixel_691")]
  public long Pixel691 { get; init; }

  [SerializedLabel("pixel_692")]
  public long Pixel692 { get; init; }

  [SerializedLabel("pixel_693")]
  public long Pixel693 { get; init; }

  [SerializedLabel("pixel_694")]
  public long Pixel694 { get; init; }

  [SerializedLabel("pixel_695")]
  public long Pixel695 { get; init; }

  [SerializedLabel("pixel_696")]
  public long Pixel696 { get; init; }

  [SerializedLabel("pixel_697")]
  public long Pixel697 { get; init; }

  [SerializedLabel("pixel_698")]
  public long Pixel698 { get; init; }

  [SerializedLabel("pixel_699")]
  public long Pixel699 { get; init; }

  [SerializedLabel("pixel_700")]
  public long Pixel700 { get; init; }

  [SerializedLabel("pixel_701")]
  public long Pixel701 { get; init; }

  [SerializedLabel("pixel_702")]
  public long Pixel702 { get; init; }

  [SerializedLabel("pixel_703")]
  public long Pixel703 { get; init; }

  [SerializedLabel("pixel_704")]
  public long Pixel704 { get; init; }

  [SerializedLabel("pixel_705")]
  public long Pixel705 { get; init; }

  [SerializedLabel("pixel_706")]
  public long Pixel706 { get; init; }

  [SerializedLabel("pixel_707")]
  public long Pixel707 { get; init; }

  [SerializedLabel("pixel_708")]
  public long Pixel708 { get; init; }

  [SerializedLabel("pixel_709")]
  public long Pixel709 { get; init; }

  [SerializedLabel("pixel_710")]
  public long Pixel710 { get; init; }

  [SerializedLabel("pixel_711")]
  public long Pixel711 { get; init; }

  [SerializedLabel("pixel_712")]
  public long Pixel712 { get; init; }

  [SerializedLabel("pixel_713")]
  public long Pixel713 { get; init; }

  [SerializedLabel("pixel_714")]
  public long Pixel714 { get; init; }

  [SerializedLabel("pixel_715")]
  public long Pixel715 { get; init; }

  [SerializedLabel("pixel_716")]
  public long Pixel716 { get; init; }

  [SerializedLabel("pixel_717")]
  public long Pixel717 { get; init; }

  [SerializedLabel("pixel_718")]
  public long Pixel718 { get; init; }

  [SerializedLabel("pixel_719")]
  public long Pixel719 { get; init; }

  [SerializedLabel("pixel_720")]
  public long Pixel720 { get; init; }

  [SerializedLabel("pixel_721")]
  public long Pixel721 { get; init; }

  [SerializedLabel("pixel_722")]
  public long Pixel722 { get; init; }

  [SerializedLabel("pixel_723")]
  public long Pixel723 { get; init; }

  [SerializedLabel("pixel_724")]
  public long Pixel724 { get; init; }

  [SerializedLabel("pixel_725")]
  public long Pixel725 { get; init; }

  [SerializedLabel("pixel_726")]
  public long Pixel726 { get; init; }

  [SerializedLabel("pixel_727")]
  public long Pixel727 { get; init; }

  [SerializedLabel("pixel_728")]
  public long Pixel728 { get; init; }

  [SerializedLabel("pixel_729")]
  public long Pixel729 { get; init; }

  [SerializedLabel("pixel_730")]
  public long Pixel730 { get; init; }

  [SerializedLabel("pixel_731")]
  public long Pixel731 { get; init; }

  [SerializedLabel("pixel_732")]
  public long Pixel732 { get; init; }

  [SerializedLabel("pixel_733")]
  public long Pixel733 { get; init; }

  [SerializedLabel("pixel_734")]
  public long Pixel734 { get; init; }

  [SerializedLabel("pixel_735")]
  public long Pixel735 { get; init; }

  [SerializedLabel("pixel_736")]
  public long Pixel736 { get; init; }

  [SerializedLabel("pixel_737")]
  public long Pixel737 { get; init; }

  [SerializedLabel("pixel_738")]
  public long Pixel738 { get; init; }

  [SerializedLabel("pixel_739")]
  public long Pixel739 { get; init; }

  [SerializedLabel("pixel_740")]
  public long Pixel740 { get; init; }

  [SerializedLabel("pixel_741")]
  public long Pixel741 { get; init; }

  [SerializedLabel("pixel_742")]
  public long Pixel742 { get; init; }

  [SerializedLabel("pixel_743")]
  public long Pixel743 { get; init; }

  [SerializedLabel("pixel_744")]
  public long Pixel744 { get; init; }

  [SerializedLabel("pixel_745")]
  public long Pixel745 { get; init; }

  [SerializedLabel("pixel_746")]
  public long Pixel746 { get; init; }

  [SerializedLabel("pixel_747")]
  public long Pixel747 { get; init; }

  [SerializedLabel("pixel_748")]
  public long Pixel748 { get; init; }

  [SerializedLabel("pixel_749")]
  public long Pixel749 { get; init; }

  [SerializedLabel("pixel_750")]
  public long Pixel750 { get; init; }

  [SerializedLabel("pixel_751")]
  public long Pixel751 { get; init; }

  [SerializedLabel("pixel_752")]
  public long Pixel752 { get; init; }

  [SerializedLabel("pixel_753")]
  public long Pixel753 { get; init; }

  [SerializedLabel("pixel_754")]
  public long Pixel754 { get; init; }

  [SerializedLabel("pixel_755")]
  public long Pixel755 { get; init; }

  [SerializedLabel("pixel_756")]
  public long Pixel756 { get; init; }

  [SerializedLabel("pixel_757")]
  public long Pixel757 { get; init; }

  [SerializedLabel("pixel_758")]
  public long Pixel758 { get; init; }

  [SerializedLabel("pixel_759")]
  public long Pixel759 { get; init; }

  [SerializedLabel("pixel_760")]
  public long Pixel760 { get; init; }

  [SerializedLabel("pixel_761")]
  public long Pixel761 { get; init; }

  [SerializedLabel("pixel_762")]
  public long Pixel762 { get; init; }

  [SerializedLabel("pixel_763")]
  public long Pixel763 { get; init; }

  [SerializedLabel("pixel_764")]
  public long Pixel764 { get; init; }

  [SerializedLabel("pixel_765")]
  public long Pixel765 { get; init; }

  [SerializedLabel("pixel_766")]
  public long Pixel766 { get; init; }

  [SerializedLabel("pixel_767")]
  public long Pixel767 { get; init; }

  [SerializedLabel("pixel_768")]
  public long Pixel768 { get; init; }

  [SerializedLabel("pixel_769")]
  public long Pixel769 { get; init; }

  [SerializedLabel("pixel_770")]
  public long Pixel770 { get; init; }

  [SerializedLabel("pixel_771")]
  public long Pixel771 { get; init; }

  [SerializedLabel("pixel_772")]
  public long Pixel772 { get; init; }

  [SerializedLabel("pixel_773")]
  public long Pixel773 { get; init; }

  [SerializedLabel("pixel_774")]
  public long Pixel774 { get; init; }

  [SerializedLabel("pixel_775")]
  public long Pixel775 { get; init; }

  [SerializedLabel("pixel_776")]
  public long Pixel776 { get; init; }

  [SerializedLabel("pixel_777")]
  public long Pixel777 { get; init; }

  [SerializedLabel("pixel_778")]
  public long Pixel778 { get; init; }

  [SerializedLabel("pixel_779")]
  public long Pixel779 { get; init; }

  [SerializedLabel("pixel_780")]
  public long Pixel780 { get; init; }

  [SerializedLabel("pixel_781")]
  public long Pixel781 { get; init; }

  [SerializedLabel("pixel_782")]
  public long Pixel782 { get; init; }

  [SerializedLabel("pixel_783")]
  public long Pixel783 { get; init; }
}
