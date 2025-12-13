using System;

namespace SportsBetting.Domain.Entities;

public class EntityBase 
{
    public long Id { get; set; }

    public bool Active { get; set; } = true; 
    
    //DataBase from my API and not user
    public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
} 