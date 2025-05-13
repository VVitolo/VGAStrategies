#region Using declarations
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
//using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Forms;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Cbi;
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

//This namespace holds Strategies in this folder and is required. Do not change it. 
namespace NinjaTrader.NinjaScript.Strategies.VGAStrategies
{
	abstract public class VGAAlgoBase : Strategy, ICustomTypeDescriptor
	{	
		
		/*
			Cambios versión 1.0 a 1.01:
			- Added Buttons XBarsContinuationSignal & optionslevelsFilter
			- Modified changeorders to avoid errors in strategy when high volatity
		
		
		*/
		#region variables
		#region Orders & Positions
				
		private double myDbl;
		private string  oco;

	//	First Order
		private Order entryOrder = null; 	// This variable holds an object representing our first entry order
        private Order stopOrder = null; 	// This variable holds an object representing our stop loss first order
        private Order targetOrder = null; 	// This variable holds an object representing our profit target first order

	//	Orders controls	
		private bool activeLongMk1 = false;
		private bool activeShortMk1 = false;
		private bool activeLongAE = false;
		private bool activeShortAE = false;
		public bool activeOrder = false;
		
        private int currentPosition = 0;	// Current Position Amount        
		private int iCurrentPosition = 0;	// New Current Position Amount    
		private double currTargetPrice = 0;
		private double currStopPrice = 0;

	//	Target & Stop
		public double	Target;
		private double	Stop;

	//	Trailing Stop 
		private double Stop_Price 	= 0;
		private double Stop_Trigger = 0;
		private double Target_Price = 0;
		
	//	Stop a BE	
		private double BE_Price 		= 0;
		private double BEPrice_Trigger 	= 0;		
		
	//	Sesiones	
		public int Session1Count;
		public int Session2Count;
		public int Session3Count;
		public int Session4Count;
		public int SessionNumber;
		
	//	Colors
		private Brush time1Color = Brushes.Transparent;
		private Brush time2Color = Brushes.Transparent;
		private Brush time3Color = Brushes.Transparent;
		private Brush time4Color = Brushes.Transparent;
		
		
		#endregion
		
		#region Management
//		Risk Management		
		private bool CanTrade; 						// Daily control to enable/disable entry trades
		private double currentPnL			= 0.0;
		private double UnrealizedPnL 		= 0.0;
		private int lastThreeTrades 		= 0;  	// This variable holds our value for how profitable the last three trades were.
		private int priorNumberOfTrades 	= 0;	// This variable holds the number of trades taken. It will be checked every OnBarUpdate() to determine when a trade has closed.
		private int priorSessionTrades		= 0;	// This variable holds the number of trades taken prior to each session break.

//		Controles para operaciones
		private bool okToTrade = false;
		private bool okToTradeEcon1 = true;
		private bool okToTradeEcon2 = true;
		private bool okToTradeMRZ = false;
		public bool okToTradeOnlyMRZ = false;
		private NinjaTrader.Gui.Tools.SimpleFont title2 = 
		new NinjaTrader.Gui.Tools.SimpleFont("Agency Fb", 16) { Size = 16, Bold = true };
//      Tomar señales contrarias cuando cerramos una operación por señal contraria a nuestra entrada.		
		private bool reverseLong = false;
		private bool reverseShort = false;
		public int longCounter;
		public int shortCounter;
		
		// KillAll 
		private Instrument inst;
		private Account chartTraderAccount;
		private AccountSelector accountSelector;
		
		#endregion	
		
		#region MVRV
	//	private NinjaTrader.NinjaScript.DrawingTools.Rectangle lastRectangle;
		private NinjaTrader.NinjaScript.DrawingTools.RegionHighlightY lastRegion;		
		private bool ShowAll;
		public bool activeMRZ = false;
		public double MRZSignalUp;
		public double MRZSignalDw;
		public bool isMRZBtnEnabled;

//		BotConf: Trending | Balanced		
		public bool BotConfTrending;		
		
		#endregion
		
		#region Strategy variables
		private double TrailStopLong;
		private double TrailStopShort;
        private bool startTrail;
		
		private double 	previousPrice		= 0;		// previous price used to calculate trailing stop
		private double 	newPrice			= 0;		// Default setting for new price used to calculate trailing stop
		private double	stopPlot			= 0;		// Value used to plot the stop level
		private double	initialBreakEven	= 0; 
		
		private bool showFixedStopLossOptions;
		private bool showATRStopLossOptions;
		private CommonEnumsVGA.StopLossType stopLossType;
	
		private CommonEnumsVGA.ProfitTargetType profitTargetType;
		private bool showFixedProfitTargetOptions;
		private bool showATRProfitTargetOptions;
		private double stopLossPriceLong;
		private double profitTargetPriceLong;
		private double stopLossPriceShort;
		private double profitTargetPriceShort;
		
		private CommonEnumsVGA.TrailStopType trailStopType;
		private bool showTickTrailOptions;
		private bool showATRTrailOptions;
		private bool showBarTrailOptions; 
		
		private bool isProfitTargetHit = false;
		private double profitTargetPrice;
		private bool jumpToProfitSet = false;
		private double orderPriceLong = 0;
		private double orderPriceShort = 0;
		
		
		#endregion
		
		#region Indicators	
		// MSTrendFilter
		public double Trend;
		public double lastTrend;		
		
		private NinjaTrader.NinjaScript.Indicators.TradeSaber.ATRTrailBands StopLoss_ATR;
		private NinjaTrader.NinjaScript.Indicators.TradeSaber.ATRTrailBands ProfitTarget_ATR; 
		private NinjaTrader.NinjaScript.Indicators.TradeSaber.ATRTrailBands TrailStop_ATR;
		
		#endregion
		
		#region ControlesAuto
		private bool useAuto		= true;
		private bool useLong		= true;
		private bool useShort		= true;		
		public bool useOLFilter		= false;
	
		#endregion	
		
		#region Chart Trader Buttons
		
		private System.Windows.Controls.RowDefinition	addedRow;
		private Gui.Chart.ChartTab						chartTab;
		private Gui.Chart.Chart							chartWindow;
		private System.Windows.Controls.Grid			chartTraderGrid, chartTraderButtonsGrid, lowerButtonsGrid;
		
//      New Toggle Buttons
		private System.Windows.Controls.Button			useAutoButton;
		private System.Windows.Controls.Button			useLongButton, useShortButton;
		private System.Windows.Controls.Button			displayButton;
		private System.Windows.Controls.Button			autoBEButton, autoTSButton;
		private System.Windows.Controls.Button			mrzBtn;
		private System.Windows.Controls.Button			autoRSButton, autoRVButton;
		private System.Windows.Controls.Button 			panicBtn, BotConfTrendingButton;
		private System.Windows.Controls.Button 			barsButton, optLvlButton;
//end New Toogle Buttons	
		private System.Windows.Controls.Button			longButtonMarket, shortButtonMarket;
		private System.Windows.Controls.Button			closeButton;
		private System.Windows.Controls.Button 			add1Button, add2Button;
		private System.Windows.Controls.Button			close1Button, close2Button;  

		private bool									panelActive;
		private System.Windows.Controls.TabItem			tabItem;
		private System.Windows.Controls.Grid 			myGrid;

		#region Button Clicked
				
		private bool add1ButtonClicked;				
		private bool add2ButtonClicked;			
		private bool close1ButtonClicked;				
		private bool close2ButtonClicked;	
		private bool add1ButtonProcessed = false;
		private bool add2ButtonProcessed = false; 

		#endregion
		
		#endregion

		#region ProgramVersion
		protected string StrategyName;
		
		#endregion
		
		#region PropertiesMsanipulation
		private bool ctrlDailyMaxLoss;
		private bool showctrlDailyMaxLoss;
		private bool ctrlDailyMinPnLToSecure;
		private bool showctrlDailyMinPnLToSecure;
		private bool time_1;
		private bool showctrlTime_1;
		private bool time_2;
		private bool showctrlTime_2;
		private bool time_3;
		private bool showctrlTime_3;
		private bool time_4;
		private bool showctrlTime_4;		
		private bool tfilterMSTrend;
		private bool showctrlFilterMSTrend;
		private bool stopLoss;
		private bool showctrlStopLoss;
		private bool bprofitTarget;
		private bool showctrlProfitTarget;
		private bool beSetAuto;
		private bool showctrlBESetAuto;
		private bool trailSetAuto;
		private bool showctrlTrailSetAuto;
		#endregion
		#endregion
		
		#region OnStateChange
		protected override void OnStateChange()
		{
		
			if (State == State.SetDefaults)
			{
				Description									= @"Base Strategy with OEB v.5.0.2 TradeSaber(Dre).";
				Name										= "OEBVGAAlboBase4B4v4";
				BaseAlgoVersion								= "1.12";
				StrategyVersion								= "1.0";
				Author										= "indiVGA";
				Credits										= "indiVGA";
				Disclaimer									= "Use this strategy at your own risk. Author take no responsibility of the losses incurred.";
				Calculate									= Calculate.OnPriceChange;
				ShowTransparentPlotsInDataBox 				= true;
				DrawOnPricePanel							= true;
				EntriesPerDirection							= 5;
				EntryHandling								= EntryHandling.AllEntries;
				IsExitOnSessionCloseStrategy				= false;
				ExitOnSessionCloseSeconds					= 30;
				IsFillLimitOnTouch							= true;
				MaximumBarsLookBack							= MaximumBarsLookBack.TwoHundredFiftySix;
				OrderFillResolution							= OrderFillResolution.Standard;
				Slippage									= 0;
				StartBehavior								= StartBehavior.WaitUntilFlat;
				TimeInForce									= TimeInForce.Gtc;
				TraceOrders									= false;
				RealtimeErrorHandling						= RealtimeErrorHandling.StopCancelClose;
				StopTargetHandling							= StopTargetHandling.PerEntryExecution;
				BarsRequiredToTrade							= 20;
                IsUnmanaged                                 = true;
                IsAdoptAccountPositionAware                 = true;				
				// Disable this property for performance gains in Strategy Analyzer optimizations
				// See the Help Guide for additional information
				IsInstantiatedOnEachOptimizationIteration	= true;
				
				#region Default Parameters
				//MRZ
				isMRZBtnEnabled 	= false;
				
				// Times
				Time_1				= true;
				time_1				= true;
				showctrlTime_1		= true;
                Time_2				= false;
				time_2				= false;
				showctrlTime_2		= false;
				Time_3				= false;
				time_3				= false;
				showctrlTime_3		= false;				
                Time_4				= false;
				time_4				= false;
				showctrlTime_4		= false;
                    
                Start_Time_1 = DateTime.Parse("00:00", System.Globalization.CultureInfo.InvariantCulture);
                Stop_Time_1 = DateTime.Parse("22:00", System.Globalization.CultureInfo.InvariantCulture);
                Start_Time_2 = DateTime.Parse("07:00", System.Globalization.CultureInfo.InvariantCulture);
                Stop_Time_2 = DateTime.Parse("09:00", System.Globalization.CultureInfo.InvariantCulture);
                Start_Time_3 = DateTime.Parse("13:00", System.Globalization.CultureInfo.InvariantCulture);
                Stop_Time_3 = DateTime.Parse("17:00", System.Globalization.CultureInfo.InvariantCulture);
                Start_Time_4 = DateTime.Parse("19:00", System.Globalization.CultureInfo.InvariantCulture);
                Stop_Time_4 = DateTime.Parse("20:00", System.Globalization.CultureInfo.InvariantCulture);
				
				
				//Position Size & Management
				activeOrder									= false;
				CustomPositionAmount						= 1;
				MaxPositionPerTrade							= 5;
				MaxTradesPerSession							= 100;
				CtrlDailyMaxLoss =	ctrlDailyMaxLoss = showctrlDailyMaxLoss	= false;
//				ctrlDailyMaxLoss							= false;
//				showctrlDailyMaxLoss						= false;
				dailyMaxLoss								= 50000;
				
				CtrlDailyMinPnLToSecure = ctrlDailyMinPnLToSecure =	showctrlDailyMinPnLToSecure	= false;
//				ctrlDailyMinPnLToSecure						= false;
//				showctrlDailyMinPnLToSecure					= false;
				dailyMinPnLToSecure							= 50000;
								
				//Stop Offset
				StopLoss									= true;
				stopLoss									= true;
				showctrlStopLoss							= true;
				meInitialStop								= 40;
				InitialStop							= 55;
				
				//Profit
				bProfitTarget								= true;
				bprofitTarget								= true;
				showctrlProfitTarget						= true;
				meProfitTarget							= 40;
				ProfitTarget								= 90;
				
				//Prints
				DisplayText									= true; 
				SystemPrint									= true;
				BreakevenPrints								= false;
				TrailPrints									= false;
				OrdersPrints								= false;
				SignalsPrints								= true;
				BaseSignalsPrints							= false;				
				ShowAll 									= false;

				
				// VGA Added
				econNumber1 								= 0;
				econNumber2 								= 0;
				MRZSignalUp									= 0;
				MRZSignalDw									= 30000;
				CanTrade									= true;
				
			//	Future use
			//	DayTrend 									= DaySessionType.Balanced;
				
			//	DisplayInfo: HistoricalTradePerformance, DeltaInfo, MRZ&Gral Info
				ShowHistorical									= true;
				DisplayHistoricalTradePerformanceOrientation 	= TextPosition.TopLeft;	
				DisplayStrategyPnL								= true;
				DisplayStrategyPnLOrientation				 	= TextPosition.BottomLeft;
				DisplayMRZInfo									= true;
				DisplayMRZInfoOrientation						= TextPosition.BottomRight;				
				
			//	Set BE Stop
				BESetAuto									= false;
				beSetAuto									= false;
				showctrlBESetAuto							= false;
				BE_Trigger									= 20;
				BE_Size										= 2;

			//	Trailing Stops
				TrailSetAuto								= false;
				trailSetAuto								= false;
				showctrlTrailSetAuto						= false;
				Trail_Trigger								= 25;
				Trail_Size									= 20;
				Trail_frequency								= 4;
				Stop_Trigger								= 0;
				
				Stop_Price									= 0;
				Target_Price								= 0;

			//	Order management 4 AutoBotConf
//				AutoBotConf									= false;
				BotConfTrending 							= true;
				Target										= 90;
				Stop										= 55;
				TargetReduced								= 20;
				StopReduced									= 20;
				autoRS										= false;
				autoRV										= false;

				// MRZDetection
			//	lastRectangle			 					= null;
				lastRegion									= null;
				useMRZ										= false;		
				
				iBarsSinceExitExecution 					= -1;
				iBarsSinceEntryExecution 					= 1;
				iMinutes 									= 200; 
				
				// XBars Continuation Signals
				useXBarsContSignal			= true;
				xBarsCounter				= 2;
				longCounter 				= 0;
				shortCounter				= 0;				
				
				// Options Levels Filter (future use)
				useOLFilter					= false;
				
				// Trend filters	
				Trend 						= 0;
				lastTrend					= 0;
				
				//Set this scripts Print() calls to the second output tab
				PrintTo 					= PrintTo.OutputTab2;
				
				stopLossType = CommonEnumsVGA.StopLossType.Fixed;
				StopLoss_ATR_Period = 14;
				StopLoss_ATR_Mult	= 2;
				showFixedStopLossOptions = false;
				showATRStopLossOptions = false;
					
				profitTargetType = CommonEnumsVGA.ProfitTargetType.Fixed;
		//		ProfitTarget_ATR_Period = 14;
		//		ProfitTarget_ATR_Mult	= 2;
				showFixedProfitTargetOptions = false;
				showATRProfitTargetOptions = false;
					
				stopLossPriceLong = InitialStop;
				stopLossPriceShort = InitialStop;
		//		profitTargetPriceLong = ProfitTargetLong;
		//		profitTargetPriceShort = ProfitTargetShort;				
				
			
				#endregion
			}
			else if (State == State.Configure)
			{
//				AddDataSeries(Data.BarsPeriodType.Second, 1);
				initializeIndicators();
				if (stopLossType == CommonEnumsVGA.StopLossType.ATR) {
					StopLoss_ATR = ATRTrailBands(StopLoss_ATR_Period, StopLoss_ATR_Mult);
				}
					
//				if (profitTargetType == CommonEnumsVGA.ProfitTargetType.ATR) {
//					ProfitTarget_ATR = ATRTrailBands(ProfitTarget_ATR_Period, ProfitTarget_ATR_Mult);
//					Runner_ATR = ATRTrailBands(ProfitTarget_ATR_Period, Runner_Mult);
//				}
				
//				if (trailStopType == CommonEnumsVGA.TrailStopType.ATRTrail) {
//					TrailStop_ATR = ATRTrailBands(TrailStop_ATR_Period, TrailStop_ATR_Mult);
//				}				
				
				
				ClearOutputWindow(); 
			}
			
			else if (State == State.Realtime)
            {
                // one time only, as we transition from historical
                // convert any old historical order object references
                // to the new live order submitted to the real-time account
                if (entryOrder != null)
                    entryOrder = GetRealtimeOrder(entryOrder);
                if (stopOrder != null)
                    stopOrder = GetRealtimeOrder(stopOrder);
                if (targetOrder != null)
                    targetOrder = GetRealtimeOrder(targetOrder);		
			}
			
			else if (State == State.DataLoaded)
			{				
				ClearOutputWindow();
				myDbl = Instrument.MasterInstrument.PointValue * Instrument.MasterInstrument.TickSize;			
			
			}

			else if (State == State.Historical)
			{
				#region Chart Trader Buttons Load
				
				Dispatcher.InvokeAsync((() => {	CreateWPFControls();	}));
										
				#endregion
				if (Calculate != Calculate.OnBarClose)
				{
				//	Enable MRZBtn
					isMRZBtnEnabled = true;	
				}	
				
				
			}
			
			else if (State == State.Terminated)
			{
				ChartControl?.Dispatcher.InvokeAsync(() =>	{	DisposeWPFControls();	});
			}
				
		}  // End OnStateChanged 
		#endregion
		
		public override string DisplayName
		{
        	get { return StrategyName; }
		}
		
		#region BotConf	
		protected void BotConfTrendingButton_Click(object sender, EventArgs e)
		{

			if (!this.BotConfTrending)
			{
				this.BotConfTrending = true;
				this.BotConfTrendingButton.Content = "BotConf: Trending";
				this.BotConfTrendingButton.Background = Brushes.ForestGreen;
				this.BotConfTrendingButton.Foreground = Brushes.Black;					         			
				PresentaDisplayText();
				return;				
			}
			this.BotConfTrending = false;
			this.BotConfTrendingButton.Content = "BotConf: Balanced";
			this.BotConfTrendingButton.Background = Brushes.LightGray;
			this.BotConfTrendingButton.Foreground = Brushes.Black; 
			PresentaDisplayText();
			
		}		
	
		protected void BotConfTrendingButton_Decore()
		{

			if (this.BotConfTrending)
			{
				this.BotConfTrendingButton.Content = "BotConf: Trending";
				this.BotConfTrendingButton.Background = Brushes.ForestGreen;
				this.BotConfTrendingButton.Foreground = Brushes.Black;					
				return;				
			}
			this.BotConfTrendingButton.Content = "BotConf: Balanced";
			this.BotConfTrendingButton.Background = Brushes.LightGray;
			this.BotConfTrendingButton.Foreground = Brushes.Black;			
		}		
		#endregion

		#region Buy Market Button 
					
		protected void longButtonMarketClick(object sender, RoutedEventArgs e)
		{
			ForceRefresh();
			
			if (Position.MarketPosition == MarketPosition.Flat)
			{
				InitializeOperation();
				Target	= (BotConfTrending ? meProfitTarget : TargetReduced);
				Stop	= (BotConfTrending ? meInitialStop : StopReduced);		
				Target_Price	= Close[0] + (Target * TickSize);
				BE_Price		= Stop_Price = (stopLossType == CommonEnumsVGA.StopLossType.ATR ? StopLoss_ATR.TrailingStopLow[0] : Close[0] - (Stop * TickSize));
				Stop_Trigger	= Close[0] + (Trail_Trigger * TickSize);
				BEPrice_Trigger	= Close[0] + (BE_Trigger * TickSize);	
				Print(String.Format("{0} {1} {2} >>>  Market|REV Target -> {3}  Stop -> {4}  TargetPrice -> {5}  Stop_Price -> {6}  Close[0] {7}", Times[0][0].TimeOfDay, "Enter LongP -> LMk1", "2", Target, Stop, Target_Price, Stop_Price, Close[0]));
				SubmitLongOrder( CustomPositionAmount, "LMk1");
				activeLongMk1 = true;
				activeOrder = true;
			}		
			return;			
			
		}
		
		#endregion
		
		#region Sell Market Button 
		
		protected void shortButtonMarketClick(object sender, RoutedEventArgs e)
		{
			ForceRefresh();
			
			if (Position.MarketPosition == MarketPosition.Flat)
			{
				InitializeOperation();
				Target	= (BotConfTrending ? meProfitTarget : TargetReduced);
				Stop	= (BotConfTrending ? meInitialStop : StopReduced);			
				Target_Price	= Close[0] - (Target * TickSize);
				BE_Price		= Stop_Price = (stopLossType == CommonEnumsVGA.StopLossType.ATR ? StopLoss_ATR.TrailingStopHigh[0] : Close[0] + (Stop * TickSize));
				Stop_Trigger	= Close[0] - (Trail_Trigger * TickSize);
				BEPrice_Trigger	= Close[0] - (BE_Trigger * TickSize);
				Print(String.Format("{0} {1} {2} >>> Market|REV Target -> {3}  Stop -> {4}  TargetPrice -> {5}  Stop_Price -> {6}  Close[0] {7}", Times[0][0].TimeOfDay, "Enter ShortP -> SMk1", "-2", Target, Stop, Target_Price, Stop_Price, Close[0]));			
				SubmitShortOrder( CustomPositionAmount, "SMk1");
				activeShortMk1 = true;
				activeOrder = true;
			}	
			return;
		}		
		
		#endregion	
				
		#region Close Position Button 
		
		protected void closeButtonClick(object sender, RoutedEventArgs e)
		{
			ForceRefresh();
			CloseAllOperation("CMTotal");
		}		
		
		private void CloseAllOperation(string txtSignal)
		{	
		//  Access the open position
        	Position openPosition = Position;

        	if (openPosition != null && openPosition.MarketPosition != MarketPosition.Flat)
        	{
				currentPosition = Position.Quantity;

				if (stopOrder != null) 
				{
					if (Position.MarketPosition == MarketPosition.Long)
					{
						SubmitShortOrder( iCurrentPosition, txtSignal);		// Change from currentPosition to iCurrentPosition
					}
					else if (Position.MarketPosition == MarketPosition.Short)
					{
						SubmitLongOrder( iCurrentPosition, txtSignal);		// Change from currentPosition to iCurrentPosition
					}	 
				}
         	}
         	CancelOrder(stopOrder);
         	CancelOrder(targetOrder);
			/*  Pasar a null todos los orderstop y profit activos en ese momento, ya que al hacer el close no se pasan a null de forma automática */
			if (stopOrder != null) stopOrder = null;
            if (targetOrder != null) targetOrder = null;	
			activeLongMk1 = false;
			activeShortMk1 = false;
			activeLongAE = false;
			activeShortAE = false;
			ResetOperation();
		}
		
		#endregion 
			
		#region Add1 Button 
		
		protected void add1ButtonClick(object sender, RoutedEventArgs e)
		{

			add1ButtonClicked = true;				

			if (stopOrder != null && (activeLongMk1 || activeShortMk1)) 
			{
				if (Position.MarketPosition == MarketPosition.Long)
				{
					if (iCurrentPosition + 1 <= MaxPositionPerTrade)		// Change from currentPosition to iCurrentPosition
					SubmitLongOrder( 1, "LMk1");
				}
				else if (Position.MarketPosition == MarketPosition.Short)
				{
					if (iCurrentPosition + 1 <= MaxPositionPerTrade)		// Change from currentPosition to iCurrentPosition
					SubmitShortOrder( 1, "SMk1");
				} 
				return;
			}
		
			if (stopOrder != null && (activeLongAE || activeShortAE))
			{
				if (Position.MarketPosition == MarketPosition.Long)
				{
					if (iCurrentPosition + 1 <= MaxPositionPerTrade)		// Change from currentPosition to iCurrentPosition
					SubmitLongOrder( 1, "LAE");
				}	
				else if (Position.MarketPosition == MarketPosition.Short)
				{
					if (iCurrentPosition + 1 <= MaxPositionPerTrade)		// Change from currentPosition to iCurrentPosition
					SubmitShortOrder( 1, "SAE");
				} 
				return;
			}	
		
			return;
		}

		
		#endregion  
		
		#region Close1 Button 
		
		protected void close1ButtonClick(object sender, RoutedEventArgs e)
		{

			close1ButtonClicked = true;				
			add1ButtonClicked = false;
			
			if (stopOrder != null) 
			{
				if (Position.MarketPosition == MarketPosition.Long)
				{
					SubmitShortOrder( -1, "CP1C");
				}
				else if (Position.MarketPosition == MarketPosition.Short)
				{
					SubmitLongOrder( -1, "CP1C");
				}	 
			}
						
			return;
			
		}		
		
		#endregion 		
		
		#region Add2 Button
		
		protected void add2ButtonClick(object sender, RoutedEventArgs e)
		{

			add2ButtonClicked = true;
				
			if (stopOrder != null && (activeLongMk1 || activeShortMk1))
			{
				if (Position.MarketPosition == MarketPosition.Long)
				{
					if (iCurrentPosition + 2 <= MaxPositionPerTrade)		// Change from currentPosition to iCurrentPosition
					SubmitLongOrder( 2, "LMk1");
				}
				else if (Position.MarketPosition == MarketPosition.Short)
				{
					if (iCurrentPosition + 2 <= MaxPositionPerTrade)		// Change from currentPosition to iCurrentPosition
					SubmitShortOrder( 2, "SMk1");
				} 
				return;
			}
							
			if (stopOrder != null && ( activeLongAE || activeShortAE))
			{
				if (Position.MarketPosition == MarketPosition.Long)
				{
					if (iCurrentPosition + 2 <= MaxPositionPerTrade)		// Change from currentPosition to iCurrentPosition
					SubmitLongOrder( 2, "LAE");
				}	
				else if (Position.MarketPosition == MarketPosition.Short)
				{
					if (iCurrentPosition + 2 <= MaxPositionPerTrade)		// Change from currentPosition to iCurrentPosition
					SubmitShortOrder( 2, "SAE");
				} 
				return;
			}		
			
			return;

		}		
		
		#endregion
		
		#region Close2 Button 
		
		protected void close2ButtonClick(object sender, RoutedEventArgs e)
		{

			close2ButtonClicked = true;				
			add2ButtonClicked = false;

			if (stopOrder != null) 
			{
				if (Position.MarketPosition == MarketPosition.Long)
				{
					SubmitShortOrder( -2, "CP2C");
				}
				else if (Position.MarketPosition == MarketPosition.Short)
				{
					SubmitLongOrder( -2, "CP2C");
				}	 
			}

			return;
			
		}		
		
		#endregion 					
			
		#region DecoreButton
		
		protected void DecoreDisabledButtons(System.Windows.Controls.Button myButton, string stringButton)
		{
			myButton.Content = stringButton;
			myButton.Background = Brushes.Firebrick;
			myButton.BorderBrush = Brushes.Black;
			myButton.Foreground = Brushes.White;
			return;
		}

		protected void DecoreEnabledButtons(System.Windows.Controls.Button myButton, string stringButton)
		{
			myButton.Content = stringButton;
			myButton.Background = Brushes.ForestGreen;
			myButton.BorderBrush = Brushes.Black;
			myButton.Foreground = Brushes.Black;
			return;
		}

		protected void DecoreNeutralButtons(System.Windows.Controls.Button myButton, string stringButton)
		{
			myButton.Content = stringButton;
			myButton.Background = Brushes.LightGray;
			myButton.BorderBrush = Brushes.Black;
			myButton.Foreground = Brushes.Black;
			return;
		}

		protected void DecoreGrayButtons(System.Windows.Controls.Button myButton, string stringButton)
		{
			myButton.Content = stringButton;
			myButton.Background = Brushes.DarkGray;
			myButton.BorderBrush = Brushes.Black;
			myButton.Foreground = Brushes.Black;
			return;
		}		
		
		
		#endregion
		
		#region Create WPF Controls
		protected void CreateWPFControls()
		{
			//	ChartWindow
			chartWindow	= System.Windows.Window.GetWindow(ChartControl.Parent) as Gui.Chart.Chart;
			
			// if not added to a chart, do nothing
			if (chartWindow == null)
				return;

			// this is the entire chart trader area grid
			chartTraderGrid			= (chartWindow.FindFirst("ChartWindowChartTraderControl") as Gui.Chart.ChartTrader).Content as System.Windows.Controls.Grid;
			
			// this grid contains the existing chart trader buttons
			chartTraderButtonsGrid	= chartTraderGrid.Children[0] as System.Windows.Controls.Grid;
			
			CreateButtons();

			// this grid is to organize stuff below
			lowerButtonsGrid = new System.Windows.Controls.Grid();
			
			// Initialize
    		InitializeButtonGrid();

			addedRow	= new System.Windows.Controls.RowDefinition() { Height = new GridLength(250) };
			
    		// SetButtons
    		SetButtonLocations();

    		// AddButtons
    		AddButtonsToGrid();			
				
			if (TabSelected())
				InsertWPFControls();

			chartWindow.MainTabControl.SelectionChanged += TabChangedHandler;

		}
		
		private void CreateButtons()
		{	
		
			#region Button Content
			
			// this style (provided by NinjaTrader_MichaelM) gives the correct default minwidth (and colors) to make buttons appear like chart trader buttons
			Style basicButtonStyle	= System.Windows.Application.Current.FindResource("BasicEntryButton") as Style;			
	
			useAutoButton = new System.Windows.Controls.Button
			{		
				Content			= "\uD83D\uDD12", Height = 25, Margin = new Thickness(1,0,1,0),	Padding	= new Thickness(0,0,0,0), Style	= basicButtonStyle, BorderThickness = new Thickness(1.5), IsEnabled = true,
					ToolTip = "Enable (Green) / Disbled (Red) Strategy"
			};	
			if (useAuto) DecoreEnabledButtons(useAutoButton, "\uD83D\uDD12");
			if (!useAuto) DecoreDisabledButtons(useAutoButton, "\uD83D\uDD13");
			useAutoButton.Click +=  OnButtonClick;
			
			useLongButton = new System.Windows.Controls.Button
			{		
				Content			= "\uD83D\uDD12 ⬆", Height = 25, Margin = new Thickness(1,0,1,0), Padding	= new Thickness(0,0,0,0), Style	= basicButtonStyle, BorderThickness = new Thickness(1.5), IsEnabled = true,
					ToolTip = "Enable (Green) / Disbled (Red) Auto Long Entry"
			};	
			if (useLong) DecoreEnabledButtons(useLongButton, "\uD83D\uDD12 ⬆");
			if (!useLong) DecoreDisabledButtons(useLongButton, "\uD83D\uDD13 ⬆");	
			useLongButton.Click += OnButtonClick;
			
			useShortButton = new System.Windows.Controls.Button
			{		
				Content			= "\uD83D\uDD12 ⬇", Height = 25, Margin	= new Thickness(1,0,1,0), Padding = new Thickness(0,0,0,0), Style = basicButtonStyle, BorderThickness = new Thickness(1.5), IsEnabled = true,
					ToolTip = "Enable (Green) / Disbled (Red) Auto Short Entry"
			};	
			if (useShort) DecoreEnabledButtons(useShortButton, "\uD83D\uDD12 ⬇");
			if (!useShort) 	DecoreDisabledButtons(useShortButton, "\uD83D\uDD13 ⬇");	
			useShortButton.Click += OnButtonClick;			
			
			longButtonMarket = new System.Windows.Controls.Button
			{				
				Content			= string.Format("⬆ BMarket"), Height = 25, Margin	= new Thickness(1,0,1,0), Padding = new Thickness(0,0,0,0), Style = basicButtonStyle, BorderThickness = new Thickness(1.5), IsEnabled = true,
					ToolTip = "Manual Buy Market Entry (Long)"
			};		
			longButtonMarket.Background	= Brushes.MediumSeaGreen;
			longButtonMarket.BorderBrush	= Brushes.Black;	
			longButtonMarket.Foreground    = Brushes.Black;	
			longButtonMarket.Click +=  longButtonMarketClick;
				
			shortButtonMarket = new System.Windows.Controls.Button
			{				
				Content			= string.Format("⬇ SMarket"),	Height	= 25, Margin = new Thickness(1,0,1,0), Padding = new Thickness(0,0,0,0), Style = basicButtonStyle, BorderThickness = new Thickness(1.5), IsEnabled = true,
					ToolTip = "Manual Sell Market Entry (Short)"
			};			
			shortButtonMarket.Background	= Brushes.IndianRed;
			shortButtonMarket.BorderBrush	= Brushes.Black;	
			shortButtonMarket.Foreground    = Brushes.White;	
			shortButtonMarket.Click +=  shortButtonMarketClick;
			
			autoBEButton = new System.Windows.Controls.Button
			{		
				Content			= "\uD83D\uDD12 BE",	Height = 25, Margin = new Thickness(1,0,1,0), Padding = new Thickness(0,0,0,0),	Style = basicButtonStyle, BorderThickness = new Thickness(1.5), IsEnabled = true,
					ToolTip = "Enable (Green) / Disbled (Red) Auto BreakEven"	
			};
			if (BESetAuto) DecoreEnabledButtons(autoBEButton, "\uD83D\uDD12 BE");
			if (!BESetAuto) DecoreDisabledButtons(autoBEButton, "\uD83D\uDD13 BE");
			autoBEButton.Click +=  OnButtonClick;
			
			autoTSButton = new System.Windows.Controls.Button
			{		
				Content			= "\uD83D\uDD12 TS",	Height = 25, Margin	= new Thickness(1,0,1,0), Padding = new Thickness(0,0,0,0), Style = basicButtonStyle, BorderThickness = new Thickness(1.5), IsEnabled = true,
					ToolTip = "Enable (Green) / Disbled (Red) Auto Trailing Stop"
			};
			if (TrailSetAuto)	DecoreEnabledButtons(autoTSButton, "\uD83D\uDD12 TS");
			if (!TrailSetAuto)		DecoreDisabledButtons(autoTSButton, "\uD83D\uDD13 TS");		
			autoTSButton.Click +=  OnButtonClick;
			
			closeButton = new System.Windows.Controls.Button
			{				
				Content			= string.Format("Flatten All"), Height = 25, Margin	= new Thickness(1,0,1,0), Padding = new Thickness(0,0,0,0),	Style = basicButtonStyle, BorderThickness = new Thickness(1.5), IsEnabled = true,
					ToolTip = "Close all open positions"
					
			};		
			closeButton.Background	= Brushes.DarkOrange;
			closeButton.BorderBrush	= Brushes.Black;	
			closeButton.Foreground  = Brushes.Black;	
			closeButton.Click +=  closeButtonClick;
						
			add1Button = new System.Windows.Controls.Button
			{			
				Content			= "Add 1",	Height = 25, Margin	= new Thickness(1,0,1,0), Padding = new Thickness(0,0,0,0),	Style = basicButtonStyle, BorderThickness = new Thickness(1.5), IsEnabled = true,
					ToolTip = "Add 1 contract to actual position"
			};		
			add1Button.Background	= Brushes.White;
			add1Button.BorderBrush	= Brushes.Black;	
			add1Button.Foreground   = Brushes.Black;	
			add1Button.Click +=  add1ButtonClick;	
			
			add2Button = new System.Windows.Controls.Button
			{				
				Content			= "Add 2", Height = 25, Margin = new Thickness(1,0,1,0), Padding = new Thickness(0,0,0,0), Style = basicButtonStyle, BorderThickness = new Thickness(1.5), IsEnabled = true,
					ToolTip = "Add 2 contract to actual position"
			};			
			add2Button.Background	= Brushes.White;
			add2Button.BorderBrush	= Brushes.Black;	
			add2Button.Foreground   = Brushes.Black;	
			add2Button.Click +=  add2ButtonClick;	
			
			close1Button = new System.Windows.Controls.Button
			{			
				Content			= "Close 1", Height	= 25, Margin = new Thickness(1,0,1,0), Padding = new Thickness(0,0,0,0), Style = basicButtonStyle, BorderThickness = new Thickness(1.5), IsEnabled = true,
					ToolTip = "Close 1 contract from actual position"
			};		
			close1Button.Background	= Brushes.Black;
			close1Button.BorderBrush = Brushes.White;	
			close1Button.Foreground = Brushes.White;	
			close1Button.Click +=  close1ButtonClick;
			
			close2Button = new System.Windows.Controls.Button
			{				
				Content			= "Close 2", Height = 25, Margin = new Thickness(1,0,1,0), Padding = new Thickness(0,0,0,0), Style = basicButtonStyle, BorderThickness = new Thickness(1.5), IsEnabled = true,
					ToolTip = "Close 2 contract from actual position"
			};				
			close2Button.Background	= Brushes.Black;
			close2Button.BorderBrush	= Brushes.White;	
			close2Button.Foreground    = Brushes.White;	
			close2Button.Click +=  close2ButtonClick;				
		
			mrzBtn = new System.Windows.Controls.Button
			{		
				Content			= "\uD83D\uDD12 MRZ", Height	= 25, Margin = new Thickness(1,0,1,0), Padding = new Thickness(0,0,0,0), Style = basicButtonStyle, BorderThickness = new Thickness(1.5), IsEnabled = true,
					ToolTip = "Enable (Green) / Disbled (Red) Manual Range Zone Detection"
			};
			if (isMRZBtnEnabled)
			{
				if (useMRZ)	DecoreEnabledButtons(mrzBtn, "\uD83D\uDD12 MRZ");
				if (!useMRZ) DecoreDisabledButtons(mrzBtn, "\uD83D\uDD13 MRZ");	
			}
			if (!isMRZBtnEnabled) DecoreGrayButtons(mrzBtn, "\uD83D\uDD13 MRZ");
			mrzBtn.Click += OnButtonClick;
			
			displayButton = new System.Windows.Controls.Button
				{		
					Content			= "\uD83D\uDCBB On", Height = 25, Margin = new Thickness(1,0,1,0),	Padding	= new Thickness(0,0,0,0), Style	= basicButtonStyle, BorderThickness = new Thickness(1.5), IsEnabled = true,
						ToolTip = "Enable (Black) / Disbled (White) Show Panel Infos"
				};						
			if (DisplayText)
			{
				displayButton.Content		= "\uD83D\uDCBB Off";
				displayButton.Background	= Brushes.Black;
				displayButton.BorderBrush	= Brushes.Black;	
				displayButton.Foreground    = Brushes.White;	
			}
				
			if (!DisplayText)
			{
				displayButton.Content		= "\uD83D\uDCBB On";
				displayButton.Background	= Brushes.White;
				displayButton.BorderBrush	= Brushes.Black;	
				displayButton.Foreground    = Brushes.Black;	
			}
			displayButton.Click +=  OnButtonClick;
			
			panicBtn = new System.Windows.Controls.Button
			{
				Name = "PanicButton", Content = "\u2620", Foreground = Brushes.Black, Background = Brushes.Goldenrod, Height = 25, Margin = new Thickness(1,0,1,0), Padding = new Thickness(0,0,0,0), Style = basicButtonStyle, BorderThickness = new Thickness(1.5), IsEnabled = true,
					ToolTip = "PanicBtn: CloseAllPosiions"
			};
        	panicBtn.Click += OpenFormButton_Click;

			BotConfTrendingButton = new System.Windows.Controls.Button
			{
				Name = "BotConfTrending", Content = "BotConf: Balanced", Foreground = Brushes.Black, Background = Brushes.LightGray, Height = 25, Margin = new Thickness(1,0,1,0),	Padding	= new Thickness(0,0,0,0), Style	= basicButtonStyle, BorderThickness = new Thickness(1.5), IsEnabled = true,
					ToolTip = "BotConf Setting: Trending --> large stop/profit, Balanced --> reduced stop/profit (see params)"
			};
        	BotConfTrendingButton.Click += BotConfTrendingButton_Click;
			if (BotConfTrending)	DecoreEnabledButtons(BotConfTrendingButton, "BotConf: Trending");           			
			if (!BotConfTrending)	DecoreNeutralButtons(BotConfTrendingButton, "BotConf: Balanced"); 
			
			autoRSButton = new System.Windows.Controls.Button
			{		
				Content			= "\uD83D\uDD12 RS", Height = 25, Margin = new Thickness(1,0,1,0), Padding	= new Thickness(0,0,0,0), Style	= basicButtonStyle, BorderThickness = new Thickness(1.5), IsEnabled = true,
					ToolTip = "Enable (Green) / Disbled (Red) Auto Close order when reversal signal is detected"
			};	
			if (autoRS) DecoreEnabledButtons(autoRSButton, "\uD83D\uDD12 RS");
			if (!autoRS) DecoreDisabledButtons(autoRSButton, "\uD83D\uDD13 RS");	
			autoRSButton.Click += OnButtonClick;
			
			autoRVButton = new System.Windows.Controls.Button
			{		
				Content			= "\uD83D\uDD12 ReV", Height = 25, Margin = new Thickness(1,0,1,0), Padding	= new Thickness(0,0,0,0), Style	= basicButtonStyle, BorderThickness = new Thickness(1.5), IsEnabled = true,
					ToolTip = "Enable (Green) / Disbled (Red) Auto Reverse Position when reversal signal is detected"
			};	
			if (autoRV) DecoreEnabledButtons(autoRVButton, "\uD83D\uDD12 ReV");
			if (!autoRV) DecoreDisabledButtons(autoRVButton, "\uD83D\uDD13 ReV");	
			autoRVButton.Click += OnButtonClick;	
			
			barsButton = new System.Windows.Controls.Button
			{		
				Content			= "\uD83D\uDD12 XBar", Height = 25, Margin = new Thickness(1,0,1,0), Padding	= new Thickness(0,0,0,0), Style	= basicButtonStyle, BorderThickness = new Thickness(1.5), IsEnabled = true,
					ToolTip = "Enable (Green) / Disbled (Red) X bars continuation signal"
			};	
			if (useXBarsContSignal) DecoreEnabledButtons(barsButton, "\uD83D\uDD12 XBar");
			if (!useXBarsContSignal) DecoreDisabledButtons(barsButton, "\uD83D\uDD13 XBar");	
			barsButton.Click += OnButtonClick;					

			optLvlButton = new System.Windows.Controls.Button
			{		
				Content			= "\uD83D\uDD12 OLF", Height = 25, Margin = new Thickness(1,0,1,0), Padding	= new Thickness(0,0,0,0), Style	= basicButtonStyle, BorderThickness = new Thickness(1.5), IsEnabled = true,
					ToolTip = "Enable (Green) / Disbled (Red) Options Levels for filter entries (future use, not implemented)"
			};	
			if (useOLFilter) DecoreEnabledButtons(optLvlButton, "\uD83D\uDD12 OLF");
			if (!useOLFilter) DecoreDisabledButtons(optLvlButton, "\uD83D\uDD13 OLF");
			optLvlButton.Click += OnButtonClick;
			
			
			#endregion		
			
		}	
		
		private void InitializeButtonGrid()
		{
    		// Crea un nuevo grid para organizar los botones
    		lowerButtonsGrid = new System.Windows.Controls.Grid();

    		// Define las columnas
    		for (int i = 0; i < 4; i++)
    		{
        		lowerButtonsGrid.ColumnDefinitions.Add(new System.Windows.Controls.ColumnDefinition());
    		}

    		// Define las filas
    		for (int i = 0; i <= 8; i++)
    		{
        		lowerButtonsGrid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition());
    		}
		}		
		
		private void SetButtonLocations()
		{
    		SetButtonLocation(useAutoButton, 0, 1);
    		SetButtonLocation(useLongButton, 1, 1);
    		SetButtonLocation(useShortButton, 2, 1);
    		SetButtonLocation(displayButton, 3, 1);
    		SetButtonLocation(longButtonMarket, 0, 2, 2); // Columna 0, fila 2, ocupa dos columnas
    		SetButtonLocation(shortButtonMarket, 2, 2, 2); // Columna 2, fila 2, ocupa dos columnas
    		SetButtonLocation(add1Button, 0, 3);
    		SetButtonLocation(close1Button, 1, 3);
    		SetButtonLocation(add2Button, 2, 3);
    		SetButtonLocation(close2Button, 3, 3);
    		SetButtonLocation(closeButton, 0, 4, 4); // Cerrar todas las posiciones
    		SetButtonLocation(autoBEButton, 0, 5);
    		SetButtonLocation(autoTSButton, 1, 5);
    		SetButtonLocation(mrzBtn, 2, 5);
    		SetButtonLocation(panicBtn, 3, 5);
    		SetButtonLocation(autoRSButton, 0, 6);
    		SetButtonLocation(autoRVButton, 1, 6);
    		SetButtonLocation(barsButton, 2, 6);
    		SetButtonLocation(optLvlButton, 3, 6);
    		SetButtonLocation(BotConfTrendingButton, 0, 7, 4); // Ocupa cuatro columnas			
			
		}
		
		// Método genérico para establecer la ubicación de un botón
		private void SetButtonLocation(System.Windows.Controls.Button button, int column, int row, int columnSpan = 1)
		{
    		System.Windows.Controls.Grid.SetColumn(button, column);
    		System.Windows.Controls.Grid.SetRow(button, row);
    
   			if (columnSpan > 1)
        		System.Windows.Controls.Grid.SetColumnSpan(button, columnSpan);
		}		
		
		private void AddButtonsToGrid()
		{
    		// Añadir todos los botones al grid
    		lowerButtonsGrid.Children.Add(useAutoButton);
    		lowerButtonsGrid.Children.Add(useLongButton);
    		lowerButtonsGrid.Children.Add(useShortButton);
    		lowerButtonsGrid.Children.Add(displayButton);
    		lowerButtonsGrid.Children.Add(longButtonMarket);
    		lowerButtonsGrid.Children.Add(shortButtonMarket);
    		lowerButtonsGrid.Children.Add(add1Button);
    		lowerButtonsGrid.Children.Add(close1Button);
    		lowerButtonsGrid.Children.Add(add2Button);
    		lowerButtonsGrid.Children.Add(close2Button);
    		lowerButtonsGrid.Children.Add(closeButton);
    		lowerButtonsGrid.Children.Add(autoBEButton);
    		lowerButtonsGrid.Children.Add(autoTSButton);
    		lowerButtonsGrid.Children.Add(mrzBtn);
    		lowerButtonsGrid.Children.Add(panicBtn);
    		lowerButtonsGrid.Children.Add(autoRSButton);
    		lowerButtonsGrid.Children.Add(autoRVButton);
    		lowerButtonsGrid.Children.Add(barsButton);
    		lowerButtonsGrid.Children.Add(optLvlButton);
    		lowerButtonsGrid.Children.Add(BotConfTrendingButton);
		}		
		
		
		#endregion
		
		#region ToggleButton Click Events
		
		private void OnButtonClick(object sender, RoutedEventArgs rea)
		{
			System.Windows.Controls.Button button = sender as System.Windows.Controls.Button;
								
			if (button == useAutoButton)
			{	
				useAuto = !useAuto;
				if (useAuto) DecoreEnabledButtons(useAutoButton, "\uD83D\uDD12");
				if (!useAuto) DecoreDisabledButtons(useAutoButton, "\uD83D\uDD13"); 
				return;
			}
		
			if (button == autoBEButton)
			{	
				BESetAuto = !BESetAuto;
				if (BESetAuto) DecoreEnabledButtons(autoBEButton, "\uD83D\uDD12 BE");
				if (!BESetAuto) DecoreDisabledButtons(autoBEButton, "\uD83D\uDD13 BE"); 
				return;
			}			
			//autoBEButton, autoTSButton;
			if (button == autoTSButton)
			{	
				TrailSetAuto = !TrailSetAuto;
				if (TrailSetAuto) DecoreEnabledButtons(autoTSButton, "\uD83D\uDD12 TS");
				if (!TrailSetAuto) DecoreDisabledButtons(autoTSButton, "\uD83D\uDD13 TS"); 
				return;
			}
			
			if (button == useLongButton)
			{	
				useLong = !useLong;
				if (useLong) DecoreEnabledButtons(useLongButton, "\uD83D\uDD12 ⬆");
				if (!useLong) DecoreDisabledButtons(useLongButton, "\uD83D\uDD13 ⬆"); 
				return;
			}			

			if (button == useShortButton)
			{	
				useShort = !useShort;
				if (useShort) DecoreEnabledButtons(useShortButton, "\uD83D\uDD12 ⬇");
				if (!useShort) DecoreDisabledButtons(useShortButton, "\uD83D\uDD13 ⬇"); 
				return;
			}							
				
			if (button == displayButton)
			{	
				DisplayText = !DisplayText;
				if (DisplayText) 
				{
					DecoreEnabledButtons(displayButton, "\uD83D\uDCBB Off");
					displayButton.Foreground = Brushes.White; 
					displayButton.Background = Brushes.Black;
					PresentaDisplayText();
				}
				if (!DisplayText) 
				{
					DecoreDisabledButtons(displayButton, "\uD83D\uDCBB On"); 
					displayButton.Foreground = Brushes.Black; 
					displayButton.Background = Brushes.White;
					if (DisplayMRZInfo) RemoveDrawObject("MRZBox");
					if (ShowHistorical) RemoveDrawObject("tradePerformanceText");
					if (DisplayStrategyPnL) RemoveDrawObject("realTimeTradeText");
				}	
				return;
			}				
					
			if (button == mrzBtn)
			{	
				if (isMRZBtnEnabled)
				{	
					useMRZ = !useMRZ;
					if (useMRZ) DecoreEnabledButtons(mrzBtn, "\uD83D\uDD12 MRZ");
					if (!useMRZ) DecoreDisabledButtons(mrzBtn, "\uD83D\uDD13 MRZ"); 
				}
				
				if (!useMRZ || !isMRZBtnEnabled)
				{
					MRZSignalUp = 0;    
					MRZSignalDw = 30000;
					activeMRZ = false;
				}
				return;
			}		
			
			if (button == autoRSButton)
			{	
				autoRS = !autoRS;
				if (autoRS) DecoreEnabledButtons(autoRSButton, "\uD83D\uDD12 RS");
				if (!autoRS) DecoreDisabledButtons(autoRSButton, "\uD83D\uDD13 RS"); 
				return;
			}
			
			if (button == autoRVButton)
			{	
				autoRV = !autoRV;
				if (autoRV) DecoreEnabledButtons(autoRVButton, "\uD83D\uDD12 ReV");
				if (!autoRV) DecoreDisabledButtons(autoRVButton, "\uD83D\uDD13 ReV"); 
				return;
			}	
			
			if (button == barsButton)
			{	
				useXBarsContSignal = !useXBarsContSignal;
				if (useXBarsContSignal) DecoreEnabledButtons(barsButton, "\uD83D\uDD12 XBar");
				if (!useXBarsContSignal) DecoreDisabledButtons(barsButton, "\uD83D\uDD13 XBar"); 
				return;
			}				

			if (button == optLvlButton)
			{	
				useOLFilter = !useOLFilter;
				if (useOLFilter) DecoreEnabledButtons(optLvlButton, "\uD83D\uDD12 OLF");
				if (useOLFilter) DecoreDisabledButtons(optLvlButton, "\uD83D\uDD13 OLF"); 
				return;
			}					
			
			
		}
		
		
		#endregion
		
		#region Dispose
		public void DisposeWPFControls() 
		{
			
			
			if (chartWindow != null)
				chartWindow.MainTabControl.SelectionChanged -= TabChangedHandler;

			if (useAutoButton != null)
				useAutoButton.Click -= OnButtonClick;
						
			if (useLongButton != null)
				useLongButton.Click -= OnButtonClick;

			if (useShortButton != null)
				useShortButton.Click -= OnButtonClick;			
			
			if (longButtonMarket != null)
				longButtonMarket.Click -= longButtonMarketClick;
			
			if (shortButtonMarket != null)
				shortButtonMarket.Click -= shortButtonMarketClick;

			if (autoBEButton != null)
				autoBEButton.Click -= OnButtonClick;

			if (autoTSButton != null)
				autoTSButton.Click -= OnButtonClick;
			
			if (closeButton != null)
				closeButton.Click -= closeButtonClick;	
			
			if (add1Button != null)
				add1Button.Click -= add1ButtonClick;
			
			if (add2Button != null)
				add2Button.Click -= add2ButtonClick;

			if (close1Button != null)
				close1Button.Click -= close1ButtonClick;
			
			if (close2Button != null)
				close2Button.Click -= close2ButtonClick;

			if (mrzBtn != null)
				mrzBtn.Click -= OnButtonClick;
			
//			if (dayTrendButton != null)
//				dayTrendButton.Click -= dayTrendButtonClick;
			
			if (displayButton != null)
				displayButton.Click -= OnButtonClick;
			
			if (panicBtn != null)
				panicBtn.Click -= OpenFormButton_Click;
				
			if (BotConfTrendingButton != null)
				BotConfTrendingButton.Click -= BotConfTrendingButton_Click;
			
			if (autoRSButton != null)
				autoRSButton.Click -= OnButtonClick;

			if (autoRVButton != null)
				autoRVButton.Click -= OnButtonClick;
			
			if (barsButton != null)
				barsButton.Click -= OnButtonClick;					

			if (optLvlButton != null)
				optLvlButton.Click -= OnButtonClick;			
			
			
			
			RemoveWPFControls();
			
			
		}
		#endregion
		
		#region Insert WPF
		public void InsertWPFControls()
		{
			
			
			if (panelActive)
				return;
			
			// add a new row (addedRow) for our lowerButtonsGrid below the ask and bid prices and pnl display			
			chartTraderGrid.RowDefinitions.Add(addedRow);
			System.Windows.Controls.Grid.SetRow(lowerButtonsGrid, (chartTraderGrid.RowDefinitions.Count - 1));
			chartTraderGrid.Children.Add(lowerButtonsGrid);

			panelActive = true;
			
			
		}
		#endregion
		
		#region Remove WPF
		protected void RemoveWPFControls()
		{
			if (!panelActive)
				return;
			
			if (chartTraderButtonsGrid != null || lowerButtonsGrid != null)
			{
				chartTraderGrid.Children.Remove(lowerButtonsGrid);
				chartTraderGrid.RowDefinitions.Remove(addedRow);
			}

			panelActive = false;
		}
		#endregion
		
		#region TabSelcected 
		private bool TabSelected()
		{
			
			
			bool tabSelected = false;

			// loop through each tab and see if the tab this indicator is added to is the selected item
			foreach (System.Windows.Controls.TabItem tab in chartWindow.MainTabControl.Items)
				if ((tab.Content as Gui.Chart.ChartTab).ChartControl == ChartControl && tab == chartWindow.MainTabControl.SelectedItem)
					tabSelected = true;

			return tabSelected;
				
			
		}
		#endregion
		
		#region TabHandler
		private void TabChangedHandler(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
		{
			if (e.AddedItems.Count <= 0)
				return;

			tabItem = e.AddedItems[0] as System.Windows.Controls.TabItem;
			if (tabItem == null)
				return;

			chartTab = tabItem.Content as Gui.Chart.ChartTab;
			if (chartTab == null)
				return;

			if (TabSelected())
				InsertWPFControls();
			else
				RemoveWPFControls();
		}		
		#endregion
		
		#region OnRender
		
		protected override void OnRender(ChartControl chartControl, ChartScale chartScale)
        {
			try
            {
				if (!IsVisible)
				return;

				base.OnRender(chartControl, chartScale);
				
				if (ShowHistorical && DisplayText) {
					DrawHistoricalTradePerformance(chartControl);
				}			
				
				if (DisplayStrategyPnL && DisplayText) {
					DrawStrategyPnl(chartControl);
				}
			}
		
            catch (Exception ex)
            {
                Print("Exception in OnRender:" + ex.Message + " " + ex.StackTrace);  //log and rethrow
                throw;
            }
        }
		
		#endregion
		
		#region DetectRectangle(
		protected void DetectRectangle()
		{
			if (isMRZBtnEnabled && useMRZ)
			{
				int cont = 0;
	       		foreach(DrawingTool DrawObject in DrawObjects.Cast<DrawingTool>())
				{
					if(DrawObject.GetType().ToString().Contains("RegionHighlightY"))
					{
						cont += 1;
						lastRegion = (NinjaTrader.NinjaScript.DrawingTools.RegionHighlightY)DrawObject;		
					//	Print(String.Format("Event OnRender >>>>  {0}  >>>  {1}    is of type     {2}      ActiveMRZ?  {3}    BarsInProgress >>> {4} ", Times[0][0].TimeOfDay, DrawObject.Name, DrawObject.GetType(), activeMRZ, BarsInProgress));	
					}
				}
				if (cont > 0)
				{
					lastRegion.CalculateMinMax();
					double regionTop = Instrument.MasterInstrument.RoundToTickSize(lastRegion.MaxValue);
					double regionBottom = Instrument.MasterInstrument.RoundToTickSize(lastRegion.MinValue);					
					MRZSignalUp = regionTop;
					MRZSignalDw = regionBottom;
					activeMRZ = true;
					PresentaDisplayText();
				}
				else
				{
					lastRegion = null;	
				//	Print(String.Format("Event OnRender >>>>  {0}  >>>   ActiveMRZ?  {1}    BarsInProgress >>> {2} ", Times[0][0].TimeOfDay, activeMRZ, BarsInProgress));	
					MRZSignalUp = 0;    
					MRZSignalDw = 30000;
					activeMRZ = false;
					PresentaDisplayText();
				}					
			}
			
		}
		#endregion

		#region Methods
	
        protected abstract bool validateEntryLong(); 
        	
        protected abstract bool validateEntryShort();

        protected virtual bool validateExitLong() {
			return false;
		}

        protected virtual bool validateExitShort() {
			return false;
		}
		
		protected abstract void initializeIndicators();

		protected abstract void OpenFormButton_Click(object sender, EventArgs e);
		// cambiar a protected virtual si vamos a usar el formulario de cambio de targets y stops en todas las estrategias
		
		
		protected virtual void addDataSeries() {}
		
		protected virtual bool isCustomStopSet() {
			return false;
		}
		
		protected virtual bool isCustomProfitSet() {
			return false;
		}
		
		protected virtual double customStopLong() {
			return -1;
		}
		
		protected virtual double customStopShort() {
			return -1;
		}
		
		protected virtual double customProfitTargetLong(double price) {
			return -1;
		}
		
		protected virtual double customProfitTargetShort(double price) {
			return -1;
		}
		
		#endregion
				
		#region OnBarUpdate
		protected override void OnBarUpdate()
		{
							
//			if (BarsInProgress == 1) 
//			{
			if (useMRZ && State != State.Historical && isMRZBtnEnabled) DetectRectangle();
//				return;
//			}
			
			if (BarsInProgress != 0) return;
			
			if (Bars.IsLastBarOfSession){
					Print(Times[0][0].TimeOfDay + " Reset Sessions Counters");
					 Session1Count = 0;
					 Session2Count = 0;
					 Session3Count = 0;
					 Session4Count = 0;
			}		
			


			if (! CanTrade)
				return;	
			
			if (Position.MarketPosition != MarketPosition.Flat)	UnrealizedPnL = Position.GetUnrealizedProfitLoss(PerformanceUnit.Currency, Close[0]);				
					
			if (useAuto)
			{
			//	Miramos si hay señal contraria para cerrar una operación abierta al recibir dicha señal	
				#region ValidateExit
				// Print("Paso validateExit-Long");
				//	Implementar ValidateExitLong() && ValidateExitShort() en caso de tener operaciones abiertas cuando se da esa señal.
				if (activeOrder && validateExitLong() && validateTimeControlsAndTradeCount())
				{
				//	Print("Paso validateExitLong()");
				//	Primero, comprobar si hay operación abierta para cerrala
					if (autoRS) CloseAllOperation("SCS");
				// Segundo, abrir la operación que nos genera dicha señal
				//	reverseShort = true;	
					if ((autoRV) && (Position.MarketPosition == MarketPosition.Flat))
					{
						InitializeOperation();
						Target	= (BotConfTrending ? meProfitTarget : TargetReduced);
						Stop	= (BotConfTrending ? meInitialStop : StopReduced);			
						Target_Price	= Close[0] - (Target * TickSize);
						BE_Price		= Stop_Price = (stopLossType == CommonEnumsVGA.StopLossType.ATR ? StopLoss_ATR.TrailingStopHigh[0] : Close[0] + (Stop * TickSize));
						Stop_Trigger	= Close[0] - (Trail_Trigger * TickSize);
						BEPrice_Trigger	= Close[0] - (BE_Trigger * TickSize);
						Print(String.Format("{0} {1} {2} >>>  Target ->{3}  Stop -> {4}  TargetPrice -> {5}  Stop_Price -> {6}  Close[0] {7}", Times[0][0].TimeOfDay, "Enter ShortP -> SMk1", "-2", Target, Stop, Target_Price, Stop_Price, Close[0]));			
						SubmitShortOrder( CustomPositionAmount, "SMk1");
						activeShortMk1 = true;
						activeOrder = true;
					}						
				}	
				// Print("Paso validateExit-Short");	
				if (activeOrder && validateExitShort() && validateTimeControlsAndTradeCount())
				{
				//	Print("Paso validateExitShort()");
				//	Primero, comprobar si hay operación abierta para cerrala
					if (autoRS) CloseAllOperation("SCL");
				// Segundo, abrir la operación que nos genera dicha señal
				//	reverseLong = true;	
					if ((autoRV) && (Position.MarketPosition == MarketPosition.Flat))
					{
						InitializeOperation();
						Target	= (BotConfTrending ? meProfitTarget : TargetReduced);
						Stop	= (BotConfTrending ? meInitialStop : StopReduced);		
						Target_Price	= Close[0] + (Target * TickSize);
						BE_Price		= Stop_Price = (stopLossType == CommonEnumsVGA.StopLossType.ATR ? StopLoss_ATR.TrailingStopLow[0] : Close[0] - (Stop * TickSize));
						Stop_Trigger	= Close[0] + (Trail_Trigger * TickSize);
						BEPrice_Trigger	= Close[0] + (BE_Trigger * TickSize);	
						Print(String.Format("{0} {1} {2} >>>  Target ->{3}  Stop -> {4}  TargetPrice -> {5}  Stop_Price -> {6}  Close[0] {7}", Times[0][0].TimeOfDay, "Enter LongP -> LMk1", "2", Target, Stop, Target_Price, Stop_Price, Close[0]));
						SubmitLongOrder( CustomPositionAmount, "LMk1");
						activeLongMk1 = true;
						activeOrder = true;
					}	
				}						
					
				#endregion			
			
			//	Manejo de las entradas
				#region Trades
				
			//	Time Setting
				if(validateTimeControlsAndTradeCount())
				{
					okToTrade = true;
				}
				else
				{
					okToTrade = false;
				}
			
				
			//	Avoid Econ Data Number
				if(econNumber1 != 0)
				{
					if((ToTime(Time[0]) >= econNumber1 - iMinutes && ToTime(Time[0]) < econNumber1 + iMinutes)) // Trade Time +- 2 min
					{
						okToTradeEcon1 = false;
						Draw.TextFixed(this, "Econ1", "Status: Ignoring Econ Data Release +- 2min", TextPosition.TopLeft, Brushes.Red, title2, Brushes.Transparent,Brushes.Black,0);
					}
					else
					{
						okToTradeEcon1 = true;
						RemoveDrawObject("Econ1");
					}
				}
			
				if(econNumber1 == 0)
				{
					okToTradeEcon1 = true;
				}
			
				if(econNumber2 != 0)
				{
					if((ToTime(Time[0]) >= econNumber2 - iMinutes && ToTime(Time[0]) < econNumber2 + iMinutes)) // Trade Time +- 2 min
					{
						okToTradeEcon2 = false;
						Draw.TextFixed(this, "Econ2", "Status: Ignoring Econ Data Release +- 2min", TextPosition.TopLeft, Brushes.Red, title2, Brushes.Transparent,Brushes.Black,0);
					}
					else
					{
						okToTradeEcon2 = true;
						RemoveDrawObject("Econ2");
					}
				}
				if(econNumber2 == 0)
				{
					okToTradeEcon2 = true;
				}

				//	MRZControl
					/*
						Implementar filtrado de operaciones automáticas, cuando está activo el uso de ZR Manual y hay una ZR Manual 
						marcada con una zona manual en el gráfico "RegionHighlightY"
						useMRZ & activeMRZ son true		
						Como ya tenemos actualizados los límites superior e inferior del rango, no hace falta mirar los valores de useMRZ & activeMRZ
						Se filtran las operaciones que se produzcan dentro de la zona de rango
					*/				
				
				okToTradeMRZ = false;
				
				if (Close[0] >= MRZSignalUp && Close[0] <= MRZSignalDw)
				{
					okToTradeMRZ = true;
				}	
				
				if (okToTradeOnlyMRZ) okToTradeMRZ = true;
				
				if (((BarsSinceExitExecution(0, "", 0) == iBarsSinceExitExecution) || (BarsSinceEntryExecution(0, "", 0) > iBarsSinceEntryExecution)) // AvoidMultiEnTries		
				 	&& (Position.MarketPosition == MarketPosition.Flat) // Comprobar que no hay posiciones abiertas
					// Comprobar horario
				 	&& (okToTradeEcon1 && okToTradeEcon2 && okToTrade && okToTradeMRZ))
				{
				//	Entradas Long
					#region UseLong
					if (useLong)
					{
					//	Print("Paso Inicio Entradas Long");	
						if (!activeOrder && validateEntryLong())																
						{
							reverseLong = false;
						//	Ver si al implementar validateExitLong(), InitializeOperation() hay que cambarla de sitio
							InitializeOperation();					
							PresentaDisplayText();
							if (Position.MarketPosition == MarketPosition.Flat)
							{	
								Target	= (BotConfTrending ? ProfitTarget : TargetReduced);
								Stop	= (BotConfTrending ? InitialStop : StopReduced);
							//	Changed to OnOrdeUpdate to take EntryPrice	
//								Target_Price	= Close[0] + (Target * TickSize);
//								BE_Price		= Stop_Price = Close[0] - (Stop	* TickSize);
//								Stop_Trigger	= Close[0] + (Trail_Trigger * TickSize);
//								BEPrice_Trigger = Close[0] + (BE_Trigger * TickSize);								
								Print(String.Format("{0} {1} --> {2}  >>>  Target -> {3}  Stop -> {4}  TargetPrice -> {5}  Stop_Price -> {6}  Close[0] {7}", 
										Times[0][0].TimeOfDay, "Enter LongP -> LAE", StrategyName, Target, Stop, Target_Price, Stop_Price, Close[0]));								
								
								SubmitLongOrder( CustomPositionAmount, "LAE");
								activeLongAE = true;	
								activeOrder = true;
							}		
                		}								
					}	// Fin UseLong
					#endregion
					
					// Short
					#region useShort
					if (useShort)
					{
					//	Print("Paso Inicio Entradas Short");	
						if (!activeOrder && validateEntryShort())	
						{
							reverseShort = false;
						//	Ver si al implementar validateExitLong(), InitializeOperation() hay que cambarla de sitio
							InitializeOperation();
							PresentaDisplayText();							
							if (Position.MarketPosition == MarketPosition.Flat)
							{	
								Target	= (BotConfTrending ? ProfitTarget : TargetReduced);
								Stop	= (BotConfTrending ? InitialStop : StopReduced);
							//	Changed to OnOrdeUpdate to take EntryPrice	
//								Target_Price	= Close[0] - (Target * TickSize);
//								BE_Price		= Stop_Price = Close[0] + (Stop * TickSize);
//								Stop_Trigger	= Close[0] - (Trail_Trigger * TickSize);
//								BEPrice_Trigger = Close[0] - (BE_Trigger * TickSize);
								Print(String.Format("{0} {1}  --> {2}  >>>  Target -> {3}  Stop -> {4}  TargetPrice -> {5}  Stop_Price -> {6}  Close[0] {7}", 
										Times[0][0].TimeOfDay, "Enter ShortP -> SAE", StrategyName, Target, Stop, Target_Price, Stop_Price, Close[0]));		
								SubmitShortOrder( CustomPositionAmount, "SAE");
								activeShortAE = true;
								activeOrder = true;
							}
						}						
						
					}	// Fin UseShort				
					#endregion
					

					
				}	// Fin Comprobaciones Generales
				
				#endregion		
				
				#region Cerrar Fuera Hora long
				if ((Position.MarketPosition == MarketPosition.Long)
					 && (!validateTimeControlsAndTradeCount()))
				{
				//  Change to unmanaged method
				//	SubmitShortOrder( Position.Quantity, "eNDsESS");
					CloseAllOperation("eNDsESS");
					return;
				}
				#endregion
				
				#region Cerrar Fuera Hora Short
				if ((Position.MarketPosition == MarketPosition.Short)
					 && (!validateTimeControlsAndTradeCount()))
				{
				//  Change to unmanaged method
				//	SubmitLongOrder( Position.Quantity, "eNDsESS");
					CloseAllOperation("eNDsESS");
					return;
				}			
				#endregion

			}  // fin UseAuto

				
			
			if (Position.MarketPosition != MarketPosition.Flat)	UnrealizedPnL = Position.GetUnrealizedProfitLoss(PerformanceUnit.Currency, Close[0]);
		
		}	
		
		
		#endregion
		
		#region Time & TradeControls
		protected bool validateTimeControlsAndTradeCount() {
			
			if (Time_1 == true 
				&& Times[0][0].TimeOfDay >= Start_Time_1.TimeOfDay
                 && Times[0][0].TimeOfDay <= Stop_Time_1.TimeOfDay
				&& Session1Count < MaxTradesPerSession) {
					SessionNumber = 1;
					return true;
					 
			}
			if (Time_2 == true 
				&& Times[0][0].TimeOfDay >= Start_Time_2.TimeOfDay
                 && Times[0][0].TimeOfDay <= Stop_Time_2.TimeOfDay
				&& Session2Count < MaxTradesPerSession) {
					SessionNumber = 2;
					 return true;
			}
			if (Time_3 == true 
				&& Times[0][0].TimeOfDay >= Start_Time_3.TimeOfDay
                 && Times[0][0].TimeOfDay <= Stop_Time_3.TimeOfDay
				&& Session3Count < MaxTradesPerSession) {
					SessionNumber = 3;
					 return true;
			}
			if (Time_4 == true 
				&& Times[0][0].TimeOfDay >= Start_Time_4.TimeOfDay
                 && Times[0][0].TimeOfDay <= Stop_Time_4.TimeOfDay
				&& Session4Count < MaxTradesPerSession) {
					SessionNumber = 4;
					 return true;
			}
			if (Session1Count >= MaxTradesPerSession) Print(Times[0][0].TimeOfDay + "  --->>>>   MaxTradesPerSession hit on Session 1");	
		    return false;
		}		

		protected bool checkTimeControlsAndTradeCount() {
			
			if (Time_1 == true 
				&& Times[0][0].TimeOfDay >= Start_Time_1.TimeOfDay
                 && Times[0][0].TimeOfDay <= Stop_Time_1.TimeOfDay
				&& Session1Count == 0 ) {
					return true;
					 
			}
			if (Time_2 == true 
				&& Times[0][0].TimeOfDay >= Start_Time_2.TimeOfDay
                 && Times[0][0].TimeOfDay <= Stop_Time_2.TimeOfDay
				&& Session2Count == 0) {
					 return true;
			}
			if (Time_3 == true 
				&& Times[0][0].TimeOfDay >= Start_Time_3.TimeOfDay
                 && Times[0][0].TimeOfDay <= Stop_Time_3.TimeOfDay
				&& Session3Count == 0) {
					 return true;
			}
			if (Time_4 == true 
				&& Times[0][0].TimeOfDay >= Start_Time_4.TimeOfDay
                 && Times[0][0].TimeOfDay <= Stop_Time_4.TimeOfDay
				&& Session4Count == 0) {
					 return true;
			}
		    return false;
		}			
		
		
		protected void regionColor() {
			
			if (Time_1 == true 
				&& Times[0][0].TimeOfDay >= Start_Time_1.TimeOfDay
                 && Times[0][0].TimeOfDay <= Stop_Time_1.TimeOfDay) {
					BackBrushAll = Time1Color;
					 
			}
			if (Time_2 == true 
				&& Times[0][0].TimeOfDay >= Start_Time_2.TimeOfDay
                 && Times[0][0].TimeOfDay <= Stop_Time_2.TimeOfDay
				&& Session2Count < MaxTradesPerSession) {
					BackBrushAll = Time2Color;
			}
			if (Time_3 == true 
				&& Times[0][0].TimeOfDay >= Start_Time_3.TimeOfDay
                 && Times[0][0].TimeOfDay <= Stop_Time_3.TimeOfDay
				&& Session3Count < MaxTradesPerSession) {
					BackBrushAll = Time3Color;
			}
			if (Time_4 == true 
				&& Times[0][0].TimeOfDay >= Start_Time_4.TimeOfDay
                 && Times[0][0].TimeOfDay <= Stop_Time_4.TimeOfDay
				&& Session4Count < MaxTradesPerSession) {
					BackBrushAll = Time4Color;
			}
		}
		
		protected void incrementSessionTradeCount() {
			if (State == State.Realtime) {
				if (Time_1 == true 
					&& Times[0][0].TimeOfDay >= Start_Time_1.TimeOfDay
	                 && Times[0][0].TimeOfDay <= Stop_Time_1.TimeOfDay) {
						 Session1Count++;
				}
				if (Time_2 == true 
					&& Times[0][0].TimeOfDay >= Start_Time_2.TimeOfDay
	                 && Times[0][0].TimeOfDay <= Stop_Time_2.TimeOfDay) {
						 Session2Count++;
				}
				if (Time_3 == true 
					&& Times[0][0].TimeOfDay >= Start_Time_3.TimeOfDay
	                 && Times[0][0].TimeOfDay <= Stop_Time_3.TimeOfDay) {
						 Session3Count++;
				}
				if (Time_4 == true 
					&& Times[0][0].TimeOfDay >= Start_Time_4.TimeOfDay
	                 && Times[0][0].TimeOfDay <= Stop_Time_4.TimeOfDay) {
						 Session4Count++;
				}
			}
		}
		
		protected void resetSessionTradeCount() {
			
			if (Time_1 == true 
                 && Times[0][0].TimeOfDay > Stop_Time_1.TimeOfDay) {
					 Session1Count = 0;
			}
			if (Time_2 == true 
                 && Times[0][0].TimeOfDay > Stop_Time_2.TimeOfDay) {
					 Session2Count = 0;
			}
			if (Time_3 == true 
                 && Times[0][0].TimeOfDay > Stop_Time_3.TimeOfDay) {
					 Session3Count = 0;
			}
			if (Time_4 == true 
                 && Times[0][0].TimeOfDay > Stop_Time_4.TimeOfDay) {
					 Session4Count = 0;
			}
		}
		
		private double getCumProfit() {
			TradeCollection realTimeTrades = SystemPerformance.RealTimeTrades;
			Print(realTimeTrades.TradesPerformance.Currency.CumProfit);
			return realTimeTrades.TradesPerformance.Currency.CumProfit;
		}
		#endregion
		
		#region OnMarketData
		protected override void OnMarketData(MarketDataEventArgs marketDataUpdate)
		{
		    if (BarsInProgress != 0) return;
		
		    // Actualiza la posición actual (si es necesario)
		    UpdateCurrentPosition();
		
		    if (marketDataUpdate.MarketDataType == MarketDataType.Last)
		    {
		        if (TrailSetAuto)
		        {
		            UpdateStopLossOnMarketData(marketDataUpdate);
		        }
		
		        if (BESetAuto)
		        {
		            UpdateBreakevenOnMarketData(marketDataUpdate);
		        }
		    }
		}
		
		private void UpdateCurrentPosition()
		{
		    Position openPosition = Position;
		    if (openPosition != null && openPosition.MarketPosition != MarketPosition.Flat)
		    {
		        currentPosition = Position.Quantity;
		    }
		}
		
		private void UpdateStopLossOnMarketData(MarketDataEventArgs marketDataUpdate)
		{
		    if (Position.MarketPosition == MarketPosition.Long)
		    {
		        if (Stop_Price != 0 && marketDataUpdate.Price >= Stop_Trigger)
		        {
		            Stop_Price = marketDataUpdate.Price - (Trail_Size * TickSize);
		            Stop_Trigger = marketDataUpdate.Price + (Trail_frequency * TickSize);
		            PrintStopModifyLog("Long", Stop_Price);
		
		            if (stopOrder != null && chkLongPositionStop(Stop_Price))
		            {
		                ChangeOrder(stopOrder, currentPosition, 0, Stop_Price);
		            }
		
		            Print("Setting Stop Longs = " + Stop_Price);
		        }
		    }
		
		    if (Position.MarketPosition == MarketPosition.Short)
		    {
		        if (Stop_Price != 0 && marketDataUpdate.Price <= Stop_Trigger)
		        {
		            Stop_Price = marketDataUpdate.Price + (Trail_Size * TickSize);
		            Stop_Trigger = marketDataUpdate.Price - (Trail_frequency * TickSize);
		            PrintStopModifyLog("Short", Stop_Price);
		
		            if (stopOrder != null && chkShortPositionStop(Stop_Price))
		            {
		                ChangeOrder(stopOrder, currentPosition, 0, Stop_Price);
		            }
		
		            Print("Setting Stop Shorts = " + Stop_Price);
		        }
		    }
		}
		
		private void UpdateBreakevenOnMarketData(MarketDataEventArgs marketDataUpdate)
		{
		    if (Position.MarketPosition == MarketPosition.Long)
		    {
		        if (BE_Price != 0 && marketDataUpdate.Price >= BEPrice_Trigger)
		        {
		            BE_Price = marketDataUpdate.Price - (BE_Size * TickSize);
		            PrintBreakevenModifyLog("Long", BE_Price);
		
		            if (stopOrder != null && chkLongPositionStop(BE_Price))
		            {
		                ChangeOrder(stopOrder, currentPosition, 0, BE_Price);
		            }
		
		            Print("Setting Stop Longs to BE = " + BE_Price);
		        }
		    }
		
		    if (Position.MarketPosition == MarketPosition.Short)
		    {
		        if (BE_Price != 0 && marketDataUpdate.Price <= BEPrice_Trigger)
		        {
		            BE_Price = marketDataUpdate.Price + (BE_Size * TickSize);
		            PrintBreakevenModifyLog("Short", BE_Price);
		
		            if (stopOrder != null && chkShortPositionStop(BE_Price))
		            {
		                ChangeOrder(stopOrder, currentPosition, 0, BE_Price);
		            }
		
		            Print("Setting Stop Shorts to BE = " + BE_Price);
		        }
		    }
		}

		private void PrintStopModifyLog(string direction, double newStopPrice)
		{
		    if (SystemPrint && OrdersPrints)
		    {
		        Print($"{Times[0][0].TimeOfDay} Event OnMarketData()  Modifying Stop Price {direction} Order to {newStopPrice}");
		    }
		}
		
		private void PrintBreakevenModifyLog(string direction, double newBEPrice)
		{
		    if (SystemPrint && OrdersPrints)
		    {
		        Print($"{Times[0][0].TimeOfDay} Event OnMarketData()  Modifying BE Price {direction} Order to {newBEPrice}");
		    }
		}
		#endregion
			
		#region OnOrderUpdate
        protected override void OnOrderUpdate(Order order, double limitPrice, double stopPrice, int quantity, int filled, double averageFillPrice, OrderState orderState, DateTime time, ErrorCode error, string nativeError)
        {
            // Handle entry orders here. The entryOrder object allows us to identify that the order that is calling the OnOrderUpdate() method is the entry order.
            // Assign entryOrder in OnOrderUpdate() to ensure the assignment occurs when expected.
            // This is more reliable than assigning Order objects in OnBarUpdate, as the assignment is not gauranteed to be complete if it is referenced immediately after submitting
			
			if (BarsInProgress != 0) return;
			
		//	if (SystemPrint && OrdersPrints) Print(String.Format("{0}  Event OnOrderUpdate()  -->  Pasa por aquí Pto. 0 ->  order:  {1}  StopPrice -> {2}  TargetPrice -> {3} currStopPrice -> {4}  currTargetPrice -> {5}  BarsInProgress -> {6}  UnrealizedPnL -> {7} >>> {8} <<< ==> {9}", Times[0][0].TimeOfDay, order.Name, stopPrice, limitPrice, currStopPrice, currTargetPrice, BarsInProgress, UnrealizedPnL, order.OrderState, (int) order.OrderState));
			
			if (stopPrice != 0 && currStopPrice != stopPrice) currStopPrice = stopPrice;
			if (limitPrice != 0 && limitPrice != currTargetPrice) currTargetPrice = limitPrice;
				
			//  Access the open position
        	Position openPosition = Position;

        	if (openPosition != null && openPosition.MarketPosition != MarketPosition.Flat)
        	{
				currentPosition = Position.Quantity;
			}			
			
			#region OrderName 
			string StopSignal = "";
			string TargetSignal = "";	
			
			switch (order.Name)
			
			{
				case "LAE":		
				case "LMk1":
					
                	entryOrder = order;
					StopSignal = "StLE";
					TargetSignal = "TgLE";
					
					if (entryOrder != null && entryOrder == order)
					{		
                	//  Reset the entryOrder object to null if order was cancelled without any fill
                		if (order.OrderState == OrderState.Cancelled && order.Filled == 0)
                		{
                    		entryOrder = null;
							if (SystemPrint && OrdersPrints) Print(String.Format("{0} Event OnOrderUpdate()  --> order:  {1} Canceled :: Destroyed   >>> {2} <<< ==> State: {3}", Times[0][0].TimeOfDay, order.Name, order.OrderState, (int) order.OrderState));
                		}
					// Reset the entryOrder if filled
						if (order.OrderState == OrderState.Filled)
                		{
                    		entryOrder = null;
							
							if (SystemPrint && OrdersPrints) Print(String.Format("{0} Event OnOrderUpdate()  --> order:  {1} Filled :: Destroyed   >>> {2} <<< ==> State: {3}  averageFillPrice  {4}", Times[0][0].TimeOfDay, order.Name, order.OrderState, (int) order.OrderState, averageFillPrice));
							if (stopOrder == null && targetOrder == null)
               				{
                  				if (State == State.Historical)
                       				oco = DateTime.Now.ToString() + CurrentBar + "LExits";
                   				else
                       				oco = GetAtmStrategyUniqueId() +  "LExits";
							
								Target_Price	= averageFillPrice + (Target * TickSize);
								BE_Price		= Stop_Price = (stopLossType == CommonEnumsVGA.StopLossType.ATR ? StopLoss_ATR.TrailingStopHigh[0] : averageFillPrice - (Stop * TickSize));
							//	BE_Price		= Stop_Price = Low[1];
								Stop_Trigger	= averageFillPrice + (Trail_Trigger * TickSize);
								BEPrice_Trigger	= averageFillPrice + (BE_Trigger * TickSize);	
								
							//	PresentaDisplayText2();

                   			// Directly assign order objects for target and stop, to ensure target/stop Orders are assigned before we see additional Executions, which can result in multiple orders firing instead of updating the orders.
                   			// We need to do this to be able to branch for ChangeOrders vs. new order submissions.
								if (SystemPrint && OrdersPrints) Print(String.Format("{0} Event OnOrderUpdate()  --> order:  {1} stopOrder && targetOrder Not Null Trying to set stopOrder and target order", Times[0][0].TimeOfDay, order.Name));
              					if (chkLongPositionStop(Stop_Price)) stopOrder = SubmitOrderUnmanaged(0, OrderAction.Sell, OrderType.StopMarket, quantity, 0, Stop_Price, oco, StopSignal);
               					if (chkLongPositionProfit(Target_Price)) targetOrder = SubmitOrderUnmanaged(0, OrderAction.Sell, OrderType.Limit, quantity, Target_Price, 0, oco, TargetSignal);	
								if (SystemPrint && OrdersPrints) Print(String.Format("{0} Event OnOrderUpdate()  --> order:  {1} stopOrder && targetOrder  Null ", Times[0][0].TimeOfDay, order.Name));
								incrementSessionTradeCount();
			
							}
							else if (stopOrder != null && targetOrder != null)
               				{
								if (SystemPrint && OrdersPrints) Print(String.Format("{0} Event OnOrderUpdate()  --> order:  {1} stopOrder && targetOrder Not Null Trying to change stopOrder and target order", Times[0][0].TimeOfDay, order.Name));
								// Check bid() and ask() ???
                   				ChangeOrder(stopOrder, iCurrentPosition, 0 , currStopPrice);			// Change from currentPosition + quantity  to iCurrentPosition
                   				ChangeOrder(targetOrder, iCurrentPosition, currTargetPrice, 0);			// Change from currentPosition + quantity  to iCurrentPosition
								if (SystemPrint && OrdersPrints) Print(String.Format("{0} Event OnOrderUpdate()  --> order:  {1} stopOrder && targetOrder Not Null ", Times[0][0].TimeOfDay, order.Name));
           					}	
                		}
						if (order.OrderState == OrderState.Rejected)
            			{
                		// Handle rejected orders (potential rogue orders)
                			Print("Rogue Order Detected: " + order.ToString());

                		// Cancel any pending orders
                			if (entryOrder != null && order.OrderState == OrderState.Working)
                			{
                    			CancelOrder(entryOrder);
                			}

                			// Flatten all positions
							// CloseAllOperation("RogueOrder?");
                			FlattenAllPositions();

                		// Log the rogue order
                			Log("Rogue Order Detected: " + order.ToString(), LogLevel.Error);

                		// Send an alert
                			Alert("RogueOrderAlert", Priority.High, "Rogue Order Detected!", "Alert.wav", 10, Brushes.Red, Brushes.White);
            			}						
						
						
					}	
    				break;
								
				case "SAE":		
            	case "SMk1":
					
            	    entryOrder = order;
					StopSignal = "StSE";
					TargetSignal = "TgSE";
					
					if (entryOrder != null && entryOrder == order)
					{					
                	// Reset the entryOrder object to null if order was cancelled without any fill
                		if (order.OrderState == OrderState.Cancelled && order.Filled == 0)
                		{
                    		entryOrder = null;
							if (SystemPrint && OrdersPrints) Print(String.Format("{0} Event OnOrderUpdate()  --> order:  {1} Cancelled :: Destroyed   >>> {2} <<< ==> State: {3}", Times[0][0].TimeOfDay, order.Name, order.OrderState, (int) order.OrderState));
                		}
                		if (order.OrderState == OrderState.Filled)
                		{
                    		entryOrder = null;
							if (SystemPrint && OrdersPrints) Print(String.Format("{0} Event OnOrderUpdate()  --> order:  {1} Filled :: Destroyed   >>> {2} <<< ==> State: {3}   averageFillPrice  {4}", Times[0][0].TimeOfDay, order.Name, order.OrderState, (int) order.OrderState, averageFillPrice));
						   	if (stopOrder == null && targetOrder == null)
               				{
                   				if (State == State.Historical)
                       				oco = DateTime.Now.ToString() + CurrentBar +  "SExits";
                   				else
                       				oco = GetAtmStrategyUniqueId() +  "SExits";
							
								Target_Price	= averageFillPrice - (Target * TickSize);
								BE_Price		= Stop_Price = (stopLossType == CommonEnumsVGA.StopLossType.ATR ? StopLoss_ATR.TrailingStopHigh[0] : averageFillPrice + (Stop * TickSize));
							//	BE_Price		= Stop_Price = High[1];
								Stop_Trigger	= averageFillPrice - (Trail_Trigger * TickSize);
								BEPrice_Trigger	= averageFillPrice - (BE_Trigger * TickSize);
								
							//	PresentaDisplayText2();
							
                	   		// Directly assign order objects for target and stop, to ensure target/stop Orders are assigned before we see additional Executions, which can result in multiple orders firing instead of updating the orders.
                   			// We need to do this to be able to branch for ChangeOrders vs. new order submissions.
								if (SystemPrint && OrdersPrints) Print(String.Format("{0} Event OnOrderUpdate()  --> order:  {1} stopOrder && targetOrder Not Null Trying to set stopOrder and target order", Times[0][0].TimeOfDay, order.Name));
               					if (chkShortPositionStop(Stop_Price)) stopOrder = SubmitOrderUnmanaged(0, OrderAction.Buy, OrderType.StopMarket, quantity, 0, Stop_Price, oco, StopSignal);
               					if (chkShortPositionProfit(Target_Price)) targetOrder = SubmitOrderUnmanaged(0, OrderAction.Buy, OrderType.Limit, quantity, Target_Price, 0, oco, TargetSignal);								
								if (SystemPrint && OrdersPrints) Print(String.Format("{0} Event OnOrderUpdate()  --> order:  {1} stopOrder && targetOrder Null ", Times[0][0].TimeOfDay, order.Name));
								incrementSessionTradeCount();
               				}	
							else if (stopOrder != null && targetOrder != null)
           					{
								if (SystemPrint && OrdersPrints) Print(String.Format("{0} Event OnOrderUpdate()  --> order:  {1} stopOrder && targetOrder Not Null Trying to change stopOrder and target order", Times[0][0].TimeOfDay, order.Name));
								// Check bid() and ask() ???
                   				ChangeOrder(stopOrder, iCurrentPosition, 0, currStopPrice);		// Change from currentPosition + quantity  to iCurrentPosition
                   				ChangeOrder(targetOrder, iCurrentPosition, currTargetPrice, 0);	// Change from currentPosition + quantity  to iCurrentPosition
								if (SystemPrint && OrdersPrints) Print(String.Format("{0} Event OnOrderUpdate()  --> order:  {1} stopOrder && targetOrder Not Null ", Times[0][0].TimeOfDay, order.Name));
           					}
                		}	
						if (order.OrderState == OrderState.Rejected)
            			{
                		// Handle rejected orders (potential rogue orders)
                			Print("Rogue Order Detected: " + order.ToString());

                		// Cancel any pending orders
                			if (entryOrder != null && order.OrderState == OrderState.Working)
                			{
                    			CancelOrder(entryOrder);
                			}

                			// Flatten all positions
                			// CloseAllOperation("RogueOrder?");
							FlattenAllPositions();
							
							

                		// Log the rogue order
                			Log("Rogue Order Detected: " + order.ToString(), LogLevel.Error);

                		// Send an alert
                			Alert("RogueOrderAlert", Priority.High, "Rogue Order Detected!", "Alert.wav", 10, Brushes.Red, Brushes.White);
            			}
					}	
					break;
								
			// Implementar controles para los stop y los targets
				case "StLE" :					
				case "StSE" :

					stopOrder = order;
					if (stopPrice != 0 && currStopPrice != stopPrice) currStopPrice = stopPrice;
					PresentaDisplayText();
					if (stopOrder != null && stopOrder == order)
					{
                		if (order.OrderState == OrderState.Cancelled && order.Filled == 0)
						{
							stopOrder = targetOrder = null;		
							ResetOperation();
							Print(String.Format("{0} Event OnOrderUpdate()  --> order: {1} Canceled|Filled = 0 :: Destroyed   ===>  Target Conseguido", Times[0][0].TimeOfDay, order.Name));
						}	
						if (order.OrderState == OrderState.Filled)
						{
							stopOrder = targetOrder = null;
							Print(String.Format("{0} Event OnOrderUpdate()  --> order: {1} Filled & Destroyed & PositionClosed", Times[0][0].TimeOfDay, order.Name));
						}	
					}
					break;				
					
				case "TgLE" :
				case "TgSE" :

					targetOrder = order;				
					if (limitPrice != 0 && limitPrice != currTargetPrice) currTargetPrice = limitPrice;
					PresentaDisplayText();
					if (targetOrder != null && targetOrder == order)
					{
			      		if (order.OrderState == OrderState.Cancelled && order.Filled == 0)
						{
							stopOrder = targetOrder = null;			
							ResetOperation();
							Print(String.Format("{0} Event OnOrderUpdate()  --> order: {1} Canceled|Filled = 0 :: Destroyed   ===>  StopLoss Alcanzado ", Times[0][0].TimeOfDay, order.Name));
						}	
						if (order.OrderState == OrderState.Filled)
						{
							stopOrder = targetOrder = null;
							Print(String.Format("{0} Event OnOrderUpdate()  --> order: {1} Filled & Destroyed & PositionClosed", Times[0][0].TimeOfDay, order.Name));
						}				
					}
					break;			

				case "CP1C" :
				case "CP2C" :
				case "CMTotal" :
				case "DeltaCntr" :	
				case "eNDsESS" :
				case "SgCntr" :				
					
            	    entryOrder = order;
					if (entryOrder != null && entryOrder == order)
					{					
                	// Reset the entryOrder object to null if order was cancelled without any fill
                		if (order.OrderState == OrderState.Cancelled && order.Filled == 0)
                		{
                    		entryOrder = null;
							if (SystemPrint && OrdersPrints) Print(String.Format("{0} Event OnOrderUpdate()  --> order:  {1} Cancelled :: Destroyed   >>> {2} <<< ==> State:  {3}", Times[0][0].TimeOfDay, order.Name, order.OrderState, (int) order.OrderState));

							if (order.Name == "CMTotal")
							{
								CancelOrder(stopOrder);
         						CancelOrder(targetOrder);
								stopOrder = targetOrder = null;
							}
                		}
                		if (order.OrderState == OrderState.Filled)
                		{
                    		entryOrder = null;
							if (SystemPrint && OrdersPrints) Print(String.Format("{0} Event OnOrderUpdate()  --> order:  {1} Filled :: Destroyed   >>> {2} <<< ==> State:  {3}", Times[0][0].TimeOfDay, order.Name, order.OrderState, (int) order.OrderState));
							
							if (order.Name == "CMTotal")
							{
								CancelOrder(stopOrder);
         						CancelOrder(targetOrder);
								stopOrder = targetOrder = null;
								Print(String.Format("{0} Event OnOrderUpdate()  --> order:  {1} Manual Closed:: Filled & Destroyed & PositionClosed   >>> {2} <<< ==> State:  {3}", Times[0][0].TimeOfDay, order.Name, order.OrderState, (int) order.OrderState));
							}

                		}	
					}	
					break;					

            }
			
//			if (SystemPrint && OrdersPrints) Print(String.Format("{0}  orderName:  {1} Created   orderState: {2}   ERROR: {3} ", Times[0][0].TimeOfDay, order.Name, order.OrderState, nativeError));
			if (nativeError != " ")
			{
				if (SystemPrint && OrdersPrints) Print(String.Format("{0} Event OnOrderUpdate()  --> order:  {1} Created   State: {2}  Txt {3}", Times[0][0].TimeOfDay, order.Name, (int)order.OrderState, order.OrderState));
			}	
			else
			{
				if (SystemPrint && OrdersPrints) Print(String.Format("{0} Event OnOrderUpdate()  --> order:  {1} Created   State: {2}  Txt {3} --> ERROR: {4} ", Times[0][0].TimeOfDay, order.Name, (int)order.OrderState, order.OrderState, nativeError));
			}
			#endregion	
        }		
		#endregion
		
		#region FlattenAllPositions
        private void FlattenAllPositions()
        {
			
			//  Access the open position
        	Position openPosition = Position;
			Account myAccount;
			AccountSelector accountSelector = Extensions.FindFirst(Window.GetWindow(ChartControl.Parent), "ChartTraderControlAccountSelector") as AccountSelector;
			this.chartTraderAccount = ((accountSelector != null) ? accountSelector.SelectedAccount : null);
			this.accountSelector = ((accountSelector != null) ? accountSelector : null);
			
			
			
			// Get the account (replace "Sim101" with your actual account name)
            myAccount = Account.All.FirstOrDefault((Account a) => a.Name == this.chartTraderAccount.DisplayName);
			Print("Account selectd: " + this.chartTraderAccount.DisplayName);
            if (myAccount == null) Print("Account selectd: " + this.chartTraderAccount.DisplayName + " Account not found !!!");
			if (myAccount == null)
			     throw new Exception("Account not found.");
			
        	if (openPosition != null && openPosition.MarketPosition != MarketPosition.Flat)
        	{
			// More drastic method, we make a Flatten All to the account
//				Account.FlattenEverything();
			// Less drastic method, we make a Flatten All to the account used in the strategy and to the instrument that we have loaded on the chart
				List<Instrument> instrumentNames = new List<Instrument>();
				foreach (Position position in this.chartTraderAccount.Positions)
	            {
	              Instrument instrument = position.Instrument;
	              if (!instrumentNames.Contains(instrument))
	                instrumentNames.Add(instrument);
	            }
	            this.chartTraderAccount.Flatten((ICollection<Instrument>) instrumentNames);

        	}		
		}
		#endregion
		
		#region OnExecutionUpdate
		protected override void OnExecutionUpdate(Execution execution, string executionId, double price, int quantity, MarketPosition marketPosition, string orderId, DateTime time)
		{
			if (BarsInProgress != 0) return;
		//	if (SystemPrint) Print(String.Format("{0}  Pasa por aquí Pto. 0 -> Event ExecutionUpdate()  --> orderName:  {1}  EntryPrice -> {2}  StopPrice -> {3}  TargetPrice -> {4}  BarsInProgress -> {5}", Times[0][0].TimeOfDay, execution.Name, price, currStopPrice, currTargetPrice, BarsInProgress));
						//  Access the open position
        	Position openPosition = Position;

        	if (openPosition != null && openPosition.MarketPosition != MarketPosition.Flat)
        	{
				currentPosition = Position.Quantity;
			}						
			
			
			if (SystemPrint && OrdersPrints) Print(String.Format("{0} Event ExecutionUpdate()  --> execution:  {1}   Order: {2} ", Times[0][0].TimeOfDay, execution.Name, execution.Order));
	
			switch (execution.Name)
			{
																
//				case "SgCntr" :
//						if (execution.Order.OrderState == OrderState.Filled) Print("Event ExecutionUpdate() Trade ended in Loss Avoid large loss when not in x ticks of profit "  + UnrealizedPnL);
//						if (SystemPrint && OrdersPrints) Print(String.Format("{0}  execution:  {1} Filled & Destroyed & PositionClosed", Times[0][0].TimeOfDay, execution.Name));
//						break;				
						
//				case "DeltaCntr" :
//						if (execution.Order.OrderState == OrderState.Filled) Print("Event ExecutionUpdate() Trade ended in Loss DeltaInverse Control " + UnrealizedPnL);
//						if (SystemPrint && OrdersPrints) Print(String.Format("{0} Event ExecutionUpdate() execution:  {1} Filled & Destroyed & PositionClosed", Times[0][0].TimeOfDay, execution.Name));
//						break;		

				case "CP1C" :
				case "CP2C" :
					
						{	
							if (stopOrder != null && targetOrder != null)
               				{
								if (SystemPrint && OrdersPrints) Print(String.Format("{0} Event OnExecutionUpdate()  -->  Trying to change stopOrder and target order", Times[0][0].TimeOfDay));
								// Check bid() and ask() ???	
                   				ChangeOrder(stopOrder, iCurrentPosition, 0, currStopPrice);					// Change from currentPosition - numContracts  to iCurrentPosition
                   				ChangeOrder(targetOrder, iCurrentPosition , currTargetPrice, 0);			// Change from currentPosition - numContracts  to iCurrentPosition
           					}
						}
						break;					
										
			}  // Fin switch   
						
		}
		#endregion
	
		#region OnPositionUpdate
		protected override void OnPositionUpdate(Position position, double averagePrice, int quantity, MarketPosition marketPosition)
		{
			if (BarsInProgress != 0) return;
			
		//	if (SystemPrint) Print(String.Format("{0}  Pasa por aquí Pto. 0 -> Event OnPositionUpdate()  EntryPrice -> {1}  StopPrice -> {2}  TargetPrice -> {3}  BarsInProgress -> {4} ", Times[0][0].TimeOfDay, averagePrice, currStopPrice, currTargetPrice, BarsInProgress));
			
			#region Control Daily Max. Loss & Max. Profit
			
			if (position.MarketPosition == MarketPosition.Flat && SystemPerformance.AllTrades.Count > 0)
			{
				// when a position is closed, add the last trade's Profit to the currentPnL
			//	currentPnL += SystemPerformance.AllTrades[SystemPerformance.AllTrades.Count - 1].ProfitCurrency;
				currentPnL = getCumProfit();
				

				// print to output window if the daily limit is hit
				if (currentPnL <= -dailyMaxLoss && CtrlDailyMaxLoss)
				{
					if (SystemPrint) Print("daily limit hit, no new orders >>> " + Times[0][0].TimeOfDay);
					CanTrade = false;
				}
				
				if (currentPnL >= dailyMinPnLToSecure && CtrlDailyMinPnLToSecure)
				{
					if (SystemPrint) Print("daily Profit limit hit, no new orders >>> " + Times[0][0].TimeOfDay); //Prints message to output
					CanTrade = false;
				}
				
			}

			/*
			//if in a position and the realized day's PnL plus the position PnL is greater than the loss limit then exit the order
			//Implementar esta parte para Long y para Short
			if (
				(position.MarketPosition == MarketPosition.Long)
					&&  (  ((currentPnL + Position.GetUnrealizedProfitLoss(PerformanceUnit.Currency, Close[0])) <= -dailyMaxLoss)
						|| ((currentPnL + Position.GetUnrealizedProfitLoss(PerformanceUnit.Currency, Close[0])) >= dailyMinPnLToSecure)  ) ///If unrealized goes under LossLimit 'OR' Above ProfitLimit
				
				)    
				
			{
			//	Print to the output window if the daily limit is hit in the middle of a trade
				Print("daily limit hit, exiting order " + Time[0].ToString());
				ExitLong("Daily Limit Exit", "long1");
			}					
			*/
			/*
				if (position.MarketPosition != MarketPosition.Flat)	UnrealizedPnL = position.GetUnrealizedProfitLoss(PerformanceUnit.Currency, Close[0]);
				if (SystemPrint) Print(Times[0][0].TimeOfDay + "Event OnPositionUpdate() --> currentActivePnL >>> " + UnrealizedPnL.ToString("F2")); 
			*/
			#endregion    
			
			#region Trail Stop Cancel

			if (position.MarketPosition == MarketPosition.Flat) 
			{
			/*
				Reiniciar variables una vez cerrado el trade activo
			*/	
			//	Trailing Stop 
				Stop_Price 	= 0;
				Stop_Trigger = 0;
				Target_Price = 0;
		
			//	Stop a BE	
				BE_Price 		= 0;
				BEPrice_Trigger 	= 0;
          				
			}


			#endregion			
		}		
		#endregion
		
		#region Trade methods
		
		private bool chkLongPositionStop(double Price)
		{
			
			if ((Price < GetCurrentAsk()) && (Price < GetCurrentBid()))	return true;
			return false;
		}	

		private bool chkLongPositionProfit(double Price)
		{
			
			if ((Price > GetCurrentAsk()) && (Price > GetCurrentBid()))	return true;
			return false;
		}			
		
		private bool chkShortPositionStop(double Price)
		{
			
			if ((Price > GetCurrentAsk()) && (Price > GetCurrentBid()))	return true;
			return false;
		}			

		private bool chkShortPositionProfit(double Price)
		{
			
			if ((Price < GetCurrentAsk()) && (Price < GetCurrentBid()))	return true;
			return false;
		}			
		
		
		
		private void InitializeOperation()
		{
			entryOrder = stopOrder = targetOrder = null;
			//
			activeLongMk1 		= false;
			activeLongAE		= false;
			//
			activeShortMk1 		= false;			
			activeShortAE	= false;
			//
			currTargetPrice = 0;
			currStopPrice 	= 0;
			UnrealizedPnL 	= 0;
			//
			ResetOperation();
			PresentaDisplayText();
			
			return;
		}
		
		private void ResetOperation()
		{
			currentPosition 	= 0;
			iCurrentPosition 	= 0;
			activeOrder 		= false;	
			return;
		}	

		private void SubmitLongOrder(int quantity, string signalName)
		{	
            /* The entry orders objects will take on a unique ID from our SubmitOrderUnmanaged() that we can use
            later for order identification purposes in the OnOrderUpdate() and OnExecution() methods. */
            if (State == State.Historical)
                oco = DateTime.Now.ToString() + CurrentBar + "LE";
            else
                oco = GetAtmStrategyUniqueId() + "LE";

            if (entryOrder == null)
			{	
                SubmitOrderUnmanaged(0, OrderAction.Buy, OrderType.Market, (quantity > 0 ? quantity : -quantity), 0, 0, oco, signalName);
				iCurrentPosition += quantity;
			}
			
            return;    	
			
        }  

		private void SubmitShortOrder(int quantity, string signalName)
		{	
            /* The entry orders objects will take on a unique ID from our SubmitOrderUnmanaged() that we can use
            later for order identification purposes in the OnOrderUpdate() and OnExecution() methods. */
            if (State == State.Historical)
                oco = DateTime.Now.ToString() + CurrentBar + "SE";
            else
                oco = GetAtmStrategyUniqueId() + "SE";

            if (entryOrder == null)
			{	
                SubmitOrderUnmanaged(0, OrderAction.SellShort, OrderType.Market, (quantity >0 ? quantity : -quantity) , 0, 0, oco, signalName);
				iCurrentPosition += quantity;
			}
			
            return;    	
        }  	
		#endregion
		
		#region Trade Performance
		private void DrawHistoricalTradePerformance(ChartControl chartControl) {

			
			TradeCollection allTrades = SystemPerformance.AllTrades;
			TradeCollection winningTrades = allTrades.WinningTrades;
			TradeCollection loosingTrades = allTrades.LosingTrades;
			
			
			int totalTradeCount = allTrades.TradesCount;
			int winningTradesCount = winningTrades.TradesCount;
			int loosingTradesCount = loosingTrades.TradesCount;
			double profitFactor = allTrades.TradesPerformance.ProfitFactor;
			double grossProfit = allTrades.TradesPerformance.GrossProfit;
			double grossLoss	= allTrades.TradesPerformance.GrossLoss;
			double netProfitLoss	= allTrades.TradesPerformance.NetProfit;
			//double profitability = (winningTradesCount/totalTradeCount) * 100;
			
			string textLine0 = "Trade Performance (Historical)";
			string textLine1 = "Total # of Trades : "+totalTradeCount;
			string textLine2 = "# of Winning Trades : "+winningTradesCount;
			string textLine3 = "# of Loosing Trades : "+loosingTradesCount;
			string textLine4 = "Gross Profit : $"+grossProfit;
			string textLine5 = "Gross Loss : ($"+ grossLoss+")";
			string textLine6 = "Net Profit: "+ (netProfitLoss < 0 ? "($"+netProfitLoss+")" : "$"+netProfitLoss);
			string textLine7 = "Profit Factor: "+profitFactor;
			
			string tradePerfText = textLine0 + "\n" + textLine1 + "\n" + textLine2 + "\n" + textLine3 + "\n" + textLine4 + "\n" + textLine5 + "\n" + textLine6 + "\n" + textLine7 + "\n";
			
			SimpleFont font = new SimpleFont("Courier New", 12) {Bold = true };
			Draw.TextFixed(this, "tradePerformanceText", tradePerfText, DisplayHistoricalTradePerformanceOrientation, chartControl.Properties.ChartText, font, Brushes.AntiqueWhite, Brushes.Transparent, 0);
		}
		
		private void DrawStrategyPnl(ChartControl chartControl) {
			
			double cumProfit = getCumProfit();
			string textLine0 = "Realtime Strategy PnL";
			string textLine1 = "Cumulative Profit: "+ (cumProfit < 0 ? "($"+cumProfit+")" : "$"+cumProfit);
			string textLine2 = "";
			string textLine3 = "";
			string textLine4 = "";
			string textLine5 = "";
			if (cumProfit <= -dailyMaxLoss) {
				textLine4 = "Max Loss level reached :( ";
			} else if (cumProfit >= dailyMinPnLToSecure) {
				textLine4 = "Max Target level reached :) ";
			}
			
			textLine2  = "Session Number: " + SessionNumber;
			
			if (SessionNumber == 1) {
				if (Session1Count == MaxTradesPerSession) {
					textLine3 = "Achieved Max trades per session in "+SessionNumber;
				} else {
					textLine3 = "Trades in this Session: " + Session1Count;
				}
				textLine5 = "From " + Start_Time_1.ToString() + " to " + Stop_Time_1.ToString("HH:mm:ss");
			}
			if (SessionNumber == 2) {
				if (Session2Count == MaxTradesPerSession) {
					textLine3 = "Achieved Max trades per session in "+SessionNumber;
				} else {
					textLine3 = "Trades in this Session: " + Session2Count;
				}
				textLine5 = "From " + Start_Time_2.ToString() + " to " + Stop_Time_2.ToString("HH:mm:ss");
			}
			if (SessionNumber == 3) {
				if (Session3Count == MaxTradesPerSession) {
					textLine3 = "Achieved Max trades per session in "+SessionNumber;
				} else {
					textLine3 = "Trades in this Session: " + Session3Count;
				}
				textLine5 = "From " + Start_Time_3.ToString() + " to " + Stop_Time_3.ToString("HH:mm:ss");
			}
			if (SessionNumber == 4) {
				if (Session4Count == MaxTradesPerSession) {
					textLine3 = "Achieved Max trades per session in "+SessionNumber;
				} else {
					textLine3 = "Trades in this Session: " + Session4Count;
				}
				textLine5 = "From " + Start_Time_4.ToString() + " to " + Stop_Time_4.ToString("HH:mm:ss");
			}
			
			string realTimeTradeText = textLine0 + "\n" + textLine1 + "\n" + textLine2 + "\n" + textLine3 + "\n" + textLine4 + "\n" + textLine5;
			SimpleFont font = new SimpleFont("Courier New", 12) { Size = 15, Bold = true };
			Draw.TextFixed(this, "realTimeTradeText", realTimeTradeText, DisplayStrategyPnLOrientation, Brushes.Aquamarine, font, Brushes.Aquamarine, Brushes.Transparent, 0);
		}				
	
		#endregion
		
		#region Display Box Info
		protected void PresentaDisplayText()
		{
			if (DisplayText && DisplayMRZInfo)
				
			{	
		
				Draw.TextFixed(this, "MRZBox", "MRZSignalUp.\t >>> \t   " + MRZSignalUp.ToString()
											+ "\nMRZSignalDw.\t >>> \t   " + MRZSignalDw.ToString()
											+  "\nDalilyMaxLoss.\t >>> \t   " + dailyMaxLoss	
											+  "\nDalilyMaxProfit.\t >>> \t   " + dailyMinPnLToSecure
											+  "\nMaxTradesPerSession.\t >>> \t   " + MaxTradesPerSession	
										//	+ "\nAutoBotConf.\t >>> \t   " + (AutoBotConf ? " SI " : " NO ")
											+ "\nBotConf.\t >>> \t   " + (BotConfTrending ? " >>> Trending" : ">>> Balanced <<<") 
											+ "\nTarget.\t >>> \t   " + (BotConfTrending ? Target.ToString() : TargetReduced.ToString())	
											+ "\nStop.\t >>> \t   " + (BotConfTrending ? Stop.ToString() : StopReduced.ToString())	
											+  "\nCurrentPosition.\t >>> \t   " + currentPosition
											+  "\niCurrentPosition.\t >>> \t   " + iCurrentPosition
				, DisplayMRZInfoOrientation, ChartControl.Properties.ChartText,
  					ChartControl.Properties.LabelFont, Brushes.Green, Brushes.Transparent, 0);		
			}			
		}
		
		protected void PresentaDisplayText2()
		{
			if (DisplayText)
				
			{	
				
				Draw.TextFixed(this, "OrderBox",    "Time.------------->\t " + Times[0][0].TimeOfDay
												+ "\nTarget_Price.----->\t   " + Target_Price.ToString()
												+ "\nStop_Price.------->\t   " + Stop_Price.ToString()
												+ "\nTrail_Trigger.---->\t   " + Stop_Trigger.ToString()
												+ "\nBE_Trigger.------->\t   " + BEPrice_Trigger.ToString()
												+ "\nTarget.----------->\t   " + (BotConfTrending ? Target.ToString() : TargetReduced.ToString())	
												+ "\nStop.------------->\t   " + (BotConfTrending ? Stop.ToString() : StopReduced.ToString())	
												
				,TextPosition.BottomLeft, Brushes.White, new Gui.Tools.SimpleFont("Arial", 12), Brushes.Gold, Brushes.Black, 80);
			}				
		}			
		#endregion
				
		#region Properties
		
		#region 00. General
		
		[ReadOnly(true)]
		[NinjaScriptProperty]
		[Display(Name="BaseAlgoVersion", Order=1, GroupName="00. Strategy Information")]
		public string BaseAlgoVersion
		{ get; set; }
		
		[ReadOnly(true)]
		[NinjaScriptProperty]
		[Display(Name="StrategyVersion", Order=2, GroupName="00. Strategy Information")]
		public string StrategyVersion
		{ get; set; }
		
		[ReadOnly(true)]
		[NinjaScriptProperty]
		[Display(Name="Author", Order=3, GroupName="00. Strategy Information")]
		public string Author
		{ get; set; }
		
		[ReadOnly(true)]
		[NinjaScriptProperty]
		[Display(Name="Credits", Order=3, GroupName="00. Strategy Information")]
		public string Credits
		{ get; set; }
		
		[ReadOnly(true)]
		[NinjaScriptProperty]
		[Display(Name="Disclaimer", Order=4, GroupName="00. Strategy Information")]
		public string Disclaimer
		{ get; set; }

			
		[NinjaScriptProperty]
		[RefreshProperties(RefreshProperties.All)]
        [Display(Name = "Time_1", Order = 1, GroupName = "00a. Time Settings")]
        public bool Time_1
			{	get{
				return time_1;
			} 
			set {
				time_1 = value;
				
				if (time_1 == true) {
					showctrlTime_1 = true;
				} else {
					showctrlTime_1 = false;
				}
			}
			}

        [NinjaScriptProperty]
        [PropertyEditor("NinjaTrader.Gui.Tools.TimeEditorKey")]
        [Display(Name = "Start_Time_1", Order = 2, Description = "Start time to activate the strategy automatically (Timer 1)", GroupName = "00a. Time Settings")]
        public DateTime Start_Time_1
        { get; set; }

        [NinjaScriptProperty]
        [PropertyEditor("NinjaTrader.Gui.Tools.TimeEditorKey")]
        [Display(Name = "Stop_Time_1", Order = 3, Description = "Stop time to deactivate the strategy automatically (Timer 1)", GroupName = "00a. Time Settings")]
        public DateTime Stop_Time_1
        { get; set; }
		
		[XmlIgnore()]
		[NinjaScriptProperty]		
		[Display(Name = "Time1Color", Description = "Time1 background color", GroupName = "00a. Time Settings", Order = 4)]
        public Brush Time1Color
        {
            get { return time1Color; }
            set { time1Color = value; }
        }
		[Browsable(false)]
		public string Time1ColorSerialize
		{
			get { return Serialize.BrushToString(Time1Color); }
			set { Time1Color = Serialize.StringToBrush(value); }
		}

		
        [NinjaScriptProperty]
		[RefreshProperties(RefreshProperties.All)]
        [Display(Name = "Time_2", Order = 5, GroupName = "00a. Time Settings")]
        public bool Time_2
			{	get{
				return time_2;
			} 
			set {
				time_2 = value;
				
				if (time_2 == true) {
					showctrlTime_2 = true;
				} else {
					showctrlTime_2 = false;
				}
			}
			}

        [NinjaScriptProperty]
        [PropertyEditor("NinjaTrader.Gui.Tools.TimeEditorKey")]
        [Display(Name = "Start_Time_2", Order = 6, GroupName = "00a. Time Settings")]
        public DateTime Start_Time_2
        { get; set; }

        [NinjaScriptProperty]
        [PropertyEditor("NinjaTrader.Gui.Tools.TimeEditorKey")]
        [Display(Name = "Stop_Time_2",  Order = 7, GroupName = "00a. Time Settings")]
        public DateTime Stop_Time_2
        { get; set; }
		
		[XmlIgnore()]
		[NinjaScriptProperty]		
		[Display(Name = "Time2Color", Description = "Time2 background color", GroupName = "00a. Time Settings", Order = 8)]
        public Brush Time2Color
        {
            get { return time2Color; }
            set { time2Color = value; }
        }
		[Browsable(false)]
		public string Time2ColorSerialize
		{
			get { return Serialize.BrushToString(Time2Color); }
			set { Time2Color = Serialize.StringToBrush(value); }
		}

        [NinjaScriptProperty]
        [RefreshProperties(RefreshProperties.All)]
		[Display(Name = "Time_3", Order = 9, GroupName = "00a. Time Settings")]
        public bool Time_3
			{	get{
				return time_3;
			} 
			set {
				time_3 = value;
				
				if (time_3 == true) {
					showctrlTime_3 = true;
				} else {
					showctrlTime_3 = false;
				}
			}
			}

        [NinjaScriptProperty]
        [PropertyEditor("NinjaTrader.Gui.Tools.TimeEditorKey")]
        [Display(Name = "Start_Time_3", Order = 10, GroupName = "00a. Time Settings")]
        public DateTime Start_Time_3
        { get; set; }

        [NinjaScriptProperty]
        [PropertyEditor("NinjaTrader.Gui.Tools.TimeEditorKey")]
        [Display(Name = "Stop_Time_3", Order = 11, GroupName = "00a. Time Settings")]
        public DateTime Stop_Time_3
        { get; set; }
		
		[XmlIgnore()]
		[NinjaScriptProperty]		
		[Display(Name = "Time3Color", Description = "Time3 background color", GroupName = "00a. Time Settings", Order = 12)]
        public Brush Time3Color
        {
            get { return time3Color; }
            set { time3Color = value; }
        }
		[Browsable(false)]
		public string Time3ColorSerialize
		{
			get { return Serialize.BrushToString(Time3Color); }
			set { Time3Color = Serialize.StringToBrush(value); }
		}

        [NinjaScriptProperty]
        [RefreshProperties(RefreshProperties.All)]
		[Display(Name = "Time_4", Order = 13, GroupName = "00a. Time Settings")]
        public bool Time_4
			{	get{
				return time_4;
			} 
			set {
				time_4 = value;
				
				if (time_4 == true) {
					showctrlTime_4 = true;
				} else {
					showctrlTime_4 = false;
				}
			}
			}

        [NinjaScriptProperty]
        [PropertyEditor("NinjaTrader.Gui.Tools.TimeEditorKey")]
        [Display(Name = "Start_Time_4", Order = 14, GroupName = "00a. Time Settings")]
        public DateTime Start_Time_4
        { get; set; }

        [NinjaScriptProperty]
        [PropertyEditor("NinjaTrader.Gui.Tools.TimeEditorKey")]
        [Display(Name = "Stop_Time_4", Order = 15, GroupName = "00a. Time Settings")]
        public DateTime Stop_Time_4
        { get; set; }
		
		[XmlIgnore()]
		[NinjaScriptProperty]		
		[Display(Name = "Time4Color", Description = "Time4 background color", GroupName = "00a. Time Settings", Order = 16)]
        public Brush Time4Color
        {
            get { return time4Color; }
            set { time4Color = value; }
        }
		[Browsable(false)]
		public string Time4ColorSerialize
		{
			get { return Serialize.BrushToString(Time4Color); }
			set { Time4Color = Serialize.StringToBrush(value); }
		}		
		
//		[NinjaScriptProperty]
//		[Display(ResourceType = typeof(Custom.Resource), Name = "Start Time", GroupName = "00a. Time Settings (HHMMSS Format)", Order = 1)]
//		public int startTime
//		{ get; set; }
		
//		[NinjaScriptProperty]
//		[Display(ResourceType = typeof(Custom.Resource), Name = "End Time", GroupName = "00a. Time Settings (HHMMSS Format)", Order = 2)]
//		public int endTime
//		{ get; set; }
		
		[NinjaScriptProperty]
		[Display(ResourceType = typeof(Custom.Resource), Name = "Skip Econ Data Time1 or (Set To 0)", Description = "News time 1 to deactivate the strategy the number of minutes set below", GroupName = "00a. Time Settings (HHMMSS Format)", Order = 3)]
		public int econNumber1
		{ get; set; }

		[NinjaScriptProperty]
		[Display(ResourceType = typeof(Custom.Resource), Name = "Skip Econ Data Time2 or (Set To 0)", GroupName = "00a. Time Settings (HHMMSS Format)", Order = 4)]
		public int econNumber2
		{ get; set; }

		[NinjaScriptProperty]
		[Display(ResourceType = typeof(Custom.Resource), Name = "Skip X Minutes (Set To Minutes x 100)", GroupName = "00a. Time Settings (HHMMSS Format)", Order = 5)]
		public int iMinutes
		{ get; set; }
	
		#endregion
		
		#region 01. Position Size & Management
		
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="MaxTradesPerSession", Order=0, GroupName="01. Position Size & Management")]
		public int MaxTradesPerSession
		{ get; set; }
		
		[Range(1, int.MaxValue)]
		[NinjaScriptProperty]
		[Display(Name="Custom Position Amount", Order=1, GroupName="01. Position Size & Management")]
		public int CustomPositionAmount
		{ get; set; }

		[Range(1, int.MaxValue)]
		[NinjaScriptProperty]
		[Display(Name="Max. Position per Trade", Order=2, GroupName="01. Position Size & Management")]
		public int MaxPositionPerTrade
		{ get; set; }	
		
		[Range(-10000, int.MaxValue)]
		[NinjaScriptProperty]
		[Display(Name="BarsSinceExitExecution", Order=3, GroupName="01. Position Size & Management")]
		public int iBarsSinceExitExecution
		{ get; set; }		

		[Range(-10000, int.MaxValue)]
		[NinjaScriptProperty]
		[Display(Name="BarsSinceEntryExecution", Order=4, GroupName="01. Position Size & Management")]
		public int iBarsSinceEntryExecution
		{ get; set; }				
		
		
		[NinjaScriptProperty]
		[RefreshProperties(RefreshProperties.All)]
		[Display(Name="Daily Risk Size Control", Order=3, GroupName="01a. Management - Daily Risk")]
		public bool CtrlDailyMaxLoss
			{	get{
				return ctrlDailyMaxLoss;
			} 
			set {
				ctrlDailyMaxLoss = value;
				
				if (ctrlDailyMaxLoss == true) {
					showctrlDailyMaxLoss = true;
				} else {
					showctrlDailyMaxLoss = false;
				}
			}
			}
			
		[NinjaScriptProperty]
		[Range(1, double.MaxValue)]
		[Display(Name="Daily Risk Size($)", Order=4, GroupName="01a. Management - Daily Risk")]
		public double dailyMaxLoss
		{ get; set; }

		[NinjaScriptProperty]
		[RefreshProperties(RefreshProperties.All)]
		[Display(Name="Daily MinPnLToSecure Control", Order=5, GroupName="01a. Management - Daily Risk")]
		public bool CtrlDailyMinPnLToSecure
			{	get{
				return ctrlDailyMinPnLToSecure;
			} 
			set {
				ctrlDailyMinPnLToSecure = value;
				
				if (ctrlDailyMinPnLToSecure == true) {
					showctrlDailyMinPnLToSecure = true;
				} else {
					showctrlDailyMinPnLToSecure = false;
				}
			}
			}
		
		[NinjaScriptProperty]
		[Range(1, double.MaxValue)]
		[Display(Name="Daily MinPnLToSecure ($)", Order=6, GroupName="01a. Management - Daily Risk")]
		public double dailyMinPnLToSecure
		{ get; set; }		
				
		#endregion
	
		#region 02. Stops
		
	
		
		[NinjaScriptProperty]
		[RefreshProperties(RefreshProperties.All)]
		[Display(Name="Set Stop Loss", Order=0, GroupName="02. Stops")]
		public bool StopLoss
			{	get{
				return stopLoss;
			} 
			set {
				stopLoss = value;
				
				if (stopLoss == true) {
					showctrlStopLoss = true;
				} else {
					showctrlStopLoss = false;
				}
			}
			}

		[NinjaScriptProperty]
        [Display(ResourceType = typeof(Custom.Resource), Name = "StopLossType", Description= "Type of Stop Loss", GroupName = "02. Stops", Order = 1)]
        [RefreshProperties(RefreshProperties.All)]
		public CommonEnumsVGA.StopLossType StopLossType
        { 
			get { return stopLossType; } 
			set { 
				stopLossType = value; 
				
				if (stopLossType == CommonEnumsVGA.StopLossType.Fixed) {
					showFixedStopLossOptions = true;
					showATRStopLossOptions = false;
				} else if (stopLossType == CommonEnumsVGA.StopLossType.ATR) {
					showFixedStopLossOptions = false;
					showATRStopLossOptions = true;
				}
				}
		}				
					
		[NinjaScriptProperty]
        [Range(1, int.MaxValue)]
		[Display(Name="StopLoss_ATR_Period", Order=4, GroupName="02. Stops")]
		public int StopLoss_ATR_Period
		{ get; set; }

		[NinjaScriptProperty]
		[Range(0.1, double.MaxValue)]
		[Display(Name="StopLoss_ATR_Mult", Order=5, GroupName="02. Stops")]
		public double StopLoss_ATR_Mult
		{ get; set; }		
		
		
			
		[NinjaScriptProperty]
		[Display(Name="Tick Offset Stop (entryMarket Button)", Order=2, GroupName="02. Stops")]
		public double meInitialStop
		{ get; set; }

		[NinjaScriptProperty]
		[Display(Name="AE Tick Offset Stop", Order=3, GroupName="02. Stops")]
		public double InitialStop
		{ get; set; }		
		
		#endregion	
	
		#region 03. Profit Targets
		
		[NinjaScriptProperty]
		[RefreshProperties(RefreshProperties.All)]
		[Display(Name="Set Profit Target", Order=0, GroupName="03. Targets")]
		public bool bProfitTarget
			{	get{
				return bprofitTarget;
			} 
			set {
				bprofitTarget = value;
				
				if (bprofitTarget == true) {
					showctrlProfitTarget = true;
				} else {
					showctrlProfitTarget = false;
				}
			}
			}

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="Final Target (entryMarket Button)", Order=1, GroupName="03. Targets")]
		public int meProfitTarget
		{ get; set; }

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="AE Final Target", Order=2, GroupName="03. Targets")]
		public int ProfitTarget
		{ get; set; }			
		
		#endregion
			
		#region 04. Breakeven
		
		//Breakeven Actual				
		[NinjaScriptProperty]
		[RefreshProperties(RefreshProperties.All)]	
		[Display(Name="Breakeven Set Auto", Order=1, GroupName="04. Breakeven")]	
		public bool BESetAuto
			{	get{
				return beSetAuto;
			} 
			set {
				beSetAuto = value;
				
				if (beSetAuto == true) {
					showctrlBESetAuto = true;
				} else {
					showctrlBESetAuto = false;
				}
			}
			}
		
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="BE_Trigger", Order=2, Description="In Ticks", GroupName="04. Breakeven")]
		public int BE_Trigger
		{ get; set; }

		[NinjaScriptProperty]
		[Display(Name="BE_Size", Order=3, Description="In Ticks", GroupName="04. Breakeven")]
		public int BE_Size
		{ get; set; }
			
		#endregion
		
		#region 05. Trail Stop Offset

		//Trail Offset
// showctrlTrailSetAuto

		[NinjaScriptProperty]
		[RefreshProperties(RefreshProperties.All)]
		[Display(Name = "Trail Stop Set Auto", Order = 0, GroupName = "05. Trail Stop")]
		public bool TrailSetAuto
			{	get{
				return trailSetAuto;
			} 
			set {
				trailSetAuto = value;
				
				if (trailSetAuto == true) {
					showctrlTrailSetAuto = true;
				} else {
					showctrlTrailSetAuto = false;
				}
			}
			}

		[NinjaScriptProperty]
		[Display(Name="Trail Trigger", Order=1, GroupName="05. Trail Stop")]
		public double Trail_Trigger	
		{ get; set; }
		
		[NinjaScriptProperty]
		[Display(Name="Trail Size in Ticks", Order=2, GroupName="05. Trail Stop")]
		public int Trail_Size
		{ get; set; }

		[NinjaScriptProperty]
		[Display(Name="Trail Frecuency in Ticks", Order=2, GroupName="05. Trail Stop")]
		public int Trail_frequency
		{ get; set; }

		
		#endregion
		
		#region 06. SignalControl
		#endregion
		
		#region 07. AutoBotConf
//		[NinjaScriptProperty]
//		[Display(Name="Use AutoBot Conf Rutines? ", Order=0, GroupName="07. AutoBot Configuration")]
//		public bool AutoBotConf
//		{ get; set; }	

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="Reduced Target (BotConf: Balanced)", Order=1, Description="In Ticks", GroupName="07. AutoBot Configuration")]
		public int TargetReduced
		{ get; set; }

		[NinjaScriptProperty]
		[Display(Name="Reduced Stop (BotConf: Balanced)", Order=2, Description="In Ticks", GroupName="07. AutoBot Configuration")]
		public int StopReduced
		{ get; set; }				

		[NinjaScriptProperty]
		[Display(Name="Use Reversal Signal to Close position? ", Order=3, GroupName="07. AutoBot Configuration")]
		public bool autoRS
		{ get; set; }			
		
		[NinjaScriptProperty]
		[Display(Name="Use Reversal Signal to REV position? ", Order=4, GroupName="07. AutoBot Configuration")]
		public bool autoRV
		{ get; set; }				
		
		[NinjaScriptProperty]
		[Display(Name="Use XBars Continuation Signals?", Order=5, Description="Continue the movement making a reentry when the condition of X bars in a row is met", GroupName="07. AutoBot Configuration")]
		public bool useXBarsContSignal
		{ get; set; }				
		
		[NinjaScriptProperty]
		[Range(0, int.MaxValue)]
        [Display(Name = "Cont. Signals Quantity", Order = 6, GroupName = "07. AutoBot Configuration")]
		public int xBarsCounter
        { 
			get; set;
		}				
		
		
		#endregion

		#region 8. Custom SignalControl		
		#endregion

		#region 9. HistoricalTradePerformance & Manual Range Zone
		[NinjaScriptProperty]
        [Display(Name = "DisplayHistoricalTradePerformance", Order = 0, GroupName = "09. HistoricalTradePerformance")]
        public bool ShowHistorical
        { 
			get; set;
		}
		
		[NinjaScriptProperty]
        [Display(ResourceType = typeof(Custom.Resource), Name = "DisplayHistoricalTradePerformanceOrientation", GroupName = "09. HistoricalTradePerformance", Order = 1)]
		public TextPosition DisplayHistoricalTradePerformanceOrientation
        { get; set; }

		[NinjaScriptProperty]
        [Display(Name = "DisplayStrategyPnL", Order = 2, GroupName = "09. HistoricalTradePerformance")]
        public bool DisplayStrategyPnL
        { 
			get; set;
		}		
		
		[NinjaScriptProperty]
        [Display(ResourceType = typeof(Custom.Resource), Name = "DisplayStrategyPnLOrientation", GroupName = "09. HistoricalTradePerformance", Order = 3)]
		public TextPosition DisplayStrategyPnLOrientation
        { get; set; }			
		
		[NinjaScriptProperty]
        [Display(Name = "Use Manual Range Zone", Order = 0, GroupName = "09a. MRZInfo")]
        public bool useMRZ
        { 
			get; set;
		}			
		
		[NinjaScriptProperty]
        [Display(Name = "DisplayMRZInfo", Order = 1, GroupName = "09a. MRZInfo")]
        public bool DisplayMRZInfo
        { 
			get; set;
		}		
		
		[NinjaScriptProperty]
        [Display(ResourceType = typeof(Custom.Resource), Name = "DisplayMRZInfoOrientation", GroupName = "09a. MRZInfo", Order = 2)]
		public TextPosition DisplayMRZInfoOrientation
        { get; set; }	
		
		#endregion
		
		#region 97. Control visual indicadores						
		#endregion		
			
		#region 98. Version		
		#endregion
		
		#region 99. Prints
		
		[NinjaScriptProperty]
		[Display(Name="Display Text", Order=0, GroupName="99. Prints")]
		public bool DisplayText
		{ get; set; }
				
		[NinjaScriptProperty]
		[Display(Name="SystemPrint", Order=4, GroupName="99. Prints")]
		public bool SystemPrint
		{ get; set; }
		
		[NinjaScriptProperty]
		[Display(Name="BreakevenPrints", Order=5, GroupName="99. Prints")]
		public bool BreakevenPrints
		{ get; set; }
		
		[NinjaScriptProperty]
		[Display(Name="TrailPrints", Order=6, GroupName="99. Prints")]
		public bool TrailPrints
		{ get; set; }
		//ProfitTatgetPrints
		[NinjaScriptProperty]
		[Display(Name="OrdersPrints", Order=7, GroupName="99. Prints")]
		public bool OrdersPrints
		{ get; set; }		

		[NinjaScriptProperty]
		[Display(Name="SignalsPrints", Order=8, GroupName="99. Prints")]
		public bool SignalsPrints
		{ get; set; }		

		[NinjaScriptProperty]
		[Display(Name="BaseSignalsPrints", Order=9, GroupName="99. Prints")]
		public bool BaseSignalsPrints
		{ get; set; }		
		
		#endregion
		
		#region ResourdeEnumConverter
		[TypeConverter("NinjaTrader.Custom.ResourceEnumConverter")]
		public enum DaySessionType
		{
			TrendingUp,
			Balanced,
			TrendingDown,
		}
		
//		[TypeConverter("NinjaTrader.Custom.ResourceEnumConverter")]
//		public enum BBBandCross
//		{
//			HigherBand,
//			MiddleBand,
//			LowerBand,
//		}
		#endregion
			
		#region Custom Property Manipulation
		
		public void ModifyDailyRiskSizeProperties(PropertyDescriptorCollection col) {
			if (showctrlDailyMaxLoss == false) {
				col.Remove(col.Find("dailyMaxLoss", true));
			}
		}

		public void ModifyDailyMinPnLToSecureProperties(PropertyDescriptorCollection col) {
			if (showctrlDailyMinPnLToSecure == false) {
				col.Remove(col.Find("dailyMinPnLToSecure", true));
			}
		}		
		
		public void ModifyTime_1Properties(PropertyDescriptorCollection col) {
			if (showctrlTime_1 == false) {
				col.Remove(col.Find("Start_Time_1", true));
				col.Remove(col.Find("Stop_Time_1", true));
				col.Remove(col.Find("Time1Color", true));
			}
		}

		public void ModifyTime_2Properties(PropertyDescriptorCollection col) {
			if (showctrlTime_2 == false) {
				col.Remove(col.Find("Start_Time_2", true));
				col.Remove(col.Find("Stop_Time_2", true));
				col.Remove(col.Find("Time2Color", true));
			}
		}		
		
		public void ModifyTime_3Properties(PropertyDescriptorCollection col) {
			if (showctrlTime_3 == false) {
				col.Remove(col.Find("Start_Time_3", true));
				col.Remove(col.Find("Stop_Time_3", true));
				col.Remove(col.Find("Time3Color", true));
			}
		}		
		
		public void ModifyTime_4Properties(PropertyDescriptorCollection col) {
			if (showctrlTime_4 == false) {
				col.Remove(col.Find("Start_Time_4", true));
				col.Remove(col.Find("Stop_Time_4", true));
				col.Remove(col.Find("Time4Color", true));
			}
		}		
		
//		public void ModifyFilterMSTrendProperties(PropertyDescriptorCollection col) {
//			if (showctrlFilterMSTrend == false) {
//				col.Remove(col.Find("Period", true));
//				col.Remove(col.Find("bLbl", true));
//				col.Remove(col.Find("iDist", true));
//				col.Remove(col.Find("iFontSz", true));
//				col.Remove(col.Find("BuyClr", true));
//				col.Remove(col.Find("SellClr", true));
//				col.Remove(col.Find("iWdth", true));
//				col.Remove(col.Find("eStyle", true));			
//			}
//		}		

		public void ModifyStopLossProperties(PropertyDescriptorCollection col) {
			if (showctrlStopLoss == false) {
				col.Remove(col.Find("StopLossType", true));
				col.Remove(col.Find("meInitialStop", true));
				col.Remove(col.Find("InitialStop", true));
			}
		}			
		
		private void ModifyStopLossTypeProperties(PropertyDescriptorCollection col) {
			if (showFixedStopLossOptions == false) {
				col.Remove(col.Find("meInitialStop", true));
				col.Remove(col.Find("InitialStop", true));
			} 
			if (showATRStopLossOptions== false) {
				col.Remove(col.Find("StopLoss_ATR_Period", true));
				col.Remove(col.Find("StopLoss_ATR_Mult", true));
			}
		}	

		public void ModifyProfitTargetProperties(PropertyDescriptorCollection col) {
			if (showctrlProfitTarget == false) {
				col.Remove(col.Find("meProfitTarget", true));
				col.Remove(col.Find("ProfitTarget", true));
			}
		}			

		public void ModifyBESetAutoProperties(PropertyDescriptorCollection col) {
			if (showctrlBESetAuto == false) {
				col.Remove(col.Find("BE_Trigger", true));
				col.Remove(col.Find("BE_Size", true));
			}
		}		
		
		public void ModifyTrailSetAutoProperties(PropertyDescriptorCollection col) {
			if (showctrlTrailSetAuto == false) {
				col.Remove(col.Find("Trail_Trigger", true));
				col.Remove(col.Find("Trail_Size", true));
				col.Remove(col.Find("Trail_frequency", true));
			}
		}		
		
		#endregion	
		
		#region ICustomTypeDescriptor Members

        public AttributeCollection GetAttributes()
        {
            return TypeDescriptor.GetAttributes(GetType());
        }

        public string GetClassName()
        {
            return TypeDescriptor.GetClassName(GetType());
        }

        public string GetComponentName()
        {
            return TypeDescriptor.GetComponentName(GetType());
        }

        public TypeConverter GetConverter()
        {
            return TypeDescriptor.GetConverter(GetType());
        }

        public EventDescriptor GetDefaultEvent()
        {
            return TypeDescriptor.GetDefaultEvent(GetType());
        }

        public PropertyDescriptor GetDefaultProperty()
        {
            return TypeDescriptor.GetDefaultProperty(GetType());
        }

        public object GetEditor(Type editorBaseType)
        {
            return TypeDescriptor.GetEditor(GetType(), editorBaseType);
        }

        public EventDescriptorCollection GetEvents(Attribute[] attributes)
        {
            return TypeDescriptor.GetEvents(GetType(), attributes);
        }

        public EventDescriptorCollection GetEvents()
        {
            return TypeDescriptor.GetEvents(GetType());
        }

        public virtual PropertyDescriptorCollection GetProperties(Attribute[] attributes)
        {
            PropertyDescriptorCollection orig = TypeDescriptor.GetProperties(GetType(), attributes);
            PropertyDescriptor[] arr = new PropertyDescriptor[orig.Count];
            orig.CopyTo(arr, 0);
            PropertyDescriptorCollection col = new PropertyDescriptorCollection(arr);

			ModifyDailyRiskSizeProperties(col);
			ModifyDailyMinPnLToSecureProperties(col);
			ModifyTime_1Properties(col);
			ModifyTime_2Properties(col);
			ModifyTime_3Properties(col);			
			ModifyTime_4Properties(col);		
//			ModifyFilterMSTrendProperties(col);
			ModifyStopLossProperties(col);
			ModifyStopLossTypeProperties(col);
			ModifyProfitTargetProperties(col);
			ModifyBESetAutoProperties(col);
			ModifyTrailSetAutoProperties(col);

			return col;

        }

        public PropertyDescriptorCollection GetProperties()
        {
            return TypeDescriptor.GetProperties(GetType());
        }

        public object GetPropertyOwner(PropertyDescriptor pd)
        {
            return this;
        }

        #endregion		

		#endregion
	}
}

#region Strategy Enums
namespace CommonEnumsVGA
{

	public enum LimitType
	{
		CLOSE,
		HILO,
		CUSTOM
	}
	
	public enum StopLossType
	{
		Fixed,
		ATR

	}
	
	public enum ProfitTargetType
	{
		Fixed,
		ATR
	}
	
	public enum TrailStopType
	{
		TickTrail,
		ATRTrail,
		BarTrail
	}
	
	public enum FilterType
	{
		VWAP,
		EMA
	}
}

#endregion