﻿using GUI;
using OxyPlot;
using Proteomics;
using Proteomics.ProteolyticDigestion;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Tasks;


namespace ProteaseGuruGUI
{
    /// <summary>
    /// Interaction logic for AllResultsWindow.xaml
    /// </summary>
    public partial class AllResultsWindow : UserControl
    {
        private readonly ObservableCollection<ProteaseSummaryForTreeView> SummaryForTreeViewObservableCollection;
        private readonly ObservableCollection<string> listOfProteinDbs; 
        ICollectionView proteinDBView;
        private readonly Dictionary<string, Dictionary<string, Dictionary<Protein, List<InSilicoPep>>>> PeptideByFile;
        List<string> DBSelected;
        Parameters UserParams;
        public Dictionary<string, Dictionary<string, string>> HistogramDataTable = new Dictionary<string, Dictionary<string, string>>();

        public AllResultsWindow()
        {
        }

        public AllResultsWindow(Dictionary<string, Dictionary<string, Dictionary<Protein, List<InSilicoPep>>>> peptideByFile, Parameters userParams) // change constructor to receive analysis information
        {
            InitializeComponent();
            PeptideByFile = peptideByFile;
            UserParams = userParams;
            listOfProteinDbs = new ObservableCollection<string>();
            DBSelected = new List<string>() { };
            SetUpDictionaries();
            SummaryForTreeViewObservableCollection = new ObservableCollection<ProteaseSummaryForTreeView>();
            GenerateResultsSummary();
            proteinDBView = CollectionViewSource.GetDefaultView(listOfProteinDbs);
            dataGridProteinDBs.DataContext = proteinDBView;

            ProteinCovMap.Content = new ProteinResultsWindow(PeptideByFile, userParams);
        }

        private void SetUpDictionaries()
        {
            // populate list of protein DBs
            foreach (var db in PeptideByFile.Keys)
            {
                listOfProteinDbs.Add(db);
            }
        }

        private void summaryProteinDB_SelectedCellsChanged(object sender, SelectedCellsChangedEventArgs e)
        {
            OnDBSelectionChanged(); // update all results tabs
        }

        private void GenerateResultsSummary()
        {
            if (PeptideByFile.Count > 1) // if there is more than one database then we need to do all database summary 
            {
                ProteaseSummaryForTreeView allDatabases = new ProteaseSummaryForTreeView("Cumulative Database Results:");                
                Dictionary<string, List<InSilicoPep>> allDatabasePeptidesByProtease = new Dictionary<string, List<InSilicoPep>>();              
                foreach (var database in PeptideByFile)
                {
                    foreach (var protease in database.Value)
                    {                        
                        if (allDatabasePeptidesByProtease.ContainsKey(protease.Key))
                        {
                            foreach (var protein in protease.Value)
                            {
                                allDatabasePeptidesByProtease[protease.Key].AddRange(protein.Value);
                            }
                        }
                        else
                        {                            
                            allDatabasePeptidesByProtease.Add(protease.Key, protease.Value.SelectMany(p=>p.Value).ToList());
                        }                       
                    }                        
                }

                if (UserParams.TreatModifiedPeptidesAsDifferent)
                {
                    foreach (var protease in allDatabasePeptidesByProtease)
                    {
                        string prot = protease.Key;
                        DigestionSummaryForTreeView thisDigestion = new DigestionSummaryForTreeView(prot + " Results:");
                        var peptidesToProteins = protease.Value.GroupBy(p => p.FullSequence).ToDictionary(group => group.Key, group => group.ToList());
                        List<InSilicoPep> allPeptides = peptidesToProteins.SelectMany(p => p.Value).ToList();
                        thisDigestion.Summary.Add(new SummaryForTreeView("Number of Peptides: " + allPeptides.Count));
                        thisDigestion.Summary.Add(new SummaryForTreeView("     Number of Distinct Peptide Sequences: " + allPeptides.Select(p => p.FullSequence).Distinct().Count()));
                        thisDigestion.Summary.Add(new SummaryForTreeView("Number of Unique Peptides: " + peptidesToProteins.Where(p => p.Value.Select(p => p.Protein).Distinct().Count() == 1).Count()));
                        thisDigestion.Summary.Add(new SummaryForTreeView("Number of Shared Peptides: " + peptidesToProteins.Where(p => p.Value.Select(p => p.Protein).Distinct().Count() > 1).Count()));
                        allDatabases.Summary.Add(thisDigestion);

                    }
                }
                else 
                {
                    foreach (var protease in allDatabasePeptidesByProtease)
                    {
                        string prot = protease.Key;
                        DigestionSummaryForTreeView thisDigestion = new DigestionSummaryForTreeView(prot + " Results:");
                        var peptidesToProteins = protease.Value.GroupBy(p => p.BaseSequence).ToDictionary(group => group.Key, group => group.ToList());
                        List<InSilicoPep> allPeptides = peptidesToProteins.SelectMany(p => p.Value).ToList();
                        thisDigestion.Summary.Add(new SummaryForTreeView("Number of Peptides: " + allPeptides.Count));
                        thisDigestion.Summary.Add(new SummaryForTreeView("     Number of Distinct Peptide Sequences: " + allPeptides.Select(p => p.BaseSequence).Distinct().Count()));
                        thisDigestion.Summary.Add(new SummaryForTreeView("Number of Unique Peptides: " + peptidesToProteins.Where(p => p.Value.Select(p => p.Protein).Distinct().Count() == 1).Count()));
                        thisDigestion.Summary.Add(new SummaryForTreeView("Number of Shared Peptides: " + peptidesToProteins.Where(p => p.Value.Select(p => p.Protein).Distinct().Count() > 1).Count()));
                        allDatabases.Summary.Add(thisDigestion);

                    }
                }

                

                SummaryForTreeViewObservableCollection.Add(allDatabases);
                foreach (var database in PeptideByFile)
                {
                    ProteaseSummaryForTreeView thisProtease = new ProteaseSummaryForTreeView(database.Key+ " Results:");
                    foreach (var protease in database.Value)
                    {
                        string prot = protease.Key;
                        DigestionSummaryForTreeView thisDigestion = new DigestionSummaryForTreeView(prot + " Results:");
                        var allPeptides = protease.Value.SelectMany(p => p.Value);
                        if (UserParams.TreatModifiedPeptidesAsDifferent)
                        {
                            thisDigestion.Summary.Add(new SummaryForTreeView("Number of Peptides: " + allPeptides.Count()));
                            thisDigestion.Summary.Add(new SummaryForTreeView("     Number of Distinct Peptide Sequences: " + allPeptides.Select(p => p.FullSequence).Distinct().Count()));
                            thisDigestion.Summary.Add(new SummaryForTreeView("Number of Unique Peptides: " + allPeptides.Where(pep => pep.Unique == true).Select(p => p.FullSequence).Distinct().Count()));
                            thisDigestion.Summary.Add(new SummaryForTreeView("Number of Shared Peptides: " + allPeptides.Where(pep => pep.Unique == false).Select(p => p.FullSequence).Distinct().Count()));
                        }
                        else 
                        {
                            thisDigestion.Summary.Add(new SummaryForTreeView("Number of Peptides: " + allPeptides.Count()));
                            thisDigestion.Summary.Add(new SummaryForTreeView("     Number of Distinct Peptide Sequences: " + allPeptides.Select(p => p.BaseSequence).Distinct().Count()));
                            thisDigestion.Summary.Add(new SummaryForTreeView("Number of Unique Peptides: " + allPeptides.Where(pep => pep.Unique == true).Select(p => p.BaseSequence).Distinct().Count()));
                            thisDigestion.Summary.Add(new SummaryForTreeView("Number of Shared Peptides: " + allPeptides.Where(pep => pep.Unique == false).Select(p => p.BaseSequence).Distinct().Count()));
                        }                       
                        
                        thisProtease.Summary.Add(thisDigestion);
                    }
                    SummaryForTreeViewObservableCollection.Add(thisProtease);
                }
                
            }
            else // if there is only one database then is results and all database results are the same thing
            {
                foreach (var database in PeptideByFile)
                {
                    ProteaseSummaryForTreeView thisProtease = new ProteaseSummaryForTreeView(database.Key + " Results:");
                    foreach (var protease in database.Value)
                    {
                        string prot = protease.Key;
                        DigestionSummaryForTreeView thisDigestion = new DigestionSummaryForTreeView( prot + " Results:");                        
                        var  allPeptides = protease.Value.SelectMany(p => p.Value);
                        if (UserParams.TreatModifiedPeptidesAsDifferent)
                        {
                            thisDigestion.Summary.Add(new SummaryForTreeView("Number of Peptides: " + allPeptides.Count()));
                            thisDigestion.Summary.Add(new SummaryForTreeView("     Number of Distinct Peptide Sequences: " + allPeptides.Select(p => p.FullSequence).Distinct().Count()));
                            thisDigestion.Summary.Add(new SummaryForTreeView("Number of Unique Peptides: " + allPeptides.Where(pep => pep.Unique == true).Select(p => p.FullSequence).Distinct().Count()));
                            thisDigestion.Summary.Add(new SummaryForTreeView("Number of Shared Peptides: " + allPeptides.Where(pep => pep.Unique == false).Select(p => p.FullSequence).Distinct().Count()));
                        }
                        else 
                        {
                            thisDigestion.Summary.Add(new SummaryForTreeView("Number of Peptides: " + allPeptides.Count()));
                            thisDigestion.Summary.Add(new SummaryForTreeView("     Number of Distinct Peptide Sequences: " + allPeptides.Select(p => p.BaseSequence).Distinct().Count()));
                            thisDigestion.Summary.Add(new SummaryForTreeView("Number of Unique Peptides: " + allPeptides.Where(pep => pep.Unique == true).Select(p => p.BaseSequence).Distinct().Count()));
                            thisDigestion.Summary.Add(new SummaryForTreeView("Number of Shared Peptides: " + allPeptides.Where(pep => pep.Unique == false).Select(p => p.BaseSequence).Distinct().Count()));
                        }
                                     
                        thisProtease.Summary.Add(thisDigestion);
                    }
                    SummaryForTreeViewObservableCollection.Add(thisProtease);
                }
            }
            ProteaseSummaryTreeView.DataContext = SummaryForTreeViewObservableCollection;          
        }

        private void OnDBSelectionChanged()
        {
            DBSelected.Clear();
            if (dataGridProteinDBs.SelectedItem == null)
            {
                DBSelected.Add(listOfProteinDbs.First());

                foreach (var db in DBSelected)
                {
                    var databasePeptides = PeptideByFile[db];
                }
            }
            else 
            {
                var dbs = dataGridProteinDBs.SelectedItems;
                foreach (var db in dbs)
                {
                    DBSelected.Add(db.ToString());
                }

                foreach (var db in DBSelected)
                {
                    var databasePeptides = PeptideByFile[db];
                }
            }
            
            
            
        }
        

        private async void PlotSelected(object sender, SelectionChangedEventArgs e)
        {
            HistogramDataTable.Clear();
            if (dataGridProteinDBs.SelectedItem == null)
            {
                DBSelected.Add(listOfProteinDbs.First());

                foreach (var db in DBSelected)
                {
                    var databasePeptides = PeptideByFile[db];
                }
            }

            ObservableCollection<InSilicoPep> peptides = new ObservableCollection<InSilicoPep>();
            Dictionary<string, ObservableCollection<InSilicoPep>> peptidesByProtease = new Dictionary<string, ObservableCollection<InSilicoPep>>();
            Dictionary<string, Dictionary<Protein, (double,double)>> sequenceCoverageByProtease = new Dictionary<string, Dictionary<Protein, (double,double)>>();
            var selectedPlot = HistogramComboBox.SelectedItem;
            var objectName = selectedPlot.ToString().Split(':');
            var plotName = objectName[1];

            //var comboBox = HistogramComboBox as ComboBox;
            //var plotName = comboBox.SelectedItem as string;

            foreach (var db in DBSelected)
            {
                var PeptidesForAllProteases = PeptideByFile[db];
                sequenceCoverageByProtease = CalculateProteinSequenceCoverage(PeptidesForAllProteases);
                foreach (var protease in PeptidesForAllProteases)
                {
                    ObservableCollection<InSilicoPep> proteasePeptides = new ObservableCollection<InSilicoPep>();
                    if (peptidesByProtease.ContainsKey(protease.Key))
                    {
                        foreach (var protein in protease.Value)
                        {
                            foreach (var peptide in protein.Value)
                            {
                                proteasePeptides.Add(peptide);
                                peptides.Add(peptide);
                            }
                        }
                        peptidesByProtease[protease.Key] = proteasePeptides;
                    }
                    else 
                    {
                        foreach (var protein in protease.Value)
                        {
                            foreach (var peptide in protein.Value)
                            {
                                proteasePeptides.Add(peptide);
                                peptides.Add(peptide);
                            }
                        }
                        peptidesByProtease.Add(protease.Key, proteasePeptides);
                    }
                }
            }            
            PlotModelStat plot = await Task.Run(() => new PlotModelStat(plotName, peptides, peptidesByProtease, sequenceCoverageByProtease));
            plotViewStat.DataContext = plot;
            HistogramDataTable = plot.DataTable;           
        }

        private void CreateTable_Click(object sender, RoutedEventArgs e)
        {
            DataTable table = new DataTable();
            table.Columns.Add("Bin Value", typeof(string));
            var proteaseList = HistogramDataTable.First().Value.Keys.ToList();
            foreach (var protease in proteaseList)
            {
                table.Columns.Add(protease, typeof(string));
            }
            foreach (var entry in HistogramDataTable)
            {
                string[] row = new string[proteaseList.Count()+1];
                int j = 0;
                row[j] = entry.Key;                
                foreach (var subentry in entry.Value)
                {
                    j++;
                    row[j] = subentry.Value;
                                        
                }
                table.Rows.Add(row);
            }

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < table.Columns.Count; i++)
            {
                sb.Append(table.Columns[i]);
                if (i < table.Columns.Count - 1)
                    sb.Append(',');
            }
            sb.AppendLine();
            foreach (DataRow dr in table.Rows)
            {
                for (int i = 0; i < table.Columns.Count; i++)
                {
                    sb.Append(dr[i].ToString());

                    if (i < table.Columns.Count - 1)
                        sb.Append(',');
                }
                sb.AppendLine();
            }

            var dataTable = sb.ToString();
            var plotName = HistogramComboBox.SelectedItem.ToString().Split(':');
            var fileDirectory = UserParams.OutputFolder;
            var fileName = String.Concat(plotName[1],"_DataTable", ".csv");
            File.WriteAllText(Path.Combine(fileDirectory, fileName), dataTable);
            MessageBox.Show("Data table Created at " + Path.Combine(fileDirectory, fileName) + "!");

        }

        private void CreatePlotPdf_Click(object sender, RoutedEventArgs e)
        {
            var selectedItem = HistogramComboBox.SelectedItem;

            if (selectedItem == null)
            {
                MessageBox.Show("Select a plot type to export!");
                return;
            }

            var plotName = HistogramComboBox.SelectedItem.ToString().Split(':');
            var fileDirectory = UserParams.OutputFolder;            
            var fileName = String.Concat(plotName[1], ".pdf");

            // update font sizes to exported PDF's size
            double tmpW = plotViewStat.Width;
            double tmpH = plotViewStat.Height;
            plotViewStat.Width = 1000;
            plotViewStat.Height = 700;
            plotViewStat.UpdateLayout();            

            using (Stream writePDF = File.Create(Path.Combine(fileDirectory, fileName)))
            {
                PdfExporter.Export(plotViewStat.Model, writePDF, 1000, 700);
            }
            plotViewStat.Width = tmpW;
            plotViewStat.Height = tmpH;
            MessageBox.Show("PDF Created at " + Path.Combine(fileDirectory, fileName) + "!");
        }

        private Dictionary<string, Dictionary<Protein, (double,double)>> CalculateProteinSequenceCoverage( Dictionary<string, Dictionary<Protein, List<InSilicoPep>>> peptidesByProtease)
        {
            Dictionary<string, Dictionary<Protein, (double,double)>> proteinSequenceCoverageByProtease = new Dictionary<string, Dictionary<Protein, (double,double)>>();
            foreach (var protease in peptidesByProtease)
            {
                Dictionary<Protein, (double,double)> sequenceCoverages = new Dictionary<Protein, (double,double)>();
                foreach (var protein in protease.Value)
                {
                    HashSet<int> coveredOneBasesResidues = new HashSet<int>();
                    HashSet<int> coveredOneBasesResiduesUnique = new HashSet<int>();
                    foreach (var peptide in protein.Value)
                    {
                        for (int i = peptide.StartResidue; i <= peptide.EndResidue; i++)
                        {
                            coveredOneBasesResidues.Add(i);
                            if (peptide.Unique == true)
                            {
                                coveredOneBasesResiduesUnique.Add(i);
                            }
                        }
                        

                    }
                    double seqCoverageFract = (double)coveredOneBasesResidues.Count / protein.Key.Length;
                    double seqCoverageFractUnique = (double)coveredOneBasesResiduesUnique.Count / protein.Key.Length;

                    sequenceCoverages.Add(protein.Key, (seqCoverageFract,seqCoverageFractUnique));
                }
                proteinSequenceCoverageByProtease.Add(protease.Key, sequenceCoverages);
            }            

            return proteinSequenceCoverageByProtease;
        }
    }
}
