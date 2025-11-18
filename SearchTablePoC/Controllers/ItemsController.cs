using Microsoft.AspNetCore.Mvc;
using SearchTablePoC.Models;
using SearchTablePoC.Services;
using SearchTablePoC.ViewModels;

namespace SearchTablePoC.Controllers;

public sealed class ItemsController : Controller
{
    private readonly MockRepository _repository;

    public ItemsController(MockRepository repository)
    {
        _repository = repository;
    }

    [HttpGet]
    public IActionResult Detail([FromQuery] string itemCode)
    {
        var record = string.IsNullOrWhiteSpace(itemCode)
            ? null
            : _repository.GetRecordByItemCode(itemCode);

        var viewModel = new ItemDetailViewModel
        {
            ItemCode = string.IsNullOrWhiteSpace(itemCode) ? "TX-0000" : itemCode,
            ItemName = record?.Field03 ?? "サンプル品目",
            FabricType = record?.Field06 ?? "織物",
            Colors = new List<ColorSizePlan>()
        };

        return View(viewModel);
    }
}
