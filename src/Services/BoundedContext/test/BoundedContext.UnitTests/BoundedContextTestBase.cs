using ECommon.Components;
using ENode.Commanding;
using System.Threading.Tasks;
using Xunit;

namespace BoundedContext.UnitTests
{
    [Collection(nameof(BoundedContextCollection))]
    public abstract class BoundedContextTestBase
    {
        protected ICommandService _commandService;
        protected Jane.IIdGenerator _idGenerator;

        public BoundedContextTestBase()
        {
            _commandService = ObjectContainer.Resolve<ICommandService>();

            _idGenerator = ObjectContainer.Resolve<Jane.IIdGenerator>();
        }

        protected Task<CommandResult> ExecuteCommandAsync(ICommand command)
        {
            return _commandService.ExecuteAsync(command, CommandReturnType.EventHandled);
        }
    }
}