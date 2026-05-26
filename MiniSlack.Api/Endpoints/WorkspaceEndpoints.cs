using System.Security.Claims;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using MiniSlack.Application.Workspaces;

namespace MiniSlack.Endpoints;

public static class WorkspaceEndpoints
{
    public static IEndpointRouteBuilder MapWorkspaceEndpoints(this IEndpointRouteBuilder app)
    {
        var workspaces = app.MapGroup("/workspaces")
            .RequireAuthorization()
            .WithTags("Workspaces");

        workspaces.MapGet("", GetWorkspacesAsync)
            .WithName("GetWorkspaces")
            .WithOpenApi();

        workspaces.MapPost("", CreateWorkspaceAsync)
            .WithName("CreateWorkspace")
            .WithOpenApi();

        workspaces.MapGet("/{workspaceId:guid}/conversations", GetConversationsAsync)
            .WithName("GetWorkspaceConversations")
            .WithOpenApi();

        workspaces.MapPost("/{workspaceId:guid}/conversations", CreateConversationAsync)
            .WithName("CreateWorkspaceConversation")
            .WithOpenApi();

        var conversations = app.MapGroup("/conversations")
            .RequireAuthorization()
            .WithTags("Conversations");

        conversations.MapGet("/{conversationId:guid}/messages", GetMessagesAsync)
            .WithName("GetConversationMessages")
            .WithOpenApi();

        conversations.MapPost("/{conversationId:guid}/messages", CreateMessageAsync)
            .WithName("CreateConversationMessage")
            .WithOpenApi();

        return app;
    }

    private static async Task<Results<Ok<IReadOnlyList<WorkspaceSummary>>, UnauthorizedHttpResult>> GetWorkspacesAsync(
        ClaimsPrincipal user,
        IWorkspaceService workspaceService,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(user, out var userId))
        {
            return TypedResults.Unauthorized();
        }

        var workspaces = await workspaceService.GetWorkspacesAsync(userId, cancellationToken);
        return TypedResults.Ok(workspaces);
    }

    private static async Task<Results<Created<WorkspaceSummary>, BadRequest<ProblemDetails>, UnauthorizedHttpResult>> CreateWorkspaceAsync(
        ClaimsPrincipal user,
        CreateWorkspaceRequest request,
        IWorkspaceService workspaceService,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(user, out var userId))
        {
            return TypedResults.Unauthorized();
        }

        try
        {
            var workspace = await workspaceService.CreateWorkspaceAsync(userId, request, cancellationToken);
            return TypedResults.Created($"/workspaces/{workspace.Id}", workspace);
        }
        catch (ArgumentException exception)
        {
            return TypedResults.BadRequest(CreateProblem(exception.Message));
        }
    }

    private static async Task<Results<Ok<IReadOnlyList<ConversationSummary>>, NotFound, UnauthorizedHttpResult>> GetConversationsAsync(
        Guid workspaceId,
        ClaimsPrincipal user,
        IWorkspaceService workspaceService,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(user, out var userId))
        {
            return TypedResults.Unauthorized();
        }

        try
        {
            var conversations = await workspaceService.GetConversationsAsync(userId, workspaceId, cancellationToken);
            return TypedResults.Ok(conversations);
        }
        catch (UnauthorizedAccessException)
        {
            return TypedResults.NotFound();
        }
    }

    private static async Task<Results<Created<ConversationSummary>, BadRequest<ProblemDetails>, NotFound, UnauthorizedHttpResult>> CreateConversationAsync(
        Guid workspaceId,
        ClaimsPrincipal user,
        CreateConversationRequest request,
        IWorkspaceService workspaceService,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(user, out var userId))
        {
            return TypedResults.Unauthorized();
        }

        try
        {
            var conversation = await workspaceService.CreateConversationAsync(userId, workspaceId, request, cancellationToken);
            return TypedResults.Created($"/conversations/{conversation.Id}", conversation);
        }
        catch (ArgumentException exception)
        {
            return TypedResults.BadRequest(CreateProblem(exception.Message));
        }
        catch (InvalidOperationException exception)
        {
            return TypedResults.BadRequest(CreateProblem(exception.Message));
        }
        catch (UnauthorizedAccessException)
        {
            return TypedResults.NotFound();
        }
    }

    private static async Task<Results<Ok<IReadOnlyList<MessageSummary>>, NotFound, UnauthorizedHttpResult>> GetMessagesAsync(
        Guid conversationId,
        DateTimeOffset? before,
        int? limit,
        ClaimsPrincipal user,
        IWorkspaceService workspaceService,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(user, out var userId))
        {
            return TypedResults.Unauthorized();
        }

        try
        {
            var messages = await workspaceService.GetMessagesAsync(
                userId,
                conversationId,
                before,
                limit ?? 50,
                cancellationToken);

            return TypedResults.Ok(messages);
        }
        catch (UnauthorizedAccessException)
        {
            return TypedResults.NotFound();
        }
    }

    private static async Task<Results<Created<MessageSummary>, BadRequest<ProblemDetails>, NotFound, UnauthorizedHttpResult>> CreateMessageAsync(
        Guid conversationId,
        ClaimsPrincipal user,
        CreateMessageRequest request,
        IWorkspaceService workspaceService,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(user, out var userId))
        {
            return TypedResults.Unauthorized();
        }

        try
        {
            var message = await workspaceService.CreateMessageAsync(userId, conversationId, request, cancellationToken);
            return TypedResults.Created($"/conversations/{conversationId}/messages/{message.Id}", message);
        }
        catch (ArgumentException exception)
        {
            return TypedResults.BadRequest(CreateProblem(exception.Message));
        }
        catch (InvalidOperationException exception)
        {
            return TypedResults.BadRequest(CreateProblem(exception.Message));
        }
        catch (UnauthorizedAccessException)
        {
            return TypedResults.NotFound();
        }
    }

    private static bool TryGetCurrentUserId(
        ClaimsPrincipal user,
        out Guid userId)
    {
        var value = user.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? user.FindFirstValue("sub");

        return Guid.TryParse(value, out userId);
    }

    private static ProblemDetails CreateProblem(string detail)
    {
        return new ProblemDetails
        {
            Title = "Request validation failed.",
            Detail = detail,
            Status = StatusCodes.Status400BadRequest
        };
    }
}
