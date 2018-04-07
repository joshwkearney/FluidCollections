﻿using System;

namespace FluidCollections.Demo {
    class Program {
        static void Main(string[] args) {
            var set1 = new ReactiveSet<int>();
            var set2 = new ReactiveSet<int>();

            var result = set1
                .Intersection(set2)
                .OrderBy()
                .Where(x => x > 8)
                .Select(x => x + 2);

            result.ToImmutableSets().Subscribe(x => Console.WriteLine(string.Join(", ", x)));

            set1.Add(1);
            set1.Add(7);
            set1.Add(8);
            set1.Add(9);
            set1.Add(10);

            set2.Add(9);
            set2.Add(10);
            set2.Add(11);

            Console.Read();
        }
    }
}