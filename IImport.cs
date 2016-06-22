using System.ComponentModel;

namespace ImportConfiguration
{
    public interface IImport
    {
        void Import(BackgroundWorker bwImport, string excelPath, string Author);
    }
}