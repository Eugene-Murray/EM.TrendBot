// -------------------------------------------------------------------------------------------------
//
//    This code is a cTrader Algo API example.
//
//    This cBot is intended to be used as a sample and does not guarantee any particular outcome or
//    profit of any kind. Use it at your own risk.
//
// -------------------------------------------------------------------------------------------------

using cAlgo.API;
using cAlgo.API.Indicators;
using System.Diagnostics;

namespace cAlgo.Robots
{
    [Robot(TimeZone = TimeZones.UTC, AccessRights = AccessRights.None, AddIndicators = true)]
    public class TrendBot : Robot
    {
        private double _volumeInUnits;

        private MovingAverage _fastMa;

        private MovingAverage _slowMa;

        [Parameter(Group = "Moving Average")]
        public DataSeries SourceSeries { get; set; }

        [Parameter("Source", Group = "Fast MA")]
        public DataSeries FastMaSource { get; set; }

        [Parameter("Period", DefaultValue = 10, Group = "Fast MA")]
        public int FastMaPeriod { get; set; }

        [Parameter("Type", DefaultValue = MovingAverageType.Exponential, Group = "Fast MA")]
        public MovingAverageType FastMaType { get; set; }

        [Parameter("Source", Group = "Slow MA")]
        public DataSeries SlowMaSource { get; set; }

        [Parameter("Period", DefaultValue = 20, Group = "Slow MA")]
        public int SlowMaPeriod { get; set; }

        [Parameter("Type", DefaultValue = MovingAverageType.Exponential, Group = "Slow MA")]
        public MovingAverageType SlowMaType { get; set; }

        [Parameter("Volume (Lots)", DefaultValue = 1, Group = "Trade")]
        public double VolumeInLots { get; set; }

        [Parameter("Stop Loss (Pips)", DefaultValue = 200, MaxValue = 2000, MinValue = 1, Step = 1)]
        public double StopLossInPips { get; set; }

        [Parameter("Take Profit (Pips)", DefaultValue = 0, MaxValue = 10000, MinValue = 0, Step = 1)]
        public double TakeProfitInPips { get; set; }

        [Parameter("Label", DefaultValue = "TrendBot", Group = "Trade")]
        public string Label { get; set; }

        private MovingAverage _200Ma;
        private AverageDirectionalMovementIndexRating _averageDirectionalMovementIndexRating;

        public Position[] BotPositions
        {
            get
            {
                return Positions.FindAll(Label);
            }
        }

        protected override void OnStart()
        {
            Print("## Algorithm started at {0}", Server.Time);

            _volumeInUnits = Symbol.QuantityToVolumeInUnits(VolumeInLots);

            _fastMa = Indicators.MovingAverage(FastMaSource, FastMaPeriod, FastMaType);
            _slowMa = Indicators.MovingAverage(SlowMaSource, SlowMaPeriod, SlowMaType);
            _200Ma = Indicators.MovingAverage(SourceSeries, 200, MovingAverageType.Exponential);
            _averageDirectionalMovementIndexRating = AverageDirectionalMovementIndexRating(20);

            

            _fastMa.Result.Line.Color = Color.White;
            _slowMa.Result.Line.Color = Color.White;
            _200Ma.Result.Line.Color = Color.White;
            _200Ma.Result.Line.Thickness = 3;
        }

        protected override void OnBarClosed()
        {
            if (_fastMa.Result.HasCrossedAbove(_slowMa.Result, 0))
            {

                Print("#########");
                Print("IsMaTrending: {0}", IsMaTrending());
                IsAdxTrending();
                Print("200 MA IsRising: {0}", _200Ma.Result.IsRising());
                Print("Open New Position: BUY");
                Print("#########");

                ClosePositions(TradeType.Sell);

                ExecuteMarketOrder(TradeType.Buy, SymbolName, _volumeInUnits, Label, StopLossInPips, TakeProfitInPips);
            }
            else if (_fastMa.Result.HasCrossedBelow(_slowMa.Result, 0))
            {
                Print("#########");
                Print("IsMaTrending: {0}", IsMaTrending());
                IsAdxTrending();
                Print("200 MA IsRising: {0}", _200Ma.Result.IsRising());
                Print("Open New Position: SELL");
                Print("#########");

                ClosePositions(TradeType.Buy);

                ExecuteMarketOrder(TradeType.Sell, SymbolName, _volumeInUnits, Label, StopLossInPips, TakeProfitInPips);
            }
        }


        private void ClosePositions(TradeType tradeType)
        {
            Print("## ClosePositions");

            foreach (var position in BotPositions)
            {
                if (position.TradeType != tradeType) continue;

                ClosePosition(position);
            }
        }

        private bool IsMaTrending()
        {
            return (_slowMa.Result.IsRising() && _fastMa.Result.IsRising() && _200Ma.Result.IsRising())
                   || (_slowMa.Result.IsFalling() && _fastMa.Result.IsFalling() && _200Ma.Result.IsFalling());

        }

        private bool IsAdxTrending()
        {
            //Print("ADXR Last 0: {0}", _averageDirectionalMovementIndexRating.ADXR.Last(0));
            //Print("ADXR Last 1: {0}", _averageDirectionalMovementIndexRating.ADXR.Last(1));
            //Print("ADX Last 0: {0}", _averageDirectionalMovementIndexRating.ADXR.Last(0));
            //Print("ADX Last 1: {0}", _averageDirectionalMovementIndexRating.ADX.Last(1));
            if (_averageDirectionalMovementIndexRating.ADXR.Last(0) < 25 && (_200Ma.Result.IsRising() || _200Ma.Result.IsFalling()))
            {
                Print("ADX IsTrending: False");
                return false;
            }
            Print("ADX IsTrending: True");
            return true;
        }

        private ExponentialMovingAverage ExponentialMovingAverage(TimeFrame timeFrame, int period)
        {
            return Indicators.ExponentialMovingAverage(MarketData.GetBars(timeFrame).ClosePrices, period);
        }

        private AverageDirectionalMovementIndexRating AverageDirectionalMovementIndexRating(int period)
        {
            return Indicators.AverageDirectionalMovementIndexRating(period);
        }

        private void DebugBot()
        {
            var result = Debugger.Launch();
        }

        protected override void OnStop()
        {
            Print("Algorithm stopped at {0}", Server.Time);
        }
    }
}