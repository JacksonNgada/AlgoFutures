using System;
using System.Windows.Media;
using NinjaTrader.Cbi;
using NinjaTrader.Data;
using NinjaTrader.Gui;
using NinjaTrader.Gui.MarketAnalyzer;
using NinjaTrader.Gui.Tools;
using NinjaTrader.NinjaScript;
using NinjaTrader.NinjaScript.DrawingTools;
using NinjaTrader.NinjaScript.Strategies;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace NinjaTrader.NinjaScript.Strategies
{
    public class xScapler : Strategy
    {
        private double recentHigh;
        private double recentLow = double.MaxValue;
        private int swingHighBarNumber = -1;
        private int swingLowBarNumber = -1;
        private readonly string highLineTag = "RecentHighLine";
        private readonly string lowLineTag = "RecentLowLine";
        private readonly string highTextTag = "HighText";
        private readonly string lowTextTag = "LowText";
        private double intendedLongStop;
        private double intendedShortStop;
        private double recentFVGHigh;
        private double recentFVGLow;
        private int fvgHighBarNumber = -1;
        private int fvgLowBarNumber = -1;
        private readonly string fvgHighTag = "FVGHighRect";
        private readonly string fvgLowTag = "FVGLowRect";
        private int fvgLowFillBarNumber = -1;
        private int fvgHighFillBarNumber = -1;
        private readonly string fvgLowFillArrowTag = "FVGLowFillArrow";
        private readonly string fvgHighFillArrowTag = "FVGHighFillArrow";
        private readonly string tradeSetupLabel = "TradeSetupLabel";
        private string currentTradeSetup = "";
        private readonly string tradingStatusLabel = "TradingStatusLabel";

        // 5M variables
        private double recentHigh5M;
        private double recentLow5M = double.MaxValue;
        private int swingHighBarNumber5M = -1;
        private int swingLowBarNumber5M = -1;
        private readonly string highLineTag5M = "RecentHighLine5M";
        private readonly string lowLineTag5M = "RecentLowLine5M";
        private double recentFVGHigh5M;
        private double recentFVGLow5M;
        private int fvgHighBarNumber5M = -1;
        private int fvgLowBarNumber5M = -1;
        private readonly string fvgHighTag5M = "FVGHighRect5M";
        private readonly string fvgLowTag5M = "FVGLowRect5M";
        private int fvgLowFillBarNumber5M = -1;
        private int fvgHighFillBarNumber5M = -1;
        private readonly string fvgLowFillArrowTag5M = "FVGLowFillArrow5M";
        private readonly string fvgHighFillArrowTag5M = "FVGHighFillArrow5M";

        // 1H variables
        private double recentHigh1H;
        private double recentLow1H = double.MaxValue;
        private int swingHighBarNumber1H = -1;
        private int swingLowBarNumber1H = -1;
        private readonly string highLineTag1H = "RecentHighLine1H";
        private readonly string lowLineTag1H = "RecentLowLine1H";
        private double recentFVGHigh1H;
        private double recentFVGLow1H;
        private int fvgHighBarNumber1H = -1;
        private int fvgLowBarNumber1H = -1;
        private readonly string fvgHighTag1H = "FVGHighRect1H";
        private readonly string fvgLowTag1H = "FVGLowRect1H";
        private int fvgLowFillBarNumber1H = -1;
        private int fvgHighFillBarNumber1H = -1;
        private readonly string fvgLowFillArrowTag1H = "FVGLowFillArrow1H";
        private readonly string fvgHighFillArrowTag1H = "FVGHighFillArrow1H";

        // Session tracking variables
        private DateTime lastDay = DateTime.MinValue;
        private int morningSessionWins = 0;
        private int morningSessionLosses = 0;
        private int afternoonSessionWins = 0;
        private int afternoonSessionLosses = 0;
        private int tradeCountToday = 0;

        // Additional variables for modifications
        private int consecutiveLosses = 0;
        private int lastTradeCount = 0;
        private List<string> openEntries = new List<string>();
        private double sharedLongStopLoss = 0;
        private double sharedLongTakeProfit = 0;
        private double sharedShortStopLoss = 0;
        private double sharedShortTakeProfit = 0;

        // User input properties
        [NinjaScriptProperty]
        [Display(Name = "Max Trades Per Day", Description = "Maximum number of trades allowed per day", Order = 1, GroupName = "Trading Parameters")]
        public int MaxTradesPerDay { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Max Daily Profit %", Description = "Maximum daily profit percentage (min 1%)", Order = 2, GroupName = "Trading Parameters")]
        public double MaxDailyProfitPct { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Max Daily Loss %", Description = "Maximum daily loss percentage (min 1%)", Order = 3, GroupName = "Trading Parameters")]
        public double MaxDailyLossPct { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Max Risk Per Trade %", Description = "Maximum risk per trade percentage (min 1%)", Order = 4, GroupName = "Trading Parameters")]
        public double MaxRiskPerTradePct { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Max Morning Session Losses", Description = "Maximum losses allowed in morning session", Order = 5, GroupName = "Trading Parameters")]
        public int MaxMorningSessionLosses { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Max Afternoon Session Losses", Description = "Maximum losses allowed in afternoon session", Order = 6, GroupName = "Trading Parameters")]
        public int MaxAfternoonSessionLosses { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Max Morning Session Wins", Description = "Maximum wins allowed in morning session", Order = 7, GroupName = "Trading Parameters")]
        public int MaxMorningSessionWins { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Max Afternoon Session Wins", Description = "Maximum wins allowed in afternoon session", Order = 8, GroupName = "Trading Parameters")]
        public int MaxAfternoonSessionWins { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Morning Session Start (HH:MM)", Description = "Morning session start time (HH:MM, 24-hour format)", Order = 9, GroupName = "Trading Parameters")]
        public string MorningSessionStart { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Morning Session End (HH:MM)", Description = "Morning session end time (HH:MM, 24-hour format)", Order = 10, GroupName = "Trading Parameters")]
        public string MorningSessionEnd { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Afternoon Session Start (HH:MM)", Description = "Afternoon session start time (HH:MM, 24-hour format)", Order = 11, GroupName = "Trading Parameters")]
        public string AfternoonSessionStart { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Afternoon Session End (HH:MM)", Description = "Afternoon session end time (HH:MM, 24-hour format)", Order = 12, GroupName = "Trading Parameters")]
        public string AfternoonSessionEnd { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Fixed Stop Loss (Ticks)", Description = "Fixed stop loss in ticks (0 to use dynamic)", Order = 13, GroupName = "Trading Parameters")]
        public double FixedStopLossTicks { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Fixed Take Profit (Ticks)", Description = "Fixed take profit in ticks (0 to use dynamic)", Order = 14, GroupName = "Trading Parameters")]
        public double FixedTakeProfitTicks { get; set; }

        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Description = @"Scalper strategy with unique OCO IDs, FVG detection, and customizable trading restrictions";
                Name = "xScapler";
                Calculate = Calculate.OnBarClose;
                EntriesPerDirection = 99; // Set high to allow multiple without auto-reverse
                EntryHandling = EntryHandling.AllEntries;
                IsExitOnSessionCloseStrategy = true;
                ExitOnSessionCloseSeconds = 30;
                IsInstantiatedOnEachOptimizationIteration = false;
                DefaultQuantity = 1;

                // Default values
                MaxTradesPerDay = 2;
                MaxDailyProfitPct = 2.0;
                MaxDailyLossPct = 1.0;
                MaxRiskPerTradePct = 1.0;
                MaxMorningSessionLosses = 3;
                MaxAfternoonSessionLosses = 3;
                MaxMorningSessionWins = 1;
                MaxAfternoonSessionWins = 1;
                MorningSessionStart = "09:00";
                MorningSessionEnd = "11:00";
                AfternoonSessionStart = "14:00";
                AfternoonSessionEnd = "18:30";
                FixedStopLossTicks = 0;
                FixedTakeProfitTicks = 0;
            }
            else if (State == State.Configure)
            {
                AddDataSeries(BarsPeriodType.Minute, 5);  // 5M timeframe
                AddDataSeries(BarsPeriodType.Minute, 60); // 1H timeframe

                // Validate user inputs
                if (MaxTradesPerDay < 1)
                    MaxTradesPerDay = 2;
                if (MaxDailyProfitPct < 1.0)
                    MaxDailyProfitPct = 2.0;
                if (MaxDailyLossPct < 1.0)
                    MaxDailyLossPct = 1.0;
                if (MaxRiskPerTradePct < 1.0)
                    MaxRiskPerTradePct = 1.0;
                if (MaxMorningSessionLosses < 0)
                    MaxMorningSessionLosses = 3;
                if (MaxAfternoonSessionLosses < 0)
                    MaxAfternoonSessionLosses = 3;
                if (MaxMorningSessionWins < 0)
                    MaxMorningSessionWins = 1;
                if (MaxAfternoonSessionWins < 0)
                    MaxAfternoonSessionWins = 1;
                if (!ValidateTimeFormat(MorningSessionStart)) MorningSessionStart = "09:00";
                if (!ValidateTimeFormat(MorningSessionEnd)) MorningSessionEnd = "11:00";
                if (!ValidateTimeFormat(AfternoonSessionStart)) AfternoonSessionStart = "14:00";
                if (!ValidateTimeFormat(AfternoonSessionEnd)) AfternoonSessionEnd = "18:30";
                if (FixedStopLossTicks < 0) FixedStopLossTicks = 0;
                if (FixedTakeProfitTicks < 0) FixedTakeProfitTicks = 0;
            }
            else if (State == State.DataLoaded)
            {
                lastDay = DateTime.MinValue;
                tradeCountToday = 0;
                Print($"State.DataLoaded: Initialized lastDay={lastDay}, tradeCountToday={tradeCountToday}");
            }
        }

        private bool ValidateTimeFormat(string timeString)
        {
            return TimeSpan.TryParse(timeString, out _);
        }

        protected override void OnBarUpdate()
        {
            // Parse session times
            TimeSpan morningStart = TimeSpan.Parse(MorningSessionStart);
            TimeSpan morningEnd = TimeSpan.Parse(MorningSessionEnd);
            TimeSpan afternoonStart = TimeSpan.Parse(AfternoonSessionStart);
            TimeSpan afternoonEnd = TimeSpan.Parse(AfternoonSessionEnd);

            // Reset daily stats at the start of a new trading day
            DateTime currentDay = Time[0].Date;
            TimeSpan currentTime = Time[0].TimeOfDay;
            bool isTradingSession = (currentTime >= morningStart && currentTime <= morningEnd) ||
                                   (currentTime >= afternoonStart && currentTime <= afternoonEnd);
            bool isNewDay = currentDay != lastDay;

            if (isNewDay)
            {
                lastDay = currentDay;
                morningSessionWins = 0;
                morningSessionLosses = 0;
                afternoonSessionWins = 0;
                afternoonSessionLosses = 0;
                tradeCountToday = 0;
                consecutiveLosses = 0;
                sharedLongStopLoss = 0;
                sharedLongTakeProfit = 0;
                sharedShortStopLoss = 0;
                sharedShortTakeProfit = 0;
                Print($"New trading day reset at {Time[0]}: lastDay={lastDay}, tradeCountToday={tradeCountToday}, all session stats cleared");
            }

            // Log trading allowance status for debugging
            string reason;
            bool isTradingAllowed = TradingIsAllowed(out reason);
            Print($"OnBarUpdate at {Time[0]}: tradeCountToday={tradeCountToday}, MaxTradesPerDay={MaxTradesPerDay}, TradingAllowed={isTradingAllowed}, Reason={reason}");

            if (BarsInProgress == 2) // 1H timeframe
            {
                if (CurrentBars[2] < 2) return;
                DetectSwingPoints1H();
                DetectFVG1H();
            }
            else if (BarsInProgress == 1) // 5M timeframe
            {
                if (CurrentBars[1] < 2) return;
                DetectSwingPoints5M();
                DetectFVG5M();
            }
            else if (BarsInProgress == 0) // 1M timeframe
            {
                if (CurrentBars[0] < 2) return;
                DetectSwingPoints1M();
                DetectFVG1M();
                DetectFVG5MFill();
                DetectFVG1HFill();
                DrawLevels1M();
                DrawFVG1M();
                DetectBreakOfStructure1M();
                BullishFVG1Min();
                BearishFVG1Min();
                DrawLevels5M();
                DrawFVG5M();
                DrawTradeSetupLabel();
                DrawTradingStatus();
            }

            // Process completed trades for win/loss tracking
            if (SystemPerformance.AllTrades.Count > lastTradeCount)
            {
                for (int i = lastTradeCount; i < SystemPerformance.AllTrades.Count; i++)
                {
                    Trade trade = SystemPerformance.AllTrades[i];
                    double pnL = trade.ProfitCurrency;
                    DateTime exitTime = trade.Exit.Time;
                    TimeSpan tradeTime = exitTime.TimeOfDay;
                    bool isMorningSession = tradeTime >= morningStart && tradeTime <= morningEnd;
                    bool isAfternoonSession = tradeTime >= afternoonStart && tradeTime <= afternoonEnd;

                    if (pnL > 0)
                    {
                        consecutiveLosses = 0; // Reset on win
                        if (isMorningSession)
                            morningSessionWins++;
                        else if (isAfternoonSession)
                            afternoonSessionWins++;
                        Print($"Trade closed with profit: {pnL:F2}, MorningWins={morningSessionWins}, AfternoonWins={afternoonSessionWins}, ConsecutiveLosses={consecutiveLosses}");
                    }
                    else if (pnL < 0)
                    {
                        consecutiveLosses++; // Increment on loss
                        if (consecutiveLosses >= 7)
                            consecutiveLosses = 0;
                        if (isMorningSession)
                            morningSessionLosses++;
                        else if (isAfternoonSession)
                            afternoonSessionLosses++;
                        Print($"Trade closed with loss: {pnL:F2}, MorningLosses={morningSessionLosses}, AfternoonLosses={afternoonSessionLosses}, ConsecutiveLosses={consecutiveLosses}");
                    }
                    Print($"Processed trade {i}: PnL={pnL:F2}, ConsecutiveLosses={consecutiveLosses}");
                }
                lastTradeCount = SystemPerformance.AllTrades.Count;
            }

            if (Position.MarketPosition == MarketPosition.Flat)
            {
                currentTradeSetup = "";
                sharedLongStopLoss = 0;
                sharedLongTakeProfit = 0;
                sharedShortStopLoss = 0;
                sharedShortTakeProfit = 0;
            }
        }

        private bool TradingIsAllowed(out string reason)
        {
            var reasons = new List<string>();
            TimeSpan currentTime = Time[0].TimeOfDay;
            TimeSpan morningStart = TimeSpan.Parse(MorningSessionStart);
            TimeSpan morningEnd = TimeSpan.Parse(MorningSessionEnd);
            TimeSpan afternoonStart = TimeSpan.Parse(AfternoonSessionStart);
            TimeSpan afternoonEnd = TimeSpan.Parse(AfternoonSessionEnd);
            bool isMorningSession = currentTime >= morningStart && currentTime <= morningEnd;
            bool isAfternoonSession = currentTime >= afternoonStart && currentTime <= afternoonEnd;

            // Log current state for debugging
            double pnL = GetDailyPnL();
            Print($"TradingIsAllowed check at {Time[0]}: MorningWins={morningSessionWins}, MorningLosses={morningSessionLosses}, AfternoonWins={afternoonSessionWins}, AfternoonLosses={afternoonSessionLosses}, TradeCountToday={tradeCountToday}, PnL={pnL}");

            // Check session time restrictions
            if (!isMorningSession && !isAfternoonSession)
                reasons.Add($"Outside trading sessions ({MorningSessionStart}–{MorningSessionEnd} or {AfternoonSessionStart}–{AfternoonSessionEnd} SAST)");

            // Check max trades per day
            if (tradeCountToday >= MaxTradesPerDay)
                reasons.Add($"Max trades per day ({MaxTradesPerDay}) reached");

            // Check daily profit/loss limits
            double accountBalance = Account.Get(AccountItem.CashValue, Currency.UsDollar);
            double maxProfit = (MaxDailyProfitPct / 100.0) * accountBalance;
            double maxLoss = -(MaxDailyLossPct / 100.0) * accountBalance;
            if (pnL >= maxProfit)
                reasons.Add($"Daily profit limit ({MaxDailyProfitPct}% = {maxProfit:F2}) reached");
            if (pnL <= maxLoss)
                reasons.Add($"Daily loss limit ({MaxDailyLossPct}% = {maxLoss:F2}) reached");

            // Check session win/loss caps
            if (isMorningSession)
            {
                if (morningSessionWins >= MaxMorningSessionWins)
                    reasons.Add($"Morning session win limit ({MaxMorningSessionWins}) reached");
                if (morningSessionLosses >= MaxMorningSessionLosses)
                    reasons.Add($"Morning session loss limit ({MaxMorningSessionLosses}) reached");
            }
            else if (isAfternoonSession)
            {
                if (afternoonSessionWins >= MaxAfternoonSessionWins)
                    reasons.Add($"Afternoon session win limit ({MaxAfternoonSessionWins}) reached");
                if (afternoonSessionLosses >= MaxAfternoonSessionLosses)
                    reasons.Add($"Afternoon session loss limit ({MaxAfternoonSessionLosses}) reached");
            }

            reason = reasons.Count == 0 ? "Trading Allowed" : string.Join(", ", reasons);
            return reasons.Count == 0;
        }

        private void DetectSwingPoints1M()
        {
            if (Close[1] > Open[1] && Close[0] < Open[0])
            {
                double high1 = High[1];
                double high0 = High[0];
                recentHigh = Math.Max(high1, high0);
                swingHighBarNumber = (high1 > high0) ? CurrentBar - 1 : CurrentBar;
            }
            if (Close[1] < Open[1] && Close[0] > Open[0])
            {
                double low1 = Low[1];
                double low0 = Low[0];
                recentLow = Math.Min(low1, low0);
                swingLowBarNumber = (low1 < low0) ? CurrentBar - 1 : CurrentBar;
            }
            if (recentHigh == 0)
                FindRecentSwingHigh1M();
            if (recentLow == double.MaxValue)
                FindRecentSwingLow1M();
        }

        private void FindRecentSwingHigh1M()
        {
            for (int barsAgo = 1; barsAgo < CurrentBar; barsAgo++)
            {
                if (Close[barsAgo + 1] <= Open[barsAgo + 1] || Close[barsAgo] >= Open[barsAgo]) continue;
                double high1 = High[barsAgo + 1];
                double high0 = High[barsAgo];
                recentHigh = Math.Max(high1, high0);
                int offset = (high1 > high0) ? barsAgo + 1 : barsAgo;
                swingHighBarNumber = CurrentBar - offset;
                break;
            }
        }

        private void FindRecentSwingLow1M()
        {
            for (int barsAgo = 1; barsAgo < CurrentBar; barsAgo++)
            {
                if (Close[barsAgo + 1] >= Open[barsAgo + 1] || Close[barsAgo] <= Open[barsAgo]) continue;
                double low1 = Low[barsAgo + 1];
                double low0 = Low[barsAgo];
                recentLow = Math.Min(low1, low0);
                int offset = (low1 < low0) ? barsAgo + 1 : barsAgo;
                swingLowBarNumber = CurrentBar - offset;
                break;
            }
        }

        private void DetectSwingPoints5M()
        {
            if (Closes[1][1] > Opens[1][1] && Closes[1][0] < Opens[1][0])
            {
                double high1 = Highs[1][1];
                double high0 = Highs[1][0];
                recentHigh5M = Math.Max(high1, high0);
                int offset = (high1 > high0) ? 1 : 0;
                swingHighBarNumber5M = CurrentBars[1] - offset;
            }
            if (Closes[1][1] < Opens[1][1] && Closes[1][0] > Opens[1][0])
            {
                double low1 = Lows[1][1];
                double low0 = Lows[1][0];
                recentLow5M = Math.Min(low1, low0);
                int offset = (low1 < low0) ? 1 : 0;
                swingLowBarNumber5M = CurrentBars[1] - offset;
            }
            if (recentHigh5M == 0)
                FindRecentSwingHigh5M();
            if (recentLow5M == double.MaxValue)
                FindRecentSwingLow5M();
        }

        private void FindRecentSwingHigh5M()
        {
            for (int barsAgo = 1; barsAgo < CurrentBars[1]; barsAgo++)
            {
                if (Closes[1][barsAgo + 1] <= Opens[1][barsAgo + 1] || Closes[1][barsAgo] >= Opens[1][barsAgo]) continue;
                double high1 = Highs[1][barsAgo + 1];
                double high0 = Highs[1][barsAgo];
                recentHigh5M = Math.Max(high1, high0);
                int offset = (high1 > high0) ? barsAgo + 1 : barsAgo;
                swingHighBarNumber5M = CurrentBars[1] - offset;
                break;
            }
        }

        private void FindRecentSwingLow5M()
        {
            for (int barsAgo = 1; barsAgo < CurrentBars[1]; barsAgo++)
            {
                if (Closes[1][barsAgo + 1] >= Opens[1][barsAgo + 1] || Closes[1][barsAgo] <= Opens[1][barsAgo]) continue;
                double low1 = Lows[1][barsAgo + 1];
                double low0 = Lows[1][barsAgo];
                recentLow5M = Math.Min(low1, low0);
                int offset = (low1 < low0) ? barsAgo + 1 : barsAgo;
                swingLowBarNumber5M = CurrentBars[1] - offset;
                break;
            }
        }

        private void DetectSwingPoints1H()
        {
            if (Closes[2][1] > Opens[2][1] && Closes[2][0] < Opens[2][0])
            {
                double high1 = Highs[2][1];
                double high0 = Highs[2][0];
                recentHigh1H = Math.Max(high1, high0);
                int offset = (high1 > high0) ? 1 : 0;
                swingHighBarNumber1H = CurrentBars[2] - offset;
            }
            if (Closes[2][1] < Opens[2][1] && Closes[2][0] > Opens[2][0])
            {
                double low1 = Lows[2][1];
                double low0 = Lows[2][0];
                recentLow1H = Math.Min(low1, low0);
                int offset = (low1 < low0) ? 1 : 0;
                swingLowBarNumber1H = CurrentBars[2] - offset;
            }
            if (recentHigh1H == 0)
                FindRecentSwingHigh1H();
            if (recentLow1H == double.MaxValue)
                FindRecentSwingLow1H();
        }

        private void FindRecentSwingHigh1H()
        {
            for (int barsAgo = 1; barsAgo < CurrentBars[2]; barsAgo++)
            {
                if (Closes[2][barsAgo + 1] <= Opens[2][barsAgo + 1] || Closes[2][barsAgo] >= Opens[2][barsAgo]) continue;
                double high1 = Highs[2][barsAgo + 1];
                double high0 = Highs[2][barsAgo];
                recentHigh1H = Math.Max(high1, high0);
                int offset = (high1 > high0) ? barsAgo + 1 : barsAgo;
                swingHighBarNumber1H = CurrentBars[2] - offset;
                break;
            }
        }

        private void FindRecentSwingLow1H()
        {
            for (int barsAgo = 1; barsAgo < CurrentBars[2]; barsAgo++)
            {
                if (Closes[2][barsAgo + 1] >= Opens[2][barsAgo + 1] || Closes[2][barsAgo] <= Opens[2][barsAgo]) continue;
                double low1 = Lows[2][barsAgo + 1];
                double low0 = Lows[2][barsAgo];
                recentLow1H = Math.Min(low1, low0);
                int offset = (low1 < low0) ? barsAgo + 1 : barsAgo;
                swingLowBarNumber1H = CurrentBars[2] - offset;
                break;
            }
        }

        private void DetectFVG1M()
        {
            if (CurrentBar < 3) return;
            if (fvgLowBarNumber > 0 && recentFVGLow < recentFVGHigh && fvgLowFillBarNumber == -1)
            {
                if (Low[0] <= recentFVGHigh)
                {
                    fvgLowFillBarNumber = CurrentBar;
                    Print($"Bullish FVG filled at bar {CurrentBar}, Low: {Low[0]}, Gap High: {recentFVGHigh}");
                }
            }
            if (fvgHighBarNumber > 0 && recentFVGLow < recentFVGHigh && fvgHighFillBarNumber == -1)
            {
                if (High[0] >= recentFVGLow)
                {
                    fvgHighFillBarNumber = CurrentBar;
                    Print($"Bearish FVG filled at bar {CurrentBar}, High: {High[0]}, Gap Low: {recentFVGLow}");
                }
            }
            if (Low[0] > High[2])
            {
                bool shouldUpdate = (fvgLowBarNumber == -1) ||
                                   (CurrentBar - fvgLowBarNumber > 2) ||
                                   (Math.Abs(Low[0] - recentFVGHigh) > TickSize) ||
                                   (Math.Abs(High[2] - recentFVGLow) > TickSize);
                if (shouldUpdate)
                {
                    recentFVGLow = High[2];
                    recentFVGHigh = Low[0];
                    fvgLowBarNumber = CurrentBar - 1;
                    fvgLowFillBarNumber = -1;
                    fvgHighBarNumber = -1;
                    fvgHighFillBarNumber = -1;
                    RemoveDrawObject(fvgHighFillArrowTag);
                    Print($"Bullish FVG detected at bar {fvgLowBarNumber}, Gap: {recentFVGLow} to {recentFVGHigh}");
                }
            }
            else if (High[0] < Low[2])
            {
                bool shouldUpdate = (fvgHighBarNumber == -1) ||
                                   (CurrentBar - fvgHighBarNumber > 2) ||
                                   (Math.Abs(High[0] - recentFVGLow) > TickSize) ||
                                   (Math.Abs(Low[2] - recentFVGHigh) > TickSize);
                if (shouldUpdate)
                {
                    recentFVGHigh = Low[2];
                    recentFVGLow = High[0];
                    fvgHighBarNumber = CurrentBar - 1;
                    fvgHighFillBarNumber = -1;
                    fvgLowBarNumber = -1;
                    fvgLowFillBarNumber = -1;
                    RemoveDrawObject(fvgLowFillArrowTag);
                    Print($"Bearish FVG detected at bar {fvgHighBarNumber}, Gap: {recentFVGLow} to {recentFVGHigh}");
                }
            }
        }

        private void DetectFVG5M()
        {
            if (CurrentBars[1] < 3) return;
            if (Lows[1][0] > Highs[1][2])
            {
                bool shouldUpdate = (fvgLowBarNumber5M == -1) ||
                                   (CurrentBars[1] - fvgLowBarNumber5M > 2) ||
                                   (Math.Abs(Lows[1][0] - recentFVGHigh5M) > TickSize) ||
                                   (Math.Abs(Highs[1][2] - recentFVGLow5M) > TickSize);
                if (shouldUpdate)
                {
                    recentFVGLow5M = Highs[1][2];
                    recentFVGHigh5M = Lows[1][0];
                    fvgLowBarNumber5M = CurrentBars[1] - 1;
                    fvgLowFillBarNumber5M = -1;
                    fvgHighBarNumber5M = -1;
                    fvgHighFillBarNumber5M = -1;
                    RemoveDrawObject(fvgHighFillArrowTag5M);
                    Print($"Bullish FVG5M detected at bar {fvgLowBarNumber5M}, Gap: {recentFVGLow5M} to {recentFVGHigh5M}");
                }
            }
            else if (Highs[1][0] < Lows[1][2])
            {
                bool shouldUpdate = (fvgHighBarNumber5M == -1) ||
                                   (CurrentBars[1] - fvgHighBarNumber5M > 2) ||
                                   (Math.Abs(Highs[1][0] - recentFVGLow5M) > TickSize) ||
                                   (Math.Abs(Lows[1][2] - recentFVGHigh5M) > TickSize);
                if (shouldUpdate)
                {
                    recentFVGHigh5M = Lows[1][2];
                    recentFVGLow5M = Highs[1][0];
                    fvgHighBarNumber5M = CurrentBars[1] - 1;
                    fvgHighFillBarNumber5M = -1;
                    fvgLowBarNumber5M = -1;
                    fvgLowFillBarNumber5M = -1;
                    RemoveDrawObject(fvgLowFillArrowTag5M);
                    Print($"Bearish FVG5M detected at bar {fvgHighBarNumber5M}, Gap: {recentFVGLow5M} to {recentFVGHigh5M}");
                }
            }
        }

        private void DetectFVG1H()
        {
            if (CurrentBars[2] < 3) return;
            if (Lows[2][0] > Highs[2][2])
            {
                bool shouldUpdate = (fvgLowBarNumber1H == -1) ||
                                   (CurrentBars[2] - fvgLowBarNumber1H > 2) ||
                                   (Math.Abs(Lows[2][0] - recentFVGHigh1H) > TickSize) ||
                                   (Math.Abs(Highs[2][2] - recentFVGLow1H) > TickSize);
                if (shouldUpdate)
                {
                    recentFVGLow1H = Highs[2][2];
                    recentFVGHigh1H = Lows[2][0];
                    fvgLowBarNumber1H = CurrentBars[2] - 1;
                    fvgLowFillBarNumber1H = -1;
                    fvgHighBarNumber1H = -1;
                    fvgHighFillBarNumber1H = -1;
                    RemoveDrawObject(fvgHighFillArrowTag1H);
                    Print($"Bullish FVG1H detected at bar {fvgLowBarNumber1H}, Gap: {recentFVGLow1H} to {recentFVGHigh1H}");
                }
            }
            else if (Highs[2][0] < Lows[2][2])
            {
                bool shouldUpdate = (fvgHighBarNumber1H == -1) ||
                                   (CurrentBars[2] - fvgHighBarNumber1H > 2) ||
                                   (Math.Abs(Highs[2][0] - recentFVGLow1H) > TickSize) ||
                                   (Math.Abs(Lows[2][2] - recentFVGHigh1H) > TickSize);
                if (shouldUpdate)
                {
                    recentFVGHigh1H = Lows[2][2];
                    recentFVGLow1H = Highs[2][0];
                    fvgHighBarNumber1H = CurrentBars[2] - 1;
                    fvgHighFillBarNumber1H = -1;
                    fvgLowBarNumber1H = -1;
                    fvgLowFillBarNumber1H = -1;
                    RemoveDrawObject(fvgLowFillArrowTag1H);
                    Print($"Bearish FVG1H detected at bar {fvgHighBarNumber1H}, Gap: {recentFVGLow1H} to {recentFVGHigh1H}");
                }
            }
        }

        private void DetectFVG5MFill()
        {
            if (fvgLowBarNumber5M > 0 && recentFVGLow5M < recentFVGHigh5M && fvgLowFillBarNumber5M == -1)
            {
                if (Low[0] <= recentFVGHigh5M)
                {
                    fvgLowFillBarNumber5M = CurrentBar;
                    Print($"Bullish FVG5M filled at bar {CurrentBar}, Low: {Low[0]}, Gap High: {recentFVGHigh5M}");
                }
            }
            if (fvgHighBarNumber5M > 0 && recentFVGLow5M < recentFVGHigh5M && fvgHighFillBarNumber5M == -1)
            {
                if (High[0] >= recentFVGLow5M)
                {
                    fvgHighFillBarNumber5M = CurrentBar;
                    Print($"Bearish FVG5M filled at bar {CurrentBar}, High: {High[0]}, Gap Low: {recentFVGLow5M}");
                }
            }
        }

        private void DetectFVG1HFill()
        {
            if (fvgLowBarNumber1H > 0 && recentFVGLow1H < recentFVGHigh1H && fvgLowFillBarNumber1H == -1)
            {
                if (Low[0] <= recentFVGHigh1H)
                {
                    fvgLowFillBarNumber1H = CurrentBar;
                    Print($"Bullish FVG1H filled at bar {CurrentBar}, Low: {Low[0]}, Gap High: {recentFVGHigh1H}");
                }
            }
            if (fvgHighBarNumber1H > 0 && recentFVGLow1H < recentFVGHigh1H && fvgHighFillBarNumber1H == -1)
            {
                if (High[0] >= recentFVGLow1H)
                {
                    fvgHighFillBarNumber1H = CurrentBar;
                    Print($"Bearish FVG1H filled at bar {CurrentBar}, High: {High[0]}, Gap Low: {recentFVGLow1H}");
                }
            }
        }

        private void DrawFVG1M()
        {
            RemoveDrawObject(fvgHighTag);
            RemoveDrawObject(fvgLowTag);
            if (fvgLowBarNumber > 0 && recentFVGLow < recentFVGHigh)
            {
                int barsAgo = CurrentBar - fvgLowBarNumber;
                if (barsAgo >= 0)
                {
                    Brush bullishBrush = new SolidColorBrush(Color.FromArgb(80, 0, 255, 0));
                    Draw.Rectangle(this, fvgLowTag, false, barsAgo + 1, recentFVGHigh, 0, recentFVGLow, bullishBrush, bullishBrush, 20, true);
                    Print($"Drawing Bullish FVG at barsAgo: {barsAgo}, High: {recentFVGHigh}, Low: {recentFVGLow}");
                    if (fvgLowFillBarNumber > 0)
                    {
                        int fillBarsAgo = CurrentBar - fvgLowFillBarNumber;
                        if (fillBarsAgo >= 0)
                        {
                            Brush goldBrush = new SolidColorBrush(Color.FromArgb(255, 255, 215, 0));
                            Draw.ArrowUp(this, fvgLowFillArrowTag, false, fillBarsAgo, Low[fillBarsAgo] - 2 * TickSize, goldBrush);
                        }
                    }
                }
            }
            if (fvgHighBarNumber > 0 && recentFVGLow < recentFVGHigh)
            {
                int barsAgo = CurrentBar - fvgHighBarNumber;
                if (barsAgo >= 0)
                {
                    Brush bearishBrush = new SolidColorBrush(Color.FromArgb(80, 255, 0, 0));
                    Draw.Rectangle(this, fvgHighTag, false, barsAgo + 1, recentFVGHigh, 0, recentFVGLow, bearishBrush, bearishBrush, 20, true);
                    Print($"Drawing Bearish FVG at barsAgo: {barsAgo}, High: {recentFVGHigh}, Low: {recentFVGLow}");
                    if (fvgHighFillBarNumber > 0)
                    {
                        int fillBarsAgo = CurrentBar - fvgHighFillBarNumber;
                        if (fillBarsAgo >= 0)
                        {
                            Brush goldBrush = new SolidColorBrush(Color.FromArgb(255, 255, 215, 0));
                            Draw.ArrowDown(this, fvgHighFillArrowTag, false, fillBarsAgo, High[fillBarsAgo] + 2 * TickSize, goldBrush);
                        }
                    }
                }
            }
        }

        private void DrawFVG5M()
        {
            RemoveDrawObject(fvgHighTag5M);
            RemoveDrawObject(fvgLowTag5M);
            if (fvgLowBarNumber5M > 0 && recentFVGLow5M < recentFVGHigh5M)
            {
                int barsAgoSec = CurrentBars[1] - fvgLowBarNumber5M;
                if (barsAgoSec >= 0)
                {
                    int startBarsAgoSec = barsAgoSec + 1;
                    DateTime startTime = Times[1][startBarsAgoSec];
                    int barIndex = Bars.GetBar(startTime);
                    if (barIndex >= 0)
                    {
                        int startBarsAgoPrim = CurrentBar - barIndex;
                        Brush bullishBrush = new SolidColorBrush(Color.FromArgb(80, 0, 255, 0));
                        Draw.Rectangle(this, fvgLowTag5M, false, startBarsAgoPrim, recentFVGHigh5M, 0, recentFVGLow5M, bullishBrush, bullishBrush, 20, true);
                        Print($"Drawing Bullish FVG5M at startBarsAgoPrim: {startBarsAgoPrim}, High: {recentFVGHigh5M}, Low: {recentFVGLow5M}");
                    }
                    if (fvgLowFillBarNumber5M > 0)
                    {
                        int fillBarsAgoPrim = CurrentBar - fvgLowFillBarNumber5M;
                        if (fillBarsAgoPrim >= 0)
                        {
                            Brush goldBrush = new SolidColorBrush(Color.FromArgb(255, 255, 215, 0));
                            Draw.ArrowUp(this, fvgLowFillArrowTag5M, false, fillBarsAgoPrim, Low[fillBarsAgoPrim] - 2 * TickSize, goldBrush);
                        }
                    }
                }
            }
            if (fvgHighBarNumber5M > 0 && recentFVGLow5M < recentFVGHigh5M)
            {
                int barsAgoSec = CurrentBars[1] - fvgHighBarNumber5M;
                if (barsAgoSec >= 0)
                {
                    int startBarsAgoSec = barsAgoSec + 1;
                    DateTime startTime = Times[1][startBarsAgoSec];
                    int barIndex = Bars.GetBar(startTime);
                    if (barIndex >= 0)
                    {
                        int startBarsAgoPrim = CurrentBar - barIndex;
                        Brush bearishBrush = new SolidColorBrush(Color.FromArgb(80, 255, 0, 0));
                        Draw.Rectangle(this, fvgHighTag5M, false, startBarsAgoPrim, recentFVGHigh5M, 0, recentFVGLow5M, bearishBrush, bearishBrush, 20, true);
                        Print($"Drawing Bearish FVG5M at startBarsAgoPrim: {startBarsAgoPrim}, High: {recentFVGHigh5M}, Low: {recentFVGLow5M}");
                    }
                    if (fvgHighFillBarNumber5M > 0)
                    {
                        int fillBarsAgoPrim = CurrentBar - fvgHighFillBarNumber5M;
                        if (fillBarsAgoPrim >= 0)
                        {
                            Brush goldBrush = new SolidColorBrush(Color.FromArgb(255, 255, 215, 0));
                            Draw.ArrowDown(this, fvgHighFillArrowTag5M, false, fillBarsAgoPrim, High[fillBarsAgoPrim] + 2 * TickSize, goldBrush);
                        }
                    }
                }
            }
        }

        private void DrawFVG1H()
        {
            RemoveDrawObject(fvgHighTag1H);
            RemoveDrawObject(fvgLowTag1H);
            if (fvgLowBarNumber1H > 0 && recentFVGLow1H < recentFVGHigh1H)
            {
                int barsAgoSec = CurrentBars[2] - fvgLowBarNumber1H;
                if (barsAgoSec >= 0)
                {
                    int startBarsAgoSec = barsAgoSec + 1;
                    DateTime startTime = Times[2][startBarsAgoSec];
                    int barIndex = Bars.GetBar(startTime);
                    if (barIndex >= 0)
                    {
                        int startBarsAgoPrim = CurrentBar - barIndex;
                        Brush bullishBrush = new SolidColorBrush(Color.FromArgb(80, 0, 255, 0));
                        Draw.Rectangle(this, fvgLowTag1H, false, startBarsAgoPrim, recentFVGHigh1H, 0, recentFVGLow1H, bullishBrush, bullishBrush, 20, true);
                        Print($"Drawing Bullish FVG1H at startBarsAgoPrim: {startBarsAgoPrim}, High: {recentFVGHigh1H}, Low: {recentFVGLow1H}");
                    }
                    if (fvgLowFillBarNumber1H > 0)
                    {
                        int fillBarsAgoPrim = CurrentBar - fvgLowFillBarNumber1H;
                        if (fillBarsAgoPrim >= 0)
                        {
                            Brush goldBrush = new SolidColorBrush(Color.FromArgb(255, 255, 215, 0));
                            Draw.ArrowUp(this, fvgLowFillArrowTag1H, false, fillBarsAgoPrim, Low[fillBarsAgoPrim] - 2 * TickSize, goldBrush);
                        }
                    }
                }
            }
            if (fvgHighBarNumber1H > 0 && recentFVGLow1H < recentFVGHigh1H)
            {
                int barsAgoSec = CurrentBars[2] - fvgHighBarNumber1H;
                if (barsAgoSec >= 0)
                {
                    int startBarsAgoSec = barsAgoSec + 1;
                    DateTime startTime = Times[2][startBarsAgoSec];
                    int barIndex = Bars.GetBar(startTime);
                    if (barIndex >= 0)
                    {
                        int startBarsAgoPrim = CurrentBar - barIndex;
                        Brush bearishBrush = new SolidColorBrush(Color.FromArgb(80, 255, 0, 0));
                        Draw.Rectangle(this, fvgHighTag1H, false, startBarsAgoPrim, recentFVGHigh1H, 0, recentFVGLow1H, bearishBrush, bearishBrush, 20, true);
                        Print($"Drawing Bearish FVG1H at startBarsAgoPrim: {startBarsAgoPrim}, High: {recentFVGHigh1H}, Low: {recentFVGLow1H}");
                    }
                    if (fvgHighFillBarNumber1H > 0)
                    {
                        int fillBarsAgoPrim = CurrentBar - fvgHighFillBarNumber1H;
                        if (fillBarsAgoPrim >= 0)
                        {
                            Brush goldBrush = new SolidColorBrush(Color.FromArgb(255, 255, 215, 0));
                            Draw.ArrowDown(this, fvgHighFillArrowTag1H, false, fillBarsAgoPrim, High[fillBarsAgoPrim] + 2 * TickSize, goldBrush);
                        }
                    }
                }
            }
        }

        private void DetectBreakOfStructure1M()
        {
            if (Close[0] > recentHigh && Close[1] <= recentHigh)
                BullishBreakout1Min();
            if (Close[0] < recentLow && Close[1] >= recentLow)
                BearishBreakout1Min();
        }

        private void BullishBreakout1Min()
        {
            if (Position.MarketPosition == MarketPosition.Short) return;
            if (Position.MarketPosition == MarketPosition.Long && openEntries.Count >= 2) return;

            string reason;
            if (!TradingIsAllowed(out reason))
            {
                Print($"Cannot enter long (Bullish Breakout): {reason}");
                return;
            }
            double entryApprox = Close[0];
            double riskPoints;
            if (Position.MarketPosition == MarketPosition.Long && sharedLongStopLoss > 0)
            {
                riskPoints = entryApprox - sharedLongStopLoss;
            }
            else if (FixedStopLossTicks > 0)
            {
                riskPoints = FixedStopLossTicks * TickSize;
                intendedLongStop = entryApprox - riskPoints;
            }
            else
            {
                intendedLongStop = recentLow;
                riskPoints = entryApprox - intendedLongStop;
            }
            int tradeQuantity = Math.Min(consecutiveLosses + 1, 7); // Cap at 7 contracts
            double accountBalance = Account.Get(AccountItem.CashValue, Currency.UsDollar);
            double maxRiskPerContract = (MaxRiskPerTradePct / 100.0) * accountBalance;
            double riskCurrencyPerContract = riskPoints * Instrument.MasterInstrument.PointValue;
            int adjustedQuantity = Math.Min(tradeQuantity, (int)(maxRiskPerContract / riskCurrencyPerContract));
            if (adjustedQuantity < 1)
            {
                Print($"Cannot enter long (Bullish Breakout): Risk per contract {riskCurrencyPerContract:F2} exceeds max risk {maxRiskPerContract:F2}");
                return;
            }
            double totalRiskCurrency = riskCurrencyPerContract * adjustedQuantity;
            double totalRiskPct = (totalRiskCurrency / accountBalance) * 100;
            if (totalRiskPct > MaxRiskPerTradePct * tradeQuantity)
            {
                Print($"Cannot enter long (Bullish Breakout): Total risk {totalRiskPct:F2}% > {MaxRiskPerTradePct * tradeQuantity}%");
                return;
            }
            string uniqueId = Guid.NewGuid().ToString("N");
            EnterLong(adjustedQuantity, $"LongEntry_{uniqueId}");
            tradeCountToday++;
            currentTradeSetup = "1M Bullish Breakout";
            Print($"Entered long (Bullish Breakout) at {Close[0]}, Stop: {intendedLongStop}, TradeCountToday: {tradeCountToday}, Quantity: {adjustedQuantity}, TotalRisk: {totalRiskCurrency:F2}");
        }

        private void BearishBreakout1Min()
        {
            if (Position.MarketPosition == MarketPosition.Long) return;
            if (Position.MarketPosition == MarketPosition.Short && openEntries.Count >= 2) return;

            string reason;
            if (!TradingIsAllowed(out reason))
            {
                Print($"Cannot enter short (Bearish Breakout): {reason}");
                return;
            }
            double entryApprox = Close[0];
            double riskPoints;
            if (Position.MarketPosition == MarketPosition.Short && sharedShortStopLoss > 0)
            {
                riskPoints = sharedShortStopLoss - entryApprox;
            }
            else if (FixedStopLossTicks > 0)
            {
                riskPoints = FixedStopLossTicks * TickSize;
                intendedShortStop = entryApprox + riskPoints;
            }
            else
            {
                intendedShortStop = recentHigh;
                riskPoints = intendedShortStop - entryApprox;
            }
            int tradeQuantity = Math.Min(consecutiveLosses + 1, 7); // Cap at 7 contracts
            double accountBalance = Account.Get(AccountItem.CashValue, Currency.UsDollar);
            double maxRiskPerContract = (MaxRiskPerTradePct / 100.0) * accountBalance;
            double riskCurrencyPerContract = riskPoints * Instrument.MasterInstrument.PointValue;
            int adjustedQuantity = Math.Min(tradeQuantity, (int)(maxRiskPerContract / riskCurrencyPerContract));
            if (adjustedQuantity < 1)
            {
                Print($"Cannot enter short (Bearish Breakout): Risk per contract {riskCurrencyPerContract:F2} exceeds max risk {maxRiskPerContract:F2}");
                return;
            }
            double totalRiskCurrency = riskCurrencyPerContract * adjustedQuantity;
            double totalRiskPct = (totalRiskCurrency / accountBalance) * 100;
            if (totalRiskPct > MaxRiskPerTradePct * tradeQuantity)
            {
                Print($"Cannot enter short (Bearish Breakout): Total risk {totalRiskPct:F2}% > {MaxRiskPerTradePct * tradeQuantity}%");
                return;
            }
            string uniqueId = Guid.NewGuid().ToString("N");
            EnterShort(adjustedQuantity, $"ShortEntry_{uniqueId}");
            tradeCountToday++;
            currentTradeSetup = "1M Bearish Breakout";
            Print($"Entered short (Bearish Breakout) at {Close[0]}, Stop: {intendedShortStop}, TradeCountToday: {tradeCountToday}, Quantity: {adjustedQuantity}, TotalRisk: {totalRiskCurrency:F2}");
        }

        private void BullishFVG1Min()
        {
            if (fvgLowFillBarNumber > 0)
            {
                if (Position.MarketPosition == MarketPosition.Short) return;
                if (Position.MarketPosition == MarketPosition.Long && openEntries.Count >= 2) return;

                int fillBarsAgo = CurrentBar - fvgLowFillBarNumber;
                if (fillBarsAgo >= 0 && fillBarsAgo < CurrentBar)
                {
                    double touchCandleHigh = High[fillBarsAgo];
                    if (Close[0] > touchCandleHigh && Close[1] <= touchCandleHigh)
                    {
                        string reason;
                        if (!TradingIsAllowed(out reason))
                        {
                            Print($"Cannot enter long (Bullish FVG): {reason}");
                            return;
                        }
                        double entryApprox = Close[0];
                        double riskPoints;
                        if (Position.MarketPosition == MarketPosition.Long && sharedLongStopLoss > 0)
                        {
                            riskPoints = entryApprox - sharedLongStopLoss;
                        }
                        else if (FixedStopLossTicks > 0)
                        {
                            riskPoints = FixedStopLossTicks * TickSize;
                        }
                        else
                        {
                            riskPoints = entryApprox - recentLow;
                        }
                        int tradeQuantity = Math.Min(consecutiveLosses + 1, 7); // Cap at 7 contracts
                        double accountBalance = Account.Get(AccountItem.CashValue, Currency.UsDollar);
                        double maxRiskPerContract = (MaxRiskPerTradePct / 100.0) * accountBalance;
                        double riskCurrencyPerContract = riskPoints * Instrument.MasterInstrument.PointValue;
                        int adjustedQuantity = Math.Min(tradeQuantity, (int)(maxRiskPerContract / riskCurrencyPerContract));
                        if (adjustedQuantity < 1)
                        {
                            Print($"Cannot enter long (Bullish FVG): Risk per contract {riskCurrencyPerContract:F2} exceeds max risk {maxRiskPerContract:F2}");
                            return;
                        }
                        double totalRiskCurrency = riskCurrencyPerContract * adjustedQuantity;
                        double totalRiskPct = (totalRiskCurrency / accountBalance) * 100;
                        if (totalRiskPct > MaxRiskPerTradePct * tradeQuantity)
                        {
                            Print($"Cannot enter long (Bullish FVG): Total risk {totalRiskPct:F2}% > {MaxRiskPerTradePct * tradeQuantity}%");
                            return;
                        }
                        string uniqueId = Guid.NewGuid().ToString("N");
                        EnterLong(adjustedQuantity, $"BullishFVG1Min_{uniqueId}");
                        tradeCountToday++;
                        currentTradeSetup = "1M Bullish FVG";
                        Print($"Entered long (Bullish FVG) at {Close[0]}, TradeCountToday: {tradeCountToday}, Quantity: {adjustedQuantity}, TotalRisk: {totalRiskCurrency:F2}");
                    }
                }
            }
        }

        private void BearishFVG1Min()
        {
            if (fvgHighFillBarNumber > 0)
            {
                if (Position.MarketPosition == MarketPosition.Long) return;
                if (Position.MarketPosition == MarketPosition.Short && openEntries.Count >= 2) return;

                int fillBarsAgo = CurrentBar - fvgHighFillBarNumber;
                if (fillBarsAgo >= 0 && fillBarsAgo < CurrentBar)
                {
                    double touchCandleLow = Low[fillBarsAgo];
                    if (Close[0] < touchCandleLow && Close[1] >= touchCandleLow)
                    {
                        string reason;
                        if (!TradingIsAllowed(out reason))
                        {
                            Print($"Cannot enter short (Bearish FVG): {reason}");
                            return;
                        }
                        double entryApprox = Close[0];
                        double riskPoints;
                        if (Position.MarketPosition == MarketPosition.Short && sharedShortStopLoss > 0)
                        {
                            riskPoints = sharedShortStopLoss - entryApprox;
                        }
                        else if (FixedStopLossTicks > 0)
                        {
                            riskPoints = FixedStopLossTicks * TickSize;
                        }
                        else
                        {
                            riskPoints = recentHigh - entryApprox;
                        }
                        int tradeQuantity = Math.Min(consecutiveLosses + 1, 7); // Cap at 7 contracts
                        double accountBalance = Account.Get(AccountItem.CashValue, Currency.UsDollar);
                        double maxRiskPerContract = (MaxRiskPerTradePct / 100.0) * accountBalance;
                        double riskCurrencyPerContract = riskPoints * Instrument.MasterInstrument.PointValue;
                        int adjustedQuantity = Math.Min(tradeQuantity, (int)(maxRiskPerContract / riskCurrencyPerContract));
                        if (adjustedQuantity < 1)
                        {
                            Print($"Cannot enter short (Bearish FVG): Risk per contract {riskCurrencyPerContract:F2} exceeds max risk {maxRiskPerContract:F2}");
                            return;
                        }
                        double totalRiskCurrency = riskCurrencyPerContract * adjustedQuantity;
                        double totalRiskPct = (totalRiskCurrency / accountBalance) * 100;
                        if (totalRiskPct > MaxRiskPerTradePct * tradeQuantity)
                        {
                            Print($"Cannot enter short (Bearish FVG): Total risk {totalRiskPct:F2}% > {MaxRiskPerTradePct * tradeQuantity}%");
                            return;
                        }
                        string uniqueId = Guid.NewGuid().ToString("N");
                        EnterShort(adjustedQuantity, $"BearishFVG1Min_{uniqueId}");
                        tradeCountToday++;
                        currentTradeSetup = "1M Bearish FVG";
                        Print($"Entered short (Bearish FVG) at {Close[0]}, TradeCountToday: {tradeCountToday}, Quantity: {adjustedQuantity}, TotalRisk: {totalRiskCurrency:F2}");
                    }
                }
            }
        }

        protected override void OnExecutionUpdate(Execution execution, string executionId, double price, int quantity,
            MarketPosition marketPosition, string orderId, DateTime time)
        {
            if (execution.Order?.OrderState != OrderState.Filled) return;

            if (string.IsNullOrEmpty(execution.Order.FromEntrySignal))
            {
                openEntries.Add(execution.Order.Name);
            }
            else
            {
                if (openEntries.Contains(execution.Order.FromEntrySignal))
                {
                    openEntries.Remove(execution.Order.FromEntrySignal);
                }
            }

            if (execution.Order.Name.StartsWith("LongEntry"))
                ProcessLongExecution(execution);
            else if (execution.Order.Name.StartsWith("ShortEntry"))
                ProcessShortExecution(execution);
            else if (execution.Order.Name.StartsWith("BullishFVG1Min"))
                ProcessBullishFVGExecution(execution);
            else if (execution.Order.Name.StartsWith("BearishFVG1Min"))
                ProcessBearishFVGExecution(execution);
        }

        private void ProcessLongExecution(Execution execution)
        {
            double stopPrice;
            double target;
            if (Position.MarketPosition == MarketPosition.Long && sharedLongStopLoss > 0 && sharedLongTakeProfit > 0)
            {
                stopPrice = sharedLongStopLoss;
                target = sharedLongTakeProfit;
            }
            else
            {
                if (FixedStopLossTicks > 0)
                {
                    stopPrice = execution.Price - FixedStopLossTicks * TickSize;
                }
                else
                {
                    stopPrice = intendedLongStop;
                    if (execution.Price <= stopPrice)
                    {
                        ExitLong(1, "", execution.Order.Name);
                        return;
                    }
                }
                double risk = execution.Price - stopPrice;
                if (FixedTakeProfitTicks > 0)
                {
                    target = execution.Price + FixedTakeProfitTicks * TickSize;
                }
                else
                {
                    target = execution.Price + risk;
                }
                sharedLongStopLoss = stopPrice;
                sharedLongTakeProfit = target;
            }
            SetStopLoss(execution.Order.Name, CalculationMode.Price, stopPrice, false);
            SetProfitTarget(execution.Order.Name, CalculationMode.Price, target, false);
            Print($"Long execution at {execution.Price}, Stop: {stopPrice}, Target: {target}");
        }

        private void ProcessShortExecution(Execution execution)
        {
            double stopPrice;
            double target;
            if (Position.MarketPosition == MarketPosition.Short && sharedShortStopLoss > 0 && sharedShortTakeProfit > 0)
            {
                stopPrice = sharedShortStopLoss;
                target = sharedShortTakeProfit;
            }
            else
            {
                if (FixedStopLossTicks > 0)
                {
                    stopPrice = execution.Price + FixedStopLossTicks * TickSize;
                }
                else
                {
                    stopPrice = intendedShortStop;
                    if (execution.Price >= stopPrice)
                    {
                        ExitShort(1, "", execution.Order.Name);
                        return;
                    }
                }
                double risk = stopPrice - execution.Price;
                if (FixedTakeProfitTicks > 0)
                {
                    target = execution.Price - FixedTakeProfitTicks * TickSize;
                }
                else
                {
                    target = execution.Price - risk;
                }
                sharedShortStopLoss = stopPrice;
                sharedShortTakeProfit = target;
            }
            SetStopLoss(execution.Order.Name, CalculationMode.Price, stopPrice, false);
            SetProfitTarget(execution.Order.Name, CalculationMode.Price, target, false);
            Print($"Short execution at {execution.Price}, Stop: {stopPrice}, Target: {target}");
        }

        private void ProcessBullishFVGExecution(Execution execution)
        {
            double stopPrice;
            double target;
            if (Position.MarketPosition == MarketPosition.Long && sharedLongStopLoss > 0 && sharedLongTakeProfit > 0)
            {
                stopPrice = sharedLongStopLoss;
                target = sharedLongTakeProfit;
            }
            else
            {
                if (FixedStopLossTicks > 0)
                {
                    stopPrice = execution.Price - FixedStopLossTicks * TickSize;
                }
                else
                {
                    stopPrice = recentLow;
                }
                double risk = execution.Price - stopPrice;
                if (FixedTakeProfitTicks > 0)
                {
                    target = execution.Price + FixedTakeProfitTicks * TickSize;
                }
                else
                {
                    target = execution.Price + risk;
                }
                sharedLongStopLoss = stopPrice;
                sharedLongTakeProfit = target;
            }
            SetStopLoss(execution.Order.Name, CalculationMode.Price, stopPrice, false);
            SetProfitTarget(execution.Order.Name, CalculationMode.Price, target, false);
            Print($"BullishFVG1Min filled at {execution.Price}, Stop: {stopPrice}, Target: {target}");
        }

        private void ProcessBearishFVGExecution(Execution execution)
        {
            double stopPrice;
            double target;
            if (Position.MarketPosition == MarketPosition.Short && sharedShortStopLoss > 0 && sharedShortTakeProfit > 0)
            {
                stopPrice = sharedShortStopLoss;
                target = sharedShortTakeProfit;
            }
            else
            {
                if (FixedStopLossTicks > 0)
                {
                    stopPrice = execution.Price + FixedStopLossTicks * TickSize;
                }
                else
                {
                    stopPrice = recentHigh;
                }
                double risk = stopPrice - execution.Price;
                if (FixedTakeProfitTicks > 0)
                {
                    target = execution.Price - FixedTakeProfitTicks * TickSize;
                }
                else
                {
                    target = execution.Price - risk;
                }
                sharedShortStopLoss = stopPrice;
                sharedShortTakeProfit = target;
            }
            SetStopLoss(execution.Order.Name, CalculationMode.Price, stopPrice, false);
            SetProfitTarget(execution.Order.Name, CalculationMode.Price, target, false);
            Print($"BearishFVG1Min filled at {execution.Price}, Stop: {stopPrice}, Target: {target}");
        }

        private void DrawTradeSetupLabel()
        {
            RemoveDrawObject(tradeSetupLabel);
            if (!string.IsNullOrEmpty(currentTradeSetup))
            {
                Brush lightBlueBrush = new SolidColorBrush(Color.FromArgb(255, 173, 216, 230));
                Draw.TextFixed(this, tradeSetupLabel, currentTradeSetup, TextPosition.TopRight,
                    lightBlueBrush, new SimpleFont("Arial", 14), Brushes.Transparent, Brushes.Transparent, 100);
            }
        }

        private void DrawLevels1M()
        {
            RemoveDrawObject(highLineTag);
            RemoveDrawObject(lowLineTag);
            RemoveDrawObject(highTextTag);
            RemoveDrawObject(lowTextTag);
            if (swingHighBarNumber > 0)
                DrawSwingHigh1M(CurrentBar - swingHighBarNumber);
            if (swingLowBarNumber > 0)
                DrawSwingLow1M(CurrentBar - swingLowBarNumber);
        }

        private void DrawSwingHigh1M(int barsAgo)
        {
            Draw.Line(this, highLineTag, false, barsAgo, recentHigh, -100, recentHigh,
                     Brushes.Pink, DashStyleHelper.Dot, 1);
        }

        private void DrawSwingLow1M(int barsAgo)
        {
            Draw.Line(this, lowLineTag, false, barsAgo, recentLow, -100, recentLow,
                     Brushes.Pink, DashStyleHelper.Dot, 1);
        }

        private void DrawLevels5M()
        {
            RemoveDrawObject(highLineTag5M);
            RemoveDrawObject(lowLineTag5M);
            if (swingHighBarNumber5M > 0)
            {
                int barsAgoSec = CurrentBars[1] - swingHighBarNumber5M;
                if (barsAgoSec >= 0)
                {
                    DateTime swingTime = Times[1][barsAgoSec];
                    int barIndex = Bars.GetBar(swingTime);
                    if (barIndex >= 0)
                    {
                        int barsAgoPrim = CurrentBar - barIndex;
                        Draw.Line(this, highLineTag5M, false, barsAgoPrim, recentHigh5M, -100, recentHigh5M,
                                 Brushes.DarkGreen, DashStyleHelper.DashDot, 1);
                    }
                }
            }
            if (swingLowBarNumber5M > 0)
            {
                int barsAgoSec = CurrentBars[1] - swingLowBarNumber5M;
                if (barsAgoSec >= 0)
                {   
                    DateTime swingTime = Times[1][barsAgoSec];
                    int barIndex = Bars.GetBar(swingTime);
                    if (barIndex >= 0)
                    {
                        int barsAgoPrim = CurrentBar - barIndex;
                        Draw.Line(this, lowLineTag5M, false, barsAgoPrim, recentLow5M, -100, recentLow5M,
                                 Brushes.DarkRed, DashStyleHelper.DashDot, 1);
                    }
                }
            }
        }

        private void DrawLevels1H()
        {
            RemoveDrawObject(highLineTag1H);
            RemoveDrawObject(lowLineTag1H);
            if (swingHighBarNumber1H > 0)
            {
                int barsAgoSec = CurrentBars[2] - swingHighBarNumber1H;
                if (barsAgoSec >= 0)
                {
                    DateTime swingTime = Times[2][barsAgoSec];
                    int barIndex = Bars.GetBar(swingTime);
                    if (barIndex >= 0)
                    {
                        int barsAgoPrim = CurrentBar - barIndex;
                        Draw.Line(this, highLineTag1H, false, barsAgoPrim, recentHigh1H, -100, recentHigh1H,
                                 Brushes.ForestGreen, DashStyleHelper.Dash, 2);
                    }
                }
            }
            if (swingLowBarNumber1H > 0)
            {
                int barsAgoSec = CurrentBars[2] - swingLowBarNumber1H;
                if (barsAgoSec >= 0)
                {
                    DateTime swingTime = Times[2][barsAgoSec];
                    int barIndex = Bars.GetBar(swingTime);
                    if (barIndex >= 0)
                    {
                        int barsAgoPrim = CurrentBar - barIndex;
                        Draw.Line(this, lowLineTag1H, false, barsAgoPrim, recentLow1H, -100, recentLow1H,
                                 Brushes.DarkRed, DashStyleHelper.Dash, 2);
                    }
                }
            }
        }

        private double GetDailyPnL()
        {
            double pnL = 0;
            DateTime today = Time[0].Date;
            for (int i = 0; i < SystemPerformance.AllTrades.Count; i++)
            {
                Trade trade = SystemPerformance.AllTrades[i];
                if (trade.Exit.Time.Date == today)
                    pnL += trade.ProfitCurrency;
            }
            pnL += Position.GetUnrealizedProfitLoss(PerformanceUnit.Currency);
            return pnL;
        }

        private void DrawTradingStatus()
        {
            string reason;
            bool allowed = TradingIsAllowed(out reason);
            RemoveDrawObject(tradingStatusLabel);
            Brush brush = allowed ? Brushes.Green : Brushes.Red;
            Draw.TextFixed(this, tradingStatusLabel, reason, TextPosition.BottomLeft, brush,
                           new SimpleFont("Arial", 14), Brushes.Transparent, Brushes.Transparent, 100);
        }
    }
}