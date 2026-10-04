using System;

namespace Echappee.Simulation
{
    /// <summary>Générateur déterministe (SplitMix64). Ne jamais utiliser System.Random dans la simulation.</summary>
    public sealed class Rng
    {
        ulong _s;

        public Rng(ulong seed) { _s = seed; }

        public ulong NextULong()
        {
            unchecked
            {
                _s += 0x9E3779B97F4A7C15UL;
                ulong z = _s;
                z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
                z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
                return z ^ (z >> 31);
            }
        }

        /// <summary>[0, 1[</summary>
        public double NextDouble() => (NextULong() >> 11) * (1.0 / (1UL << 53));

        /// <summary>[0, max[</summary>
        public int NextInt(int max) => max <= 0 ? 0 : (int)(NextULong() % (ulong)max);

        public int NextInt(int min, int maxExclusive) => min + NextInt(maxExclusive - min);

        public double Range(double min, double max) => min + (max - min) * NextDouble();

        public bool Chance(double p) => NextDouble() < p;

        /// <summary>Approximation gaussienne (somme de 4 uniformes), moyenne 0, écart-type ~1.</summary>
        public double Gauss()
        {
            double s = NextDouble() + NextDouble() + NextDouble() + NextDouble();
            return (s - 2.0) * 1.7320508;
        }

        public Rng Fork(ulong salt) => new Rng(NextULong() ^ (salt * 0xD6E8FEB86659FD93UL));
    }
}
