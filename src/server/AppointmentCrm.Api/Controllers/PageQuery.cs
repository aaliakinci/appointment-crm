using System.ComponentModel.DataAnnotations;
using AppointmentCrm.Application.Common;
using Microsoft.AspNetCore.Mvc;

namespace AppointmentCrm.Api.Controllers;

public abstract class PageQuery : IValidatableObject
{
    [FromQuery(Name = "page")]
    [Range(1, int.MaxValue, ErrorMessage = "Page must be greater than zero.")]
    public int Page { get; init; } = 1;

    [FromQuery(Name = "pageSize")]
    [Range(
        1,
        PageRequest.MaximumPageSize,
        ErrorMessage = "PageSize must be between 1 and 100.")]
    public int PageSize { get; init; } = PageRequest.DefaultPageSize;

    [FromQuery(Name = "search")]
    [StringLength(160, ErrorMessage = "Search cannot exceed 160 characters.")]
    public string? Search { get; init; }

    [FromQuery(Name = "sortBy")]
    public string? SortBy { get; init; }

    [FromQuery(Name = "sortDirection")]
    [RegularExpression(
        @"^\s*(?:(?i:asc|desc))?\s*$",
        ErrorMessage = "SortDirection must be 'asc' or 'desc'.")]
    public string? SortDirection { get; init; }

    protected virtual string DefaultSort => "createdAt";

    protected virtual IReadOnlySet<string> AllowedSorts { get; } = new HashSet<string>(
        ["createdAt"],
        StringComparer.Ordinal);

    internal PageRequest ToPageRequest()
    {
        string selectedSort = string.IsNullOrWhiteSpace(SortBy)
            ? DefaultSort
            : SortBy.Trim();
        bool descending = string.Equals(
            SortDirection?.Trim(),
            "desc",
            StringComparison.OrdinalIgnoreCase);
        return new PageRequest(
            Page,
            PageSize,
            Search?.Trim(),
            selectedSort,
            descending);
    }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        ArgumentNullException.ThrowIfNull(validationContext);
        if (!string.IsNullOrWhiteSpace(SortBy)
            && !AllowedSorts.Contains(SortBy.Trim()))
        {
            yield return new ValidationResult(
                "SortBy is not valid for this resource.",
                [nameof(SortBy)]);
        }

        foreach (ValidationResult result in ValidateAdditional(validationContext))
        {
            yield return result;
        }
    }

    protected virtual IEnumerable<ValidationResult> ValidateAdditional(
        ValidationContext validationContext) =>
        [];
}
