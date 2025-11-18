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
            Colors = BuildSampleColors()
        };

        return View(viewModel);
    }

    private static List<ColorSizePlan> BuildSampleColors()
    {
        return new List<ColorSizePlan>
        {
            new()
            {
                FabricColor = "ネイビー",
                ColorNumber = "C101",
                ColorName = "ダークネイビー",
                Sizes =
                {
                    new("S", 120, "2024-09-10"),
                    new("M", 160, "2024-09-12"),
                    new("L", 140, "2024-09-14")
                }
            },
            new()
            {
                FabricColor = "ホワイト",
                ColorNumber = "C205",
                ColorName = "オフホワイト",
                Sizes =
                {
                    new("S", 80, "2024-09-05"),
                    new("M", 110, "2024-09-08"),
                    new("L", 95, "2024-09-12")
                }
            },
            new()
            {
                FabricColor = "オリーブ",
                ColorNumber = "C312",
                ColorName = "オリーブドラブ",
                Sizes =
                {
                    new("S", 60, "2024-09-18"),
                    new("M", 75, "2024-09-20"),
                    new("L", 90, "2024-09-22"),
                    new("XL", 50, "2024-09-24")
                }
            }
        };
    }
}
