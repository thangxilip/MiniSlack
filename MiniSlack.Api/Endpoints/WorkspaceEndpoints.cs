using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using MiniSlack.Application.Workspaces;
using MiniSlack.Application.Workspaces.Commands.AcceptWorkspaceInvite;
using MiniSlack.Application.Workspaces.Commands.CreateConversation;
using MiniSlack.Application.Workspaces.Commands.CreateMessage;
using MiniSlack.Application.Workspaces.Commands.CreateWorkspace;
using MiniSlack.Application.Workspaces.Commands.CreateWorkspaceInvite;
using MiniSlack.Application.Workspaces.Commands.RemoveWorkspaceMember;
using MiniSlack.Application.Workspaces.Commands.RevokeWorkspaceInvite;
using MiniSlack.Application.Workspaces.Commands.StartDirectMessage;
using MiniSlack.Application.Workspaces.Commands.UpdateWorkspaceMemberRole;
using MiniSlack.Application.Workspaces.Queries.GetConversations;
using MiniSlack.Application.Workspaces.Queries.GetMessages;
using MiniSlack.Application.Workspaces.Queries.GetWorkspaceInvites;
using MiniSlack.Application.Workspaces.Queries.GetWorkspaceMembers;
using MiniSlack.Application.Workspaces.Queries.GetWorkspaces;
using MiniSlack.Infrastructure.Auth;

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

        workspaces.MapGet("/{workspaceId:guid}/members", GetWorkspaceMembersAsync)
            .WithName("GetWorkspaceMembers")
            .WithOpenApi();

        workspaces.MapGet("/{workspaceId:guid}/invites", GetWorkspaceInvitesAsync)
            .WithName("GetWorkspaceInvites")
            .WithOpenApi();

        workspaces.MapPost("/{workspaceId:guid}/conversations", CreateConversationAsync)
            .WithName("CreateWorkspaceConversation")
            .WithOpenApi();

        workspaces.MapPost("/{workspaceId:guid}/invites", CreateWorkspaceInviteAsync)
            .WithName("CreateWorkspaceInvite")
            .WithOpenApi();

        workspaces.MapPost("/{workspaceId:guid}/direct-messages", StartDirectMessageAsync)
            .WithName("StartWorkspaceDirectMessage")
            .WithOpenApi();

        workspaces.MapDelete("/{workspaceId:guid}/invites/{inviteId:guid}", RevokeWorkspaceInviteAsync)
            .WithName("RevokeWorkspaceInvite")
            .WithOpenApi();

        workspaces.MapDelete("/{workspaceId:guid}/members/{targetUserId:guid}", RemoveWorkspaceMemberAsync)
            .WithName("RemoveWorkspaceMember")
            .WithOpenApi();

        workspaces.MapPatch("/{workspaceId:guid}/members/{targetUserId:guid}/role", UpdateWorkspaceMemberRoleAsync)
            .WithName("UpdateWorkspaceMemberRole")
            .WithOpenApi();

        app.MapPost("/workspace-invites/accept", AcceptWorkspaceInviteAsync)
            .RequireAuthorization()
            .WithTags("Workspace Invites")
            .WithName("AcceptWorkspaceInvite")
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
        ISender sender,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(user, out var userId))
        {
            return TypedResults.Unauthorized();
        }

        var workspaces = await sender.Send(new GetWorkspacesQuery(userId), cancellationToken);
        return TypedResults.Ok(workspaces);
    }

    private static async Task<Results<Created<WorkspaceSummary>, BadRequest<ProblemDetails>, UnauthorizedHttpResult>> CreateWorkspaceAsync(
        ClaimsPrincipal user,
        CreateWorkspaceRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(user, out var userId))
        {
            return TypedResults.Unauthorized();
        }

        try
        {
            var workspace = await sender.Send(new CreateWorkspaceCommand(userId, request), cancellationToken);
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
        ISender sender,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(user, out var userId))
        {
            return TypedResults.Unauthorized();
        }

        try
        {
            var conversations = await sender.Send(new GetConversationsQuery(userId, workspaceId), cancellationToken);
            return TypedResults.Ok(conversations);
        }
        catch (UnauthorizedAccessException)
        {
            return TypedResults.NotFound();
        }
    }

    private static async Task<Results<Ok<IReadOnlyList<WorkspaceMemberSummary>>, NotFound, UnauthorizedHttpResult>> GetWorkspaceMembersAsync(
        Guid workspaceId,
        ClaimsPrincipal user,
        ISender sender,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(user, out var userId))
        {
            return TypedResults.Unauthorized();
        }

        try
        {
            var members = await sender.Send(new GetWorkspaceMembersQuery(userId, workspaceId), cancellationToken);
            return TypedResults.Ok(members);
        }
        catch (UnauthorizedAccessException)
        {
            return TypedResults.NotFound();
        }
    }

    private static async Task<Results<Ok<IReadOnlyList<WorkspaceInviteSummary>>, NotFound, UnauthorizedHttpResult>> GetWorkspaceInvitesAsync(
        Guid workspaceId,
        ClaimsPrincipal user,
        ISender sender,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(user, out var userId))
        {
            return TypedResults.Unauthorized();
        }

        try
        {
            var invites = await sender.Send(new GetWorkspaceInvitesQuery(userId, workspaceId), cancellationToken);
            return TypedResults.Ok(invites);
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
        ISender sender,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(user, out var userId))
        {
            return TypedResults.Unauthorized();
        }

        try
        {
            var conversation = await sender.Send(
                new CreateConversationCommand(userId, workspaceId, request),
                cancellationToken);
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

    private static async Task<Results<Created<CreatedWorkspaceInviteSummary>, BadRequest<ProblemDetails>, NotFound, UnauthorizedHttpResult>> CreateWorkspaceInviteAsync(
        Guid workspaceId,
        ClaimsPrincipal user,
        CreateWorkspaceInviteRequest request,
        ISender sender,
        IOptions<AuthOptions> options,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(user, out var userId))
        {
            return TypedResults.Unauthorized();
        }

        try
        {
            var invite = await sender.Send(
                new CreateWorkspaceInviteCommand(
                    userId,
                    workspaceId,
                    request,
                    BuildFrontendInviteAcceptUrl(options.Value)),
                cancellationToken);

            return TypedResults.Created($"/workspaces/{workspaceId}/invites/{invite.Id}", invite);
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

    private static async Task<Results<Ok<ConversationSummary>, BadRequest<ProblemDetails>, NotFound, UnauthorizedHttpResult>> StartDirectMessageAsync(
        Guid workspaceId,
        ClaimsPrincipal user,
        StartDirectMessageRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(user, out var userId))
        {
            return TypedResults.Unauthorized();
        }

        try
        {
            var conversation = await sender.Send(
                new StartDirectMessageCommand(userId, workspaceId, request),
                cancellationToken);
            return TypedResults.Ok(conversation);
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

    private static async Task<Results<Ok<AcceptWorkspaceInviteResult>, BadRequest<ProblemDetails>, UnauthorizedHttpResult>> AcceptWorkspaceInviteAsync(
        ClaimsPrincipal user,
        AcceptWorkspaceInviteRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(user, out var userId))
        {
            return TypedResults.Unauthorized();
        }

        try
        {
            var result = await sender.Send(new AcceptWorkspaceInviteCommand(userId, request), cancellationToken);
            return TypedResults.Ok(result);
        }
        catch (ArgumentException exception)
        {
            return TypedResults.BadRequest(CreateProblem(exception.Message));
        }
        catch (InvalidOperationException exception)
        {
            return TypedResults.BadRequest(CreateProblem(exception.Message));
        }
    }

    private static async Task<Results<NoContent, BadRequest<ProblemDetails>, NotFound, UnauthorizedHttpResult>> RevokeWorkspaceInviteAsync(
        Guid workspaceId,
        Guid inviteId,
        ClaimsPrincipal user,
        ISender sender,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(user, out var userId))
        {
            return TypedResults.Unauthorized();
        }

        try
        {
            await sender.Send(new RevokeWorkspaceInviteCommand(userId, workspaceId, inviteId), cancellationToken);
            return TypedResults.NoContent();
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

    private static async Task<Results<Ok<RemovedWorkspaceMemberSummary>, BadRequest<ProblemDetails>, NotFound, UnauthorizedHttpResult>> RemoveWorkspaceMemberAsync(
        Guid workspaceId,
        Guid targetUserId,
        ClaimsPrincipal user,
        ISender sender,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(user, out var userId))
        {
            return TypedResults.Unauthorized();
        }

        try
        {
            var removed = await sender.Send(
                new RemoveWorkspaceMemberCommand(userId, workspaceId, targetUserId),
                cancellationToken);

            return TypedResults.Ok(removed);
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

    private static async Task<Results<Ok<WorkspaceMemberSummary>, BadRequest<ProblemDetails>, NotFound, UnauthorizedHttpResult>> UpdateWorkspaceMemberRoleAsync(
        Guid workspaceId,
        Guid targetUserId,
        ClaimsPrincipal user,
        UpdateWorkspaceMemberRoleRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(user, out var userId))
        {
            return TypedResults.Unauthorized();
        }

        try
        {
            var member = await sender.Send(
                new UpdateWorkspaceMemberRoleCommand(userId, workspaceId, targetUserId, request),
                cancellationToken);

            return TypedResults.Ok(member);
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
        ISender sender,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(user, out var userId))
        {
            return TypedResults.Unauthorized();
        }

        try
        {
            var messages = await sender.Send(
                new GetMessagesQuery(
                    userId,
                    conversationId,
                    before,
                    limit ?? 50),
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
        ISender sender,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(user, out var userId))
        {
            return TypedResults.Unauthorized();
        }

        try
        {
            var message = await sender.Send(new CreateMessageCommand(userId, conversationId, request), cancellationToken);
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

    private static string BuildFrontendInviteAcceptUrl(AuthOptions options)
    {
        var callbackUri = new Uri(options.Frontend.LoginCallbackUrl);
        return $"{callbackUri.Scheme}://{callbackUri.Authority}/invites/accept";
    }
}
