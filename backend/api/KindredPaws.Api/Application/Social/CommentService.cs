using KindredPaws.Api.Application.Audit;
using KindredPaws.Api.Application.Notifications;
using KindredPaws.Api.Application.Shared;
using KindredPaws.Api.Domain.Audit;
using KindredPaws.Api.Domain.Identity;
using KindredPaws.Api.Domain.Notifications;
using KindredPaws.Api.Domain.Social;
using KindredPaws.Api.Infrastructure.Persistence;
using KindredPaws.Contracts;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace KindredPaws.Api.Application.Social;

public sealed class CommentService(CommentRepository comments, CommentLikeRepository commentLikes, SocialRepository posts, UserManager<ApplicationUser> userManager, INotificationService notifications, IEventPublisher eventPublisher, IAuditService audit) : ICommentService
{
    public async Task<CommentResponse> CreateAsync(Guid postId, Guid authorId, CreateCommentRequest r, CancellationToken ct)
    {
        var post = await posts.GetPostAsync(postId, ct) ?? throw new KeyNotFoundException("Publicación no encontrada.");
        Comment? parent = null;
        if (r.ParentCommentId.HasValue)
        {
            parent = await comments.GetAsync(r.ParentCommentId.Value, ct) ?? throw new KeyNotFoundException("Comentario no encontrado.");
            if (parent.PostId != postId) throw new ArgumentException("El comentario padre no pertenece a esta publicación.");
            if (parent.ParentCommentId.HasValue) throw new ArgumentException("No se puede responder a una respuesta.");
        }

        var comment = new Comment { PostId = postId, AuthorId = authorId, ParentCommentId = r.ParentCommentId, Body = r.Body.Trim() };
        await comments.AddAsync(comment, ct);
        await comments.SaveAsync(ct);

        if (parent is not null)
        {
            if (parent.AuthorId != authorId)
            {
                await notifications.CreateAsync(parent.AuthorId, NotificationType.Reply, "Nueva respuesta", "Alguien respondió tu comentario.", $"/p/{postId}", postId, ct);
                var recipient = await userManager.FindByIdAsync(parent.AuthorId.ToString());
                if (recipient is not null)
                    await eventPublisher.PublishAsync(new CommentReplyCreatedEvent(comment.Id, parent.Id, parent.AuthorId, recipient.Email ?? string.Empty, recipient.FullName, authorId, Excerpt(comment.Body)), ct);
            }
        }
        else if (post.CreatedByUserId is { } ownerId && ownerId != authorId)
        {
            await notifications.CreateAsync(ownerId, NotificationType.Comment, "Nuevo comentario", "Alguien comentó tu publicación.", $"/p/{postId}", postId, ct);
            var owner = await userManager.FindByIdAsync(ownerId.ToString());
            if (owner is not null)
                await eventPublisher.PublishAsync(new CommentCreatedEvent(comment.Id, postId, ownerId, owner.Email ?? string.Empty, owner.FullName, authorId, Excerpt(comment.Body)), ct);
        }

        var author = await userManager.FindByIdAsync(authorId.ToString());
        return ToResponse(comment, author?.FullName ?? "Usuario", authorId, 0, false);
    }

    public async Task<IReadOnlyCollection<CommentResponse>> ListAsync(Guid postId, Guid? currentUserId, CancellationToken ct)
    {
        var items = await comments.ListByPostAsync(postId, ct);
        var ids = items.Select(x => x.Id).ToArray();
        var authorIds = items.Select(x => x.AuthorId).Distinct().ToArray();
        var counts = await commentLikes.CountManyAsync(ids, ct);
        var liked = currentUserId.HasValue
            ? await commentLikes.ListLikedCommentIdsAsync(currentUserId.Value, ids, ct)
            : [];
        var names = await userManager.Users.Where(u => authorIds.Contains(u.Id)).ToDictionaryAsync(u => u.Id, u => u.FullName, ct);
        return items.Select(x => ToResponse(x, names.GetValueOrDefault(x.AuthorId, "Usuario"), currentUserId, counts.GetValueOrDefault(x.Id), liked.Contains(x.Id))).ToArray();
    }

    public async Task LikeAsync(Guid commentId, Guid userId, CancellationToken ct)
    {
        _ = await comments.GetAsync(commentId, ct) ?? throw new KeyNotFoundException("Comentario no encontrado.");
        if (await commentLikes.ExistsAsync(commentId, userId, ct)) return;
        await commentLikes.AddAsync(new CommentLike { CommentId = commentId, UserId = userId }, ct);
        await commentLikes.SaveAsync(ct);
    }

    public async Task UnlikeAsync(Guid commentId, Guid userId, CancellationToken ct)
    {
        var like = await commentLikes.FindAsync(commentId, userId, ct);
        if (like is null) return;
        commentLikes.Remove(like);
        await commentLikes.SaveAsync(ct);
    }

    public async Task DeleteOwnAsync(Guid commentId, Guid userId, CancellationToken ct)
    {
        var comment = await comments.GetAsync(commentId, ct) ?? throw new KeyNotFoundException("Comentario no encontrado.");
        if (comment.AuthorId != userId) throw new UnauthorizedAccessException("Solo puedes eliminar tus propios comentarios.");
        comment.DeletedAt = DateTimeOffset.UtcNow;
        comment.Visibility = ContentVisibility.Deleted;
        await comments.SaveAsync(ct);
    }

    public async Task HideAsync(Guid commentId, Guid actorUserId, CancellationToken ct)
    {
        var comment = await comments.GetAsync(commentId, ct) ?? throw new KeyNotFoundException("Comentario no encontrado.");
        comment.Visibility = ContentVisibility.Hidden;
        await comments.SaveAsync(ct);
        await audit.RecordAsync(actorUserId, AuditAction.CommentHidden, "Comment", commentId, null, ct);
    }

    private static string Excerpt(string body) => body.Length <= 140 ? body : body[..140];
    private static CommentResponse ToResponse(Comment x, string authorName, Guid? currentUserId, int likeCount, bool likedByCurrentUser) =>
        new(x.Id, x.PostId, x.AuthorId, authorName, x.ParentCommentId, x.Body, x.CreatedAt, currentUserId.HasValue && x.AuthorId == currentUserId.Value, likeCount, likedByCurrentUser);
}
