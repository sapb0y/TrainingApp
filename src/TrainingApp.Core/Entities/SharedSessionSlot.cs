namespace TrainingApp.Core.Entities;

public class SharedSessionSlot
{
    public Guid Id { get; set; }
    public Guid SharedSessionId { get; set; }
    public SharedSession? SharedSession { get; set; }
    public int SlotOrder { get; set; }
    public Guid? UserAExerciseId { get; set; }
    public int? UserASetNumber { get; set; }
    public string? UserAAction { get; set; }
    public Guid? UserBExerciseId { get; set; }
    public int? UserBSetNumber { get; set; }
    public string? UserBAction { get; set; }
    public int DurationSeconds { get; set; }
    public bool IsParallel { get; set; }
    public string? EquipmentNote { get; set; }
}
