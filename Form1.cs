using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Data.Common;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using AutoMapper;
using LinqToExcel;

namespace ImportConfiguration
{
    public partial class Form1 : Form
    {
        private string excelPath = "";
        private string Author = "eoffice";
        private string _configFolder;

        //DEBUG
        

        public Form1()
        {
            _configFolder =  Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Constants.ConfigurationDirectory);
            InitializeComponent();
            PopulateRemappingTextBox();
            PopulateSingleFieldsTextBox();
            PopulateGroupsGridView();
            PopulateTransformationsTable();
        }

        private void btnOpenExcel_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "Excel Files|*.xls;*.xlsx;*.xlsm|All Files (*.*)|*.*";
            ofd.FilterIndex = 0;

            var ofdResult = ofd.ShowDialog();
            if (ofdResult == DialogResult.OK)
            {
                excelPath = ofd.FileName;
                lblPath.Text = ofd.FileName;
            }
        }

        private void btnRead_Click(object sender, EventArgs e)
        {
            //if (excelPath == "")
            //{
            //    MessageBox.Show("No excel file is selected!");
            //    return;
            //}

            Cursor.Current = Cursors.WaitCursor;
            if (bwImport.IsBusy == false)
            {
                txtOutput.Text += "Importing started...";       
                bwImport.RunWorkerAsync();
                progressBar1.Visible = true;
            }
        }

        public void PopulateRemappingTextBox()
        {
            var remappings = (from l in File.ReadAllLines(Path.Combine(_configFolder,Constants.ReMappingsFileName))
                          select l.Split(',')).ToList();
            var tableWidth = 120;
            foreach (var remapping in remappings.Skip(1))
            {
                dgRemappings.Rows.Add(remapping);
                // PrintRow(reMappingsTb, tableWidth, remapping);
                //reMappingsTb.Text += String.Format("|{0,15}|{1,15}|{2,15}|{3,15}|", remapping) + Environment.NewLine;
            }
            //remappingTb.Text = text;
        }

        public void PopulateSingleFieldsTextBox()
        {
            var singleFields = (from l in File.ReadAllLines(Path.Combine(_configFolder, Constants.SingleFieldsFileName),Encoding.UTF8)
                              select l.Split(',')).ToList();
            
            foreach (var remapping in singleFields)
            {

                dgSingleFields.Rows.Add(remapping);
            }
            //remappingTb.Text = text;
        }

        public void PopulateGroupsGridView()
        {
            var rows = (from l in File.ReadAllLines(Path.Combine(_configFolder, Constants.GroupingFileName))
                                select l.Split(',')).ToList();

            foreach (var remapping in rows)
            {

                dgGroups.Rows.Add(String.Join(" ⁞ ", remapping));
            }
            //remappingTb.Text = text;
        }

        public void PopulateTransformationsTable()
        {
            var singleFields = (from l in File.ReadAllLines(Path.Combine(_configFolder, Constants.TransformationsFileName), Encoding.UTF8)
                                select l.Split(',')).ToList();

            foreach (var remapping in singleFields.Skip(1))
            {

                dgTransformations.Rows.Add(remapping);
            }
            //remappingTb.Text = text;
        }

        private void bwImport_DoWork(object sender, DoWorkEventArgs e)
        {
            IImport importClass = null;
           
            importClass = new ImportConfiguration(this);
           
            importClass.Import(bwImport, excelPath, "import");
        }

        private void bwImport_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            if (e.UserState != null)
                txtOutput.Text += Environment.NewLine + e.UserState;
        }

        private void bwImport_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            try
            {
                Cursor.Current = Cursors.Default;
                txtOutput.Text += Environment.NewLine + "Izvoz zaključen.";                
                progressBar1.Visible = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }


        private void txtOutput_TextChanged(object sender, EventArgs e)
        {
            txtOutput.SelectionStart = txtOutput.Text.Length;
            txtOutput.ScrollToCaret();
        }

        static void PrintLine(TextBox tb, int tableWidth)
        {
            tb.Text += (new string('-', tableWidth))+Environment.NewLine;
        }

        static void PrintRow(TextBox tb, int tableWidth, params string[] columns)
        {
            int width = (tableWidth - columns.Length) / columns.Length;
            string row = "|";

            foreach (string column in columns)
            {
                row += AlignCentre(column, width) + "|";
            }

            tb.Text += row + Environment.NewLine;
        }

        static string AlignCentre(string text, int width)
        {
            text = text.Length > width ? text.Substring(0, width - 3) + "..." : text;

            if (string.IsNullOrEmpty(text))
            {
                return new string(' ', width);
            }
            else
            {
                return text.PadRight(width - (width - text.Length) / 2).PadLeft(width);
            }
        }

        private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {

        }
    }
}
