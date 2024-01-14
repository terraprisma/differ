using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Tomat.Differ.DotnetPatcher {
    public class WorkTask {
        public delegate void Worker();

        public readonly Worker task;

        public WorkTask(Worker worker) {
            task = worker;
        }

        public static void ExecuteParallel(List<WorkTask> items) {
            try {
                var working = new List<string>();
                Parallel.ForEach(
                    Partitioner.Create(items, EnumerablePartitionerOptions.NoBuffering),
                    // leave some cores to not use the entire cpu
                    new ParallelOptions { MaxDegreeOfParallelism = Math.Max(Environment.ProcessorCount - 1, 1) },
                    item => {
                        item.task();
                    }
                );
            }
            catch (AggregateException ex) {
                IEnumerable<Exception> actual = ex.Flatten()
                    .InnerExceptions.Where(e => !(e is OperationCanceledException));

                if (!actual.Any())
                    throw new OperationCanceledException();

                throw new AggregateException(actual);
            }
        }
    }
}
