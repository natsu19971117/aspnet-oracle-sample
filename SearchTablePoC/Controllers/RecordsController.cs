using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SearchTablePoC.Models;
using SearchTablePoC.Services;
using SearchTablePoC.ViewModels;

namespace SearchTablePoC.Controllers;

public sealed class RecordsController : Controller
{
    private readonly MockRepository _repository;

    public RecordsController(MockRepository repository)
    {
        _repository = repository;
    }

    [HttpGet]
    public IActionResult Index([FromQuery] RecordQuery query)
    {
        query.ApplyColumnFiltersFromQuery(Request.Query);
        var error = query.ValidateDateRange();

        RecordsResult result;
        if (error is null)
        {
            result = _repository.GetRecords(query);
        }
        else
        {
            result = new RecordsResult
            {
                Items = Array.Empty<Record>(),
                TotalCount = 0
            };
        }

        var viewModel = new RecordIndexViewModel
        {
            Query = query,
            Records = result.Items,
            Columns = RecordMetadata.Columns,
            TotalCount = result.TotalCount,
            ErrorMessage = error
        };

        return View(viewModel);
    }

    [HttpGet]
    public IActionResult ListJson([FromQuery] RecordQuery query)
    {
        query.ApplyColumnFiltersFromQuery(Request.Query);
        var error = query.ValidateDateRange();
        if (error is not null)
        {
            Response.StatusCode = StatusCodes.Status400BadRequest;
            return Json(new { error });
        }

        var result = _repository.GetRecords(query);
        var rows = result.Items.Select(record => RecordMetadata.Columns.ToDictionary(
            column => column.PropertyName,
            column => RecordMetadata.FormatValue(record, column)));

        var pageCount = result.TotalCount == 0 ? 1 : (int)Math.Ceiling(result.TotalCount / (double)query.PageSize);

        return Json(new
        {
            rows,
            totalCount = result.TotalCount,
            page = query.Page,
            pageSize = query.PageSize,
            pageCount,
            sortBy = string.IsNullOrWhiteSpace(query.SortBy) ? "Id" : query.SortBy,
            sortDir = query.SortDir,
            summary = BuildSummaryText(result.TotalCount, query)
        });
    }

    [HttpGet]
    public IActionResult Csv([FromQuery] RecordQuery query)
    {
        query.ApplyColumnFiltersFromQuery(Request.Query);
        var error = query.ValidateDateRange();
        if (error is not null)
        {
            return BadRequest(error);
        }

        var result = _repository.GetRecords(query, applyPaging: false);
        var csv = _repository.ToCsv(result.Items);
        return File(System.Text.Encoding.UTF8.GetBytes(csv), "text/csv", "records.csv");
    }

    private static string BuildSummaryText(int totalCount, RecordQuery query)
    {
        if (totalCount == 0)
        {
            return "No records found";
        }

        var start = (query.Page - 1) * query.PageSize + 1;
        var end = Math.Min(query.Page * query.PageSize, totalCount);
        return $"Showing {start} - {end} of {totalCount} records";
    }
}
