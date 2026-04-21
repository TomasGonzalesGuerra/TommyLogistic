namespace TommyLogistic.Shared.DTOs.Dashboards;

public class DashboardAdminDTO
{
    public int Failed { get; set; }
    public int OnTheWay { get; set; }
    public int Assigned { get; set; }
    public int PickedUp { get; set; }
    public int Delivered { get; set; }
    public int Returning { get; set; }
    public int OnStorage { get; set; }
    public int TotalToday { get; set; }
    public int Registered { get; set; }
    public int Rescheduled { get; set; }
    public int RecipientAbsent { get; set; }
    public decimal EstimatedIncomeToday { get; set; }
}
