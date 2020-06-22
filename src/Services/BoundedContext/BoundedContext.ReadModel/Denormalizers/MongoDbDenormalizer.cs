using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BoundedContext.ReadModel.Denormalizers
{
    public abstract class MongoDbDenormalizer
    {
        public async Task TryExcuteTaskAsync(Func<Task> action)
        {
            await action();
        }

        public async Task TryExcuteTaskAsync(Func<IEnumerable<Task>> actions)
        {
            await Task.WhenAll(actions()).ConfigureAwait(false);
        }

        public async Task TryExcuteTasksAsync(params Func<Task>[] actions)
        {
            var tasks = actions.Select(a => a());

            await Task.WhenAll(tasks);
        }

        protected async Task TryInsertAsync(Func<Task> action)
        {
            try
            {
                await action();
            }
            catch (MongoWriteException ex)
            {
                //duplicate key
                if (ex.WriteError.Code == 11000)
                {
                    return;
                }
                throw;
            }
        }
    }
}