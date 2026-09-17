using HKERP.Application.Interfaces;
using HKERP.Domain.Common;
using HKERP.Infrastructure.Common;

namespace HKERP.Infrastructure.Services
{
    public class GenericSpService : IGenericSpService
    {
        private readonly ISqlHelper _sqlHelper;

        public GenericSpService(ISqlHelper sqlHelper)
        {
            _sqlHelper = sqlHelper;
        }

        public List<Dictionary<string, object>> ExecuteSp(string spName, string xml = null)
        {
            var ds = string.IsNullOrEmpty(xml)
                ? _sqlHelper.ExecuteSpNoParams(spName)
                : _sqlHelper.ExecuteSpWithXml(xml, spName);

            return ds.Tables.Count > 0
                ? CommonMethods.ConvertDataTableToList(ds.Tables[0])
                : new List<Dictionary<string, object>>();
        }

        public List<List<Dictionary<string, object>>> ExecuteSpMultiTable(string spName, string xml = null)
        {
            var ds = string.IsNullOrEmpty(xml)
                ? _sqlHelper.ExecuteSpNoParams(spName)
                : _sqlHelper.ExecuteSpWithXml(xml, spName);

            return CommonMethods.ConvertDataSetToList(ds);
        }
    }
}
