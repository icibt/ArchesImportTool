using System;

namespace ImportConfiguration.Entities
{
    public class FieldRemapping
    {
        public String From { get; set; }
        public String ToField { get; set; }
        public String TypeField { get; set; }
        public String TypeFieldValue { get; set; }
    }
}