namespace Qwack.Futures
{
    public interface IFutureSettingsProvider
    {
        FutureSettings this[string futureName] { get; }
        bool TryGet(string futureName, out FutureSettings futureSettings);

        bool TryGet(string code, string codeProvider, out FutureSettings futureSettings);
    }
}
