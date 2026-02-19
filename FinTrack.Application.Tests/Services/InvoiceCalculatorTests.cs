using FluentAssertions;
using Xunit;
using FinTrack.Application.Services;

namespace FinTrack.Application.Tests.Services;

public class InvoiceCalculatorTests
{
    [Fact]
    public void Calculate_Should_return_zero_when_no_lines()
    {
        
        var lines = Array.Empty<(int qty, decimal unitPrice, decimal vatRate)>();

     
        var (subtotal, vat, total) = InvoiceCalculator.Calculate(lines);

        
        subtotal.Should().Be(0);
        vat.Should().Be(0);
        total.Should().Be(0);
    }

    [Fact]
    public void Calculate_Should_compute_subtotal_correctly()
    {
        var lines = new[]
        {
            (qty: 2, unitPrice: 10m, vatRate: 0m), 
            (qty: 1, unitPrice: 5m, vatRate: 0m)  
        };

        var (subtotal, vat, total) = InvoiceCalculator.Calculate(lines);

        
        subtotal.Should().Be(25);
        vat.Should().Be(0);
        total.Should().Be(25);
    }

    [Fact]
    public void Calculate_Should_compute_vat_correctly()
    {
        var lines = new[]
        {
            (qty: 2, unitPrice: 100m, vatRate: 0.15m) 
        };

        var (subtotal, vat, total) = InvoiceCalculator.Calculate(lines);

       
        subtotal.Should().Be(200);
        vat.Should().Be(30);
        total.Should().Be(230);
    }

    [Fact]
    public void Calculate_Should_handle_multiple_lines_with_vat()
    {
        
        var lines = new[]
        {
            (qty: 1, unitPrice: 100m, vatRate: 0.15m), 
            (qty: 2, unitPrice: 50m, vatRate: 0.10m)   
        };

       
        var (subtotal, vat, total) = InvoiceCalculator.Calculate(lines);

       
        subtotal.Should().Be(200);
        vat.Should().Be(25);
        total.Should().Be(225);
    }
}
