namespace WorkoutLoggingService.BLL.DTOs;

public class GymVisitDTO
{
    public int Id { get; set; }
    public int GymId { get; set; }
    public DateTime VisitDate { get; set; }
}

public class LogGymVisitDTO
{
    public int GymId { get; set; }
}
