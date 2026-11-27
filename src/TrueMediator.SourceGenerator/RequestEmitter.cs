using System.Text;

namespace TrueMediator.SourceGenerator;

internal static class RequestEmitter
{
    internal static void Emit(StringBuilder code, RequestDispatchModel request, int index)
    {
        string message = request.Message;
        string response = request.Response;
        string resultType = request.Streams
            ? $"global::System.Collections.Generic.IAsyncEnumerable<{response}>"
            : $"global::System.Threading.Tasks.Task<{response}>";
        string dispatcherContract = request.Streams ? "IStreamRequestDispatcher" : "IRequestDispatcher";
        string handlerContract = request.Streams ? "IStreamRequestHandler" : "IRequestHandler";
        string behaviorContract = request.Streams ? "IStreamPipelineBehavior" : "IPipelineBehavior";
        string delegateContract = request.Streams ? "StreamHandlerDelegate" : "RequestHandlerDelegate";
        string handlerArguments = request.ReturnsVoid ? message : $"{message}, {response}";
        code.AppendLine($$"""
                private sealed class __TrueMediatorRequest{{index}} : global::TrueMediator.{{dispatcherContract}}<{{response}}>
                {
                    public {{resultType}} Invoke(object value, global::System.IServiceProvider sp, global::System.Threading.CancellationToken cancellationToken)
                    {
                        var request = ({{message}})value;
                        global::TrueMediator.Contracts.{{handlerContract}}<{{handlerArguments}}> handler = {{request.HandlerResolution}};
            """);
        if (request.ReturnsVoid)
        {
            code.AppendLine("""
                            async global::System.Threading.Tasks.Task<global::TrueMediator.Contracts.Unit> InvokeHandler()
                            {
                                cancellationToken.ThrowIfCancellationRequested();
                                await handler.Handle(request, cancellationToken).ConfigureAwait(false);
                                return global::TrueMediator.Contracts.Unit.Value;
                            }
                """);
        }
        else
        {
            code.AppendLine($$"""
                            {{resultType}} InvokeHandler()
                            {
                                cancellationToken.ThrowIfCancellationRequested();
                                return handler.Handle(request, cancellationToken);
                            }
                """);
        }
        code.AppendLine($$"""
                        global::TrueMediator.Contracts.{{delegateContract}}<{{response}}> next = InvokeHandler;
            """);
        for (int behaviorIndex = request.BehaviorResolutions.Count - 1; behaviorIndex >= 0; behaviorIndex--)
        {
            code.AppendLine($$"""
                            global::TrueMediator.Contracts.{{behaviorContract}}<{{message}}, {{response}}> behavior{{behaviorIndex}} = {{request.BehaviorResolutions[behaviorIndex]}};
                            var continuation{{behaviorIndex}} = next;
                            next = () =>
                            {
                                cancellationToken.ThrowIfCancellationRequested();
                                return behavior{{behaviorIndex}}.Handle(request, continuation{{behaviorIndex}}, cancellationToken);
                            };
                """);
        }
        code.AppendLine("""
                        return next();
                    }
                }
            """);
    }
}
