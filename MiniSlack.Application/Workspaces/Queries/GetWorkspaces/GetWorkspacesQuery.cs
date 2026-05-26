using MediatR;

namespace MiniSlack.Application.Workspaces.Queries.GetWorkspaces;

public sealed record GetWorkspacesQuery(Guid UserId) : IRequest<IReadOnlyList<WorkspaceSummary>>;
