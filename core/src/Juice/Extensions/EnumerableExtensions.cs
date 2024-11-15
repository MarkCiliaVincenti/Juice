﻿namespace Juice.Extensions
{
    public static class EnumerableExtensions
    {
        public static IEnumerable<T> DistinctBy<T, TIdentity>(this IEnumerable<T> source, Func<T?, TIdentity> identitySelector)
        {
            return source.Distinct(By(identitySelector));
        }

        public static IEqualityComparer<TSource> By<TSource, TIdentity>(Func<TSource?, TIdentity> identitySelector)
        {
            return new DelegateComparer<TSource, TIdentity>(identitySelector);
        }

        private class DelegateComparer<T, TIdentity> : IEqualityComparer<T>
        {
            private readonly Func<T?, TIdentity> identitySelector;

            public DelegateComparer(Func<T?, TIdentity> identitySelector)
            {
                this.identitySelector = identitySelector;
            }

            public bool Equals(T? x, T? y) => Equals(identitySelector(x), identitySelector(y));

            public int GetHashCode(T? obj) => identitySelector(obj)!.GetHashCode();
        }
    }
}
