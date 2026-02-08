namespace TrainingApp.Core.Services;

public static class PartnerSchedulingService
{
    public record ExercisePlan(Guid UserId, Guid ExerciseId, string ExerciseName,
        List<string> Equipment, string Category, int Sets,
        int SetDurationSeconds, int RestSeconds, int OrderIndex);

    public enum ActionType { Work, Rest, Idle }

    public record SlotAction(Guid? ExerciseId, string? ExerciseName,
        int? SetNumber, ActionType Type, List<string>? Equipment);

    public record ScheduleBlock(int BlockOrder, SlotAction UserA, SlotAction UserB, int DurationSeconds);

    public record ScheduleSummary(int TotalSeconds, int SoloSecondsA, int SoloSecondsB,
        int TimeSavedSeconds, int EquipmentConflicts,
        decimal UtilizationPercentA, decimal UtilizationPercentB, List<string> Warnings);

    public record PartnerSchedule(List<ScheduleBlock> Blocks, ScheduleSummary Summary);

    private record PendingSet(Guid UserId, Guid ExerciseId, string ExerciseName,
        List<string> Equipment, string Category, int SetNumber, int SetDuration, int RestDuration, int OrderIndex);

    public static PartnerSchedule GenerateSchedule(List<ExercisePlan> planA, List<ExercisePlan> planB)
    {
        var blocks = new List<ScheduleBlock>();
        var queueA = BuildQueue(planA);
        var queueB = BuildQueue(planB);

        var idxA = 0;
        var idxB = 0;
        var blockOrder = 1;
        var equipmentConflicts = 0;

        // Track rest state: after work, must rest before next work
        var aRestRemaining = 0;
        var bRestRemaining = 0;

        while (idxA < queueA.Count || idxB < queueB.Count)
        {
            var aAvailable = idxA < queueA.Count && aRestRemaining <= 0;
            var bAvailable = idxB < queueB.Count && bRestRemaining <= 0;
            var aHasWork = idxA < queueA.Count;
            var bHasWork = idxB < queueB.Count;

            if (aAvailable && bAvailable)
            {
                var setA = queueA[idxA];
                var setB = queueB[idxB];

                if (!HasEquipmentConflict(setA.Equipment, setB.Equipment))
                {
                    // Both work in parallel
                    var duration = Math.Max(setA.SetDuration, setB.SetDuration);
                    blocks.Add(new ScheduleBlock(blockOrder++,
                        new SlotAction(setA.ExerciseId, setA.ExerciseName, setA.SetNumber, ActionType.Work, setA.Equipment),
                        new SlotAction(setB.ExerciseId, setB.ExerciseName, setB.SetNumber, ActionType.Work, setB.Equipment),
                        duration));
                    aRestRemaining = setA.RestDuration;
                    bRestRemaining = setB.RestDuration;
                    idxA++;
                    idxB++;
                }
                else
                {
                    // Equipment conflict — sequential: A works, B rests
                    equipmentConflicts++;
                    var setDur = setA.SetDuration;
                    blocks.Add(new ScheduleBlock(blockOrder++,
                        new SlotAction(setA.ExerciseId, setA.ExerciseName, setA.SetNumber, ActionType.Work, setA.Equipment),
                        new SlotAction(null, null, null, ActionType.Rest, null),
                        setDur));
                    aRestRemaining = setA.RestDuration;
                    bRestRemaining = Math.Max(0, bRestRemaining - setDur);
                    idxA++;
                }
            }
            else if (aAvailable && !bAvailable)
            {
                var setA = queueA[idxA];
                var duration = setA.SetDuration;
                blocks.Add(new ScheduleBlock(blockOrder++,
                    new SlotAction(setA.ExerciseId, setA.ExerciseName, setA.SetNumber, ActionType.Work, setA.Equipment),
                    new SlotAction(null, null, null, bHasWork ? ActionType.Rest : ActionType.Idle, null),
                    duration));
                aRestRemaining = setA.RestDuration;
                bRestRemaining = Math.Max(0, bRestRemaining - duration);
                idxA++;
            }
            else if (!aAvailable && bAvailable)
            {
                var setB = queueB[idxB];
                var duration = setB.SetDuration;
                blocks.Add(new ScheduleBlock(blockOrder++,
                    new SlotAction(null, null, null, aHasWork ? ActionType.Rest : ActionType.Idle, null),
                    new SlotAction(setB.ExerciseId, setB.ExerciseName, setB.SetNumber, ActionType.Work, setB.Equipment),
                    duration));
                bRestRemaining = setB.RestDuration;
                aRestRemaining = Math.Max(0, aRestRemaining - duration);
                idxB++;
            }
            else
            {
                // Both resting — use shorter rest as duration
                var restDur = Math.Min(
                    aHasWork ? aRestRemaining : 0,
                    bHasWork ? bRestRemaining : 0);
                if (restDur <= 0)
                    restDur = Math.Max(aRestRemaining, bRestRemaining);
                if (restDur <= 0)
                    break;

                blocks.Add(new ScheduleBlock(blockOrder++,
                    new SlotAction(null, null, null, aHasWork ? ActionType.Rest : ActionType.Idle, null),
                    new SlotAction(null, null, null, bHasWork ? ActionType.Rest : ActionType.Idle, null),
                    restDur));
                aRestRemaining = Math.Max(0, aRestRemaining - restDur);
                bRestRemaining = Math.Max(0, bRestRemaining - restDur);
            }
        }

        var totalSeconds = blocks.Sum(b => b.DurationSeconds);
        var soloA = EstimateSoloDuration(planA);
        var soloB = EstimateSoloDuration(planB);
        var maxSolo = Math.Max(soloA, soloB);
        var timeSaved = Math.Max(0, soloA + soloB - totalSeconds);

        var workSecondsA = blocks.Where(b => b.UserA.Type == ActionType.Work).Sum(b => b.DurationSeconds);
        var workSecondsB = blocks.Where(b => b.UserB.Type == ActionType.Work).Sum(b => b.DurationSeconds);
        var utilizationA = totalSeconds > 0 ? Math.Round((decimal)workSecondsA / totalSeconds * 100, 1) : 0m;
        var utilizationB = totalSeconds > 0 ? Math.Round((decimal)workSecondsB / totalSeconds * 100, 1) : 0m;

        var warnings = new List<string>();
        if (equipmentConflicts > 0)
            warnings.Add($"{equipmentConflicts} equipment conflict(s) required sequential execution.");

        var summary = new ScheduleSummary(totalSeconds, soloA, soloB, timeSaved, equipmentConflicts,
            utilizationA, utilizationB, warnings);

        return new PartnerSchedule(blocks, summary);
    }

    public static bool HasEquipmentConflict(List<string> equipA, List<string> equipB)
    {
        if (equipA.Count == 0 || equipB.Count == 0)
            return false;

        return equipA.Any(a => equipB.Any(b => string.Equals(a, b, StringComparison.OrdinalIgnoreCase)));
    }

    public static int EstimateSoloDuration(List<ExercisePlan> plan)
    {
        return plan.Sum(e => e.Sets * (e.SetDurationSeconds + e.RestSeconds));
    }

    public static int EstimateSetDuration(string category)
    {
        return category.ToLowerInvariant() switch
        {
            "chest" or "back" or "legs" or "shoulders" => 45,
            "arms" or "biceps" or "triceps" or "calves" or "abs" => 30,
            _ => 40
        };
    }

    private static List<PendingSet> BuildQueue(List<ExercisePlan> plan)
    {
        var queue = new List<PendingSet>();
        foreach (var exercise in plan.OrderBy(e => e.OrderIndex))
        {
            for (int s = 1; s <= exercise.Sets; s++)
            {
                queue.Add(new PendingSet(exercise.UserId, exercise.ExerciseId, exercise.ExerciseName,
                    exercise.Equipment, exercise.Category, s, exercise.SetDurationSeconds,
                    exercise.RestSeconds, exercise.OrderIndex));
            }
        }
        return queue;
    }
}
