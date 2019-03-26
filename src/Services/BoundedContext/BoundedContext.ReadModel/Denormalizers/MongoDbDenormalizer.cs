using ECommon.IO;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BoundedContext.ReadModel.Denormalizers
{
    public abstract class MongoDbDenormalizer
    {
        public async Task<AsyncTaskResult> TryExcuteTaskAsync(Func<Task> action)
        {
            await action();
            return AsyncTaskResult.Success;
        }

        public async Task<AsyncTaskResult> TryExcuteTaskAsync(Func<IEnumerable<Task>> actions)
        {
            await Task.WhenAll(actions()).ConfigureAwait(false);
            return AsyncTaskResult.Success;
        }

        public async Task<AsyncTaskResult> TryExcuteTasksAsync(params Func<Task<AsyncTaskResult>>[] actions)
        {
            var tasks = actions.Select(a => a());

            await Task.WhenAll(tasks);

            if (tasks.Any(t => t.Result.Status == AsyncTaskStatus.Failed || t.Result.Status == AsyncTaskStatus.IOException))
            {
                var errorTask = tasks.FirstOrDefault(t => t.Result.Status == AsyncTaskStatus.Failed || t.Result.Status == AsyncTaskStatus.IOException);
                return new AsyncTaskResult(errorTask.Result.Status, errorTask.Result.ErrorMessage);
            }

            return AsyncTaskResult.Success;
        }

        protected async Task<AsyncTaskResult> TryInsertAsync<TReturn>(Func<Task<TReturn>> action)
        {
            try
            {
                await action();
                return AsyncTaskResult.Success;
            }
            catch (MongoWriteException ex)
            {
                //duplicate key
                if (ex.WriteError.Code == 11000)
                {
                    return AsyncTaskResult.Success;
                }
                throw;
            }
        }
    }
}