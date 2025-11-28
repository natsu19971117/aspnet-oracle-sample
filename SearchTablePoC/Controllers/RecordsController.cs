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
    public IActionResult Suggestions([FromQuery] string field, [FromQuery] RecordQuery query, [FromQuery] string? term = null, [FromQuery] string scope = "column", [FromQuery] int limit = 10)
    {
        if (string.IsNullOrWhiteSpace(field))
        {
            return Json(new { values = Array.Empty<string>() });
        }

        query.ApplyColumnFiltersFromQuery(Request.Query);
        var workingQuery = query.Clone();

        if (string.Equals(scope, "query", StringComparison.OrdinalIgnoreCase))
        {
            if (field.Equals(nameof(Record.Category), StringComparison.OrdinalIgnoreCase))
            {
                workingQuery.Category = null;
            }
            else if (field.Equals(nameof(Record.Status), StringComparison.OrdinalIgnoreCase))
            {
                workingQuery.Status = null;
            }
            else if (field.Equals(nameof(Record.Id), StringComparison.OrdinalIgnoreCase))
            {
                workingQuery.Id = null;
            }
            else if (field.Equals(nameof(Record.Field01), StringComparison.OrdinalIgnoreCase))
            {
                workingQuery.Field01 = null;
            }
            else if (field.Equals(nameof(Record.Name), StringComparison.OrdinalIgnoreCase))
            {
                workingQuery.Name = null;
            }
            else if (field.Equals(nameof(RecordQuery.Keyword), StringComparison.OrdinalIgnoreCase))
            {
                workingQuery.Keyword = null;
            }
        }

        var suggestions = _repository.GetSuggestions(field, workingQuery, term, limit);
        return Json(new { values = suggestions });
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

    [HttpGet]
    public IActionResult Integration([FromQuery] IntegrationFilter filter, [FromQuery] IntegrationOverrides overrides)
    {
        var viewModel = BuildIntegrationViewModel(filter, overrides);
        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Integrate(
        [FromForm] List<string> orderNumbers,
        [FromForm] IntegrationFilter filter,
        [FromForm] IntegrationOverrides overrides)
    {
        filter ??= new IntegrationFilter();
        overrides ??= new IntegrationOverrides();

        var result = _repository.IntegrateOrders(orderNumbers ?? new List<string>(), overrides);
        if (!result.Success)
        {
            TempData["IntegrationError"] = result.Error;
        }
        else if (result.Record is not null)
        {
            TempData["IntegrationMessage"] = $"発注No {result.Record.Field01} で統合しました。";
        }

        return RedirectToAction(nameof(Integration), BuildIntegrationRoute(filter, overrides));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult UndoIntegration([FromForm] string integrationOrderNo, [FromForm] IntegrationFilter filter)
    {
        filter ??= new IntegrationFilter();

        var result = _repository.UndoIntegration(integrationOrderNo);
        if (!result.Success)
        {
            TempData["IntegrationError"] = result.Error;
        }
        else
        {
            TempData["IntegrationMessage"] = $"発注No {integrationOrderNo} の統合を解除しました。";
        }

        return RedirectToAction(nameof(Integration), BuildIntegrationRoute(filter, new IntegrationOverrides()));
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

    private OrderIntegrationViewModel BuildIntegrationViewModel(IntegrationFilter filter, IntegrationOverrides overrides)
    {
        filter ??= new IntegrationFilter();
        overrides ??= new IntegrationOverrides();

        var viewModel = new OrderIntegrationViewModel
        {
            AvailableRecords = _repository.GetIntegrationCandidates(filter),
            IntegratedOrders = _repository.GetIntegrationGroups(filter),
            Filter = filter,
            Overrides = overrides
        };

        if (TempData.TryGetValue("IntegrationMessage", out var message))
        {
            viewModel.Message = message as string;
        }

        if (TempData.TryGetValue("IntegrationError", out var error))
        {
            viewModel.Error = error as string;
        }

        return viewModel;
    }

    private static object BuildIntegrationRoute(IntegrationFilter filter, IntegrationOverrides overrides)
    {
        return new
        {
            filter.Keyword,
            filter.PersonInCharge,
            filter.UpdatedFrom,
            filter.UpdatedTo,
            overrides.ManualRequestNo,
            overrides.ManualContractDate,
            overrides.ManualPersonInCharge
        };
    }
}
