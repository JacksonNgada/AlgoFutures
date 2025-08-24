using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using NinjaTrader.Cbi;
using NinjaTrader.Data;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.Tools;
using NinjaTrader.NinjaScript;
using NinjaTrader.NinjaScript.DrawingTools;
using System.Windows.Media;
using System.Text;

namespace NinjaTrader.NinjaScript.Strategies
{
    public class AlgoTeamLeadTraderNT8 : Strategy
    {
        #region Parameters
        [NinjaScriptProperty]
        [Display(Name = "MNQ Instrument Name", Description = "Instrument name for MNQ", Order = 1, GroupName = "Instrument Settings")]
        public string MnqInstrumentName { get; set; } = "MNQ";

        [NinjaScriptProperty]
        [Display(Name = "MES Instrument Name", Description = "Instrument name for MES", Order = 2, GroupName = "Instrument Settings")]
        public string MesInstrumentName { get; set; } = "MES";
        #endregion

        #region Fields
        private BarsPeriod[] timeframes = new BarsPeriod[]
        {
            new BarsPeriod { BarsPeriodType = BarsPeriodType.Minute, Value = 1 },
            new BarsPeriod { BarsPeriodType = BarsPeriodType.Minute, Value = 5 },
            new BarsPeriod { BarsPeriodType = BarsPeriodType.Minute, Value = 60 },
            new BarsPeriod { BarsPeriodType = BarsPeriodType.Minute, Value = 240 },
            new BarsPeriod { BarsPeriodType = BarsPeriodType.Day, Value = 1 },
            new BarsPeriod { BarsPeriodType = BarsPeriodType.Week, Value = 1 },
            new BarsPeriod { BarsPeriodType = BarsPeriodType.Month, Value = 1 }
        };
        private string[] timeframeLabels = new[] { "1M", "5M", "1H", "4H", "D", "W", "MN" };

        private int? lastSwingHighIndex1M, lastSwingLowIndex1M;
        private double? lastSwingHighPrice1M, lastSwingLowPrice1M;
        private int? lastSwingHighIndex5M, lastSwingLowIndex5M;
        private double? lastSwingHighPrice5M, lastSwingLowPrice5M;
        private int? lastSwingHighIndex1H, lastSwingLowIndex1H;
        private double? lastSwingHighPrice1H, lastSwingLowPrice1H;

        private bool hasOpenPosition;
        private string currentTradeType;
        private int tradesTaken, tradesWon, tradesLost;
        private DateTime? lastTradeDate;
        private int dailyTradeCount, dailyWins, dailyLosses;

        private double? lastFVGHigh1M, lastFVGLow1M;
        private int? lastFVGIndex1M;
        private bool hasTouchedBullishProxyZone, hasTouchedBearishProxyZone;

        private bool hasTouched1HBullishSwing, hasTouched1HBearishSwing;
        private bool prioritize1HSetup;
        private DateTime? last1HBullishTouchTime, last1HBearishTouchTime;

        private bool isBullishBosConfirmed, isBearishBosConfirmed;

        private double? lastIFVGHigh5M, lastIFVGLow5M;
        private int? lastIFVGIndex5M;
        private double? lastBreakerHigh5M, lastBreakerLow5M;
        private int? lastBreakerIndex5M;
        private double? lastFVGHigh5M, lastFVGLow5M;
        private int? lastFVGIndex5M;
        private int? lastBullishBreakoutSwingHighIndex, lastBearishBreakoutSwingLowIndex;

        private DateTime? last1HPrioritizationTime;
        private const double PrioritizationTimeoutHours = 1;
        private int? lastSmtSwingIndex5M;

        private int? lastMnqSwingHighIndex5M, lastMnqSwingLowIndex5M;
        private double? lastMnqSwingHighPrice5M, lastMnqSwingLowPrice5M;
        private int? lastMesSwingHighIndex5M, lastMesSwingLowIndex5M;
        private double? lastMesSwingHighPrice5M, lastMesSwingLowPrice5M;

        private double? equilibriumPrice5M;
        private bool hasScaledIn;
        private string entryConfluenceType;

        private bool pendingBullishLiquidityGrab5M, pendingBearishLiquidityGrab5M;
        private double? pendingBullishClose5M, pendingBullishAsk5M;
        private double? pendingBearishClose5M, pendingBearishBid5M;
        private bool? pendingBullishSmtConfirmed5M, pendingBearishSmtConfirmed5M;
        private string pendingBullishSmtType5M, pendingBearishSmtType5M;
        private bool pendingBullishBreakout5M, pendingBearishBreakout5M;
        private double? pendingBullishClose5MBreakout, pendingBullishAsk5MBreakout;
        private double? pendingBearishClose5MBreakout, pendingBearishBid5MBreakout;
        private bool? pendingBullishSmtConfirmed5MBreakout, pendingBearishSmtConfirmed5MBreakout;
        private string pendingBullishSmtType5MBreakout, pendingBearishSmtType5MBreakout;
        private DateTime? lastSmtTime5M;

        private bool isBullishFVG, isBullishIFVG;
        private bool hasConfluenceTrade;
        private DateTime? lastBullishTouchCandleTime, lastBearishTouchCandleTime;
        private int lastLiquidityGrabSwingIndex5M = -1;
        private int lastBreakoutSwingIndex5M = -1;
        private int lastFVGSwingIndex5M = -1;
        private string currentConfluenceType;
        private bool waitingForBullishBos1M, waitingForBearishBos1M;
        private DateTime? lastSwingUpdateTime;
        #endregion

        #region Lifecycle Methods
        protected override void OnStateChange()
        {
//#if DEBUG
//            System.Diagnostics.Debugger.Launch();
//#endif
            if (State == State.SetDefaults)
            {
                Description = @"AlgoTeamLeadTrader strategy for NinjaTrader 8";
                Name = "AlgoTeamLeadTraderNT8";
                Calculate = Calculate.OnBarClose;
                EntriesPerDirection = 1;
                EntryHandling = EntryHandling.AllEntries;
                IsExitOnSessionCloseStrategy = true;
                ExitOnSessionCloseSeconds = 30;
                BarsRequiredToTrade = 20;
                TraceOrders = true;
            }
            else if (State == State.Configure)
            {
                for (int i = 1; i < timeframes.Length; i++)
                    AddDataSeries(Instrument.FullName, timeframes[i].BarsPeriodType, timeframes[i].Value);

                AddDataSeries(MnqInstrumentName, BarsPeriodType.Minute, 5);
                AddDataSeries(MesInstrumentName, BarsPeriodType.Minute, 5);
            }
            else if (State == State.DataLoaded)
            {
                tradesTaken = 0;
                tradesWon = 0;
                tradesLost = 0;
                dailyTradeCount = 0;
                dailyWins = 0;
                dailyLosses = 0;
                lastTradeDate = null;
                hasTouched1HBullishSwing = false;
                hasTouched1HBearishSwing = false;
                prioritize1HSetup = false;
                last1HBullishTouchTime = null;
                last1HBearishTouchTime = null;
                lastSmtSwingIndex5M = null;
                hasScaledIn = false;
                entryConfluenceType = null;
                isBullishBosConfirmed = false;
                isBearishBosConfirmed = false;
                hasConfluenceTrade = false;
                hasOpenPosition = false;

                DrawLatestSwings();
                DrawFVG5M(true);
                UpdateHigherTimeframeSwings();
                DrawMarketDirectionLabels();
            }
            else if (State == State.Terminated)
            {
                OnStop();
            }
        }

        protected void OnStart()
        {
            Print("AlgoTeamLeadTraderNT8 started");
        }

        protected void OnStop()
        {
            Print("AlgoTeamLeadTraderNT8 stopped");
            ClearSmtVisuals();
        }

        protected override void OnBarUpdate()
        {
            if (CurrentBars[0] < BarsRequiredToTrade) return;

            if (BarsInProgress == 0) // 1M
            {
                DrawSwingHigh1M();
                DrawSwingLow1M();
            }
            else if (BarsInProgress == 1) // 5M
            {
                UpdateHigherTimeframeSwings();
                DetectFVG5M();
                DetectBreakOfStructure5M();
                DrawFVG5M(true);
                DrawLatestSwings();
                DrawMarketDirectionLabels();
            }
            else if (BarsInProgress == 2) // 1H
            {
                Update1HSwingTouches();
                DrawSwingHigh1H();
                DrawSwingLow1H();
            }
        }

        protected void OnBar()
        {
            if (CurrentBars[0] < BarsRequiredToTrade) return;

            if (BarsInProgress == 1)
            {
                UpdateHigherTimeframeSwings();
                DetectFVG5M();
                DetectBreakOfStructure5M();
                DrawFVG5M(true);
                DrawLatestSwings();
                DrawMarketDirectionLabels();
                UpdateEquilibrium5M();
                DrawEquilibrium5M();
            }
            else if (BarsInProgress == 2)
            {
                Update1HSwingTouches();
            }
            else if (BarsInProgress == 7)
            {
                UpdateMnqSwings();
            }
            else if (BarsInProgress == 8)
            {
                UpdateMesSwings();
            }

            if (BarsInProgress != 0) return;
            if (!IsTradingAllowed()) return;

            double currentAsk = GetCurrentAsk();
            double currentBid = GetCurrentBid();
            double currentClose = Closes[1][0];

            var direction1H = GetMarketDirection(2);
            var direction4H = GetMarketDirection(3);
            var direction5M = GetMarketDirection(1);

            bool is1HBullish = direction1H.Contains("Bullish");
            bool is4HBullish = direction4H.Contains("Bullish");
            bool is5MBullish = direction5M.Contains("Bullish");
            bool isAlignedBullish = is1HBullish && is4HBullish;
            bool isAlignedBearish = !is1HBullish && !is4HBullish;

            string smtType;
            double mnqPrice, mesPrice;
            bool smtDivergenceConfirmed = IsSmtDivergenceConfirmed(out smtType, out mnqPrice, out mesPrice);
            BullishFVG5Min(currentClose, currentAsk, smtDivergenceConfirmed, smtType);
            BearishFVG5Min(currentClose, currentBid, smtDivergenceConfirmed, smtType);
            BullishLiquidityGrab5Min(currentClose, currentAsk, smtDivergenceConfirmed, smtType);
            BearishLiquidityGrab5Min(currentClose, currentBid, smtDivergenceConfirmed, smtType);
            BullishBreakout5Min(currentAsk, currentClose, smtDivergenceConfirmed, smtType);
            BearishBreakout5Min(currentBid, currentClose, smtDivergenceConfirmed, smtType);
            if (hasTouched1HBullishSwing)
                BullishLiquidityGrab1H(currentClose, currentAsk, smtDivergenceConfirmed, smtType);
            if (hasTouched1HBearishSwing)
                BearishLiquidityGrab1H(currentClose, currentBid, smtDivergenceConfirmed, smtType);
        }

        protected override void OnMarketData(MarketDataEventArgs marketDataUpdate)
        {
            OnTick();
        }

        protected void OnTick()
        {
            if (Position.MarketPosition != MarketPosition.Flat)
            {
                UpdateAllPositionsTakeProfit();
                UpdateTrailingStopLoss();
            }
        }

        protected override void OnExecutionUpdate(Execution execution, string executionId, double price, int quantity, MarketPosition marketPosition, string orderId, DateTime time)
        {
            if (execution.Order == null || execution.Order.OrderState != OrderState.Filled) return;

            if (execution.Order.Name.Contains("Entry"))
            {
                Print($"Position opened: {execution.Order.Name} at {price}");
                hasOpenPosition = true;
                SetPartialTakeProfit(Position);
            }
            else if (execution.Order.Name.Contains("TP") || execution.Order.Name.Contains("SL"))
            {
                Print($"Position closed: {execution.Order.Name} at {price}");
                OnPositionClosed(Position);
            }
        }
        #endregion

        #region Core Methods
        private void UpdateHigherTimeframeSwings()
        {
            Print($"5M Swing Update Check: HighIndex={lastSwingHighIndex5M}, HighPrice={lastSwingHighPrice5M:F2}, LowIndex={lastSwingLowIndex5M}, LowPrice={lastSwingLowPrice5M:F2}");
            
            // 1M Swing High Detection
            for (int i = CurrentBars[0] - 4; i >= 1; i--)
            {
                if (i + 2 >= Highs[0].Count || i < 1) continue;

                bool green = Closes[0][i] > Opens[0][i];
                bool red1 = Closes[0][i + 1] < Opens[0][i + 1];
                if (!green || !red1) continue;

                double candidateHigh = Math.Max(Highs[0][i], Highs[0][i + 1]);
                bool thirdCandleRed = Closes[0][i + 2] < Opens[0][i + 2];
                bool thirdCandleGreen = Closes[0][i + 2] > Opens[0][i + 2];
                double thirdHigh = Highs[0][i + 2];

                bool isConfirmed = thirdHigh > candidateHigh ? thirdCandleRed : (thirdCandleRed || (thirdCandleGreen && thirdHigh < candidateHigh));

                if (isConfirmed && (!lastSwingHighIndex1M.HasValue || i > lastSwingHighIndex1M.Value))
                {
                    lastSwingHighIndex1M = i;
                    lastSwingHighPrice1M = candidateHigh;
                    Print($"Updated 1M Swing High: Index={i}, Price={candidateHigh:F2}, Time={Times[0][i]}");
                    break;
                }
            }

            // 1M Swing Low Detection
            for (int i = CurrentBars[0] - 4; i >= 1; i--)
            {
                if (i + 2 >= Lows[0].Count || i < 1) continue;

                bool red = Closes[0][i] < Opens[0][i];
                bool green1 = Closes[0][i + 1] > Opens[0][i + 1];
                if (!red || !green1) continue;

                double candidateLow = Math.Min(Lows[0][i], Lows[0][i + 1]);
                bool thirdCandleGreen = Closes[0][i + 2] > Opens[0][i + 2];
                bool thirdCandleRed = Closes[0][i + 2] < Opens[0][i + 2];
                double thirdLow = Lows[0][i + 2];

                bool isConfirmed = thirdLow < candidateLow ? thirdCandleGreen : (thirdCandleGreen || (thirdCandleRed && thirdLow > candidateLow));

                if (isConfirmed && (!lastSwingLowIndex1M.HasValue || i > lastSwingLowIndex1M.Value))
                {
                    lastSwingLowIndex1M = i;
                    lastSwingLowPrice1M = candidateLow;
                    Print($"Updated 1M Swing Low: Index={i}, Price={candidateLow:F2}, Time={Times[0][i]}");
                    break;
                }
            }

            // 5M Swing High Detection
            int last5M = CurrentBars[1] - 2;
            int lookback5M = Math.Min(last5M, CurrentBars[1] - 1);

            for (int i = last5M - 1; i >= Math.Max(1, last5M - lookback5M); i--)
            {
                if (i + 2 >= Highs[1].Count || i < 1) continue;

                bool green = Closes[1][i] > Opens[1][i];
                bool red1 = Closes[1][i + 1] < Opens[1][i + 1];
                if (green && red1)
                {
                    double candidateHigh = Math.Max(Highs[1][i], Highs[1][i + 1]);
                    bool thirdCandleRed = Closes[1][i + 2] < Opens[1][i + 2];
                    bool thirdCandleGreen = Closes[1][i + 2] > Opens[1][i + 2];
                    double thirdHigh = Highs[1][i + 2];

                    bool isConfirmed = false;
                    if (thirdHigh > candidateHigh)
                    {
                        if (thirdCandleRed)
                            isConfirmed = true;
                    }
                    else
                    {
                        if (thirdCandleRed || (thirdCandleGreen && thirdHigh < candidateHigh))
                            isConfirmed = true;
                    }

                    if (isConfirmed)
                    {
                        if (!lastSwingHighIndex5M.HasValue || i > lastSwingHighIndex5M.Value)
                        {
                            lastSwingHighIndex5M = i;
                            lastSwingHighPrice5M = candidateHigh;
                            Print($"Updated 5M Swing High: Index={i}, Price={candidateHigh:F2}, Time={Times[1][i]}, Confirmed");
                            DrawSwingHigh5M();
                        }
                        break;
                    }
                }
            }

            // 5M Swing Low Detection
            for (int i = last5M - 1; i >= Math.Max(1, last5M - lookback5M); i--)
            {
                if (i + 2 >= Lows[1].Count || i < 1) continue;

                bool red = Closes[1][i] < Opens[1][i];
                bool green1 = Closes[1][i + 1] > Opens[1][i + 1];
                if (red && green1)
                {
                    double candidateLow = Math.Min(Lows[1][i], Lows[1][i + 1]);
                    bool thirdCandleGreen = Closes[1][i + 2] > Opens[1][i + 2];
                    bool thirdCandleRed = Closes[1][i + 2] < Opens[1][i + 2];
                    double thirdLow = Lows[1][i + 2];

                    bool isConfirmed = false;
                    if (thirdLow < candidateLow)
                    {
                        if (thirdCandleGreen)
                            isConfirmed = true;
                    }
                    else
                    {
                        if (thirdCandleGreen || (thirdCandleRed && thirdLow > candidateLow))
                            isConfirmed = true;
                    }

                    if (isConfirmed)
                    {
                        if (!lastSwingLowIndex5M.HasValue || i > lastSwingLowIndex5M.Value)
                        {
                            lastSwingLowIndex5M = i;
                            lastSwingLowPrice5M = candidateLow;
                            Print($"Updated 5M Swing Low: Index={i}, Price={candidateLow:F2}, Time={Times[1][i]}, Confirmed");
                            DrawSwingLow5M();
                        }
                        break;
                    }
                }
            }

            // 1H Swing High Detection
            for (int i = CurrentBars[2] - 4; i >= 1; i--)
            {
                if (i + 2 >= Highs[2].Count || i < 1) continue;

                bool green = Closes[2][i] > Opens[2][i];
                bool red1 = Closes[2][i + 1] < Opens[2][i + 1];
                if (!green || !red1) continue;

                double candidateHigh = Math.Max(Highs[2][i], Highs[2][i + 1]);
                bool thirdCandleRed = Closes[2][i + 2] < Opens[2][i + 2];
                bool thirdCandleGreen = Closes[2][i + 2] > Opens[2][i + 2];
                double thirdHigh = Highs[2][i + 2];

                bool isConfirmed = thirdHigh > candidateHigh ? thirdCandleRed : (thirdCandleRed || (thirdCandleGreen && thirdHigh < candidateHigh));

                if (isConfirmed && (!lastSwingHighIndex1H.HasValue || i > lastSwingHighIndex1H.Value))
                {
                    lastSwingHighIndex1H = i;
                    lastSwingHighPrice1H = candidateHigh;
                    Print($"Updated 1H Swing High: Index={i}, Price={candidateHigh:F2}, Time={Times[2][i]}");
                    break;
                }
            }

            // 1H Swing Low Detection
            for (int i = CurrentBars[2] - 4; i >= 1; i--)
            {
                if (i + 2 >= Lows[2].Count || i < 1) continue;

                bool red = Closes[2][i] < Opens[2][i];
                bool green1 = Closes[2][i + 1] > Opens[2][i + 1];
                if (!red || !green1) continue;

                double candidateLow = Math.Min(Lows[2][i], Lows[2][i + 1]);
                bool thirdCandleGreen = Closes[2][i + 2] > Opens[2][i + 2];
                bool thirdCandleRed = Closes[2][i + 2] < Opens[2][i + 2];
                double thirdLow = Lows[2][i + 2];

                bool isConfirmed = thirdLow < candidateLow ? thirdCandleGreen : (thirdCandleGreen || (thirdCandleRed && thirdLow > candidateLow));

                if (isConfirmed && (!lastSwingLowIndex1H.HasValue || i > lastSwingLowIndex1H.Value))
                {
                    lastSwingLowIndex1H = i;
                    lastSwingLowPrice1H = candidateLow;
                    Print($"Updated 1H Swing Low: Index={i}, Price={candidateLow:F2}, Time={Times[2][i]}");
                    break;
                }
            }

            Print($"Swings Updated: 1M High={lastSwingHighPrice1M:F2}, Low={lastSwingLowPrice1M:F2}, 5M High={lastSwingHighPrice5M:F2}, Low={lastSwingLowPrice5M:F2}, 1H High={lastSwingHighPrice1H:F2}, Low={lastSwingLowPrice1H:F2}, Time={Time[0]}");
        }

        private bool IsTradingAllowed()
        {
            DateTime sastTime = Time[0].AddHours(2);
            if (lastTradeDate == null || sastTime.Date != lastTradeDate.Value.Date)
            {
                dailyTradeCount = 0;
                dailyWins = 0;
                dailyLosses = 0;
                lastTradeDate = sastTime.Date;
            }

            bool isTradingSession = sastTime.TimeOfDay >= new TimeSpan(15, 55, 0) &&
                                   sastTime.TimeOfDay <= new TimeSpan(18, 0, 0);
            bool withinWinLimit = dailyWins < 1;
            bool withinLossLimit = dailyLosses < 2;

            return isTradingSession && withinWinLimit && withinLossLimit;
        }

        private void Update1HSwingTouches()
        {
            if (CurrentBars[2] < 1) return;

            double distanceThreshold = 100 * TickSize;
            double candleLow1H = Lows[2][0];
            double candleHigh1H = Highs[2][0];

            if (lastSwingLowPrice1H.HasValue)
            {
                bool isTouching = candleLow1H <= lastSwingLowPrice1H.Value;

                if (isTouching && (!hasTouched1HBullishSwing || Time[0] > lastBullishTouchCandleTime.GetValueOrDefault()))
                {
                    hasTouched1HBullishSwing = true;
                    last1HBullishTouchTime = Time[0];
                    lastBullishTouchCandleTime = Time[0];
                    Draw.ArrowUp(this, "1HBullishSwingTouch", true, 0, candleLow1H - (10 * TickSize), Brushes.LimeGreen);
                }
                else if (!isTouching && hasTouched1HBullishSwing && candleLow1H > lastSwingLowPrice1H.Value + distanceThreshold)
                {
                    // Touch invalidated
                }
            }

            if (lastSwingHighPrice1H.HasValue)
            {
                bool isTouching = candleHigh1H >= lastSwingHighPrice1H.Value;

                if (isTouching && (!hasTouched1HBearishSwing || Time[0] > lastBearishTouchCandleTime.GetValueOrDefault()))
                {
                    hasTouched1HBearishSwing = true;
                    last1HBearishTouchTime = Time[0];
                    lastBearishTouchCandleTime = Time[0];
                    Draw.ArrowDown(this, "1HBearishSwingTouch", true, 0, candleHigh1H + (10 * TickSize), Brushes.Red);
                }
                else if (!isTouching && hasTouched1HBearishSwing && candleHigh1H < lastSwingHighPrice1H.Value - distanceThreshold)
                {
                    // Touch invalidated
                }
            }
        }
        #endregion

        #region FVG and Order Block Methods
        private void IdentifyFVG()
        {
            DetectFVG5M();
        }

        private void DetectFVG5M()
        {
            if (CurrentBars[1] < 3) return;

            for (int i = CurrentBars[1] - 3; i >= CurrentBars[1] - 10; i--)
            {
                double highTwoBarsAgo = Highs[1][i];
                double lowOneBarAgo = Lows[1][i + 1];
                double highOneBarAgo = Highs[1][i + 1];
                double lowTwoBarsAgo = Lows[1][i];

                if (lowOneBarAgo > highTwoBarsAgo)
                {
                    lastFVGLow5M = highTwoBarsAgo;
                    lastFVGHigh5M = lowOneBarAgo;
                    lastFVGIndex5M = i + 1;
                    isBullishFVG = true;
                    break;
                }
                else if (highOneBarAgo < lowTwoBarsAgo)
                {
                    lastFVGLow5M = highOneBarAgo;
                    lastFVGHigh5M = lowTwoBarsAgo;
                    lastFVGIndex5M = i + 1;
                    isBullishFVG = false;
                    break;
                }
            }
        }

        private void DrawFVG5M(bool drawForTrade = false)
        {
            if (CurrentBars[1] < 3) return;

            int last5M = CurrentBars[1] - 1;
            int? fvgIndex = null;
            double? fvgHigh = null, fvgLow = null;
            double minGapSize = TickSize;

            for (int i = last5M; i >= Math.Max(1, last5M - 50); i--)
            {
                if (i < 2 || i >= Closes[1].Count - 1) continue;
                double candle1High = Highs[1][i - 2];
                double candle1Low = Lows[1][i - 2];
                double candle3High = Highs[1][i];
                double candle3Low = Lows[1][i];

                if (candle3Low > candle1High && (candle3Low - candle1High) >= minGapSize)
                {
                    fvgIndex = i;
                    fvgHigh = candle3Low;
                    fvgLow = candle1High;
                    isBullishFVG = true;
                    break;
                }
                else if (candle3High < candle1Low && (candle1Low - candle3High) >= minGapSize)
                {
                    fvgIndex = i;
                    fvgHigh = candle1Low;
                    fvgLow = candle3High;
                    isBullishFVG = false;
                    break;
                }
            }

            if (fvgIndex.HasValue && fvgHigh.HasValue && fvgLow.HasValue)
            {
                bool isFilled = false;
                for (int i = fvgIndex.Value; i < Closes[1].Count - 1; i++)
                {
                    if (isBullishFVG && Lows[1][i] <= fvgLow.Value ||
                        !isBullishFVG && Highs[1][i] >= fvgHigh.Value)
                    {
                        isFilled = true;
                        break;
                    }
                }

                if (!isFilled && (!lastFVGIndex5M.HasValue || fvgIndex > lastFVGIndex5M))
                {
                    lastFVGHigh5M = fvgHigh;
                    lastFVGLow5M = fvgLow;
                    lastFVGIndex5M = fvgIndex;

                    Brush color = isBullishFVG ? Brushes.Green : Brushes.Red;
                    Draw.Rectangle(this, "FVG5M_" + fvgIndex, true, fvgIndex.Value - 2, fvgHigh.Value, 0, fvgLow.Value, color, color, 50);
                    Draw.Text(this, "FVGLabel5M_" + fvgIndex, isBullishFVG ? "5M Bullish FVG" : "5M Bearish FVG",
                              fvgIndex.Value - 2, fvgHigh.Value + (5 * TickSize), color);
                }
            }
        }

        private void DrawIFVG5M(bool drawForTrade = false)
        {
            if (CurrentBars[1] < 3) return;

            int last5M = CurrentBars[1] - 1;
            int? ifvgIndex = null;
            double? ifvgHigh = null, ifvgLow = null;
            double minGapSize = TickSize;

            for (int i = last5M; i >= Math.Max(1, last5M - 50); i--)
            {
                if (i < 2 || i >= Closes[1].Count - 1) continue;
                double candle1High = Highs[1][i - 2];
                double candle1Low = Lows[1][i - 2];
                double candle3High = Highs[1][i];
                double candle3Low = Lows[1][i];

                if (candle3Low > candle1High && (candle3Low - candle1High) >= minGapSize)
                {
                    ifvgIndex = i;
                    ifvgHigh = candle3Low;
                    ifvgLow = candle1High;
                    isBullishIFVG = true;
                    break;
                }
                else if (candle3High < candle1Low && (candle1Low - candle3High) >= minGapSize)
                {
                    ifvgIndex = i;
                    ifvgHigh = candle1Low;
                    ifvgLow = candle3High;
                    isBullishIFVG = false;
                    break;
                }
            }

            if (ifvgIndex.HasValue && ifvgHigh.HasValue && ifvgLow.HasValue)
            {
                bool isFilled = false;
                for (int i = ifvgIndex.Value; i < Closes[1].Count - 1; i++)
                {
                    if (isBullishIFVG && Lows[1][i] <= ifvgLow.Value ||
                        !isBullishIFVG && Highs[1][i] >= ifvgHigh.Value)
                    {
                        isFilled = true;
                        break;
                    }
                }

                if (!isFilled && (!lastIFVGIndex5M.HasValue || ifvgIndex > lastIFVGIndex5M))
                {
                    lastIFVGHigh5M = ifvgHigh;
                    lastIFVGLow5M = ifvgLow;
                    lastIFVGIndex5M = ifvgIndex;

                    Brush color = isBullishIFVG ? Brushes.LimeGreen : Brushes.OrangeRed;
                    Draw.Rectangle(this, "IFVG5M_" + ifvgIndex, true, ifvgIndex.Value - 2, ifvgHigh.Value, 0, ifvgLow.Value, color, color, 30);
                    Draw.Text(this, "IFVGLabel5M_" + ifvgIndex, isBullishIFVG ? "5M Bullish IFVG" : "5M Bearish IFVG",
                              ifvgIndex.Value - 2, ifvgHigh.Value + (5 * TickSize), color);
                }
            }
        }

        private void DrawOrderBlock5M(bool drawForTrade = false)
        {
            if (CurrentBars[1] < 3) return;

            double? zoneHigh, zoneLow;
            int lastIndex = CurrentBars[1] - 1;
            bool isBullish = GetMarketDirection(1).Contains("Bullish");

            if (GetOrderBlockZone5M(isBullish, lastIndex, out zoneHigh, out zoneLow))
            {
                Brush color = isBullish ? Brushes.Blue : Brushes.Purple;
                Draw.Rectangle(this, "OrderBlock5M_" + lastIndex, true, lastIndex - 2, zoneHigh.Value, 0, zoneLow.Value, color, color, 20);
                Draw.Text(this, "OrderBlockLabel5M_" + lastIndex, isBullish ? "5M Bullish OB" : "5M Bearish OB",
                          lastIndex - 2, zoneHigh.Value + (5 * TickSize), color);
            }
        }

        private void DrawBreakerBlock5M(bool drawForTrade = false)
        {
            if (CurrentBars[1] < 3) return;

            int last5M = CurrentBars[1] - 1;
            int? breakerIndex = null;
            double? breakerHigh = null, breakerLow = null;

            for (int i = last5M; i >= Math.Max(1, last5M - 50); i--)
            {
                if (i < 2 || i >= Closes[1].Count - 1) continue;
                if (isBullishBosConfirmed && lastSwingHighPrice5M.HasValue && Closes[1][i] > lastSwingHighPrice5M.Value)
                {
                    breakerIndex = i;
                    breakerHigh = Highs[1][i - 1];
                    breakerLow = Lows[1][i - 1];
                    break;
                }
                else if (isBearishBosConfirmed && lastSwingLowPrice5M.HasValue && Closes[1][i] < lastSwingLowPrice5M.Value)
                {
                    breakerIndex = i;
                    breakerHigh = Highs[1][i - 1];
                    breakerLow = Lows[1][i - 1];
                    break;
                }
            }

            if (breakerIndex.HasValue && breakerHigh.HasValue && breakerLow.HasValue)
            {
                lastBreakerHigh5M = breakerHigh;
                lastBreakerLow5M = breakerLow;
                lastBreakerIndex5M = breakerIndex;

                Brush color = isBullishBosConfirmed ? Brushes.Cyan : Brushes.Magenta;
                Draw.Rectangle(this, "Breaker5M_" + breakerIndex, true, breakerIndex.Value - 2, breakerHigh.Value, 0, breakerLow.Value, color, color, 40);
                Draw.Text(this, "BreakerLabel5M_" + breakerIndex, isBullishBosConfirmed ? "5M Bullish Breaker" : "5M Bearish Breaker",
                          breakerIndex.Value - 2, breakerHigh.Value + (5 * TickSize), color);
            }
        }
        #endregion

        #region BOS Methods
        private void DetectBreakOfStructure5Mold()
        {
            if (CurrentBars[1] < 3) return;

            int lastClosedBarIndex = CurrentBars[1] - 1;
            double closePrice = Closes[1][lastClosedBarIndex];

            if (lastSwingHighPrice5M.HasValue && closePrice > lastSwingHighPrice5M.Value)
            {
                isBullishBosConfirmed = true;
                isBearishBosConfirmed = false;
            }
            else if (lastSwingLowPrice5M.HasValue && closePrice < lastSwingLowPrice5M.Value)
            {
                isBearishBosConfirmed = true;
                isBullishBosConfirmed = false;
            }
        }

        private void DetectBreakOfStructure5M()
        {
            if (CurrentBars[1] < 3) return;

            int lastClosedBarIndex = CurrentBars[1] - 1;
            double closePrice = Closes[1][lastClosedBarIndex];

            if (lastSwingHighPrice5M.HasValue && closePrice > lastSwingHighPrice5M.Value)
            {
                isBullishBosConfirmed = true;
                isBearishBosConfirmed = false;
                lastBullishBreakoutSwingHighIndex = lastClosedBarIndex;
            }
            else if (lastSwingLowPrice5M.HasValue && closePrice < lastSwingLowPrice5M.Value)
            {
                isBearishBosConfirmed = true;
                isBullishBosConfirmed = false;
                lastBearishBreakoutSwingLowIndex = lastClosedBarIndex;
            }
        }
        #endregion

        #region Equilibrium Methods
        private void UpdateEquilibrium5M()
        {
            if (CurrentBars[1] < 20) return;

            int lookback = Math.Min(20, CurrentBars[1]);
            double high = MAX(Highs[1], lookback)[0];
            double low = MIN(Lows[1], lookback)[0];
            equilibriumPrice5M = (high + low) / 2;
        }

        private void DrawEquilibrium5M()
        {
            if (!equilibriumPrice5M.HasValue) return;

            //Draw.HorizontalLine(this, "Equilibrium5M", equilibriumPrice5M.Value, Brushes.Yellow);
            //Draw.Text(this, "EquilibriumLabel5M", "5M EQ", 0, equilibriumPrice5M.Value + (5 * TickSize), Brushes.Yellow);
        }
        #endregion

        #region Trade Management
        private void CheckPartialTakeProfit()
		{
		    foreach (Position position in Positions)
		    {
		        if (position.Instrument.FullName != Instrument.FullName || position.MarketPosition == MarketPosition.Flat) continue;
		
		        double currentPrice = position.MarketPosition == MarketPosition.Long ? GetCurrentBid() : GetCurrentAsk();
		        double entryPrice = position.AveragePrice;
		        double takeProfitPrice = position.MarketPosition == MarketPosition.Long ?
		            entryPrice + (40 * TickSize) : // 10 pips = 40 ticks
		            entryPrice - (40 * TickSize);
		
		        if ((position.MarketPosition == MarketPosition.Long && currentPrice >= takeProfitPrice) ||
		            (position.MarketPosition == MarketPosition.Short && currentPrice <= takeProfitPrice))
		        {
		            int sharesToClose = (int)(position.Quantity * 0.8); // 80%
		            if (sharesToClose > 0)
		            {
		                if (position.MarketPosition == MarketPosition.Long)
		                    ExitLong(sharesToClose.ToString(), "PartialTP_" + CurrentBar);
		                else
		                    ExitShort(sharesToClose.ToString(), "PartialTP_" + CurrentBar);
		                Print($"Partial TP Hit: Position={position.Account.Name}, Closed 80% at {takeProfitPrice:F2}, Shares={sharesToClose}, Time={Time[0]}");
		            }
		        }
		    }
		}

        private void UpdateTakeProfitTo1HSwing(Position position)
		{
		    if (position.MarketPosition == MarketPosition.Flat) return;
		
		    double takeProfitPrice = position.MarketPosition == MarketPosition.Long ?
		        lastSwingHighPrice1H.HasValue ? lastSwingHighPrice1H.Value : position.AveragePrice + (40 * TickSize) : // 10 pips
		        lastSwingLowPrice1H.HasValue ? lastSwingLowPrice1H.Value : position.AveragePrice - (40 * TickSize);
		
		    if (position.MarketPosition == MarketPosition.Long)
		        ExitLongLimit(0, takeProfitPrice.ToString(), "TP1H_" + CurrentBar);
		    else
		        ExitShortLimit(0, takeProfitPrice.ToString(), "TP1H_" + CurrentBar);
		}
		
		private void UpdateAllPositionsTakeProfit()
        {
            if (Position.MarketPosition != MarketPosition.Flat)
            {
                UpdateTakeProfitTo1HSwing(Position);
                CheckPartialTakeProfit();
            }
        }

        private void SetTrailingStopLoss(Position position)
		{
		    if (position.MarketPosition == MarketPosition.Flat) return;
		
		    double trailingStopPrice = position.MarketPosition == MarketPosition.Long ?
		        Math.Max(position.AveragePrice - (20 * TickSize), MIN(Lows[1], 5)[0]) : // 5 pips = 20 ticks
		        Math.Min(position.AveragePrice + (20 * TickSize), MAX(Highs[1], 5)[0]);
		
		    if (position.MarketPosition == MarketPosition.Long)
		        ExitLongStopMarket(0, trailingStopPrice.ToString(), "TrailingSL_" + CurrentBar);
		    else
		        ExitShortStopMarket(0, trailingStopPrice.ToString(), "TrailingSL_" + CurrentBar);
		}
		private void UpdateTrailingStopLoss()
        {
            if (Position.MarketPosition != MarketPosition.Flat)
                SetTrailingStopLoss(Position);
        }

        private void SetPartialTakeProfit(Position position)
		{
		    if (position.MarketPosition == MarketPosition.Flat) return;
		
		    double takeProfitPrice = position.MarketPosition == MarketPosition.Long ?
		        position.AveragePrice + (40 * TickSize) : // 10 pips
		        position.AveragePrice - (40 * TickSize);
		
		    int sharesToClose = (int)(position.Quantity * 0.8); // 80%
		    if (sharesToClose > 0)
		    {
		        if (position.MarketPosition == MarketPosition.Long)
		            ExitLongLimit(sharesToClose, takeProfitPrice.ToString(), "PartialTP_" + CurrentBar);
		        else
		            ExitShortLimit(sharesToClose, takeProfitPrice.ToString(), "PartialTP_" + CurrentBar);
		        Print($"Set Partial TP: Position={position.Account.Name}, Partial TP={takeProfitPrice:F2}, Shares={sharesToClose}, Time={Time[0]}");
		    }
		}
        private void OnPositionClosed(Position position)
        {
            hasOpenPosition = false;
            if (position.GetUnrealizedProfitLoss(PerformanceUnit.Currency, Closes[0][0]) > 0)
            {
                tradesWon++;
                dailyWins++;
            }
            else
            {
                tradesLost++;
                dailyLosses++;
            }

            if (Position.MarketPosition == MarketPosition.Flat)
            {
                hasConfluenceTrade = false;
                currentTradeType = null;
                currentConfluenceType = null;
                entryConfluenceType = null;
                hasScaledIn = false;
            }
        }
        #endregion

        #region Trade Execution Methods
        private void ExecuteScaleIn(string tradeType, bool isSmtConfirmed, string confluenceType, double currentClose, double zoneLow, double zoneHigh)
        {
            if (hasScaledIn || !hasOpenPosition) return;

            double riskPercentage = 0.005;
            double entryPrice = Position.MarketPosition == MarketPosition.Long ? GetCurrentAsk() : GetCurrentBid();
            double stopLossPrice = Position.MarketPosition == MarketPosition.Long ? zoneLow - (2 * TickSize) : zoneHigh + (2 * TickSize);
            double takeProfitPrice = Position.MarketPosition == MarketPosition.Long ?
                entryPrice + (3 * (entryPrice - stopLossPrice)) :
                entryPrice - (3 * (stopLossPrice - entryPrice));

            ExecuteTrade(Position.MarketPosition == MarketPosition.Long ? TradeType.Buy : TradeType.Sell,
                         entryPrice, stopLossPrice, takeProfitPrice, isSmtConfirmed, $"ScaleIn_{CurrentBar}", riskPercentage);
            hasScaledIn = true;
        }

        private void BullishFVG5Min(double currentClose, double currentAsk, bool isSmtConfirmed, string smtType)
        {
            if (hasOpenPosition || !isBullishFVG || !lastFVGHigh5M.HasValue || !lastFVGLow5M.HasValue) return;

            bool isWithinFVG = currentClose >= lastFVGLow5M.Value && currentClose <= lastFVGHigh5M.Value;
            bool isAlignedBullish = IsAligned(GetMarketDirection(2), GetMarketDirection(3));

            if (isWithinFVG && isAlignedBullish && (isSmtConfirmed || !hasConfluenceTrade))
            {
                double entryPrice = currentAsk;
                double stopLossPrice = lastFVGLow5M.Value - (2 * TickSize);
                double takeProfitPrice = entryPrice + (3 * (entryPrice - stopLossPrice));
                string label = $"BullishFVG_{CurrentBar}";

                currentTradeType = "Bullish FVG";
                currentConfluenceType = isSmtConfirmed ? $"FVG + {smtType}" : "FVG";
                hasConfluenceTrade = isSmtConfirmed;
                ExecuteTrade(TradeType.Buy, entryPrice, stopLossPrice, takeProfitPrice, isSmtConfirmed, label, 0.01);
                ExecuteScaleIn(currentTradeType, isSmtConfirmed, currentConfluenceType, currentClose, lastFVGLow5M.Value, lastFVGHigh5M.Value);
            }
        }

        private void BearishFVG5Min(double currentClose, double currentBid, bool isSmtConfirmed, string smtType)
        {
            if (hasOpenPosition || isBullishFVG || !lastFVGHigh5M.HasValue || !lastFVGLow5M.HasValue) return;

            bool isWithinFVG = currentClose <= lastFVGHigh5M.Value && currentClose >= lastFVGLow5M.Value;
            bool isAlignedBearish = !IsAligned(GetMarketDirection(2), GetMarketDirection(3));

            if (isWithinFVG && isAlignedBearish && (isSmtConfirmed || !hasConfluenceTrade))
            {
                double entryPrice = currentBid;
                double stopLossPrice = lastFVGHigh5M.Value + (2 * TickSize);
                double takeProfitPrice = entryPrice - (3 * (stopLossPrice - entryPrice));
                string label = $"BearishFVG_{CurrentBar}";

                currentTradeType = "Bearish FVG";
                currentConfluenceType = isSmtConfirmed ? $"FVG + {smtType}" : "FVG";
                hasConfluenceTrade = isSmtConfirmed;
                ExecuteTrade(TradeType.Sell, entryPrice, stopLossPrice, takeProfitPrice, isSmtConfirmed, label, 0.01);
                ExecuteScaleIn(currentTradeType, isSmtConfirmed, currentConfluenceType, currentClose, lastFVGLow5M.Value, lastFVGHigh5M.Value);
            }
        }

        private void BullishLiquidityGrab5Min(double currentClose, double currentAsk, bool isSmtConfirmed, string smtType)
        {
            if (hasOpenPosition || !lastSwingLowPrice5M.HasValue) return;

            bool isAboveSwingLow = currentClose >= lastSwingLowPrice5M.Value;
            bool isAlignedBullish = IsAligned(GetMarketDirection(2), GetMarketDirection(3));

            if (isAboveSwingLow && isAlignedBullish && (isSmtConfirmed || !hasConfluenceTrade))
            {
                double entryPrice = currentAsk;
                double stopLossPrice = lastSwingLowPrice5M.Value - (2 * TickSize);
                double takeProfitPrice = entryPrice + (3 * (entryPrice - stopLossPrice));
                string label = $"BullishLiq5M_{CurrentBar}";

                currentTradeType = "Bullish Liquidity Grab 5M";
                currentConfluenceType = isSmtConfirmed ? $"LiqGrab5M + {smtType}" : "LiqGrab5M";
                hasConfluenceTrade = isSmtConfirmed;
                ExecuteTrade(TradeType.Buy, entryPrice, stopLossPrice, takeProfitPrice, isSmtConfirmed, label, 0.01);
                ExecuteScaleIn(currentTradeType, isSmtConfirmed, currentConfluenceType, currentClose, lastSwingLowPrice5M.Value, lastSwingHighPrice5M.Value);
            }
        }

        private void BearishLiquidityGrab5Min(double currentClose, double currentBid, bool isSmtConfirmed, string smtType)
        {
            if (hasOpenPosition || !lastSwingHighPrice5M.HasValue) return;

            bool isBelowSwingHigh = currentClose <= lastSwingHighPrice5M.Value;
            bool isAlignedBearish = !IsAligned(GetMarketDirection(2), GetMarketDirection(3));

            if (isBelowSwingHigh && isAlignedBearish && (isSmtConfirmed || !hasConfluenceTrade))
            {
                double entryPrice = currentBid;
                double stopLossPrice = lastSwingHighPrice5M.Value + (2 * TickSize);
                double takeProfitPrice = entryPrice - (3 * (stopLossPrice - entryPrice));
                string label = $"BearishLiq5M_{CurrentBar}";

                currentTradeType = "Bearish Liquidity Grab 5M";
                currentConfluenceType = isSmtConfirmed ? $"LiqGrab5M + {smtType}" : "LiqGrab5M";
                hasConfluenceTrade = isSmtConfirmed;
                ExecuteTrade(TradeType.Sell, entryPrice, stopLossPrice, takeProfitPrice, isSmtConfirmed, label, 0.01);
                ExecuteScaleIn(currentTradeType, isSmtConfirmed, currentConfluenceType, currentClose, lastSwingLowPrice5M.Value, lastSwingHighPrice5M.Value);
            }
        }

        private void BullishBreakout5Min(double currentAsk, double currentClose, bool isSmtConfirmed, string smtType)
        {
            if (hasOpenPosition || !isBullishBosConfirmed || !lastSwingHighPrice5M.HasValue) return;

            bool isAboveBreakout = currentClose > lastSwingHighPrice5M.Value;
            bool isAlignedBullish = IsAligned(GetMarketDirection(2), GetMarketDirection(3));

            if (isAboveBreakout && isAlignedBullish && (isSmtConfirmed || !hasConfluenceTrade))
            {
                double entryPrice = currentAsk;
                double stopLossPrice = lastSwingHighPrice5M.Value - (2 * TickSize);
                double takeProfitPrice = entryPrice + (3 * (entryPrice - stopLossPrice));
                string label = $"BullishBreakout5M_{CurrentBar}";

                currentTradeType = "Bullish Breakout 5M";
                currentConfluenceType = isSmtConfirmed ? $"Breakout5M + {smtType}" : "Breakout5M";
                hasConfluenceTrade = isSmtConfirmed;
                ExecuteTrade(TradeType.Buy, entryPrice, stopLossPrice, takeProfitPrice, isSmtConfirmed, label, 0.01);
                ExecuteScaleIn(currentTradeType, isSmtConfirmed, currentConfluenceType, currentClose, lastSwingLowPrice5M.Value, lastSwingHighPrice5M.Value);
            }
        }

        private void BearishBreakout5Min(double currentBid, double currentClose, bool isSmtConfirmed, string smtType)
        {
            if (hasOpenPosition || !isBearishBosConfirmed || !lastSwingLowPrice5M.HasValue) return;

            bool isBelowBreakout = currentClose < lastSwingLowPrice5M.Value;
            bool isAlignedBearish = !IsAligned(GetMarketDirection(2), GetMarketDirection(3));

            if (isBelowBreakout && isAlignedBearish && (isSmtConfirmed || !hasConfluenceTrade))
            {
                double entryPrice = currentBid;
                double stopLossPrice = lastSwingLowPrice5M.Value + (2 * TickSize);
                double takeProfitPrice = entryPrice - (3 * (stopLossPrice - entryPrice));
                string label = $"BearishBreakout5M_{CurrentBar}";

                currentTradeType = "Bearish Breakout 5M";
                currentConfluenceType = isSmtConfirmed ? $"Breakout5M + {smtType}" : "Breakout5M";
                hasConfluenceTrade = isSmtConfirmed;
                ExecuteTrade(TradeType.Sell, entryPrice, stopLossPrice, takeProfitPrice, isSmtConfirmed, label, 0.01);
                ExecuteScaleIn(currentTradeType, isSmtConfirmed, currentConfluenceType, currentClose, lastSwingLowPrice5M.Value, lastSwingHighPrice5M.Value);
            }
        }

        private void BullishLiquidityGrab1H(double currentClose, double currentAsk, bool isSmtConfirmed, string smtType)
        {
            if (hasOpenPosition || !hasTouched1HBullishSwing || !lastSwingLowPrice1H.HasValue) return;

            bool isAboveSwingLow = currentClose >= lastSwingLowPrice1H.Value;
            bool isAlignedBullish = IsAligned(GetMarketDirection(2), GetMarketDirection(3));

            if (isAboveSwingLow && isAlignedBullish && (isSmtConfirmed || !hasConfluenceTrade))
            {
                double entryPrice = currentAsk;
                double stopLossPrice = lastSwingLowPrice1H.Value - (2 * TickSize);
                double takeProfitPrice = entryPrice + (3 * (entryPrice - stopLossPrice));
                string label = $"BullishLiq1H_{CurrentBar}";

                currentTradeType = "Bullish Liquidity Grab 1H";
                currentConfluenceType = isSmtConfirmed ? $"LiqGrab1H + {smtType}" : "LiqGrab1H";
                hasConfluenceTrade = isSmtConfirmed;
                ExecuteTrade(TradeType.Buy, entryPrice, stopLossPrice, takeProfitPrice, isSmtConfirmed, label, 0.01);
                ExecuteScaleIn(currentTradeType, isSmtConfirmed, currentConfluenceType, currentClose, lastSwingLowPrice1H.Value, lastSwingHighPrice1H.Value);
            }
        }

        private void BearishLiquidityGrab1H(double currentClose, double currentBid, bool isSmtConfirmed, string smtType)
        {
            if (hasOpenPosition || !hasTouched1HBearishSwing || !lastSwingHighPrice1H.HasValue) return;

            bool isBelowSwingHigh = currentClose <= lastSwingHighPrice1H.Value;
            bool isAlignedBearish = !IsAligned(GetMarketDirection(2), GetMarketDirection(3));

            if (isBelowSwingHigh && isAlignedBearish && (isSmtConfirmed || !hasConfluenceTrade))
            {
                double entryPrice = currentBid;
                double stopLossPrice = lastSwingHighPrice1H.Value + (2 * TickSize);
                double takeProfitPrice = entryPrice - (3 * (stopLossPrice - entryPrice));
                string label = $"BearishLiq1H_{CurrentBar}";

                currentTradeType = "Bearish Liquidity Grab 1H";
                currentConfluenceType = isSmtConfirmed ? $"LiqGrab1H + {smtType}" : "LiqGrab1H";
                hasConfluenceTrade = isSmtConfirmed;
                ExecuteTrade(TradeType.Sell, entryPrice, stopLossPrice, takeProfitPrice, isSmtConfirmed, label, 0.01);
                ExecuteScaleIn(currentTradeType, isSmtConfirmed, currentConfluenceType, currentClose, lastSwingLowPrice1H.Value, lastSwingHighPrice1H.Value);
            }
        }
        #endregion

        #region SMT Methods
        private bool IsSmtDivergenceConfirmed(out string smtType, out double mnqPrice, out double mesPrice)
        {
            smtType = "None";
            mnqPrice = 0;
            mesPrice = 0;

            if (!lastMnqSwingHighIndex5M.HasValue || !lastMnqSwingLowIndex5M.HasValue ||
                !lastMesSwingHighIndex5M.HasValue || !lastMesSwingLowIndex5M.HasValue)
                return false;

            if (lastMnqSwingLowIndex5M > lastMesSwingLowIndex5M &&
                lastMnqSwingLowPrice5M > lastMesSwingLowPrice5M)
            {
                smtType = "Bullish SMT";
                mnqPrice = lastMnqSwingLowPrice5M.Value;
                mesPrice = lastMesSwingLowPrice5M.Value;
                DrawSmtVisuals(smtType, mnqPrice, mesPrice, lastMnqSwingLowIndex5M.Value);
                return true;
            }
            else if (lastMnqSwingHighIndex5M > lastMesSwingHighIndex5M &&
                     lastMnqSwingHighPrice5M < lastMesSwingHighPrice5M)
            {
                smtType = "Bearish SMT";
                mnqPrice = lastMnqSwingHighPrice5M.Value;
                mesPrice = lastMesSwingHighPrice5M.Value;
                DrawSmtVisuals(smtType, mnqPrice, mesPrice, lastMnqSwingHighIndex5M.Value);
                return true;
            }

            return false;
        }

        private void DrawSmtVisuals(string smtType, double smtPrice1, double smtPrice2, int newSwingIndex)
        {
            if (lastSmtSwingIndex5M.HasValue && newSwingIndex <= lastSmtSwingIndex5M.Value) return;

            lastSmtSwingIndex5M = newSwingIndex;
            lastSmtTime5M = Time[1];
            Brush color = smtType.Contains("Bullish") ? Brushes.Green : Brushes.Red;
            Draw.Text(this, $"SMT_{newSwingIndex}", smtType, newSwingIndex, smtPrice1 + (5 * TickSize), color);
        }

        private void ClearSmtVisuals()
        {
            if (lastSmtSwingIndex5M.HasValue)
            {
                RemoveDrawObject($"SMT_{lastSmtSwingIndex5M.Value}");
                lastSmtSwingIndex5M = null;
                lastSmtTime5M = null;
            }
        }
        #endregion

        #region Drawing Methods
       

        private void DrawLatestSwings()
        {
            DrawSwingHigh1M();
            DrawSwingLow1M();
            DrawSwingHigh5M();
            DrawSwingLow5M();
            DrawSwingHigh1H();
            DrawSwingLow1H();
        }



        private void DrawSwingHigh1M()
        {
            if (CurrentBars[0] < 4 || BarsArray[0] == null || CurrentBars[1] < 4 || BarsArray[1] == null) return;

            for (int i = CurrentBars[0] - 4; i >= 1; i--)
            {
                if (i + 2 >= Highs[0].Count || i < 1) continue;

                bool green = Closes[0][i] > Opens[0][i];
                bool red1 = Closes[0][i + 1] < Opens[0][i + 1];
                if (!green || !red1) continue;

                double candidateHigh = Math.Max(Highs[0][i], Highs[0][i + 1]);
                bool thirdCandleRed = Closes[0][i + 2] < Opens[0][i + 2];
                bool thirdCandleGreen = Closes[0][i + 2] > Opens[0][i + 2];
                double thirdHigh = Highs[0][i + 2];

                bool isConfirmed = false;

                if (thirdHigh > candidateHigh)
                {
                    if (thirdCandleRed)
                        isConfirmed = true;
                }
                else
                {
                    if (thirdCandleRed || (thirdCandleGreen && thirdHigh < candidateHigh))
                        isConfirmed = true;
                }

                if (isConfirmed)
                {
                    if (!lastSwingHighIndex1M.HasValue || i > lastSwingHighIndex1M.Value)
                    {
                        lastSwingHighIndex1M = i;
                        lastSwingHighPrice1M = candidateHigh;

                        int barsAgo5M = CurrentBars[1] - BarsArray[1].GetBar(Times[0][i]);
                        if (barsAgo5M < 0) continue;

                        int endBarsAgo5M = Math.Max(0, barsAgo5M - 50);

                        Draw.Line(this, "SwingHighLine1M", false, barsAgo5M, candidateHigh, endBarsAgo5M, candidateHigh, Brushes.GreenYellow, DashStyleHelper.Dash, 1);
                        Draw.Text(this, "SwingHighLabel1M", "📈 1M Swing High", barsAgo5M, candidateHigh + (2 * TickSize), Brushes.GreenYellow);




                        Print($"Drawn 1M Swing High on 5M chart at 1M index {i}: Price={candidateHigh:F2}, Time={Times[0][i]}");
                        break;
                    }
                }
            }
        }

        private void DrawSwingLow1M()
        {
            if (CurrentBars[0] < 4 || BarsArray[0] == null || CurrentBars[1] < 4 || BarsArray[1] == null) return;

            for (int i = CurrentBars[0] - 4; i >= 1; i--)
            {
                if (i + 2 >= Lows[0].Count || i < 1) continue;

                bool red = Closes[0][i] < Opens[0][i];
                bool green1 = Closes[0][i + 1] > Opens[0][i + 1];
                if (!red || !green1) continue;

                double candidateLow = Math.Min(Lows[0][i], Lows[0][i + 1]);
                bool thirdCandleGreen = Closes[0][i + 2] > Opens[0][i + 2];
                bool thirdCandleRed = Closes[0][i + 2] < Opens[0][i + 2];
                double thirdLow = Lows[0][i + 2];

                bool isConfirmed = false;

                if (thirdLow < candidateLow)
                {
                    if (thirdCandleGreen)
                        isConfirmed = true;
                }
                else
                {
                    if (thirdCandleGreen || (thirdCandleRed && thirdLow > candidateLow))
                        isConfirmed = true;
                }

                if (isConfirmed)
                {
                    if (!lastSwingLowIndex1M.HasValue || i > lastSwingLowIndex1M.Value)
                    {
                        lastSwingLowIndex1M = i;
                        lastSwingLowPrice1M = candidateLow;

                        int barsAgo5M = CurrentBars[1] - BarsArray[1].GetBar(Times[0][i]);
                        if (barsAgo5M < 0) continue;

                        int endBarsAgo5M = Math.Max(0, barsAgo5M - 50);

                        Draw.Line(this, "SwingLowLine1M", false, barsAgo5M, candidateLow, endBarsAgo5M, candidateLow, Brushes.Pink, DashStyleHelper.Dash, 1);
                        Draw.Text(this, "SwingLowLabel1M", "📉 1M Swing Low", barsAgo5M, candidateLow - (2 * TickSize), Brushes.Pink);

                        Print($"Drawn 1M Swing Low on 5M chart at 1M index {i}: Price={candidateLow:F2}, Time={Times[0][i]}");
                        break;
                    }
                }
            }
        }

        private void DrawSwingHigh5M()
        {
            if (!lastSwingHighIndex5M.HasValue || !lastSwingHighPrice5M.HasValue || CurrentBars[1] < 4) return;

            int i = lastSwingHighIndex5M.Value;
            if (i + 2 >= Highs[1].Count || i < 1) return;

            // Confirm the pattern for drawing
            bool green = Closes[1][i] > Opens[1][i];
            bool red1 = Closes[1][i + 1] < Opens[1][i + 1];
            if (!green || !red1) return;

            double candidateHigh = Math.Max(Highs[1][i], Highs[1][i + 1]);
            bool thirdCandleRed = Closes[1][i + 2] < Opens[1][i + 2];
            bool thirdCandleGreen = Closes[1][i + 2] > Opens[1][i + 2];
            double thirdHigh = Highs[1][i + 2];

            bool isConfirmed = false;
            if (thirdHigh > candidateHigh)
            {
                if (thirdCandleRed)
                    isConfirmed = true;
            }
            else
            {
                if (thirdCandleRed || (thirdCandleGreen && thirdHigh < candidateHigh))
                    isConfirmed = true;
            }

            if (isConfirmed && Math.Abs(candidateHigh - lastSwingHighPrice5M.Value) < Instrument.MasterInstrument.TickSize)
            {
                RemoveDrawObject("SwingHighLine5M");
                RemoveDrawObject("SwingHighLabel5M");

                DateTime time1 = Times[1][i];
                DateTime time2 = Times[1][0].AddHours(2);

                Draw.Line(this, "SwingHighLine5M", false, time1, candidateHigh, time2, candidateHigh, Brushes.LimeGreen, DashStyleHelper.Dash, 2);
                Draw.Text(this, "SwingHighLabel5M", $"📈 5M Swing High ({candidateHigh:F2})", i, candidateHigh + (2 * Instrument.MasterInstrument.TickSize), Brushes.LimeGreen);

                Print($"Drawn 5M Swing High at index {i}: Price={candidateHigh:F2}, Time={time1}");
            }
        }

        private void DrawSwingLow5M()
        {
            if (!lastSwingLowIndex5M.HasValue || !lastSwingLowPrice5M.HasValue || CurrentBars[1] < 4) return;

            int i = lastSwingLowIndex5M.Value;
            if (i + 2 >= Lows[1].Count || i < 1) return;

            // Confirm the pattern for drawing
            bool red = Closes[1][i] < Opens[1][i];
            bool green1 = Closes[1][i + 1] > Opens[1][i + 1];
            if (!red || !green1) return;

            double candidateLow = Math.Min(Lows[1][i], Lows[1][i + 1]);
            bool thirdCandleGreen = Closes[1][i + 2] > Opens[1][i + 2];
            bool thirdCandleRed = Closes[1][i + 2] < Opens[1][i + 2];
            double thirdLow = Lows[1][i + 2];

            bool isConfirmed = false;
            if (thirdLow < candidateLow)
            {
                if (thirdCandleGreen)
                    isConfirmed = true;
            }
            else
            {
                if (thirdCandleGreen || (thirdCandleRed && thirdLow > candidateLow))
                    isConfirmed = true;
            }

            if (isConfirmed && Math.Abs(candidateLow - lastSwingLowPrice5M.Value) < Instrument.MasterInstrument.TickSize)
            {
                RemoveDrawObject("SwingLowLine5M");
                RemoveDrawObject("SwingLowLabel5M");

                DateTime time1 = Times[1][i];
                DateTime time2 = Times[1][0].AddHours(2);

                Draw.Line(this, "SwingLowLine5M", false, time1, candidateLow, time2, candidateLow, Brushes.Red, DashStyleHelper.Dash, 2);
                Draw.Text(this, "SwingLowLabel5M", $"📉 5M Swing Low ({candidateLow:F2})", i, candidateLow - (2 * Instrument.MasterInstrument.TickSize), Brushes.Red);

                Print($"Drawn 5M Swing Low at index {i}: Price={candidateLow:F2}, Time={time1}");
            }
        }
       
        
        private void DrawSwingHigh1H()
        {
            if (CurrentBars[2] < 4 || BarsArray[2] == null) return;

            for (int i = CurrentBars[2] - 4; i >= 1; i--)
            {
                if (i + 2 >= Highs[2].Count || i < 1) continue;

                bool green = Closes[2][i] > Opens[2][i];
                bool red1 = Closes[2][i + 1] < Opens[2][i + 1];
                if (!green || !red1) continue;

                double candidateHigh = Math.Max(Highs[2][i], Highs[2][i + 1]);
                bool thirdCandleRed = Closes[2][i + 2] < Opens[2][i + 2];
                bool thirdCandleGreen = Closes[2][i + 2] > Opens[2][i + 2];
                double thirdHigh = Highs[2][i + 2];

                bool isConfirmed = false;

                if (thirdHigh > candidateHigh)
                {
                    if (thirdCandleRed)
                        isConfirmed = true;
                }
                else
                {
                    if (thirdCandleRed || (thirdCandleGreen && thirdHigh < candidateHigh))
                        isConfirmed = true;
                }

                if (isConfirmed)
                {
                    if (!lastSwingHighIndex1H.HasValue || i > lastSwingHighIndex1H.Value)
                    {
                        lastSwingHighIndex1H = i;
                        lastSwingHighPrice1H = candidateHigh;

                        int barsAgoD = CurrentBars[3] - BarsArray[3].GetBar(Times[2][i]);
                        if (barsAgoD < 0) continue;

                        int endBarsAgoD = Math.Max(0, barsAgoD - 50);

                        Draw.Line(this, "SwingHighLine1H", false, barsAgoD, candidateHigh, endBarsAgoD, candidateHigh, Brushes.DarkGreen, DashStyleHelper.Dash, 3);
                        Draw.Text(this, "SwingHighLabel1H", "📈 1H Swing High", barsAgoD, candidateHigh + (2 * TickSize), Brushes.DarkGreen);

                        Print($"Drawn 1H Swing High on Daily chart at 1H index {i}: Price={candidateHigh:F2}, Time={Times[2][i]}");
                        break;
                    }
                }
            }
        }

        private void DrawSwingLow1H()
        {
            if (CurrentBars[2] < 4 || BarsArray[2] == null) return;

            for (int i = CurrentBars[2] - 4; i >= 1; i--)
            {
                if (i + 2 >= Lows[2].Count || i < 1) continue;

                bool red = Closes[2][i] < Opens[2][i];
                bool green1 = Closes[2][i + 1] > Opens[2][i + 1];
                if (!red || !green1) continue;

                double candidateLow = Math.Min(Lows[2][i], Lows[2][i + 1]);
                bool thirdCandleGreen = Closes[2][i + 2] > Opens[2][i + 2];
                bool thirdCandleRed = Closes[2][i + 2] < Opens[2][i + 2];
                double thirdLow = Lows[2][i + 2];

                bool isConfirmed = false;

                if (thirdLow < candidateLow)
                {
                    if (thirdCandleGreen)
                        isConfirmed = true;
                }
                else
                {
                    if (thirdCandleGreen || (thirdCandleRed && thirdLow > candidateLow))
                        isConfirmed = true;
                }

                if (isConfirmed)
                {
                    if (!lastSwingLowIndex1H.HasValue || i > lastSwingLowIndex1H.Value)
                    {
                        lastSwingLowIndex1H = i;
                        lastSwingLowPrice1H = candidateLow;

                        int barsAgoD = CurrentBars[3] - BarsArray[3].GetBar(Times[2][i]);
                        if (barsAgoD < 0) continue;

                        int endBarsAgoD = Math.Max(0, barsAgoD - 50);

                        Draw.Line(this, "SwingLowLine1H", false, barsAgoD, candidateLow, endBarsAgoD, candidateLow, Brushes.DarkRed, DashStyleHelper.Dash, 3);
                        Draw.Text(this, "SwingLowLabel1H", "📉 1H Swing Low", barsAgoD, candidateLow - (2 * TickSize), Brushes.DarkRed);

                        Print($"Drawn 1H Swing Low on Daily chart at 1H index {i}: Price={candidateLow:F2}, Time={Times[2][i]}");
                        break;
                    }
                }
            }
        }




        private void DrawMarketDirectionLabels()
        {
            // Clear all possible draw objects to eliminate residuals
            for (int i = 0; i < timeframes.Length; i++)
                RemoveDrawObject($"MarketDir_{i}");
            RemoveDrawObject("MarketDir_Display");
            RemoveDrawObject("MarketDir_Header");
            RemoveDrawObject("MarketDir_Separator");
            RemoveDrawObject("TradeTypeLabel");
            RemoveDrawObject("TradesTakenLabel");
            RemoveDrawObject("TradesWonLabel");
            RemoveDrawObject("TradesLostLabel");
            RemoveDrawObject("TradingAllowedLabel");
            RemoveDrawObject("TradeSetupLabel");
            RemoveDrawObject("TradingSessionLabel");
            RemoveDrawObject("TradingStatusLabel");
            RemoveDrawObject("ConfluenceLabel");
            RemoveDrawObject("SectionDivider1");
            RemoveDrawObject("SectionDivider2");
            RemoveDrawObject("StatsHeader");
            RemoveDrawObject("TradeEntriesHeader");
            // Clear all Line_, Text_, Label_, and generic objects
            for (int i = 0; i < 200; i++)
            {
                RemoveDrawObject($"Line_{i}");
                RemoveDrawObject($"Text_{i}");
                RemoveDrawObject($"Label_{i}");
                RemoveDrawObject($"MarketLabel_{i}");
            }

            int sectionSpacing = 3;
            string horizontalDivider = "═════════════════════════";

            // Build the complete text block
            StringBuilder fullText = new StringBuilder();

            // Market Structure Section
            fullText.AppendLine("📊 Market Structure");
            fullText.AppendLine(horizontalDivider);
            for (int i = 0; i < timeframes.Length; i++)
            {
                string directionResult = GetMarketDirection(i);
                // Debug output
                Print($"Timeframe {timeframeLabels[i]}: {directionResult}");

                // Match old code's logic
                bool isBullish = directionResult.Contains("Bullish");
                string arrow = isBullish ? "↑" : "↓";
                string directionText = directionResult.Replace("↑ ", "").Replace("↓ ", "");

                // Ensure ↓ pairs with Bearish and no Neutral
                if (arrow == "↓" && directionText != "Bearish")
                {
                    Print($"Error: ↓ paired with {directionText} for {timeframeLabels[i]}, forcing Bearish");
                    directionText = "Bearish";
                }
                if (directionText != "Bullish" && directionText != "Bearish")
                {
                    Print($"Error: Invalid direction {directionText} for {timeframeLabels[i]}, forcing Bearish");
                    directionText = "Bearish";
                    arrow = "↓";
                }

                fullText.AppendLine($"【{timeframeLabels[i],-4}】 {arrow} {directionText}");
            }

            // Stats Section
            fullText.Append(new string('\n', sectionSpacing));
            fullText.AppendLine("📈 Stats");
            fullText.AppendLine(horizontalDivider);

            string tradeLabel = currentTradeType != null ? $"Trade: {currentTradeType}" : "Trade: No Active Trade";
            fullText.AppendLine(tradeLabel);

            string tradingAllowedLabel = IsTradingAllowed() ? "Trading Allowed: Yes" : "Trading Allowed: No";
            fullText.AppendLine(tradingAllowedLabel);

            string tradingStatusLabel = dailyWins >= 1 ? "1 Win Today: Trading Not Allowed" :
                dailyLosses == 1 ? "Trading Reason: 1/2 Losses" :
                dailyLosses >= 2 ? "2/2 Max Losses Reached Today" : "Trading Reason: 0/2 Losses";
            fullText.AppendLine(tradingStatusLabel);

            fullText.AppendLine($"Trades Taken: {tradesTaken}");
            fullText.AppendLine($"Trades Won: {tradesWon}");
            fullText.AppendLine($"Trades Lost: {tradesLost}");

            // Trade Entries Section
            fullText.Append(new string('\n', sectionSpacing));
            fullText.AppendLine("🎯 Trade Entries");
            fullText.AppendLine(horizontalDivider);

            string tradeSetupLabel = "Looking for: ";
            if (currentTradeType != null)
                tradeSetupLabel += currentTradeType;
            else if (hasTouched1HBullishSwing)
                tradeSetupLabel += "5M Bullish Setup Post-1H Touch";
            else if (hasTouched1HBearishSwing)
                tradeSetupLabel += "5M Bearish Setup Post-1H Touch";
            else
                tradeSetupLabel += "No Setup";
            fullText.AppendLine(tradeSetupLabel);

            DateTime sastTime = Time[0].AddHours(2);
            bool isTradingSession = sastTime.TimeOfDay >= new TimeSpan(15, 55, 0) && sastTime.TimeOfDay <= new TimeSpan(18, 0, 0);
            fullText.AppendLine($"Trading Session (SAST {sastTime:HH:mm:ss}): {(isTradingSession ? "Yes" : "No")}");

            string confluenceLabel = currentConfluenceType != null ? $"Confluence: {currentConfluenceType}" : "Confluence: None";
            fullText.AppendLine(confluenceLabel);

            // Debug full text
            Print($"Full Market Direction Text:\n{fullText.ToString()}");

            // Draw transparent background box
            Draw.TextFixed(
                this,
                "MarketDir_Display",
                fullText.ToString(),
                TextPosition.TopLeft,
                Brushes.Transparent,
                new SimpleFont("Arial", 12),
                Brushes.Transparent,
                Brushes.Black,
                10
            );

            // Draw individual colored lines
            int currentLine = 0;
            string[] lines = fullText.ToString().Split('\n');
            foreach (var line in lines)
            {
                if (string.IsNullOrEmpty(line))
                {
                    currentLine++;
                    continue;
                }

                Brush lineColor = Brushes.White;

                if (line.Contains("Market Structure") || line.Contains("Stats") || line.Contains("Trade Entries"))
                    lineColor = Brushes.White;
                else if (line.Contains(horizontalDivider))
                    lineColor = Brushes.Gray;
                else if (line.Contains("【") && line.Contains("Bullish"))
                    lineColor = Brushes.LimeGreen;
                else if (line.Contains("【") && line.Contains("Bearish"))
                    lineColor = Brushes.Red;
                else if (line.Contains("Trade:"))
                    lineColor = currentTradeType != null ? Brushes.Cyan : Brushes.Gray;
                else if (line.Contains("Trading Allowed:"))
                    lineColor = IsTradingAllowed() ? Brushes.Green : Brushes.Red;
                else if (line.Contains("Trading Reason:") || line.Contains("Max Losses") || line.Contains("Win Today"))
                    lineColor = (dailyWins >= 1 || dailyLosses >= 2) ? Brushes.Red : Brushes.Green;
                else if (line.Contains("Trades Taken:"))
                    lineColor = Brushes.White;
                else if (line.Contains("Trades Won:"))
                    lineColor = Brushes.Green;
                else if (line.Contains("Trades Lost:"))
                    lineColor = Brushes.Red;
                else if (line.Contains("Looking for:"))
                {
                    if (currentTradeType != null)
                        lineColor = currentTradeType.Contains("Bullish") ? Brushes.LimeGreen : Brushes.Red;
                    else if (hasTouched1HBullishSwing)
                        lineColor = Brushes.LimeGreen;
                    else if (hasTouched1HBearishSwing)
                        lineColor = Brushes.Red;
                    else
                        lineColor = Brushes.Gray;
                }
                else if (line.Contains("Trading Session"))
                    lineColor = isTradingSession ? Brushes.Green : Brushes.Red;
                else if (line.Contains("Confluence:"))
                    lineColor = currentConfluenceType != null ? Brushes.Cyan : Brushes.Gray;

                Draw.TextFixed(
                    this,
                    $"Line_{currentLine}",
                    new string('\n', currentLine) + line,
                    TextPosition.TopLeft,
                    lineColor,
                    new SimpleFont("Arial", 12),
                    Brushes.Transparent,
                    null,
                    0
                );
                currentLine++;
            }
        }

        private void DrawPressureVisual(bool isBullish, DateTime time, double price)
        {
            Brush color = isBullish ? Brushes.Green : Brushes.Red;
            string label = isBullish ? "BullishPressure" : "BearishPressure";
            Draw.ArrowUp(this, $"{label}_{CurrentBar}", true, time, price - (10 * TickSize), color);
        }
        #endregion

        #region Utility Methods
        private bool IsAligned(string dir1H, string dir4H)
        {
            return dir1H.Contains("Bullish") && dir4H.Contains("Bullish");
        }

        private string GetMarketDirection(int timeframeIndex)
        {
            Bars bars = BarsArray[timeframeIndex];
            if (bars.Count < 3) return "↓ Bearish";
            int last = bars.Count - 2;
            if (last < 2) return "↓ Bearish";
            int? swingHighIndex = null;
            int? swingLowIndex = null;
            for (int i = last - 5; i >= 2; i--)
            {
                if (i >= bars.Count - 1 || i < 1) continue;
                if (bars.GetHigh(i) > bars.GetHigh(i - 1) && bars.GetHigh(i) > bars.GetHigh(i + 1))
                {
                    swingHighIndex = i;
                    break;
                }
            }
            for (int i = last - 5; i >= 2; i--)
            {
                if (i >= bars.Count - 1 || i < 1) continue;
                if (bars.GetLow(i) < bars.GetLow(i - 1) && bars.GetLow(i) < bars.GetLow(i + 1))
                {
                    swingLowIndex = i;
                    break;
                }
            }
            double lastClose = bars.GetClose(last);
            if (swingHighIndex.HasValue && lastClose > bars.GetHigh(swingHighIndex.Value))
                return "↑ Bullish";
            if (swingLowIndex.HasValue && lastClose < bars.GetLow(swingLowIndex.Value))
                return "↓ Bearish";
            if (swingHighIndex.HasValue && swingLowIndex.HasValue)
            {
                double high = bars.GetHigh(swingHighIndex.Value);
                double low = bars.GetLow(swingLowIndex.Value);
                double mid = (high + low) / 2;
                return lastClose > mid ? "↑ Bullish" : "↓ Bearish";
            }
            return "↓ Bearish";
        }
        
        
        private bool GetOrderBlockZone5M(bool isBullish, int lastIndex, out double? zoneHigh, out double? zoneLow)
        {
            zoneHigh = null;
            zoneLow = null;

            if (CurrentBars[1] < 3) return false;

            for (int i = lastIndex - 1; i >= Math.Max(1, lastIndex - 10); i--)
            {
                double high = Highs[1][i];
                double low = Lows[1][i];
                bool isBullishCandle = Closes[1][i] > Opens[1][i];
                bool isBearishCandle = Closes[1][i] < Opens[1][i];

                if (isBullish && isBearishCandle)
                {
                    zoneHigh = high;
                    zoneLow = low;
                    return true;
                }
                else if (!isBullish && isBullishCandle)
                {
                    zoneHigh = high;
                    zoneLow = low;
                    return true;
                }
            }

            return false;
        }

        private void ExecuteTrade(TradeType type, double entryPrice, double stopLossPrice, double takeProfitPrice, bool isSmtConfirmed, string label, double riskPercentage)
        {
            double riskAmount = Account.Get(AccountItem.CashValue, Currency.UsDollar) * riskPercentage;
            double riskPerShare = Math.Abs(entryPrice - stopLossPrice);
            int shares = (int)Math.Max(1, Math.Round(riskAmount / riskPerShare));

            if (type == TradeType.Buy)
            {
                EnterLong(shares, label);
                ExitLongStopMarket(stopLossPrice, label + "_SL");
                ExitLongLimit(takeProfitPrice, label + "_TP");
            }
            else
            {
                EnterShort(shares, label);
                ExitShortStopMarket(stopLossPrice, label + "_SL");
                ExitShortLimit(takeProfitPrice, label + "_TP");
            }

            tradesTaken++;
            dailyTradeCount++;
            lastTradeDate = Time[0].Date;
        }

        private enum TradeType
        {
            Buy,
            Sell
        }
        #endregion

        #region Instrument Swing Methods
        private void UpdateMnqSwings()
        {
            if (CurrentBars[7] < 3) return;

            int last5M = CurrentBars[7] - 1;

            for (int i = last5M - 1; i >= Math.Max(1, last5M - 10); i--)
            {
                if (i + 2 >= Highs[7].Count || i < 1) continue;

                bool green = Closes[7][i] > Opens[7][i];
                bool red1 = Closes[7][i + 1] < Opens[7][i + 1];
                if (green && red1)
                {
                    double candidateHigh = Math.Max(Highs[7][i], Highs[7][i + 1]);
                    bool thirdCandleRed = Closes[7][i + 2] < Opens[7][i + 2];
                    double thirdHigh = Highs[7][i + 2];

                    bool isConfirmed = thirdHigh > candidateHigh ? thirdCandleRed :
                        (thirdCandleRed || (Closes[7][i + 2] > Opens[7][i + 2] && thirdHigh < candidateHigh));

                    if (isConfirmed)
                    {
                        if (!lastMnqSwingHighIndex5M.HasValue || i > lastMnqSwingHighIndex5M.Value)
                        {
                            lastMnqSwingHighIndex5M = i;
                            lastMnqSwingHighPrice5M = candidateHigh;
                        }
                        break;
                    }
                }
            }

            for (int i = last5M - 1; i >= Math.Max(1, last5M - 10); i--)
            {
                if (i + 2 >= Lows[7].Count || i < 1) continue;

                bool red = Closes[7][i] < Opens[7][i];
                bool green1 = Closes[7][i + 1] > Opens[7][i + 1];
                if (red && green1)
                {
                    double candidateLow = Math.Min(Lows[7][i], Lows[7][i + 1]);
                    bool thirdCandleGreen = Closes[7][i + 2] > Opens[7][i + 2];
                    double thirdLow = Lows[7][i + 2];

                    bool isConfirmed = thirdLow < candidateLow ? thirdCandleGreen :
                        (thirdCandleGreen || (Closes[7][i + 2] < Opens[7][i + 2] && thirdLow > candidateLow));

                    if (isConfirmed)
                    {
                        if (!lastMnqSwingLowIndex5M.HasValue || i > lastMnqSwingLowIndex5M.Value)
                        {
                            lastMnqSwingLowIndex5M = i;
                            lastMnqSwingLowPrice5M = candidateLow;
                        }
                        break;
                    }
                }
            }
        }

        private void UpdateMesSwings()
        {
            if (CurrentBars[8] < 3) return;

            int last5M = CurrentBars[8] - 1;

            for (int i = last5M - 1; i >= Math.Max(1, last5M - 10); i--)
            {
                if (i + 2 >= Highs[8].Count || i < 1) continue;

                bool green = Closes[8][i] > Opens[8][i];
                bool red1 = Closes[8][i + 1] < Opens[8][i + 1];
                if (green && red1)
                {
                    double candidateHigh = Math.Max(Highs[8][i], Highs[8][i + 1]);
                    bool thirdCandleRed = Closes[8][i + 2] < Opens[8][i + 2];
                    double thirdHigh = Highs[8][i + 2];

                    bool isConfirmed = thirdHigh > candidateHigh ? thirdCandleRed :
                        (thirdCandleRed || (Closes[8][i + 2] > Opens[8][i + 2] && thirdHigh < candidateHigh));

                    if (isConfirmed)
                    {
                        if (!lastMesSwingHighIndex5M.HasValue || i > lastMesSwingHighIndex5M.Value)
                        {
                            lastMesSwingHighIndex5M = i;
                            lastMesSwingHighPrice5M = candidateHigh;
                        }
                        break;
                    }
                }
            }

            for (int i = last5M - 1; i >= Math.Max(1, last5M - 10); i--)
            {
                if (i + 2 >= Lows[8].Count || i < 1) continue;

                bool red = Closes[8][i] < Opens[8][i];
                bool green1 = Closes[8][i + 1] > Opens[8][i + 1];
                if (red && green1)
                {
                    double candidateLow = Math.Min(Lows[8][i], Lows[8][i + 1]);
                    bool thirdCandleGreen = Closes[8][i + 2] > Opens[8][i + 2];
                    double thirdLow = Lows[8][i + 2];

                    bool isConfirmed = thirdLow < candidateLow ? thirdCandleGreen :
                        (thirdCandleGreen || (Closes[8][i + 2] < Opens[8][i + 2] && thirdLow > candidateLow));

                    if (isConfirmed)
                    {
                        if (!lastMesSwingLowIndex5M.HasValue || i > lastMesSwingLowIndex5M.Value)
                        {
                            lastMesSwingLowIndex5M = i;
                            lastMesSwingLowPrice5M = candidateLow;
                        } 
                        break;
                    }
                }
            }
        }
        #endregion
    }
}