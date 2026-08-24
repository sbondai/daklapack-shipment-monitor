using DaklaPack.Shipments.Domain;
using DaklaPack.Shipments.Domain.ValueObjects;

namespace DaklaPack.Shipments.Infrastructure.Shipments;

/// <summary>
/// A fixed set of sample shipments for running the application without a database.
/// </summary>
/// <remarks>
/// Deterministic: same rows, same identifiers, every time. Dates are day <em>offsets</em> from a
/// supplied reference date rather than absolute values, so the sample always holds a realistic mix
/// of delivered, in-flight and overdue work whenever it runs, while a test can pin the date and
/// assert exact results.
/// </remarks>
internal static class ShipmentSeedData
{
    // trackingSuffix, status, kg, city, country, postcode, carrier, dispatchedDaysAgo, dueInDays
    private static readonly (int Suffix, ShipmentStatus Status, decimal Kg, string City, string Country,
        string Postcode, string Carrier, int DispatchedDaysAgo, int DueInDays)[] Rows =
    [
        (100_001, ShipmentStatus.Delivered,      2.4m,  "Amsterdam",   "NL", "1011AB", "PostNL",    14, -10),
        (100_002, ShipmentStatus.Delivered,     18.0m,  "Rotterdam",   "NL", "3011AA", "DHL",       13,  -9),
        (100_003, ShipmentStatus.Delivered,      0.8m,  "Utrecht",     "NL", "3511LX", "PostNL",    12,  -8),
        (100_004, ShipmentStatus.Delivered,     5.5m,  "Eindhoven",   "NL", "5611AA", "DPD",       12,  -7),
        (100_005, ShipmentStatus.Cancelled,      7.2m,  "Groningen",   "NL", "9711AA", "DHL",       11,  -6),
        (100_006, ShipmentStatus.Delivered,     31.6m,  "Antwerpen",   "BE", "2000",   "Bpost",     11,  -5),
        (100_007, ShipmentStatus.Delivered,      1.2m,  "Brussel",     "BE", "1000",   "Bpost",     10,  -4),
        (100_008, ShipmentStatus.Delayed,       12.9m,  "Gent",        "BE", "9000",   "DPD",       10,  -3),
        (100_009, ShipmentStatus.InTransit,     45.0m,  "Koln",        "DE", "50667",  "DHL",        9,  -2),
        (100_010, ShipmentStatus.Delayed,        3.3m,  "Dusseldorf",  "DE", "40213",  "DHL",        9,  -1),
        (100_011, ShipmentStatus.InTransit,      9.9m,  "Hamburg",     "DE", "20095",  "DPD",        8,   0),
        (100_012, ShipmentStatus.OutForDelivery, 2.1m,  "Berlin",      "DE", "10115",  "DHL",        8,   0),
        (100_013, ShipmentStatus.OutForDelivery, 6.4m,  "Munchen",     "DE", "80331",  "DPD",        7,   1),
        (100_014, ShipmentStatus.InTransit,     22.5m,  "Paris",       "FR", "75001",  "Chronopost", 7,   1),
        (100_015, ShipmentStatus.InTransit,      4.7m,  "Lyon",        "FR", "69001",  "Chronopost", 6,   2),
        (100_016, ShipmentStatus.Delayed,       55.0m,  "Marseille",   "FR", "13001",  "DPD",        6,   2),
        (100_017, ShipmentStatus.InTransit,      0.5m,  "Lille",       "FR", "59000",  "Chronopost", 5,   3),
        (100_018, ShipmentStatus.Created,       14.8m,  "Madrid",      "ES", "28001",  "Correos",    5,   4),
        (100_019, ShipmentStatus.InTransit,      8.2m,  "Barcelona",   "ES", "08001",  "Correos",    4,   4),
        (100_020, ShipmentStatus.Created,       27.3m,  "Valencia",    "ES", "46001",  "DHL",        4,   5),
        (100_021, ShipmentStatus.InTransit,      3.9m,  "Lisboa",      "PT", "1100",   "CTT",        3,   5),
        (100_022, ShipmentStatus.Created,       11.1m,  "Porto",       "PT", "4000",   "CTT",        3,   6),
        (100_023, ShipmentStatus.InTransit,     19.4m,  "Milano",      "IT", "20121",  "BRT",        3,   6),
        (100_024, ShipmentStatus.Delayed,        2.8m,  "Roma",        "IT", "00184",  "BRT",        2,   7),
        (100_025, ShipmentStatus.Created,       64.0m,  "Torino",      "IT", "10121",  "DHL",        2,   7),
        (100_026, ShipmentStatus.InTransit,      1.6m,  "Wien",        "AT", "1010",   "Post AT",    2,   8),
        (100_027, ShipmentStatus.Created,       38.7m,  "Zurich",      "CH", "8001",   "Swiss Post", 1,   8),
        (100_028, ShipmentStatus.Created,        7.7m,  "Geneve",      "CH", "1201",   "Swiss Post", 1,   9),
        (100_029, ShipmentStatus.InTransit,     16.2m,  "Kobenhavn",   "DK", "1050",   "PostNord",   1,   9),
        (100_030, ShipmentStatus.Created,        5.1m,  "Stockholm",   "SE", "11120",  "PostNord",   1,  10),
        (100_031, ShipmentStatus.Created,      120.0m,  "Oslo",        "NO", "0150",   "PostNord",   0,  10),
        (100_032, ShipmentStatus.Created,        0.3m,  "Helsinki",    "FI", "00100",  "Posti",      0,  11),
        (100_033, ShipmentStatus.Created,       43.5m,  "Dublin",      "IE", "D01",    "An Post",    0,  11),
        (100_034, ShipmentStatus.Created,        9.0m,  "London",      "GB", "EC1A1BB","Royal Mail", 0,  12),
        (100_035, ShipmentStatus.Created,       25.9m,  "Manchester",  "GB", "M11AE",  "Royal Mail", 0,  12),
        (100_036, ShipmentStatus.Delayed,      210.4m,  "Warszawa",    "PL", "00001",  "InPost",     6,   3),
        (100_037, ShipmentStatus.InTransit,     33.3m,  "Praha",       "CZ", "11000",  "PPL",        5,   4),
        (100_038, ShipmentStatus.Delayed,        4.2m,  "Budapest",    "HU", "1051",   "GLS",        7,  -1),
        (100_039, ShipmentStatus.OutForDelivery,13.7m,  "Bratislava",  "SK", "81101",  "GLS",        4,   0),
        (100_040, ShipmentStatus.InTransit,     88.8m,  "Ljubljana",   "SI", "1000",   "GLS",        3,   5),
    ];

    /// <summary>Builds the sample set relative to the given reference date.</summary>
    public static IReadOnlyList<Shipment> Create(DateOnly today) =>
    [
        .. Rows.Select(row => new Shipment(
            new Guid($"00000000-0000-0000-0000-{row.Suffix:D12}"),
            new TrackingId($"DP-2026-{row.Suffix:D6}"),
            row.Status,
            new Weight(row.Kg),
            new Destination(row.City, row.Country, row.Postcode),
            row.Carrier,
            new DateTimeOffset(today.AddDays(-row.DispatchedDaysAgo).ToDateTime(new TimeOnly(9, 15)), TimeSpan.FromHours(2)),
            today.AddDays(row.DueInDays)))
    ];
}
