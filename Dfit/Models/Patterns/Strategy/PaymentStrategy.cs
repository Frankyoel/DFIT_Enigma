using Google.Cloud.Firestore;
namespace Dfit.Models.Patterns.Strategy
{
    public interface IPaymentStrategy
    {
        decimal CalculateFinalAmount(decimal baseAmount);
    }

    [FirestoreData]
    public class CashStrategy : IPaymentStrategy
    {
        public decimal CalculateFinalAmount(decimal baseAmount)
        {
            return baseAmount; // Efectivo: Sin recargo
        }
    }

    [FirestoreData]
    public class CardStrategy : IPaymentStrategy
    {
        public decimal CalculateFinalAmount(decimal baseAmount)
        {
            return baseAmount * 1.05m; // Tarjeta: 5% de recargo
        }
    }

    [FirestoreData]
    public class PaymentContext
    {
        private IPaymentStrategy _strategy;

        public void SetStrategy(IPaymentStrategy strategy)
        {
            _strategy = strategy;
        }

        public decimal ExecutePayment(decimal baseAmount)
        {
            if (_strategy == null)
            {
                _strategy = new CashStrategy(); // Default
            }
            return _strategy.CalculateFinalAmount(baseAmount);
        }
    }
}
