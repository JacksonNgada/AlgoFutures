#region Using declarations
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Windows.Media;
using NinjaTrader.Cbi;
using NinjaTrader.Data;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.Tools;
using NinjaTrader.NinjaScript;
using NinjaTrader.NinjaScript.DrawingTools;
#endregion

//This namespace holds Strategies in this folder and is required. Do not change it.
namespace NinjaTrader.NinjaScript.Strategies
{
    public class Algox7Scalper : Strategy
    {
        private Dictionary<DateTime, string> swingHighLines;
        private Dictionary<DateTime, string> swingLowLines;
        
        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Description = "Marks 5-minute swing turning points with lines extending to the right";
                Name = "Algox7Scalper";
                Calculate = Calculate.OnBarClose;
                EntriesPerDirection = 1;
                EntryHandling = EntryHandling.AllEntries;
                IsExitOnSessionCloseStrategy = true;
                ExitOnSessionCloseSeconds = 30;
                IsFillLimitOnTouch = false;
                MaximumBarsLookBack = MaximumBarsLookBack.TwoHundredFiftySix;
                OrderFillResolution = OrderFillResolution.Standard;
                Slippage = 0;
                StartBehavior = StartBehavior.WaitUntilFlat;
                TimeInForce = TimeInForce.Gtc;
                TraceOrders = false;
                RealtimeErrorHandling = RealtimeErrorHandling.StopCancelClose;
                StopTargetHandling = StopTargetHandling.PerEntryExecution;
                BarsRequiredToTrade = 20;
                
                // Parameters
                LookbackPeriod = 5;
                SwingDetectionLength = 3;
                MaxLinesOnChart = 10;
                HighSwingColor = Brushes.Red;
                LowSwingColor = Brushes.Green;
                LineWidth = 2;
            }
            else if (State == State.Configure)
            {
                swingHighLines = new Dictionary<DateTime, string>();
                swingLowLines = new Dictionary<DateTime, string>();
            }
            else if (State == State.Terminated)
            {
                // Clean up all drawing objects when the strategy is removed
                RemoveDrawObjects();
            }
        }

        protected override void OnBarUpdate()
        {
            // Only process if we have enough bars
            if (CurrentBar < Math.Max(LookbackPeriod + SwingDetectionLength, 20))
                return;

            // Check if we're on a 5-minute bar timeframe
            if (BarsPeriod.BarsPeriodType != BarsPeriodType.Minute || BarsPeriod.Value != 5)
                return;

            DetectAndMarkSwings();
            CleanupOldLines();
        }
        private void DetectAndMarkSwings()
        {
            // Detect swing high (local maximum)
            bool isSwingHigh = true;
            for (int i = 1; i <= SwingDetectionLength; i++)
            {
                if (High[LookbackPeriod] <= High[LookbackPeriod - i] ||
                    High[LookbackPeriod] <= High[LookbackPeriod + i])
                {
                    isSwingHigh = false;
                    break;
                }
            }

            // Detect swing low (local minimum)
            bool isSwingLow = true;
            for (int i = 1; i <= SwingDetectionLength; i++)
            {
                if (Low[LookbackPeriod] >= Low[LookbackPeriod - i] ||
                    Low[LookbackPeriod] >= Low[LookbackPeriod + i])
                {
                    isSwingLow = false;
                    break;
                }
            }
            
            // Mark swing high with horizontal line
            if (isSwingHigh)
            {
                DateTime swingTime = Time[LookbackPeriod];
                double swingPrice = High[LookbackPeriod];
                
                string lineId = "SwingHigh_" + swingTime.Ticks;

                // Draw a horizontal line from the swing high extending right
                Draw.Ray(this, lineId, false, LookbackPeriod, swingPrice, 0, swingPrice, HighSwingColor, DashStyleHelper.Solid, LineWidth);
                swingHighLines[swingTime] = lineId;
            }
            
            // Mark swing low with horizontal line
            if (isSwingLow)
            {
                DateTime swingTime = Time[LookbackPeriod];
                double swingPrice = Low[LookbackPeriod];
                
                string lineId = "SwingLow_" + swingTime.Ticks;

                // Draw a horizontal line from the swing low extending right
                Draw.Ray(this, lineId, false, LookbackPeriod, swingPrice, 0, swingPrice, LowSwingColor, DashStyleHelper.Solid, LineWidth);

                swingLowLines[swingTime] = lineId;
            }
        }
        
        private void CleanupOldLines()
        {
            // Keep only the most recent lines to avoid chart clutter
            while (swingHighLines.Count > MaxLinesOnChart)
            {
                DateTime oldestKey = swingHighLines.Keys.Min();
                string lineId = swingHighLines[oldestKey];
                RemoveDrawObject(lineId);
                swingHighLines.Remove(oldestKey);
            }
            
            while (swingLowLines.Count > MaxLinesOnChart)
            {
                DateTime oldestKey = swingLowLines.Keys.Min();
                string lineId = swingLowLines[oldestKey];
                RemoveDrawObject(lineId);
                swingLowLines.Remove(oldestKey);
            }
        }
        
        #region Properties
        [NinjaScriptProperty]
        [Range(1, 20)]
        [Display(Name = "Lookback Period", Description = "Number of bars to look back", Order = 1, GroupName = "Parameters")]
        public int LookbackPeriod { get; set; }
        
        [NinjaScriptProperty]
        [Range(1, 10)]
        [Display(Name = "Swing Detection Length", Description = "Number of bars to compare for swing detection", Order = 2, GroupName = "Parameters")]
        public int SwingDetectionLength { get; set; }
        
        [NinjaScriptProperty]
        [Range(1, 50)]
        [Display(Name = "Max Lines On Chart", Description = "Maximum number of swing lines to display", Order = 3, GroupName = "Parameters")]
        public int MaxLinesOnChart { get; set; }
        
        [NinjaScriptProperty]
        [Display(Name = "High Swing Color", Description = "Color for swing high lines", Order = 4, GroupName = "Visual")]
        public Brush HighSwingColor { get; set; }
        
        [NinjaScriptProperty]
        [Display(Name = "Low Swing Color", Description = "Color for swing low lines", Order = 5, GroupName = "Visual")]
        public Brush LowSwingColor { get; set; }
        
        [NinjaScriptProperty]
        [Range(1, 5)]
        [Display(Name = "Line Width", Description = "Width of the drawn lines", Order = 6, GroupName = "Visual")]
        public int LineWidth { get; set; }
        #endregion
    }
}