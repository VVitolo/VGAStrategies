#region Using declarations
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Forms;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Cbi;
using NinjaTrader.Core;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.NinjaScript;
using NinjaTrader.Gui.SuperDom;
using NinjaTrader.Gui.Tools;
using NinjaTrader.Data;
using NinjaTrader.NinjaScript;
using NinjaTrader.Core.FloatingPoint;
using NinjaTrader.NinjaScript.Indicators;
using NinjaTrader.NinjaScript.DrawingTools;
#endregion

/*
		2.0.1:
			- Initial version

*/


//This namespace holds Strategies in this folder and is required. Do not change it. 
namespace NinjaTrader.NinjaScript.Strategies.VGAStrategies
{
	public class VGA_KBOTV19 : VGAAlgoBase
	{
	// 	Variables for strategy
        // --- Private Variables ---
        private EMA ema21;
        private EMA ema5;
		private WaveTrendV2 WaveTrendV21;
		
		private int pullbackBarCount;
		private int barsSinceRev = -1;
		private bool canTradeLong = false;
		private bool canTradeShort = false;
		private bool canTradeEmaDistanceThreshold = true;
		
		public bool enterLong = false;
		public bool enterShort = false;
		private bool lastBarESIsBullish;
		
		private bool linesDrawn = false;
        private double priceLevel26 = -1;
		private double priceLevel33 = -1;
		private double priceLevel50 = -1;
		private double priceLevel77 = -1;
		
		private string displayText = " ";
		private string displayText2 = " ";
		private double lowerWickSize2 = 0;
		private double upperWickSize2 = 0;
		
		private double _NearestLevel = 0;
		private double _NearestLevelDist = 0;
		private bool canTradeNearestLevel = false;
		
		
		private Dictionary<string, double> priceLevels = new Dictionary<string, double>();		
		

		protected override void OnStateChange()
		{
			base.OnStateChange();
			if (State == State.SetDefaults)
			{
				Description 		= @"This is a trading strategy based on the ActiveSwing indicator.";
                Name 				= "KBOT v19.0.0";
				StrategyName 		= "KBOT v19.0.0";
				StrategyVersion		= "Version 19.0.0 November 2024";
				Credits 			= "Strategy provided by Leo & VGA";
			//	StrategySettings	= @"WickedRenko 34, SL = 70, PT = 34";
				IncludeCommission 	= true;
				Calculate 			= Calculate.OnBarClose;
				BarsRequiredToTrade	= 51;

				// Stop Loss
				InitialStop			= 80;

				// Profit Target
				ProfitTarget		= 33;
							
				//Varios
				activeOrder			= false;
						
                viewSW3				= false;	
				
	            // Default Values for Properties
				fastEmaPeriod = 5;
				slowEmaPeriod = 21;			
				midEmaPeriod = 10;
							
                // Default Values for Properties
                EmaDistanceThreshold = 5;  		// Default minimum distance between EMA 5 and EMA 21
				
				EmaExceedPoints = 3;
                MaxPullbackBars = 1;
                UseWickFilter = true;  			// Default is to use the wick size filter
				MinWickSize = 8;				// Default minimum wick size
                PullbackProximityToEMA = 7; 	// Default minimum distance for pullback from EMA
				
				MinDistanceFromEMA = 18;
				MaxPullbackToEMA = 8.5;
				
				
				UseRevCandleSignals = true;
				UseWaveTrendSignals = true;
				UseWaveTrendSignals2 = false;				
				lastBarESIsBullish = false;
				UseESForConfluence = false;

			}
			else if (State == State.Configure)
			{
			//	AddDataSeries("NQ 09-24", Data.BarsPeriodType.Range, 30, Data.MarketDataType.Last);		
				AddDataSeries("ES 12-24", new BarsPeriod() { BarsPeriodType = BarsPeriodType.Minute, Value = 20 });  
			}
			else if (State == State.DataLoaded)
			{
			// 	
			}
		}

		protected override void OnBarUpdate()
		{

			/*
				Put your logic here	 from OnBarUpdate
			*/
			if (BarsInProgress == 1)
			{
			
                string TextTag = "ES Bars Info";
				lastBarESIsBullish = (Closes[1][0] >= Opens[1][0] ? true : false);
				string sBullish = (Closes[1][0] > Opens[1][0] ? "Bullish" : " Bearish");
            //	displayText = $"Open: {Opens[1][0]} - Close: {Closes[1][0]} - BarType: {sBullish}";
				displayText = string.Format("Open: {0} - Close: {1} - BarType: {2}", Opens[1][0], Closes[1][0], sBullish);
                Draw.TextFixed(this, TextTag, displayText, TextPosition.TopRight, Brushes.White, new SimpleFont("Arial", 12), Brushes.DarkGray, new SolidColorBrush(Color.FromArgb(128, 0, 0, 0)), 50);				
				
				return;
			}
			
			
            if (CurrentBar < BarsRequiredToTrade)
                return;		
			
			//Add your custom indicator logic here.
			if (!linesDrawn)
            {
                DrawPriceLevels(Close[0]); // Drawing price levels
                linesDrawn = true;
            }
			
            double currentPriceLevel = CalculatePriceLevelFromCurrentPrice(Close[0]);
            if (currentPriceLevel != -1)
            {
                // Update priceLevels dictionary *only* when currentPriceLevel changes
                if (!priceLevels.ContainsValue(currentPriceLevel)) 
                {
                    priceLevels.Clear(); // Clear previous levels
                    UpdatePriceLevels(currentPriceLevel);
                }
            }

			if(priceLevels.Count > 0) { // Only proceed if priceLevels has data

				KeyValuePair<string, double> nearestLevel = FindNearestLevel(Close[0]);
				if (!string.IsNullOrEmpty(nearestLevel.Key))
				{
					string nearestLevelTag = "Nearest Level Label";
				//	string displayText2 = $"Nearest: {nearestLevel.Key} - {nearestLevel.Value.ToString()}";
					displayText2 = string.Format("  --  {0}", nearestLevel.Value.ToString());
					_NearestLevel = nearestLevel.Value;
					_NearestLevelDist = Math.Abs(_NearestLevel - Close[0]);
					canTradeNearestLevel = true;
					if (_NearestLevelDist <= 5) canTradeNearestLevel = false; 
					Draw.TextFixed(this, nearestLevelTag, displayText + displayText2, TextPosition.TopRight, Brushes.White, new SimpleFont("Arial", 12), Brushes.DarkGray, new SolidColorBrush(Color.FromArgb(128, 0, 0, 0)), 50);
				}
			}					
			
			enterLong 			= false;
			enterShort 			= false;	
	
			barsSinceRev++;
		
		    // Get EMA values

			double fastEMA = ema5[0];
		    double slowEMA = ema21[0];
		    double emaDistance = Math.Abs(fastEMA - slowEMA);	
			canTradeEmaDistanceThreshold = false;
		    if (emaDistance >= EmaDistanceThreshold)
			{	
				canTradeEmaDistanceThreshold = true;	
			}
					
			canTradeLong = true;
			if (High[0] == High[1] && High[1] == High[2]) canTradeLong = false;
			
			canTradeShort = true;
			if (Low[0] == Low[1] && Low[1] == Low[2]) canTradeLong = false;
			
		    // Check for a sequence of pullback bars (e.g., 3 consecutive pullback bars)
		    bool isPullbackSequenceValid4L = true;
		    double lastPullbackBarDistance = 0;
		    for (int i = 1; i <= MaxPullbackBars; i++)
		    {
				if (Low[i] < slowEMA) 
				{
		            isPullbackSequenceValid4L = false;
		            break;					
				}				
		        // Calculate the distance of each pullback bar from the 21 EMA
		        double pullbackDistance = Math.Abs(Low[i] - slowEMA);
		
		        // Check if this bar respects the PullbackProximityToEMA condition
		        if (pullbackDistance < PullbackProximityToEMA)
		        {
		            isPullbackSequenceValid4L = false;
		            break;
		        }
		
		        // Track the distance of the last bar in the pullback sequence
		        if (i == MaxPullbackBars)
		            lastPullbackBarDistance = pullbackDistance;
		    }
			
			double distanceToEMA4L = Math.Abs(Open[0] - slowEMA);
						
		    // --- Long Entry Condition ---
		    if (isPullbackSequenceValid4L && Close[0] > Open[0] && Close[0] > slowEMA && lastPullbackBarDistance >= PullbackProximityToEMA)
		    {
		        double lowerWickSize = Math.Min(Open[0], Close[0]) - Low[0];
		        double wickExceedAmount = Math.Abs(Low[0] - slowEMA);
				
		
		        // Long entry condition
		        if (Low[0] < fastEMA && canTradeEmaDistanceThreshold
		            && (!UseWickFilter || lowerWickSize >= MinWickSize)
		        //  && (!UseSwing5BiasFilter || swingIndicator.Bias[0] == 1)
					&& (UseESForConfluence ? lastBarESIsBullish : true)
		            && (wickExceedAmount <= EmaExceedPoints)
				//	&& (WaveTrendV21.WTSlow[0] < WaveTrendV21.WTFast[0])
					&& !enterLong
					)
		        {
		        //  EnterLong("250LEOLong");
					enterLong 			= true;
					enterShort 			= false;		
		            Draw.ArrowUp(this, "Long_" + CurrentBar, true, 0, Low[0] - 2, Brushes.Goldenrod);
		        }
		    }
			
		    // --- Short Entry Condition ---
		    bool isPullbackSequenceValid4S = true;
		    for (int i = 1; i <= MaxPullbackBars; i++)
		    {
				if (High[i] > slowEMA) 
				{
		            isPullbackSequenceValid4S = false;
		            break;					
				}				
		        double pullbackDistance = Math.Abs(High[i] - slowEMA);
		        if (pullbackDistance < PullbackProximityToEMA)
		        {
		            isPullbackSequenceValid4S = false;
		            break;
		        }
		        if (i == MaxPullbackBars)
		            lastPullbackBarDistance = pullbackDistance;
		    }
			
			double distanceToEMA4S = Math.Abs(slowEMA - Open[0]);
			
		    if (isPullbackSequenceValid4S && Close[0] < Open[0] && Close[0] < slowEMA && lastPullbackBarDistance >= PullbackProximityToEMA)
		    {
		        double upperWickSize = High[0] - Math.Max(Open[0], Close[0]);
		        double wickExceedAmount = Math.Abs(High[0] - slowEMA);
				
		
		        if (High[0] > fastEMA && canTradeEmaDistanceThreshold
		            && (!UseWickFilter || upperWickSize >= MinWickSize)
		        //  && (!UseSwing5BiasFilter || swingIndicator.Bias[0] == -1)
					&& (UseESForConfluence ? !lastBarESIsBullish : true)
		            && (wickExceedAmount <= EmaExceedPoints)
				//	&& (WaveTrendV21.WTSlow[0] > WaveTrendV21.WTFast[0])
					&& !enterShort
					)
		        {
		        //  EnterShort("250LEOShort");
					enterLong 			= false;
					enterShort 			= true;	
		            Draw.ArrowDown(this, "Short_" + CurrentBar, true, 0, High[0] + 2, Brushes.Goldenrod);
		        }
		    }
				
			if (UseRevCandleSignals)
			{				
			// We will look for entries in reversal candles that enter the EMA cloud at maximum of X points and exit it again.
			// Long entries
				if ((Close[1] < Open[1] && Close[0] > Open[0] && ema5[0] > ema21[0])
				)
				{
					barsSinceRev = 0;
					if ((canTradeLong  && canTradeEmaDistanceThreshold && !enterLong)
					&&	(Low[0] < fastEMA) && (distanceToEMA4L >= MinDistanceFromEMA/2)
					&& (UseESForConfluence ? lastBarESIsBullish : true)	
					&& (WaveTrendV21.Analyzer[0] != -2)
				//	&& (fastEMA - Low[0] <= MaxPullbackToEMA) 		
				//	&& (WaveTrendV21.WTSlow[0] < WaveTrendV21.WTFast[0])

				//	&& canTradeNearestLevel		
					)																	 									 
					{
						Draw.ArrowUp(this, "Long_" + CurrentBar, true, 0, Low[0] - 6, Brushes.Indigo);
					//	EnterLong("270LEOLong");
						enterLong 			= true;
						enterShort 			= false;	
					}
				}
				
//				if ((Close[1] < Open[1] && Close[0] > Open[0] && canTradeLong) 
//				&& (WaveTrendV21.Analyzer[0] == -1 || WaveTrendV21.Analyzer[1] == -1)  
//				&& !enterLong)
//				{
//					barsSinceRev = 0;
//					Draw.TriangleUp(this, "Long_" + CurrentBar, true, 0, Low[0] - 6, Brushes.Yellow);
//				//	EnterLong("270LEOLong");
//					enterLong 			= true;
//					enterShort 			= false;	
//				}
							
			// Short entries 	
				if (Close[1] > Open[1] && Close[0] < Open[0] && ema5[0] < ema21[0])
				//	&& isPullbackSequenceValid4S && lastPullbackBarDistance >= PullbackProximityToEMA)
				{
					barsSinceRev = 0;
					if (( canTradeShort && canTradeEmaDistanceThreshold && !enterShort)
					&& (High[0] > fastEMA) && (distanceToEMA4S >= MinDistanceFromEMA/2)
					&& (UseESForConfluence ? !lastBarESIsBullish : true)
					&& (WaveTrendV21.Analyzer[0] != 2)
				//	&& (High[0] - fastEMA <= MaxPullbackToEMA))	
				//	&& (WaveTrendV21.WTSlow[0] > WaveTrendV21.WTFast[0])
				//	&& canTradeNearestLevel		
					)																	 									  
					{
						Draw.ArrowDown(this, "Short_" + CurrentBar, true, 0, High[0] + 6, Brushes.Indigo);
					//	EnterShort("270LEOShort");
						enterLong 			= false;
						enterShort 			= true;		
					}
				}
				
//				if ((Close[1] > Open[1] && Close[0] < Open[0] && canTradeShort)
//				&& (WaveTrendV21.Analyzer[0] == 1 || WaveTrendV21.Analyzer[1] == 1) 
//				&& !enterShort) 										
//				{
//					Draw.TriangleDown(this, "Short_" + CurrentBar, true, 0, High[0] + 6, Brushes.Yellow);
//					//	EnterShort("270LEOShort");
//					enterLong 			= false;
//					enterShort 			= true;							
//				}				
			}
		
			if (UseWaveTrendSignals2)    
			{				
			// WAveTrendV2 entries				
				if (WaveTrendV21.Analyzer[0] == 1 && Close[0] > fastEMA && Close[0] > Open[0] && Close[0] > slowEMA
				&& (UseESForConfluence ? lastBarESIsBullish : true)	
				&& (WaveTrendV21.WTFast[0] > WaveTrendV21.WTSlow[0])	
				&& canTradeNearestLevel	
				&& !enterLong	
				) 																											
				{
					Draw.ArrowUp(this, "Long_" + CurrentBar, true, 0, Low[0] - 10, Brushes.White);
				//	EnterLong("290LEOLong");
					enterLong 			= true;
					enterShort 			= false;	
				}	

				if (WaveTrendV21.Analyzer[0] == 2 && Close[0] > fastEMA && Close[0] > Open[0] && Close[0] > slowEMA
				&& (UseESForConfluence ? lastBarESIsBullish : true)			
				&& (WaveTrendV21.WTSlow[0] > 0)	
				&& (WaveTrendV21.WTFast[0] > WaveTrendV21.WTSlow[0])	
				&& canTradeNearestLevel	
				&& !enterLong	
				) 																											
				{
					Draw.ArrowUp(this, "Long_" + CurrentBar, true, 0, Low[0] - 10, Brushes.Yellow);
				//	EnterLong("290LEOLong");
					enterLong 			= true;
					enterShort 			= false;						
				}					
				
				
				if (WaveTrendV21.Analyzer[0] == -1 && Close[0] < fastEMA && Close[0] < Open[0] && Close[0] < slowEMA
				&& (UseESForConfluence ? !lastBarESIsBullish : true)	
				&& (WaveTrendV21.WTFast[0] < WaveTrendV21.WTSlow[0])	
				&& canTradeNearestLevel	
				&& !enterShort
				) 																											
				{
					Draw.ArrowDown(this, "Short_" + CurrentBar, true, 0, High[0] + 10, Brushes.White);
				//	EnterShort("290LEOShort");			
					enterLong 			= false;
					enterShort 			= true;						
				}				

				if (WaveTrendV21.Analyzer[0] == -1 && Close[0] < fastEMA && Close[0] < Open[0] && Close[0] < slowEMA
				&& (UseESForConfluence ? !lastBarESIsBullish : true)	
				&& (WaveTrendV21.WTSlow[0] > -20 && WaveTrendV21.WTSlow[0] < 20)
				&& (WaveTrendV21.WTFast[0] < WaveTrendV21.WTSlow[0])	
				&& canTradeNearestLevel	
				&& !enterShort	
				) 																											
				{
					Draw.ArrowDown(this, "Short_" + CurrentBar, true, 0, High[0] + 10, Brushes.Yellow);
				//	EnterShort("290LEOShort");			
					enterLong 			= false;
					enterShort 			= true;						
				}						
				
			}				
			
			if (UseWaveTrendSignals)
			{
			
				if ((Close[0] > Open[0]) && (WaveTrendV21.Analyzer[0] == 2 ) && (WaveTrendV21.WTFast[0] > 30) && !enterLong)
				{
					barsSinceRev = 0;
					Draw.TriangleUp(this, "Long_" + CurrentBar, true, 0, Low[0] - 6, Brushes.White);
				//	EnterLong("270LEOLong");
					enterLong 			= true;
					enterShort 			= false;	
				}				
				
				
				
				if ((Close[0] < Open[0]) && (WaveTrendV21.Analyzer[0] == -2 ) && (WaveTrendV21.WTFast[0] < 20) && !enterShort) 										//	&& isPullbackSequenceValid4S && lastPullbackBarDistance >= PullbackProximityToEMA)
				{
					Draw.TriangleDown(this, "Short_" + CurrentBar, true, 0, High[0] + 6, Brushes.White);
					//	EnterShort("270LEOShort");
					enterLong 			= false;
					enterShort 			= true;							
				}					
			}	
			
			base.OnBarUpdate();
		
		}			
					
        private void UpdatePriceLevels(double currentPriceLevel)
        {
            priceLevel26 = currentPriceLevel;
            priceLevel33 = currentPriceLevel + 7;
            priceLevel50 = currentPriceLevel + 24;
            priceLevel77 = currentPriceLevel + 51;

            priceLevels["xxx26"] = priceLevel26;
            priceLevels["xxx33"] = priceLevel33;
            priceLevels["xxx50"] = priceLevel50;
            priceLevels["xxx77"] = priceLevel77;

        }		
		
		
        private void DrawPriceLevels(double currentPrice)
        {
            double basePrice26 = Math.Floor(currentPrice / 100.0) * 100 + 26;
			priceLevels["baseLevel26_0"] = basePrice26; // Add to dictionary
			double basePrice33 = Math.Floor(currentPrice / 100.0) * 100 + 33;
            priceLevels["baseLevel33_0"] = basePrice33; // Add to dictionary
			double basePrice50 = Math.Floor(currentPrice / 100.0) * 100 + 50;
			priceLevels["baseLevel50_0"] = basePrice50; // Add to dictionary
			double basePrice77 = Math.Floor(currentPrice / 100.0) * 100 + 77;
			priceLevels["baseLevel77_0"] = basePrice77; // Add to dictionary
			
            for (int i = 0; i <= 20; i++)
            {
                double higherLevel26 = basePrice26 + i * 100;
				priceLevels["higherLevel26_0" + i] = higherLevel26; // Add to dictionary
				double higherLevel33 = basePrice33 + i * 100;
                priceLevels["higherLevel33_0" + i] = higherLevel33; // Add to dictionary
				double higherLevel50 = basePrice50 + i * 100;
				priceLevels["higherLevel50_0" + i] = higherLevel50; // Add to dictionary
                double higherLevel77 = basePrice77 + i * 100;
				priceLevels["higherLevel77_0" + i] = higherLevel77; // Add to dictionary

			}

            for (int i = 1; i <= 20; i++)
            {
                double lowerLevel26 = basePrice26 - i * 100;
				priceLevels["lowerLevel26_0" + i] = lowerLevel26; // Add to dictionary
                double lowerLevel33 = basePrice33 - i * 100;
				priceLevels["lowerLevel33_0" + i] = lowerLevel33; // Add to dictionary
				double lowerLevel50 = basePrice50 - i * 100;
				priceLevels["lowerLevel50_0" + i] = lowerLevel50; // Add to dictionary
				double lowerLevel77 = basePrice77 - i * 100;				
				priceLevels["lowerLevel77_0" + i] = lowerLevel77; // Add to dictionary

            }
        }		
		
        public KeyValuePair<string, double> FindNearestLevel(double currentPrice)
        {
			 if (priceLevels == null || priceLevels.Count == 0)
            {
                 return new KeyValuePair<string, double>(string.Empty, 0); // Return default if no levels are loaded.
            }
            double minDifference = double.MaxValue;
            KeyValuePair<string, double> nearest = default(KeyValuePair<string, double>);

            foreach (var level in priceLevels)
            {
                double difference = Math.Abs(level.Value - currentPrice);
                if (difference < minDifference)
                {
                    minDifference = difference;
                    nearest = level;
                }
            }

            return nearest;
        }		

        private double CalculatePriceLevelFromCurrentPrice(double currentPrice)
        {
            return Math.Floor(currentPrice / 100) * 100 + 26;
        }				
		
		
		
		
		protected override bool validateEntryLong() {
			/*
				Put your logic here	from validateEntryLong()
			*/		
			return (enterLong);							
        }
		
		protected override bool validateEntryShort() {
			/*
				Put your logic here	validateEntryShort()
			*/			

				return (enterShort);				
        }
			
		protected override bool validateExitLong() {
			if (true)		//	autoRS
			{
			/*
				Put your logic here	validateExitLong()
			*/	
			}
			
			return false;
		}
		
		protected override bool validateExitShort() {
			if (true)		//	autoRS
			{	
			/*
				Put your logic here	validateExitShort()
			*/		
			}
			
			return false;
		}			
	
		protected override void OpenFormButton_Click(object sender, EventArgs e)
		{
			PresentaDisplayText();
		}			
		
		#region Strategy Management
		protected override void initializeIndicators() {
			/*
				Indicators to be initialized
			*/								
                // Initialize EMAs
                ema21 = EMA(slowEmaPeriod);
                ema5 = EMA(fastEmaPeriod);
				WaveTrendV21				= WaveTrendV2(Close, 10, 21);
				WaveTrendV21.Plots[0].Brush = Brushes.Teal;
				WaveTrendV21.Plots[1].Brush = Brushes.Black;
				WaveTrendV21.Plots[2].Brush = Brushes.Black;				
			
			
			
			if (viewSW3)
			{
				AddChartIndicator(ema21);
                AddChartIndicator(ema5);
				AddChartIndicator(WaveTrendV21);
			}	
			
		}
		
		#endregion	
		
		#region Properties
		
		#region 04. Filter Settings		
			/*
				Put your logic here	for filters settings in Properties
			*/		
		
        // --- Properties ---

        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name = "FastEMA Period", Description = "Fast EMA Period", Order = 1, GroupName = "06. Filter Settings - Parameters")]
        public int fastEmaPeriod { get; set; }		

        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name = "SlowEMA Period", Description = "Slow EMA Period", Order = 2, GroupName = "06. Filter Settings - Parameters")]
        public int slowEmaPeriod { get; set; }			
		
        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name = "MidEMA Period", Description = "Mid EMA Period", Order = 3, GroupName = "06. Filter Settings - Parameters")]
        public int midEmaPeriod { get; set; }				
			
        // Add the property
        [NinjaScriptProperty]
        [Display(Name = "Use Swing Range Filter", Description = "Enable or Disable Swing Range Amplitude Filter", Order = 10, GroupName = "06. Parameters")]
        public bool UseSwingRangeFilter { get; set; }
		
        [NinjaScriptProperty]
        [Range(0, double.MaxValue)]
        [Display(Name = "Min Swing Range", Description = "Minimum swing range amplitude to take trades", Order = 11, GroupName = "06. Parameters")]
        public double MinSwingRange { get; set; }
		
		[NinjaScriptProperty]
		[Display(Name = "Use Swing5 Bias Filter", Description = "Enable or Disable Swing5 Bias Filter", Order = 12, GroupName = "06. Parameters")]
		public bool UseSwing5BiasFilter { get; set; }		
		
	
        [NinjaScriptProperty]
        [Range(0.1, double.MaxValue)]
        [Display(Name = "EMA Distance Threshold", Description = "Minimum distance between EMA 5 and EMA 21 (in points)", Order = 20, GroupName = "06. Parameters")]
        public double EmaDistanceThreshold { get; set; }				
		
		[NinjaScriptProperty]
        [Range(1, 5)]
        [Display(Name = "Max Pullback Bars", Description = "Maximum number of pullback bars to consider", Order = 21, GroupName = "06. Parameters")]
        public int MaxPullbackBars { get; set; }

		// Add this property for the pullback proximity to EMA
        [NinjaScriptProperty]
        [Range(0.1, double.MaxValue)]
        [Display(Name = "Min Pullback Proximity to EMA", Description = "Minimum distance for pullback from EMA", Order = 22, GroupName = "06. Parameters")]
        public double PullbackProximityToEMA { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Use Wick Filter", Description = "Enable or Disable Wick Size Filter", Order = 23, GroupName = "06. Parameters")]
        public bool UseWickFilter { get; set; }
		
		[NinjaScriptProperty]
        [Range(0.1, double.MaxValue)]
        [Display(Name = "Minimum Wick Size", Description = "Minimum wick size for the candle to be considered valid", Order = 24, GroupName = "06. Parameters")]
        public double MinWickSize { get; set; }

        [NinjaScriptProperty]
        [Range(0.1, double.MaxValue)]
        [Display(Name = "EMA Exceed Points", Description = "Points that the wick can exceed the EMA", Order = 25, GroupName = "06. Parameters")]
        public double EmaExceedPoints { get; set; }		
				
        [NinjaScriptProperty]
        [Range(0.1, double.MaxValue)]
        [Display(Name = "Min Distance From EMA", Description = "Minimum distance of the bar's high or low from the EMA (in points)", Order = 26, GroupName = "06. Parameters")]
        public double MinDistanceFromEMA { get; set; }

        // Add this property for the pullback proximity to EMA
        [NinjaScriptProperty]
        [Range(0.1, double.MaxValue)]
        [Display(Name = "Max Pullback to EMA", Description = "Maximum distance for pullback to EMA", Order = 31, GroupName = "06. Parameters")]
        public double MaxPullbackToEMA { get; set; }				

		[NinjaScriptProperty]
		[Display(Name = "Trading Reversal Candle signals?", Description = "Enable or Disable Trading Reversal Candles signals", Order = 14, GroupName = "06. Parameters")]
		public bool UseRevCandleSignals { get; set; }			
		
		[NinjaScriptProperty]
		[Display(Name = "Trading WaveTrendV2 signals?", Description = "Enable or Disable Trading WaveTrend2 signals", Order = 15, GroupName = "06. Parameters")]
		public bool UseWaveTrendSignals2 { get; set; }		

		[NinjaScriptProperty]
		[Display(Name = "Trading WaveTrend signals?", Description = "Enable or Disable Trading WaveTrend signals", Order = 16, GroupName = "06. Parameters")]
		public bool UseWaveTrendSignals { get; set; }				
		
		[NinjaScriptProperty]
		[Display(Name = "Filter entries with ES last Bar Type?", Description = "Enable or Disable filtering siganl with data on ES", Order = 15, GroupName = "06. Parameters")]
		public bool UseESForConfluence { get; set; }	
		
		
		#endregion		

		#region 09. Control visual indicators		
/*
	//	Put your logic here	for visual settings
		
		[NinjaScriptProperty]
		[Display(Name="Display HalfTrend Indicator?", Order=3, GroupName="97b. Indicators: Visual control")]
		public bool viewHT
		{ get; set; }		
		
*/		

		[NinjaScriptProperty]
		[Display(Name="Show Indicators on Chart?", Order=3, GroupName="09b. Indicators: Visual control")]
		public bool viewSW3
		{ get; set; }	
		
		#endregion			
		
		#endregion		
	
	}
	
	
	
	
}
