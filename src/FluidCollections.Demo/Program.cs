using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;

namespace FluidCollections.Demo {
    class Program {
        static void Main(string[] args) {
            var set = new ReactiveSet<int>(new[] { 1 });

            var buffered = set.Buffer(3);

            buffered.ToImmutableSets().Select(x => string.Join(", ", x)).Subscribe(Console.WriteLine);

            set.Add(4);
            set.Add(8);
            set.Add(12);
            set.Add(16);

            Console.Read();
        }
    }
}