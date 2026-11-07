using System.Collections.Generic;
using System.Text;
using static SwiftDispatch.SourceGenerator.SymbolHelpers;
using static SwiftDispatch.SourceGenerator.SourceEmitter;

namespace SwiftDispatch.SourceGenerator;

internal static class RequestEmitter
{
    internal static void Emit(StringBuilder code, List<Request> requests)
    {
        for (int index = 0; index < requests.Count; index++)
        {
            Request request = requests[index];
            string message = Name(request.Message);
            string response = Name(request.Response);
            string resultType = request.Streams ? "global::System.Collections.Generic.IAsyncEnumerable<" : "global::System.Threading.Tasks.Task<";
            string dispatcherContract = request.Streams ? "IStreamRequestDispatcher" : "IRequestDispatcher";
            string handlerContract = request.Streams ? "IStreamRequestHandler" : "IRequestHandler";
            string behaviorContract = request.Streams ? "IStreamPipelineBehavior" : "IPipelineBehavior";
            string delegateContract = request.Streams ? "StreamHandlerDelegate" : "RequestHandlerDelegate";
            code.Append("    private sealed class __SwiftDispatchRequest").Append(index)
                .Append(" : global::SwiftDispatch.").Append(dispatcherContract).Append("<").Append(response).AppendLine(">\n    {");
            code.Append("        public ").Append(resultType).Append(response)
                .AppendLine("> Invoke(object value, global::System.IServiceProvider sp, global::System.Threading.CancellationToken cancellationToken)\n        {");
            code.Append("            var request = (").Append(message).AppendLine(")value;");
            string handlerInterface = "global::SwiftDispatch.Contracts." + handlerContract + "<" + message + (request.ReturnsVoid ? "" : ", " + response) + ">";
            code.Append("            ").Append(handlerInterface).Append(" handler = ").Append(Resolve(request.Handler)).AppendLine(";");
            if (request.ReturnsVoid)
            {
                code.AppendLine("            async global::System.Threading.Tasks.Task<global::SwiftDispatch.Contracts.Unit> InvokeHandler()\n            {\n                cancellationToken.ThrowIfCancellationRequested();\n                await handler.Handle(request, cancellationToken).ConfigureAwait(false);\n                return global::SwiftDispatch.Contracts.Unit.Value;\n            }");
            }
            else
            {
                code.Append("            ").Append(resultType).Append(response).AppendLine("> InvokeHandler()\n            {\n                cancellationToken.ThrowIfCancellationRequested();\n                return handler.Handle(request, cancellationToken);\n            }");
            }
            code.Append("            global::SwiftDispatch.Contracts.").Append(delegateContract).Append("<").Append(response).AppendLine("> next = InvokeHandler;");
            for (int behaviorIndex = request.Behaviors.Count - 1; behaviorIndex >= 0; behaviorIndex--)
            {
                code.Append("            global::SwiftDispatch.Contracts.").Append(behaviorContract).Append("<").Append(message).Append(", ").Append(response)
                    .Append("> behavior").Append(behaviorIndex).Append(" = ").Append(Resolve(request.Behaviors[behaviorIndex])).AppendLine(";");
                code.Append("            var continuation").Append(behaviorIndex).AppendLine(" = next;");
                code.Append("            next = () =>\n            {\n                cancellationToken.ThrowIfCancellationRequested();\n                return behavior")
                    .Append(behaviorIndex).Append(".Handle(request, continuation").Append(behaviorIndex).AppendLine(", cancellationToken);\n            };");
            }
            code.AppendLine("            return next();\n        }\n    }");
        }

    }
}
