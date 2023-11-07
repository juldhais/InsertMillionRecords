using Bogus;
using InsertMillionRecords;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

// initialize data context
var connectionString = "Data Source=localhost; Initial Catalog=Product; Integrated Security=True";
var contextOptionsBuilder = new DbContextOptionsBuilder<DataContext>();
contextOptionsBuilder.UseSqlServer(connectionString);
var context = new DataContext(contextOptionsBuilder.Options);

// create database
await context.Database.EnsureDeletedAsync();
await context.Database.EnsureCreatedAsync();

// setup bogus faker
var faker = new Faker<Product>();
faker.RuleFor(p => p.Code, f => f.Commerce.Ean13());
faker.RuleFor(p => p.Description, f => f.Commerce.ProductName());
faker.RuleFor(p => p.Category, f => f.Commerce.Categories(1)[0]);
faker.RuleFor(p => p.Price, f => f.Random.Decimal(1, 1000));

// generate 1 million products
var products = faker.Generate(1_000_000);

var batches = products
    .Select((p, i) => (Product: p, Index: i))
    .GroupBy(x => x.Index / 100_000)
    .Select(g => g.Select(x => x.Product).ToList())
    .ToList();

// insert batches
var stopwatch = new Stopwatch();
stopwatch.Start();

var count = 0;
foreach (var batch in batches)
{
    count++;
    Console.WriteLine($"Inserting batch {count} of {batches.Count}...");

    await context.Products.AddRangeAsync(batch);
    await context.SaveChangesAsync();
}

stopwatch.Stop();

Console.WriteLine($"Elapsed time: {stopwatch.Elapsed}");
Console.WriteLine("Press any key to exit...");