﻿using System;
using System.Collections.Concurrent;

namespace GeneaGrab.Core.Helpers
{
    public static class Singleton<T> where T : new()
    {
        private static readonly ConcurrentDictionary<Type, T> Instances = new ConcurrentDictionary<Type, T>();
        public static T Instance => Instances.GetOrAdd(typeof(T), t => new T());
    }
}
