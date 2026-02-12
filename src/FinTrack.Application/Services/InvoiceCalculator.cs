namespace FinTrack.Application.Services;

public static class InvoiceCalculator
{
    public static (decimal subtotal, decimal vatTotal, decimal total) Calculate(IEnumerable<(int qty, decimal unitPrice, decimal vatRate)> lines)
    {
        decimal subtotal = 0, vat = 0;

        foreach (var (qty, unitPrice, vatRate) in lines)
        {
            var lineTotal = qty * unitPrice;
            var vatAmount = lineTotal * vatRate;
            subtotal += lineTotal;
            vat += vatAmount;
        }

        return (subtotal, vat, subtotal + vat);
    }
}
