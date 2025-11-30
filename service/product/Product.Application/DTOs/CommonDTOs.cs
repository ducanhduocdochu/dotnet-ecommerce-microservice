namespace Product.Application.DTOs;

public record PagedResponse<T>(
    List<T> Items,
    int Total,
    int Page,
    int PageSize
);

public record ProductFilterRequest(
    Guid? CategoryId = null,
    Guid? BrandId = null,
    decimal? MinPrice = null,
    decimal? MaxPrice = null,
    string? Search = null,
    string? Status = null,
    string Sort = "newest", // newest, oldest, price_asc, price_desc, best_selling, rating
    int Page = 1,
    int PageSize = 20
);

