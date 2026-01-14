namespace SportsBetting.Domain.Entities;

public class Wallet : EntityBase
{
    public long UserId { get; set; }
    
    public User User { get; set; } = default!;
    public decimal Balance { get; set; }

    public byte[] RowVersion { get; set; } = default!;

}