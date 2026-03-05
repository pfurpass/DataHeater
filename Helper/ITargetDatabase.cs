using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace DataHeater.Helper
{
    internal interface ITargetDatabase
    {
        Task<List<string>> GetTablesAsync();
        Task<DataTable> GetTableDataAsync(string tableName);
        Task CreateTableIfNotExistsAsync(DataTable schema, string tableName);
        Task TruncateTableAsync(string tableName);
        Task InsertDataAsync(string tableName, DataTable data);
    }
}