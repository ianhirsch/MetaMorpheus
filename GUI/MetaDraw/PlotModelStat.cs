﻿using EngineLayer;
using MetaMorpheusGUI;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using Proteomics.Fragmentation;
using Proteomics.ProteolyticDigestion;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace MetaMorpheusGUI
{
    public class PlotModelStat : INotifyPropertyChanged , IPlotModel
    {
        private const double STROKE_THICKNESS_UNANNOTATED = 0.5;
        private const double STROKE_THICKNESS_ANNOTATED = 2.0;
        private PlotModel privateModel;
        private List<MetaDrawPsm> allPSM;
        public List<string> plotNames = new List<string>{ "Histogram of Precursor PPM Errors (around 0 Da mass-difference notch only)",
                                                          "Histogram of Fragment PPM Errors",
                                                          "Histogram of Precursor Charges",
                                                          "Histogram of Fragment Charges",
                                                          "Precursor PPM Error vs. RT",
                                                          "Fragment PPM Error vs. RT",
                                                          "Histograms of Count of Different PTMs Seen at 1% FDR"};

        private static Dictionary<ProductType, OxyColor> productTypeDrawColors = new Dictionary<ProductType, OxyColor>
        {
          { ProductType.b, OxyColors.Blue },
          { ProductType.y, OxyColors.Purple },
          { ProductType.c, OxyColors.Gold },
          { ProductType.zPlusOne, OxyColors.Orange },
          { ProductType.D, OxyColors.DodgerBlue },
          { ProductType.M, OxyColors.Firebrick }
        };

        public PlotModel Model
        {
            get
            {
                return privateModel;
            }
            private set
            {
                privateModel = value;
                NotifyPropertyChanged("Model");
            }
        }

        public OxyColor Background => OxyColors.White;

        public event PropertyChangedEventHandler PropertyChanged;

        protected void NotifyPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public PlotModelStat()
        {

        }

        public PlotModelStat(string plotName, List<MetaDrawPsm> psms)
        {
            privateModel = new PlotModel { Title = plotName, Subtitle = "using OxyPlot" };
            allPSM = psms;
            createPlot(plotName);
        }

        public List<string> plotTypes()
        {
            return plotNames;
        }

        private void createPlot(string plotType)
        {
            if (plotType.Equals("Histogram of Precursor PPM Errors (around 0 Da mass-difference notch only)"))
            {
                histogramPlot(1);
            }
            else if (plotType.Equals("Histogram of Fragment PPM Errors"))
            {
                histogramPlot(2);
            }
            else if (plotType.Equals("Histogram of Precursor Charges"))
            {
                histogramPlot(3);
            }
            else if (plotType.Equals("Histogram of Fragment Charges"))
            {
                histogramPlot(4);
            }
            else if (plotType.Equals("Precursor PPM Error vs. RT"))
            {
                linePlot(1);
            }
            else if (plotType.Equals("Fragment PPM Error vs. RT"))
            {
                linePlot(2);
            }
            else if (plotType.Equals("Histograms of Count of Different PTMs Seen at 1% FDR"))
            {
                histogramPlot(5);
            }
        }

        private void histogramPlot(int plotType)
        {
            double binSize = -1;
            SortedList<double, double> numCategory = new SortedList<double, double>();
            IEnumerable<double> numbers = new List<double>();
            Dictionary<string, int> dict = new Dictionary<string, int>();
            HistogramSeries histogram = new HistogramSeries();

            switch (plotType)
            {
                case 1:
                    numbers = allPSM.Where(p => !p.MassDiffDa.Contains("|") && Math.Round(double.Parse(p.MassDiffDa), 0) == 0).Select(p => double.Parse(p.MassDiffPpm));
                    binSize = 0.1;
                    break;
                case 2:
                    numbers = allPSM.SelectMany(p => p.MatchedIons.Select(v => v.MassErrorPpm));
                    binSize = 0.1;
                    break;
                case 3:
                    numbers = allPSM.Select(p => (double)(p.PrecursorCharge));
                    var results = numbers.GroupBy(p => p).OrderBy(p => p.Key).Select(p => p);
                    dict = results.ToDictionary(p => p.Key.ToString(), v => v.Count());
                    break;
                case 4:
                    numbers = allPSM.SelectMany(p => p.MatchedIons.Select(v => (double)v.Charge));
                    results = numbers.GroupBy(p => p).OrderBy(p => p.Key).Select(p => p);
                    dict = results.ToDictionary(p => p.Key.ToString(), v => v.Count());
                    break;
                case 5:
                    var psmsWithMods = allPSM.Where(p => !p.FullSequence.Contains("|") && p.FullSequence.Contains("["));
                    var mods = psmsWithMods.Select(p => new PeptideWithSetModifications(p.FullSequence, GlobalVariables.AllModsKnownDictionary)).Select(p => p.AllModsOneIsNterminus).SelectMany(p => p.Values);
                    var groupedMods = mods.GroupBy(p => p.IdWithMotif).ToList();
                    dict = groupedMods.ToDictionary(p => p.Key, v => v.Count());
                    break;
            }
            if (plotType >= 3)
            {
                ColumnSeries column = new ColumnSeries { ColumnWidth = 200, IsStacked = false };
                var counter = 0;
                String[] category = new string[dict.Count];
                foreach (var d in dict)
                {
                    column.Items.Add(new ColumnItem(d.Value, counter));
                    category[counter] = d.Key;
                    counter++;
                }
                this.privateModel.Axes.Add(new CategoryAxis
                {
                    ItemsSource = category
                });
                privateModel.Series.Add(column);
            }
            else
            {
                double end = numbers.Max();
                double start = numbers.Min();
                double bins = (end - start) / binSize;
                double numbins = bins * Math.Pow(10, normalizeNumber(bins));
                if (numbins == 0) { numbins++; end = end + binSize; }
                IEnumerable<HistogramItem> bars = HistogramHelpers.Collect(numbers, start, end, (int)numbins, true);
                foreach (var bar in bars)
                {
                    histogram.Items.Add(bar);
                }
                histogram.StrokeThickness = 0.5;
                histogram.LabelFontSize = 12;
                privateModel.Series.Add(histogram);
            }
        }

        private void linePlot(int plotType)
        {
            ScatterSeries series = new ScatterSeries();
            List<Tuple<double, double>> xy = new List<Tuple<double, double>>();
            var filteredList = allPSM.Where(p => !p.MassDiffDa.Contains("|") && Math.Round(double.Parse(p.MassDiffDa), 0) == 0).ToList();
            var test = allPSM.SelectMany(p => p.MatchedIons.Select(v => v.MassErrorPpm));
            switch (plotType)
            {
                case 1:
                    foreach (var psm in filteredList)
                    {
                        xy.Add(new Tuple<double, double>(double.Parse(psm.MassDiffPpm), psm.RetentionTime));
                    }
                    break;
                case 2:
                    foreach (var psm in allPSM)
                    {
                        foreach (var ion in psm.MatchedIons)
                        {
                            xy.Add(psm.RetentionTime, ion.MassErrorPpm);
                        }
                    }
                    break;
            }
            IOrderedEnumerable<Tuple<double, double>> sorted = xy.OrderBy(x => x.Item1);
            foreach (var val in sorted)
            {
                series.Points.Add(new ScatterPoint(val.Item1, val.Item2));
            }
            privateModel.Series.Add(series);
        }

        private static int normalizeNumber(double number)
        {
            string s = number.ToString("00.00E0");
            int i = Convert.ToInt32(s.Substring(s.Length - 1));
            return i;
        }

        public void Update(bool updateData)
        {
            
        }

        public void Render(IRenderContext rc, double width, double height)
        {
            
        }

        public void AttachPlotView(IPlotView plotView)
        {
            
        }
    }
}
