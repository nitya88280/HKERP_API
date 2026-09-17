namespace HKERP.Application.Interfaces
{
    public interface IGenericSpService
    {
        List<Dictionary<string, object>> ExecuteSp(string spName, string xml = null);

        List<List<Dictionary<string, object>>> ExecuteSpMultiTable(string spName, string xml = null);
    }
}
