using ECommon.IO;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
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