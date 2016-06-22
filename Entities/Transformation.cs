using System;

namespace ImportConfiguration.Entities
{
    public class Transformation
    {
        public String Expression { get; set; }
        public String Cells { get; set; }
        public String ToField { get; set; }
        public String WithType { get; set; }
        public String TypeValue { get; set; }
    }
}