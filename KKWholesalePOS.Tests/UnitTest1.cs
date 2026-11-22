using KKWholesalePOS.Core.Models.Inventory;
using KKWholesalePOS.Data;
using Microsoft.EntityFrameworkCore;

namespace KKWholesalePOS.Tests
{
    public class UnitTest1
    {

        [Fact]
        public void Database_CanSaveAndRetrieveProduct()
        {

            using var connection = new Microsoft.Data.Sqlite.SqliteConnection(
                "Data Source=:memory:");

            connection.Open();

            var testOptions = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite(connection)
                .Options;

            using var db = new AppDbContext(testOptions);

            // Create the database schema.
            db.Database.EnsureCreated();

            // Create a test product.
            var product = new Product
            {
                Name = "Test Product",
                Barcode = "TEST-001",
                SellingPrice = 100.00m,
                StockQuantity = 10,
                UnitsPerCase = 1
            };

            // Act
            db.Products.Add(product);
            db.SaveChanges();

            var retrievedProduct = db.Products
                .AsNoTracking()
                .FirstOrDefault(p => p.Barcode == "TEST-001");

            // Assert
            Assert.NotNull(retrievedProduct);
            Assert.Equal("Test Product", retrievedProduct.Name);
            Assert.Equal(100.00m, retrievedProduct.SellingPrice);
            Assert.Equal(10, retrievedProduct.StockQuantity);
        }
    }
}
