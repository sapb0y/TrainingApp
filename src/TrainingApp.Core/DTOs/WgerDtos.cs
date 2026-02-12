namespace TrainingApp.Core.DTOs;

public record WgerExerciseResponse(
    int Count,
    string? Next,
    string? Previous,
    List<WgerExerciseDto> Results
);

public record WgerExerciseDto(
    int Id,
    string Name,
    string? Description,
    int Category,
    List<int> Muscles,
    List<int> MusclesSecondary,
    List<int> Equipment
);

public record WgerCategoryResponse(
    int Count,
    List<WgerCategoryDto> Results
);

public record WgerCategoryDto(
    int Id,
    string Name
);

public record WgerMuscleResponse(
    int Count,
    List<WgerMuscleDto> Results
);

public record WgerMuscleDto(
    int Id,
    string Name,
    string NameEn,
    bool IsFront
);

public record WgerEquipmentResponse(
    int Count,
    List<WgerEquipmentDto> Results
);

public record WgerEquipmentDto(
    int Id,
    string Name
);

public record WgerImageResponse(
    int Count,
    string? Next,
    List<WgerImageDto> Results
);

public record WgerImageDto(
    int Id,
    int Exercise_Base,
    string Image,
    bool Is_Main
);
