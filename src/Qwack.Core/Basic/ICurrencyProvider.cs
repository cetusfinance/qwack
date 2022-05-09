namespace Qwack.Core.Basic
{
    public interface ICurrencyProvider
    {
        Currency this[string ccy] { get; }
        bool TryGetCurrency(string ccy, out Currency output);
        Currency GetCurrency(string ccy);
        Currency GetCurrencySafe(string ccy);
        Currency[] GetAllCurrencies();
    }
}
