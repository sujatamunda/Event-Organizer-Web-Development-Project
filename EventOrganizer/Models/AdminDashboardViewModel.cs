namespace EventOrganizer.Models
{
    public class AdminDashboardViewModel
    {

       
            public int TotalPendingBookings { get; set; }
            public int TotalApprovedBookings { get; set; }
            public int TotalRejectedBookings { get; set; }
            public int TotalCompletedBookings { get; set; }
            public int TotalServices { get; set; }
        

    }
}
