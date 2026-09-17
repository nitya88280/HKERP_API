namespace HKERP.Domain.Entities
{
    public class ApiAccessLog
    {
        public long Id { get; set; }
        public Guid Ledger_ID { get; set; }
        public string ApiName { get; set; }
        public DateTime CallDateIST { get; set; }     
        public DateTime StartTimeIST { get; set; }
        public DateTime? EndTimeIST { get; set; }
        public int? ResponseTimeMs { get; set; }
        public bool IsSuccess { get; set; }
        public string Message { get; set; }
    }
}
