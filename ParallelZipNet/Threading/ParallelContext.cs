using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using ParallelZipNet.Logger;
using ParallelZipNet.Utils;

namespace ParallelZipNet.Threading {
    public static class ParallelContextBuilder {
        public static ParallelContext<T> AsParallel<T>(this IEnumerable<T> enumeration, int jobCount) {
            return new ParallelContext<T>(new LockContext<T>(enumeration).AsEnumerable(), jobCount);
        }
    }

     public class ParallelContext<T> {        
        readonly IEnumerable<T> enumeration;
        readonly int jobCount;        

        public ParallelContext(IEnumerable<T> enumeration, int jobCount) {
            Guard.NotNull(enumeration, nameof(enumeration));
            Guard.NotZeroOrNegative(jobCount, nameof(jobCount));
            
            this.enumeration = enumeration;
            this.jobCount = jobCount;
        }

        public ParallelContext<U> Map<U>(Func<T, U> transform) {
            Guard.NotNull(transform, nameof(transform));

            return new ParallelContext<U>(enumeration.Select(transform), jobCount);
        }

        public ParallelContext<T> Do(Action<T> action) {
            Guard.NotNull(action, nameof(action));

            return Map<T>(t => { action(t); return t; });
        }

        public IEnumerable<T> AsEnumerable(CancellationToken cancellationToken = null, Action<Exception> errorHandler = null, IJobLogger logger = null) {
            if(cancellationToken == null)
                cancellationToken = new CancellationToken();

            Job<T>[] jobs = Enumerable.Range(1, jobCount)
                .Select(i => new Job<T>($"{i}", enumeration, cancellationToken))
                .ToArray();

            while(true) {
                if(cancellationToken.IsCancelled)
                    break;

                var failedJobs = jobs
                    .Where(job => job.Error != null)
                    .ToArray();

                if(failedJobs.Length > 0) {
                    cancellationToken.Cancel();
                    if(errorHandler != null)
                        foreach(var job in failedJobs)
                            errorHandler(job.Error);                    
                    break;
                }                   
                
                if(logger != null)
                    logger.LogResultsCount(jobs.Select(job => job.ResultsCount).Sum());
                
                List<T> results = new List<T>(jobs.Length);
                foreach(var job in jobs)
                    if(job.TryGetResult(out T result))
                        results.Add(result);                

                foreach(T result in results)
                    yield return result;

                if(jobs.All(job => job.IsFinished))
                    break;
                else
                    Thread.Sleep(5);                    
            }

            foreach(var job in jobs)
                job.Dispose();
        }
    }
}