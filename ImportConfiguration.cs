using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.Common;
using System.Data.Objects;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;
using AutoMapper;
using AutoMapper.Internal;
using ImportConfiguration.Entities;
using LinqToExcel;
using Remotion.Logging;
using GeoUtility;
using GeoUtility.GeoSystem;
using GeoUtility.GeoSystem.Helper;

namespace ImportConfiguration
{
    public class ImportConfiguration : IImport
    {
        private string Worksheet = "";
        private int rowNumber;
        private ExcelQueryFactory excelFile;
        private BackgroundWorker bwImport;
        private List<FieldRemapping> _remappings;
        private List<Transformation> _transformations;
        private List<string> _fieldsToImport;
        private FileInfo _archesFile;
        private int _currentGroup = 0;
        private Form1 form;
        private List<String[]> _groupings;
        private int _numInvalidCoordinate = 0;

        public ImportConfiguration(Form1 form)
        {
            this.form = form;
            InitializeConfiguration();
            _archesFile = new FileInfo(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"Import.arches"));
            if (_archesFile.Exists)
            {
                _archesFile.Delete();
            }
            
            
        }

        private void SetRemappingTextBox()
        {
            StringBuilder sb = new StringBuilder();
            foreach (var remapping in _remappings)
            {
                
            }
        }

        private void InitializeConfiguration()
        {
            var configFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Constants.ConfigurationDirectory);
            
            excelFile = new ExcelQueryFactory(Path.Combine(configFolder, Constants.ReMappingsFileName));

            _remappings = (from a in excelFile.Worksheet<FieldRemapping>()
                           select a).ToList();

            excelFile = new ExcelQueryFactory(Path.Combine(configFolder, Constants.TransformationsFileName));
            _transformations = (from a in excelFile.Worksheet<Transformation>() select a).ToList();

            _groupings = (from l in File.ReadAllLines(Path.Combine(configFolder, Constants.GroupingFileName))
                         select l.Split(',')).ToList();

            excelFile = new ExcelQueryFactory(Path.Combine(configFolder, Constants.SingleFieldsFileName));
            _fieldsToImport = (from a in excelFile.WorksheetNoHeader() select (string)a[0]).ToList().Where(x => !x.StartsWith("--")).ToList();

         
            //throw new NotImplementedException();
        }

        public void Import(BackgroundWorker bw, string excelPath, string Author)
        {
            excelFile = new ExcelQueryFactory(excelPath);
            int i = 1;
            WriteToArchesFile("RESOURCEID|RESOURCETYPE|ATTRIBUTENAME|ATTRIBUTEVALUE|GROUPID");
            foreach (var row in excelFile.Worksheet(0))
            {
                var resourceId = "ZVKDS" + i;
                ProcessSingleEntries(row, resourceId);
                ProcessReMappings(row, resourceId);
                ProcessGroupings(row,resourceId);
                ProcessTransformations(row, resourceId);
                ProcessMapCordinates(row,resourceId);
                bw.ReportProgress(0,String.Format("Izvoz entite: ZVKDS{0}",i));
                i++;
                _currentGroup = 0;
                //for (int i=0; i < row.Count; i++)
                //{
                //    //Cell cell = row[i];
                //    //var cellName = row.ColumnNames.ElementAt(i);

                //    //Console.WriteLine(String.Format("{0} : {1}",cellName,cell.Value));



                //}
            }
            Console.WriteLine("Neveljavne koordinate: "+_numInvalidCoordinate);
        }

        public void ProcessSingleEntries(Row row, string resourceId)
        {
            foreach (var field in _fieldsToImport)
            {
                var columnName = field.Replace('.', '#');
                var value = row[columnName].ToString().Trim();
                if (!string.IsNullOrEmpty(value))
                {
                    var splitedValues = value.Split(';');
                    foreach (var splitedValue in splitedValues)
                    {
                        WriteToArchesFile(resourceId, field, splitedValue, _currentGroup);
                        _currentGroup++;
                    }
                    
                }
                
            }
        }

        public void ProcessMapCordinates(Row row, string resourceId)
        {
            string x = row["x"];
            string y = row["y"];
            
            if (!string.IsNullOrEmpty(x) && !string.IsNullOrEmpty(y))
            {
                
                switch (y.Split(',')[0].Length)
                {
                    case 5:
                        var prefix = "50";//double.Parse(y) > 70000 ? "51" : "51";
                        y = prefix + y;
                        break;
                    case 6:
                        y = "5" + y;
                        break;
                }
                x = "5" + x;
                
                GaussKrueger gauss = new GaussKrueger(double.Parse(x), double.Parse(y));
                gauss.Precision = 1;
                //GaussKrueger gauss = new GaussKrueger(double.Parse(y) * 10 , double.Parse(x) * 10);
                //GaussKrueger gauss = new GaussKrueger(468238, 97016);
                try
                {
                    //MapService maps = (MapService) gauss;
                    //gauss.Precision
                    //maps.MapServer = MapService.Info.MapServer.GoogleMaps;
                   // maps.Zoom = 18;
                    Geographic geo = (Geographic) gauss;
                    var test = new Geographic(geo.Longitude,geo.Latitude,GeoDatum.Potsdam);
                    //geo.Datum = GeoDatum.Potsdam;
                    double lon = geo.Longitude;
                    double lat = geo.Latitude;
                    if (test.Longitude != lon)
                    {
                        Console.Out.Write("{0} {1}",lon, test.Longitude);
                    }
                    
                    string archesPoint = String.Format("POINT({0} {1})", lon.ToString("G", CultureInfo.InvariantCulture), lat.ToString("G", CultureInfo.InvariantCulture));
                    WriteToArchesFile(resourceId, "SPATIAL_COORDINATES_GEOMETRY.E47", archesPoint, _currentGroup);
                }
                catch (Exception e)
                {
                    _numInvalidCoordinate++;
                }
            }
            _currentGroup++;
        }

        public void ProcessGroupings(Row row, string resourceId)
        {
            foreach (var group in _groupings)
            {
                bool itemsAddedInGroup = false;
                foreach (var groupItem in group.Where(x => !x.Contains("|")))
                {
                    var columnName = groupItem.Replace('.', '#');
                    if (row.ColumnNames.Contains(columnName))
                    {
                        var cell = row[columnName];
                        if (cell != null && cell.Value != DBNull.Value)
                        {
                            itemsAddedInGroup = true;
                            WriteToArchesFile(resourceId, groupItem, cell.Value.ToString(), _currentGroup);
                        }
                        
                    }
                }
                foreach (var groupItem in group.Where(x => x.Contains("|")))
                {
                    var splitedValues = groupItem.Split('|');
                    if (splitedValues.Length == 2 && itemsAddedInGroup)
                    {
                       WriteToArchesFile(resourceId, splitedValues[0], splitedValues[1], _currentGroup);
                    }
                }
                _currentGroup++;
            }
        }

        public void ProcessReMappings(Row row,string resourceId)
        {
            foreach (var remapping in _remappings)
            {
                if (row.ColumnNames.Contains(remapping.From))
                {
                    var cell = row[remapping.From];
                    if (cell != null && cell.Value != DBNull.Value)
                    {
                        WriteToArchesFile(resourceId, remapping.ToField, cell.Value.ToString(), _currentGroup);
                        WriteToArchesFile(resourceId, remapping.TypeField, remapping.TypeFieldValue, _currentGroup);
                    }
                    _currentGroup++;
                }
                
            }
            
        }

        public void ProcessTransformations(Row row, string resourceId)
        {
            foreach (var transformation in _transformations)
            {
                var excelCells = transformation.Cells.Split(';');
                List<string> expressionValues = new List<string>();
                foreach (var excelCell in excelCells)
                {
                    if (row.ColumnNames.Contains(excelCell))
                    {
                        expressionValues.Add(row[excelCell].Value.ToString());
                    }
                    else
                    {
                        return;
                    }
                }

                if (expressionValues.Any(x => !string.IsNullOrEmpty(x)))
                {
                    WriteToArchesFile(resourceId, transformation.ToField,
                        String.Format(transformation.Expression, expressionValues.ToArray()), _currentGroup);
                    if (!string.IsNullOrEmpty(transformation.WithType))
                    {
                        WriteToArchesFile(resourceId, transformation.WithType, transformation.TypeValue, _currentGroup);
                    }
                    _currentGroup++;
                }
               
            }
        }

        public void WriteToArchesFile(string resourceId, string attributeName, string attributeValue, int? group, string resourceType= "HERITAGE_RESOURCE.E18")
        {
            using (StreamWriter sw = File.AppendText(_archesFile.FullName))
            {
                sw.WriteLine("{0}|{1}|{2}|{3}|{4}",resourceId,resourceType,attributeName, Regex.Replace(attributeValue.Trim(), @"\t|\n|\r", ""), 
                    group.HasValue ? "GRP"+ _currentGroup : string.Empty);
            }
        }

        public void WriteToArchesFile(string text)
        {
            using (StreamWriter sw = File.AppendText(_archesFile.FullName))
            {
                sw.WriteLine(text);
            }
        }
    }


}
