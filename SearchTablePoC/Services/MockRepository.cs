using System.Globalization;
using System.Text;
using SearchTablePoC.Models;
using SearchTablePoC.ViewModels;

namespace SearchTablePoC.Services;

public sealed class MockRepository
{
    private readonly List<Record> _records;
    private readonly Random _random = new();

    public MockRepository()
    {
        _records = GenerateRecords(520);
    }

    public RecordsResult GetRecords(RecordQuery query, bool applyPaging = true)
    {
        query.Normalize();

        var working = ApplySorting(ApplyColumnFilters(ApplySearch(_records.AsEnumerable(), query), query), query).ToList();
        var totalCount = working.Count;

        var pageCount = totalCount == 0 ? 1 : (int)Math.Ceiling(totalCount / (double)query.PageSize);
        if (query.Page > pageCount)
        {
            query.Page = pageCount;
        }

        List<Record> items;
        if (applyPaging)
        {
            items = working
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToList();
        }
        else
        {
            items = working;
        }

        return new RecordsResult
        {
            Items = items,
            TotalCount = totalCount
        };
    }

    public string ToCsv(IEnumerable<Record> records)
    {
        var builder = new StringBuilder();
        var headers = RecordMetadata.Columns.Select(c => Escape(c.PropertyName));
        builder.AppendLine(string.Join(',', headers));

        foreach (var record in records)
        {
            var values = RecordMetadata.Columns
                .Select(column => Escape(RecordMetadata.FormatValue(record, column)));
            builder.AppendLine(string.Join(',', values));
        }

        return builder.ToString();
    }

    public IReadOnlyList<string> GetSuggestions(string propertyName, RecordQuery query, string? term, int limit = 10)
    {
        if (string.IsNullOrWhiteSpace(propertyName) || !RecordMetadata.ColumnLookup.TryGetValue(propertyName, out var column))
        {
            return Array.Empty<string>();
        }

        var cappedLimit = limit <= 0 ? 10 : Math.Min(limit, 50);
        var workingQuery = query.Clone();
        workingQuery.Normalize();
        workingQuery.ColumnFilters.Remove(propertyName);

        var filtered = ApplyColumnFilters(ApplySearch(_records.AsEnumerable(), workingQuery), workingQuery);
        var filterTerm = term?.Trim();

        var suggestions = filtered
            .Select(record => RecordMetadata.FormatValue(record, column))
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Where(value => string.IsNullOrEmpty(filterTerm) || value.IndexOf(filterTerm, StringComparison.OrdinalIgnoreCase) >= 0)
            .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
            .Take(cappedLimit)
            .ToList();

        return suggestions;
    }

    private static string Escape(string value)
    {
        var escaped = value.Replace("\"", "\"\"");
        return $"\"{escaped}\"";
    }

    private IEnumerable<Record> ApplySearch(IEnumerable<Record> source, RecordQuery query)
    {
        var results = source;

        if (query.Id.HasValue)
        {
            results = results.Where(r => r.Id == query.Id.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            results = results.Where(r => r.Name.Contains(query.Keyword, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(query.Category))
        {
            results = results.Where(r => string.Equals(r.Category, query.Category, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            results = results.Where(r => string.Equals(r.Status, query.Status, StringComparison.OrdinalIgnoreCase));
        }

        if (query.UpdatedFrom.HasValue)
        {
            results = results.Where(r => r.UpdatedAt >= query.UpdatedFrom.Value);
        }

        if (query.UpdatedTo.HasValue)
        {
            results = results.Where(r => r.UpdatedAt <= query.UpdatedTo.Value);
        }

        return results;
    }

    private IEnumerable<Record> ApplyColumnFilters(IEnumerable<Record> source, RecordQuery query)
    {
        var results = source;

        foreach (var (propertyName, value) in query.ColumnFilters)
        {
            if (!RecordMetadata.ColumnLookup.TryGetValue(propertyName, out var column) || column.PropertyInfo is null)
            {
                continue;
            }

            var filterValue = value;
            results = results.Where(record =>
            {
                var cell = column.Accessor(record);
                if (cell is null)
                {
                    return false;
                }

                return cell switch
                {
                    DateOnly date => date.ToString("yyyy-MM-dd").Contains(filterValue, StringComparison.OrdinalIgnoreCase),
                    IFormattable formattable => formattable.ToString(null, null)?.IndexOf(filterValue, StringComparison.OrdinalIgnoreCase) >= 0,
                    _ => cell.ToString()?.IndexOf(filterValue, StringComparison.OrdinalIgnoreCase) >= 0
                };
            });
        }

        return results;
    }

    private IEnumerable<Record> ApplySorting(IEnumerable<Record> source, RecordQuery query)
    {
        var sortBy = query.SortBy;
        if (string.IsNullOrWhiteSpace(sortBy) || !RecordMetadata.ColumnLookup.TryGetValue(sortBy, out var column))
        {
            column = RecordMetadata.ColumnLookup["Id"];
            sortBy = "Id";
        }

        var comparer = Comparer<object?>.Create((left, right) =>
        {
            if (left is null && right is null)
            {
                return 0;
            }

            if (left is null)
            {
                return -1;
            }

            if (right is null)
            {
                return 1;
            }

            return (left, right) switch
            {
                (DateOnly dateLeft, DateOnly dateRight) => dateLeft.CompareTo(dateRight),
                (IComparable comparableLeft, IComparable comparableRight) => comparableLeft.CompareTo(comparableRight),
                _ => string.Compare(left.ToString(), right.ToString(), StringComparison.OrdinalIgnoreCase)
            };
        });

        var sorted = query.SortDir == "desc"
            ? source.OrderByDescending(column.Accessor, comparer)
            : source.OrderBy(column.Accessor, comparer);

        return sorted;
    }

    private List<Record> GenerateRecords(int count)
    {
        var familyNames = new[] { "佐藤", "鈴木", "高橋", "田中", "伊藤", "渡辺", "山本", "中村" };
        var givenNames = new[] { "太郎", "花子", "健一", "美咲", "裕介", "彩香", "翔太", "恵理" };
        var categories = new[] { "原料部", "紡績部", "テキスタイル営業部", "輸出入管理部" };
        var statuses = new[] { "輸入係", "国内係", "品質係", "物流係" };
        var productNames = new[]
        {
            "コットンツイル", "オーガニックデニム", "リネンキャンバス", "ウールトロピカル", "テンセルサテン",
            "シルクシフォン", "ナイロンタフタ", "ポリエステルジャージ"
        };
        var countriesOfOrigin = new[] { "日本", "中国", "ベトナム", "インド", "インドネシア", "タイ", "トルコ", "イタリア" };
        var destinationCountries = new[] { "日本", "アメリカ", "ドイツ", "フランス", "ベトナム", "インドネシア", "中国", "韓国" };
        var fabricTypes = new[] { "織物", "編物", "不織布", "起毛生地" };
        var yarnCounts = new[] { "20/1", "30/2", "40/1", "50/2", "60/1" };
        var weaveStructures = new[] { "平織", "綾織", "朱子織", "二重織" };
        var brands = new[] { "GLOBAL TEXTILE", "NIPPON FABRIC", "ASIA THREADS", "OCEANIC LINEN" };
        var seasons = new[] { "SS24", "AW24", "SS25", "AW25" };
        var usages = new[] { "スーツ", "ワンピース", "ユニフォーム", "スポーツウェア", "寝具" };
        var tradingHouses = new[] { "東亜繊維商事", "北海テキスタイル", "大洋商社", "京浜トレーディング" };
        var suppliers = new[] { "上海繊維有限公司", "ハノイファブリック", "デリーコットン", "大阪糸業" };
        var mills = new[] { "蘇州第一紡績", "ホーチミン織布", "名古屋撚糸", "バンコク染工" };
        var grades = new[] { "A", "B", "C" };
        var transportModes = new[] { "海上", "航空", "鉄道", "トラック" };
        var incoterms = new[] { "FOB", "CIF", "CFR", "DAP" };
        var currencies = new[] { "JPY", "USD", "EUR", "CNY" };
        var paymentTerms = new[] { "L/C at sight", "TT 30 days", "TT 60 days", "D/P" };
        var vesselNames = new[] { "MV ORIENT STAR", "MV PACIFIC WIND", "MV ASIA BREEZE", "MV TOKYO BAY" };
        var routes = new[] { "上海-横浜", "ホーチミン-神戸", "ムンバイ-大阪", "ハンブルク-東京" };
        var shippingCompanies = new[] { "東洋海運", "太平洋ライン", "北極海運", "南星ライン" };
        var containerPrefixes = new[] { "MSCU", "NYKU", "ONEU", "TGHU" };
        var packageUnits = new[] { "反", "ケース", "ロール" };
        var warehouses = new[] { "東京湾倉庫A", "横浜物流センター", "神戸港第3倉庫", "名古屋保税庫" };
        var warehouseStaff = new[] { "山田", "小林", "加藤", "石井" };
        var clearanceStaff = new[] { "松本", "阿部", "長谷川", "森" };
        var customsBrokers = new[] { "日本通関サービス", "港湾通関", "東亜申告", "ワールドカスタム" };
        var insuranceCompanies = new[] { "東京海上", "三井住友海上", "損保ジャパン", "AIG損保" };
        var inspectionStaff = new[] { "佐々木", "岡本", "村上", "福田" };
        var inspectionAgencies = new[] { "日本繊維検査協会", "アジア品質センター", "国際検査機構" };
        var sampleResponses = new[] { "承認", "条件付き承認", "差戻し" };
        var claimDetails = new[] { "色差異", "寸法ズレ", "汚れ", "糸抜け" };
        var responseStatuses = new[] { "対応中", "完了", "調整中" };
        var finalCustomers = new[] { "東京アパレル", "京都テキスタイル", "ニューヨークファッション", "パリコレクション" };
        var endUses = new[] { "スーツ", "カジュアルシャツ", "インテリア", "スポーツユニフォーム" };
        var deliveryDestinations = new[] { "大阪物流センター", "東京本社倉庫", "名古屋配送拠点", "福岡DC" };
        var deliveryAddresses = new[]
        {
            "大阪府堺市築港南町1-1",
            "東京都江東区青海2-3-5",
            "愛知県海部郡飛島村大宝7-12",
            "福岡県福岡市東区箱崎ふ頭4-8"
        };
        var remarks = new[] { "特記事項なし", "要サンプル確認", "輸送温度管理要", "次回価格改定予定" };

        var list = new List<Record>(count);
        var today = DateOnly.FromDateTime(DateTime.Today);

        for (var i = 1; i <= count; i++)
        {
            var contractDate = today.AddDays(-_random.Next(0, 365));
            var quoteDate = contractDate.AddDays(-_random.Next(5, 30));
            var shipmentDate = contractDate.AddDays(_random.Next(15, 90));
            var arrivalDate = shipmentDate.AddDays(_random.Next(7, 25));
            var inboundDate = arrivalDate.AddDays(_random.Next(1, 5));
            var outboundDate = inboundDate.AddDays(_random.Next(3, 14));
            var inspectionDate = inboundDate.AddDays(_random.Next(0, 3));
            var sampleSentDate = contractDate.AddDays(_random.Next(0, 10));
            var sampleResponseDate = sampleSentDate.AddDays(_random.Next(1, 10));

            var quantity = _random.Next(500, 5000);
            var exchangeRate = Math.Round(0.7 + _random.NextDouble() * 0.8, 3);
            var unitPrice = 250 + _random.Next(100, 950);
            var packageCount = _random.Next(5, 50);
            var weight = Math.Round(quantity * (0.25 + _random.NextDouble() * 0.4), 1);
            var claimFlag = _random.Next(0, 5) == 0 ? "有" : "無";
            var sampleResponse = sampleResponses[_random.Next(sampleResponses.Length)];

            var record = new Record
            {
                Id = i,
                Category = categories[_random.Next(categories.Length)],
                Status = statuses[_random.Next(statuses.Length)],
                UpdatedAt = contractDate,
                Amount = quantity,
                Name = $"{familyNames[_random.Next(familyNames.Length)]} {givenNames[_random.Next(givenNames.Length)]}"
            };

            record.Field01 = $"PO-{contractDate:yyyy}-{i:0000}";
            record.Field02 = $"TX-{_random.Next(1000, 9999)}";
            record.Field03 = productNames[_random.Next(productNames.Length)];
            record.Field04 = countriesOfOrigin[_random.Next(countriesOfOrigin.Length)];
            record.Field05 = destinationCountries[_random.Next(destinationCountries.Length)];
            record.Field06 = fabricTypes[_random.Next(fabricTypes.Length)];
            record.Field07 = yarnCounts[_random.Next(yarnCounts.Length)];
            record.Field08 = weaveStructures[_random.Next(weaveStructures.Length)];
            record.Field09 = $"{_random.Next(120, 320)} g/m²";
            record.Field10 = $"{_random.Next(90, 160)} cm";
            record.Field11 = $"C{_random.Next(100, 999)}";
            record.Field12 = $"LOT{_random.Next(1000, 9999)}";
            record.Field13 = brands[_random.Next(brands.Length)];
            record.Field14 = seasons[_random.Next(seasons.Length)];
            record.Field15 = usages[_random.Next(usages.Length)];
            record.Field16 = tradingHouses[_random.Next(tradingHouses.Length)];
            record.Field17 = suppliers[_random.Next(suppliers.Length)];
            record.Field18 = mills[_random.Next(mills.Length)];
            record.Field19 = grades[_random.Next(grades.Length)];
            record.Field20 = _random.Next(0, 2) == 0 ? "輸入" : "輸出";
            record.Field21 = transportModes[_random.Next(transportModes.Length)];
            record.Field22 = incoterms[_random.Next(incoterms.Length)];
            record.Field23 = currencies[_random.Next(currencies.Length)];
            record.Field24 = exchangeRate.ToString("F3", CultureInfo.InvariantCulture);
            record.Field25 = $"{unitPrice:N0} JPY/kg";
            record.Field26 = $"QT-{quoteDate:yyyy}-{i:0000}";
            record.Field27 = quoteDate.ToString("yyyy-MM-dd");
            record.Field28 = $"CN-{contractDate:yyyy}-{i:0000}";
            record.Field29 = paymentTerms[_random.Next(paymentTerms.Length)];
            record.Field30 = shipmentDate.ToString("yyyy-MM-dd");
            record.Field31 = vesselNames[_random.Next(vesselNames.Length)];
            record.Field32 = routes[_random.Next(routes.Length)];
            record.Field33 = shippingCompanies[_random.Next(shippingCompanies.Length)];
            record.Field34 = $"BL{contractDate:yy}{shipmentDate:MMdd}{_random.Next(100, 999)}";
            record.Field35 = $"{containerPrefixes[_random.Next(containerPrefixes.Length)]}{_random.Next(1000000, 9999999)}";
            record.Field36 = packageCount.ToString(CultureInfo.InvariantCulture);
            record.Field37 = packageUnits[_random.Next(packageUnits.Length)];
            record.Field38 = $"{weight:F1} kg";
            record.Field39 = "kg";
            record.Field40 = warehouses[_random.Next(warehouses.Length)];
            record.Field41 = warehouseStaff[_random.Next(warehouseStaff.Length)];
            record.Field42 = inboundDate.ToString("yyyy-MM-dd");
            record.Field43 = outboundDate.ToString("yyyy-MM-dd");
            record.Field44 = clearanceStaff[_random.Next(clearanceStaff.Length)];
            record.Field45 = customsBrokers[_random.Next(customsBrokers.Length)];
            record.Field46 = $"{(5 + _random.NextDouble() * 7):F2}%";
            record.Field47 = $"{_random.Next(10000, 90000):N0} JPY";
            record.Field48 = insuranceCompanies[_random.Next(insuranceCompanies.Length)];
            record.Field49 = $"IC-{contractDate:yy}{_random.Next(10000, 99999)}";
            record.Field50 = $"{_random.Next(500000, 2000000):N0} JPY";
            record.Field51 = inspectionDate.ToString("yyyy-MM-dd");
            record.Field52 = inspectionStaff[_random.Next(inspectionStaff.Length)];
            record.Field53 = inspectionAgencies[_random.Next(inspectionAgencies.Length)];
            record.Field54 = $"SMP-{contractDate:yy}{_random.Next(1000, 9999)}";
            record.Field55 = sampleSentDate.ToString("yyyy-MM-dd");
            record.Field56 = $"{sampleResponse} ({sampleResponseDate:yyyy-MM-dd})";
            record.Field57 = claimFlag;
            record.Field58 = claimFlag == "有" ? claimDetails[_random.Next(claimDetails.Length)] : "なし";
            record.Field59 = responseStatuses[_random.Next(responseStatuses.Length)];
            record.Field60 = finalCustomers[_random.Next(finalCustomers.Length)];
            record.Field61 = endUses[_random.Next(endUses.Length)];
            record.Field62 = deliveryDestinations[_random.Next(deliveryDestinations.Length)];
            record.Field63 = deliveryAddresses[_random.Next(deliveryAddresses.Length)];
            record.Field64 = remarks[_random.Next(remarks.Length)];

            list.Add(record);
        }

        return list;
    }
}

public sealed class RecordsResult
{
    public required IReadOnlyList<Record> Items { get; init; }
    public required int TotalCount { get; init; }
}
