﻿using System;
using System.Collections.Concurrent;

namespace GeneaGrab.Core.Helpers
{
    public static class Singleton<T> where T : new()
    {
        private static ConcurrentDictionary<Type, T> _instances = new ConcurrentDictionary<Type, T>();
        public static T Instance => _instances.GetOrAdd(typeof(T), (t) => new T());
    }
}
