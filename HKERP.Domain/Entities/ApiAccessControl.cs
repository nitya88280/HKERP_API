namespace HKERP.Domain.Entities
{
    public class ApiAccessControl
    {
        public int Id { get; set; }
        public Guid Ledger_ID { get; set; }
        public string ApiName { get; set; }
        public int MaxCallsPerDay { get; set; }
        public TimeSpan AllowedFromTime { get; set; }   
        public TimeSpan AllowedToTime { get; set; }     
        public bool IsActive { get; set; }
    }
}
