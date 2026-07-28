using NightCafe.Core;

namespace NightCafe.Services
{
    /// <summary>
    /// GDD 5.3: the cat meows on roughly 30% of its crossings, not every one.
    /// </summary>
    public sealed class CatCueService
    {
        readonly float _chance;
        readonly IRandom _rng;

        public CatCueService(float chance, IRandom rng)
        {
            _chance = chance;
            _rng = rng;
        }

        public bool ShouldMeow() => _rng.NextFloat() < _chance;
    }
}
