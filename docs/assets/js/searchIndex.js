
var camelCaseTokenizer = function (obj) {
    var previous = '';
    return obj.toString().trim().split(/[\s\-]+|(?=[A-Z])/).reduce(function(acc, cur) {
        var current = cur.toLowerCase();
        if(acc.length === 0) {
            previous = current;
            return acc.concat(current);
        }
        previous = previous.concat(current);
        return acc.concat([current, previous]);
    }, []);
}
lunr.tokenizer.registerFunction(camelCaseTokenizer, 'camelCaseTokenizer')
var searchModule = function() {
    var idMap = [];
    function y(e) { 
        idMap.push(e); 
    }
    var idx = lunr(function() {
        this.field('title', { boost: 10 });
        this.field('content');
        this.field('description', { boost: 5 });
        this.field('tags', { boost: 50 });
        this.ref('id');
        this.tokenizer(camelCaseTokenizer);

        this.pipeline.remove(lunr.stopWordFilter);
        this.pipeline.remove(lunr.stemmer);
    });
    function a(e) { 
        idx.add(e); 
    }

    a({
        id:0,
        title:"FlowType",
        content:"FlowType",
        description:'',
        tags:''
    });

    a({
        id:1,
        title:"Statistics",
        content:"Statistics",
        description:'',
        tags:''
    });

    a({
        id:2,
        title:"LinearInterpolatorFlatExtrap",
        content:"LinearInterpolatorFlatExtrap",
        description:'',
        tags:''
    });

    a({
        id:3,
        title:"NewtonRaphsonMultiCurveSolver",
        content:"NewtonRaphsonMultiCurveSolver",
        description:'',
        tags:''
    });

    a({
        id:4,
        title:"ExceptionType",
        content:"ExceptionType",
        description:'',
        tags:''
    });

    a({
        id:5,
        title:"SwapPayReceiveType",
        content:"SwapPayReceiveType",
        description:'',
        tags:''
    });

    a({
        id:6,
        title:"DayCountBasis",
        content:"DayCountBasis",
        description:'',
        tags:''
    });

    a({
        id:7,
        title:"CashFlowSchedule",
        content:"CashFlowSchedule",
        description:'',
        tags:''
    });

    a({
        id:8,
        title:"DateExtensions",
        content:"DateExtensions",
        description:'',
        tags:''
    });

    a({
        id:9,
        title:"ICurve",
        content:"ICurve",
        description:'',
        tags:''
    });

    a({
        id:10,
        title:"FxPair",
        content:"FxPair",
        description:'',
        tags:''
    });

    a({
        id:11,
        title:"CalendarsFromJson",
        content:"CalendarsFromJson",
        description:'',
        tags:''
    });

    a({
        id:12,
        title:"MonthEnum",
        content:"MonthEnum",
        description:'',
        tags:''
    });

    a({
        id:13,
        title:"FxForward",
        content:"FxForward",
        description:'',
        tags:''
    });

    a({
        id:14,
        title:"OptionType",
        content:"OptionType",
        description:'',
        tags:''
    });

    a({
        id:15,
        title:"SettlementType",
        content:"SettlementType",
        description:'',
        tags:''
    });

    a({
        id:16,
        title:"BlackFunctions",
        content:"BlackFunctions",
        description:'',
        tags:''
    });

    a({
        id:17,
        title:"CurrenciesFromJson",
        content:"CurrenciesFromJson",
        description:'',
        tags:''
    });

    a({
        id:18,
        title:"DatePeriodType",
        content:"DatePeriodType",
        description:'',
        tags:''
    });

    a({
        id:19,
        title:"CdsScheduleType",
        content:"CdsScheduleType",
        description:'',
        tags:''
    });

    a({
        id:20,
        title:"IrBasisSwap",
        content:"IrBasisSwap",
        description:'',
        tags:''
    });

    a({
        id:21,
        title:"SobolDirectionInfo",
        content:"SobolDirectionInfo",
        description:'',
        tags:''
    });

    a({
        id:22,
        title:"BlockSet",
        content:"BlockSet",
        description:'',
        tags:''
    });

    a({
        id:23,
        title:"ResetType",
        content:"ResetType",
        description:'',
        tags:''
    });

    a({
        id:24,
        title:"FraDiscountingType",
        content:"FraDiscountingType",
        description:'',
        tags:''
    });

    a({
        id:25,
        title:"IrSwap",
        content:"IrSwap",
        description:'',
        tags:''
    });

    a({
        id:26,
        title:"XccyBasisSwap",
        content:"XccyBasisSwap",
        description:'',
        tags:''
    });

    a({
        id:27,
        title:"IrCurve",
        content:"IrCurve",
        description:'',
        tags:''
    });

    a({
        id:28,
        title:"Brent",
        content:"Brent",
        description:'',
        tags:''
    });

    a({
        id:29,
        title:"SwapLegType",
        content:"SwapLegType",
        description:'',
        tags:''
    });

    a({
        id:30,
        title:"RollType",
        content:"RollType",
        description:'',
        tags:''
    });

    a({
        id:31,
        title:"IInterpolator",
        content:"IInterpolator",
        description:'',
        tags:''
    });

    a({
        id:32,
        title:"ITenorDate",
        content:"ITenorDate",
        description:'',
        tags:''
    });

    a({
        id:33,
        title:"ListedUtils",
        content:"ListedUtils",
        description:'',
        tags:''
    });

    a({
        id:34,
        title:"ExceptionHelper",
        content:"ExceptionHelper",
        description:'',
        tags:''
    });

    a({
        id:35,
        title:"AtmVolType",
        content:"AtmVolType",
        description:'',
        tags:''
    });

    a({
        id:36,
        title:"Calendar",
        content:"Calendar",
        description:'',
        tags:''
    });

    a({
        id:37,
        title:"PathBlock",
        content:"PathBlock",
        description:'',
        tags:''
    });

    a({
        id:38,
        title:"Halley",
        content:"Halley",
        description:'',
        tags:''
    });

    a({
        id:39,
        title:"FrequencyExtensions",
        content:"FrequencyExtensions",
        description:'',
        tags:''
    });

    a({
        id:40,
        title:"CompoundingType",
        content:"CompoundingType",
        description:'',
        tags:''
    });

    a({
        id:41,
        title:"Currency",
        content:"Currency",
        description:'',
        tags:''
    });

    a({
        id:42,
        title:"DoubleArrayFunctions",
        content:"DoubleArrayFunctions",
        description:'',
        tags:''
    });

    a({
        id:43,
        title:"IFundingInstrument",
        content:"IFundingInstrument",
        description:'',
        tags:''
    });

    a({
        id:44,
        title:"ExchangeType",
        content:"ExchangeType",
        description:'',
        tags:''
    });

    a({
        id:45,
        title:"Frequency",
        content:"Frequency",
        description:'',
        tags:''
    });

    a({
        id:46,
        title:"DeltaType",
        content:"DeltaType",
        description:'',
        tags:''
    });

    a({
        id:47,
        title:"FundingModel",
        content:"FundingModel",
        description:'',
        tags:''
    });

    a({
        id:48,
        title:"Newton",
        content:"Newton",
        description:'',
        tags:''
    });

    a({
        id:49,
        title:"FundingInstrumentCollection",
        content:"FundingInstrumentCollection",
        description:'',
        tags:''
    });

    a({
        id:50,
        title:"TenorDateAbsolute",
        content:"TenorDateAbsolute",
        description:'',
        tags:''
    });

    a({
        id:51,
        title:"LinearInterpolatorFlatExtrapNoBinSearch",
        content:"LinearInterpolatorFlatExtrapNoBinSearch",
        description:'',
        tags:''
    });

    a({
        id:52,
        title:"RandomOptions",
        content:"RandomOptions",
        description:'',
        tags:''
    });

    a({
        id:53,
        title:"ICalendarProvider",
        content:"ICalendarProvider",
        description:'',
        tags:''
    });

    a({
        id:54,
        title:"MTMSwapType",
        content:"MTMSwapType",
        description:'',
        tags:''
    });

    a({
        id:55,
        title:"ICurrencyProvider",
        content:"ICurrencyProvider",
        description:'',
        tags:''
    });

    a({
        id:56,
        title:"FxMatrix",
        content:"FxMatrix",
        description:'',
        tags:''
    });

    a({
        id:57,
        title:"TenorDateRelative",
        content:"TenorDateRelative",
        description:'',
        tags:''
    });

    a({
        id:58,
        title:"RateType",
        content:"RateType",
        description:'',
        tags:''
    });

    a({
        id:59,
        title:"STIRFuture",
        content:"STIRFuture",
        description:'',
        tags:''
    });

    a({
        id:60,
        title:"StubType",
        content:"StubType",
        description:'',
        tags:''
    });

    a({
        id:61,
        title:"Sobol",
        content:"Sobol",
        description:'',
        tags:''
    });

    a({
        id:62,
        title:"FloatRateIndex",
        content:"FloatRateIndex",
        description:'',
        tags:''
    });

    a({
        id:63,
        title:"FuturesConvexityUtils",
        content:"FuturesConvexityUtils",
        description:'',
        tags:''
    });

    a({
        id:64,
        title:"OffsetRelativeToType",
        content:"OffsetRelativeToType",
        description:'',
        tags:''
    });

    a({
        id:65,
        title:"InterpolatorFactory",
        content:"InterpolatorFactory",
        description:'',
        tags:''
    });

    a({
        id:66,
        title:"GenericSwapLeg",
        content:"GenericSwapLeg",
        description:'',
        tags:''
    });

    a({
        id:67,
        title:"OptionType",
        content:"OptionType",
        description:'',
        tags:''
    });

    a({
        id:68,
        title:"CashFlow",
        content:"CashFlow",
        description:'',
        tags:''
    });

    a({
        id:69,
        title:"Interpolator DType",
        content:"Interpolator DType",
        description:'',
        tags:''
    });

    a({
        id:70,
        title:"ForwardRateAgreement",
        content:"ForwardRateAgreement",
        description:'',
        tags:''
    });

    a({
        id:71,
        title:"NewtonRaphsonMultiDimensionalSolver",
        content:"NewtonRaphsonMultiDimensionalSolver",
        description:'',
        tags:''
    });

    a({
        id:72,
        title:"AverageType",
        content:"AverageType",
        description:'',
        tags:''
    });

    a({
        id:73,
        title:"FxQouteType",
        content:"FxQouteType",
        description:'',
        tags:''
    });

    a({
        id:74,
        title:"SR",
        content:"SR",
        description:'',
        tags:''
    });

    a({
        id:75,
        title:"CalendarCollection",
        content:"CalendarCollection",
        description:'',
        tags:''
    });

    a({
        id:76,
        title:"NewtonRaphsonMultiCurveSolverStaged",
        content:"NewtonRaphsonMultiCurveSolverStaged",
        description:'',
        tags:''
    });

    a({
        id:77,
        title:"NewtonRaphsonMultiCurveSolverStagedWithAnalyticJacobian",
        content:"NewtonRaphsonMultiCurveSolverStagedWithAnalyticJacobian",
        description:'',
        tags:''
    });

    a({
        id:78,
        title:"SobolDirectionNumbers",
        content:"SobolDirectionNumbers",
        description:'',
        tags:''
    });

    a({
        id:79,
        title:"LinearInterpolator",
        content:"LinearInterpolator",
        description:'',
        tags:''
    });

    y({
        url:'/qwack/qwack/api/Qwack.Core.Basic/FlowType',
        title:"FlowType",
        description:""
    });

    y({
        url:'/qwack/qwack/api/Qwack.Math/Statistics',
        title:"Statistics",
        description:""
    });

    y({
        url:'/qwack/qwack/api/Qwack.Math.Interpolation/LinearInterpolatorFlatExtrap',
        title:"LinearInterpolatorFlatExtrap",
        description:""
    });

    y({
        url:'/qwack/qwack/api/Qwack.Core.Calibrators/NewtonRaphsonMultiCurveSolver',
        title:"NewtonRaphsonMultiCurveSolver",
        description:""
    });

    y({
        url:'/qwack/qwack/api/Qwack.Utils.Exceptions/ExceptionType',
        title:"ExceptionType",
        description:""
    });

    y({
        url:'/qwack/qwack/api/Qwack.Core.Basic/SwapPayReceiveType',
        title:"SwapPayReceiveType",
        description:""
    });

    y({
        url:'/qwack/qwack/api/Qwack.Dates/DayCountBasis',
        title:"DayCountBasis",
        description:""
    });

    y({
        url:'/qwack/qwack/api/Qwack.Core.Instruments/CashFlowSchedule',
        title:"CashFlowSchedule",
        description:""
    });

    y({
        url:'/qwack/qwack/api/Qwack.Dates/DateExtensions',
        title:"DateExtensions",
        description:""
    });

    y({
        url:'/qwack/qwack/api/Qwack.Core.Curves/ICurve',
        title:"ICurve",
        description:""
    });

    y({
        url:'/qwack/qwack/api/Qwack.Core.Basic/FxPair',
        title:"FxPair",
        description:""
    });

    y({
        url:'/qwack/qwack/api/Qwack.Json.Providers/CalendarsFromJson',
        title:"CalendarsFromJson",
        description:""
    });

    y({
        url:'/qwack/qwack/api/Qwack.Dates/MonthEnum',
        title:"MonthEnum",
        description:""
    });

    y({
        url:'/qwack/qwack/api/Qwack.Core.Instruments.Funding/FxForward',
        title:"FxForward",
        description:""
    });

    y({
        url:'/qwack/qwack/api/Qwack.Options/OptionType',
        title:"OptionType",
        description:""
    });

    y({
        url:'/qwack/qwack/api/Qwack.Core.Basic/SettlementType',
        title:"SettlementType",
        description:""
    });

    y({
        url:'/qwack/qwack/api/Qwack.Options/BlackFunctions',
        title:"BlackFunctions",
        description:""
    });

    y({
        url:'/qwack/qwack/api/Qwack.Providers.Json/CurrenciesFromJson',
        title:"CurrenciesFromJson",
        description:""
    });

    y({
        url:'/qwack/qwack/api/Qwack.Dates/DatePeriodType',
        title:"DatePeriodType",
        description:""
    });

    y({
        url:'/qwack/qwack/api/Qwack.Core.Basic/CdsScheduleType',
        title:"CdsScheduleType",
        description:""
    });

    y({
        url:'/qwack/qwack/api/Qwack.Core.Instruments.Funding/IrBasisSwap',
        title:"IrBasisSwap",
        description:""
    });

    y({
        url:'/qwack/qwack/api/Qwack.Random.Sobol/SobolDirectionInfo',
        title:"SobolDirectionInfo",
        description:""
    });

    y({
        url:'/qwack/qwack/api/Qwack.Paths/BlockSet',
        title:"BlockSet",
        description:""
    });

    y({
        url:'/qwack/qwack/api/Qwack.Core.Basic/ResetType',
        title:"ResetType",
        description:""
    });

    y({
        url:'/qwack/qwack/api/Qwack.Core.Basic/FraDiscountingType',
        title:"FraDiscountingType",
        description:""
    });

    y({
        url:'/qwack/qwack/api/Qwack.Core.Instruments.Funding/IrSwap',
        title:"IrSwap",
        description:""
    });

    y({
        url:'/qwack/qwack/api/Qwack.Core.Instruments.Funding/XccyBasisSwap',
        title:"XccyBasisSwap",
        description:""
    });

    y({
        url:'/qwack/qwack/api/Qwack.Core.Curves/IrCurve',
        title:"IrCurve",
        description:""
    });

    y({
        url:'/qwack/qwack/api/Qwack.Math.Solvers/Brent',
        title:"Brent",
        description:""
    });

    y({
        url:'/qwack/qwack/api/Qwack.Core.Basic/SwapLegType',
        title:"SwapLegType",
        description:""
    });

    y({
        url:'/qwack/qwack/api/Qwack.Dates/RollType',
        title:"RollType",
        description:""
    });

    y({
        url:'/qwack/qwack/api/Qwack.Math.Interpolation/IInterpolator1D',
        title:"IInterpolator1D",
        description:""
    });

    y({
        url:'/qwack/qwack/api/Qwack.Dates/ITenorDate',
        title:"ITenorDate",
        description:""
    });

    y({
        url:'/qwack/qwack/api/Qwack.Dates/ListedUtils',
        title:"ListedUtils",
        description:""
    });

    y({
        url:'/qwack/qwack/api/Qwack.Utils.Exceptions/ExceptionHelper',
        title:"ExceptionHelper",
        description:""
    });

    y({
        url:'/qwack/qwack/api/Qwack.Core.Basic/AtmVolType',
        title:"AtmVolType",
        description:""
    });

    y({
        url:'/qwack/qwack/api/Qwack.Dates/Calendar',
        title:"Calendar",
        description:""
    });

    y({
        url:'/qwack/qwack/api/Qwack.Paths/PathBlock',
        title:"PathBlock",
        description:""
    });

    y({
        url:'/qwack/qwack/api/Qwack.Math.Solvers/Halley1d',
        title:"Halley1d",
        description:""
    });

    y({
        url:'/qwack/qwack/api/Qwack.Dates/FrequencyExtensions',
        title:"FrequencyExtensions",
        description:""
    });

    y({
        url:'/qwack/qwack/api/Qwack.Core.Basic/CompoundingType',
        title:"CompoundingType",
        description:""
    });

    y({
        url:'/qwack/qwack/api/Qwack.Core.Basic/Currency',
        title:"Currency",
        description:""
    });

    y({
        url:'/qwack/qwack/api/Qwack.Math.Matrix/DoubleArrayFunctions',
        title:"DoubleArrayFunctions",
        description:""
    });

    y({
        url:'/qwack/qwack/api/Qwack.Core.Instruments/IFundingInstrument',
        title:"IFundingInstrument",
        description:""
    });

    y({
        url:'/qwack/qwack/api/Qwack.Core.Basic/ExchangeType',
        title:"ExchangeType",
        description:""
    });

    y({
        url:'/qwack/qwack/api/Qwack.Dates/Frequency',
        title:"Frequency",
        description:""
    });

    y({
        url:'/qwack/qwack/api/Qwack.Core.Basic/DeltaType',
        title:"DeltaType",
        description:""
    });

    y({
        url:'/qwack/qwack/api/Qwack.Core.Models/FundingModel',
        title:"FundingModel",
        description:""
    });

    y({
        url:'/qwack/qwack/api/Qwack.Math.Solvers/Newton1d',
        title:"Newton1d",
        description:""
    });

    y({
        url:'/qwack/qwack/api/Qwack.Core.Instruments.Funding/FundingInstrumentCollection',
        title:"FundingInstrumentCollection",
        description:""
    });

    y({
        url:'/qwack/qwack/api/Qwack.Dates/TenorDateAbsolute',
        title:"TenorDateAbsolute",
        description:""
    });

    y({
        url:'/qwack/qwack/api/Qwack.Math.Interpolation/LinearInterpolatorFlatExtrapNoBinSearch',
        title:"LinearInterpolatorFlatExtrapNoBinSearch",
        description:""
    });

    y({
        url:'/qwack/qwack/api/Qwack.Random/RandomOptions',
        title:"RandomOptions",
        description:""
    });

    y({
        url:'/qwack/qwack/api/Qwack.Dates/ICalendarProvider',
        title:"ICalendarProvider",
        description:""
    });

    y({
        url:'/qwack/qwack/api/Qwack.Core.Basic/MTMSwapType',
        title:"MTMSwapType",
        description:""
    });

    y({
        url:'/qwack/qwack/api/Qwack.Core.Basic/ICurrencyProvider',
        title:"ICurrencyProvider",
        description:""
    });

    y({
        url:'/qwack/qwack/api/Qwack.Core.Models/FxMatrix',
        title:"FxMatrix",
        description:""
    });

    y({
        url:'/qwack/qwack/api/Qwack.Dates/TenorDateRelative',
        title:"TenorDateRelative",
        description:""
    });

    y({
        url:'/qwack/qwack/api/Qwack.Core.Basic/RateType',
        title:"RateType",
        description:""
    });

    y({
        url:'/qwack/qwack/api/Qwack.Core.Instruments.Funding/STIRFuture',
        title:"STIRFuture",
        description:""
    });

    y({
        url:'/qwack/qwack/api/Qwack.Core.Basic/StubType',
        title:"StubType",
        description:""
    });

    y({
        url:'/qwack/qwack/api/Qwack.Random.Sobol/Sobol',
        title:"Sobol",
        description:""
    });

    y({
        url:'/qwack/qwack/api/Qwack.Core.Instruments.Funding/FloatRateIndex',
        title:"FloatRateIndex",
        description:""
    });

    y({
        url:'/qwack/qwack/api/Qwack.Math.Utils/FuturesConvexityUtils',
        title:"FuturesConvexityUtils",
        description:""
    });

    y({
        url:'/qwack/qwack/api/Qwack.Core.Basic/OffsetRelativeToType',
        title:"OffsetRelativeToType",
        description:""
    });

    y({
        url:'/qwack/qwack/api/Qwack.Math.Interpolation/InterpolatorFactory',
        title:"InterpolatorFactory",
        description:""
    });

    y({
        url:'/qwack/qwack/api/Qwack.Core.Instruments.Funding/GenericSwapLeg',
        title:"GenericSwapLeg",
        description:""
    });

    y({
        url:'/qwack/qwack/api/Qwack.Core.Basic/OptionType',
        title:"OptionType",
        description:""
    });

    y({
        url:'/qwack/qwack/api/Qwack.Core.Instruments/CashFlow',
        title:"CashFlow",
        description:""
    });

    y({
        url:'/qwack/qwack/api/Qwack.Math.Interpolation/Interpolator1DType',
        title:"Interpolator1DType",
        description:""
    });

    y({
        url:'/qwack/qwack/api/Qwack.Core.Instruments.Funding/ForwardRateAgreement',
        title:"ForwardRateAgreement",
        description:""
    });

    y({
        url:'/qwack/qwack/api/Qwack.Math.Solvers/NewtonRaphsonMultiDimensionalSolver',
        title:"NewtonRaphsonMultiDimensionalSolver",
        description:""
    });

    y({
        url:'/qwack/qwack/api/Qwack.Core.Basic/AverageType',
        title:"AverageType",
        description:""
    });

    y({
        url:'/qwack/qwack/api/Qwack.Core.Basic/FxQouteType',
        title:"FxQouteType",
        description:""
    });

    y({
        url:'/qwack/qwack/api/Qwack.Utils.Exceptions/SR',
        title:"SR",
        description:""
    });

    y({
        url:'/qwack/qwack/api/Qwack.Dates/CalendarCollection',
        title:"CalendarCollection",
        description:""
    });

    y({
        url:'/qwack/qwack/api/Qwack.Core.Calibrators/NewtonRaphsonMultiCurveSolverStaged',
        title:"NewtonRaphsonMultiCurveSolverStaged",
        description:""
    });

    y({
        url:'/qwack/qwack/api/Qwack.Core.Calibrators/NewtonRaphsonMultiCurveSolverStagedWithAnalyticJacobian',
        title:"NewtonRaphsonMultiCurveSolverStagedWithAnalyticJacobian",
        description:""
    });

    y({
        url:'/qwack/qwack/api/Qwack.Random.Sobol/SobolDirectionNumbers',
        title:"SobolDirectionNumbers",
        description:""
    });

    y({
        url:'/qwack/qwack/api/Qwack.Math.Interpolation/LinearInterpolator',
        title:"LinearInterpolator",
        description:""
    });

    return {
        search: function(q) {
            return idx.search(q).map(function(i) {
                return idMap[i.ref];
            });
        }
    };
}();
