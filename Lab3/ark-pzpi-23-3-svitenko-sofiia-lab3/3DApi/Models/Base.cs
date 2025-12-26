namespace _3DApi.Models;

public class Base
{
    public int Id { get; set; }
    
    public DateTimeOffset CreatedOn { get; set; }
    
    public DateTimeOffset LastModifiedOn { get; set; }
}