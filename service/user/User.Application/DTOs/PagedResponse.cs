namespace User.Application.DTOs;

public record PagedResponse<T>(
    List<T> Items,
    int Total,
    int Page,
    int PageSize
);

