namespace MiniSlack.Domain.Configurations;

public record AppConfig
{
    public required ConnectionStrings ConnectionStrings { get; set; }
}

public record ConnectionStrings
{
    public required string DefaultConnection { get; set; }
}